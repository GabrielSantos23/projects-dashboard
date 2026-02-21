using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectDashboard.Shared.Data;
using ProjectDashboard.Shared.Models;

namespace ProjectDashboard.Shared.Services;

public class ScannerService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<ScannerService> _logger;

    public ScannerService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<ScannerService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task ScanAsync(string rootPath, int cutoffDays)
    {
        if (!Directory.Exists(rootPath))
        {
            _logger.LogWarning("Root path does not exist: {RootPath}", rootPath);
            return;
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-cutoffDays);
        var gitFolders = FindGitFolders(rootPath);

        _logger.LogInformation("Found {Count} git repositories under {RootPath}", gitFolders.Count(), rootPath);

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var foundPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var gitDir in gitFolders)
        {
            try
            {
                using var repo = new Repository(gitDir);

                if (!repo.Commits.Any()) continue;

                var lastCommit = repo.Head.Tip;
                var lastCommitDate = lastCommit?.Author.When.UtcDateTime;
                if (lastCommitDate < cutoffDate) continue;

                var parentDir = Directory.GetParent(gitDir)?.FullName ?? gitDir;
                var projectName = new DirectoryInfo(parentDir).Name;

                foundPaths.Add(parentDir);

                var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Path == parentDir);
                if (project == null)
                {
                    project = new Project { Path = parentDir };
                    dbContext.Projects.Add(project);
                }

                project.Name = projectName;
                project.LastCommit = lastCommitDate;
                project.LastCommitMessage = lastCommit?.MessageShort ?? "";
                project.CurrentBranch = repo.Head.FriendlyName;
                project.ScannedAt = DateTime.UtcNow;

                
                var originRemote = repo.Network.Remotes.FirstOrDefault(r => r.Name == "origin");
                var upstreamRemote = repo.Network.Remotes.FirstOrDefault(r => r.Name == "upstream");
                project.RemoteUrl = originRemote?.Url;
                project.UpstreamUrl = upstreamRemote?.Url;
                project.IsForked = upstreamRemote != null;

                
                var metadata = project.Metadata;
                metadata["inferred_type"] = DetectPrimaryStack(parentDir);

                var techs = DetectAllTechs(parentDir);
                metadata["techs"] = string.Join(",", techs);

                
                var contributors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var commitCount = 0;
                foreach (var c in repo.Commits.Take(500))
                {
                    contributors.Add(c.Author.Name);
                    commitCount++;
                }
                metadata["contributors"] = contributors.Count.ToString();
                metadata["contributor_names"] = string.Join(",", contributors.Take(20));
                metadata["total_commits"] = commitCount.ToString();

                
                var branchCount = repo.Branches.Count(b => !b.IsRemote);
                metadata["branch_count"] = branchCount.ToString();
                metadata["branches"] = string.Join(",", repo.Branches.Where(b => !b.IsRemote).Select(b => b.FriendlyName).Take(20));

                
                var recentCommits = repo.Commits.Take(10)
                    .Select(c => $"{c.Author.When.UtcDateTime:O}|{c.Author.Name}|{c.MessageShort}")
                    .ToList();
                metadata["recent_commits"] = string.Join(";;", recentCommits);

                
                var docs = ListDocs(parentDir);
                metadata["doc_count"] = docs.Count.ToString();
                metadata["doc_files"] = string.Join(",", docs.Take(50));

                
                metadata["has_dockerfile"] = (File.Exists(Path.Combine(parentDir, "Dockerfile")) ||
                    File.Exists(Path.Combine(parentDir, "docker-compose.yml")) ||
                    File.Exists(Path.Combine(parentDir, "docker-compose.yaml"))).ToString();
                metadata["has_ci"] = (Directory.Exists(Path.Combine(parentDir, ".github", "workflows")) ||
                    File.Exists(Path.Combine(parentDir, ".gitlab-ci.yml")) ||
                    File.Exists(Path.Combine(parentDir, "Jenkinsfile"))).ToString();
                metadata["has_readme"] = File.Exists(Path.Combine(parentDir, "README.md")).ToString();
                metadata["has_license"] = (File.Exists(Path.Combine(parentDir, "LICENSE")) ||
                    File.Exists(Path.Combine(parentDir, "LICENSE.md"))).ToString();

                project.Metadata = metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process repository at {GitDir}", gitDir);
            }
        }

        await dbContext.SaveChangesAsync();
        _logger.LogInformation("Scan complete. Processed {Count} repositories.", foundPaths.Count);
    }

    private IEnumerable<string> FindGitFolders(string startLocation)
    {
        var dirs = new Queue<string>();
        dirs.Enqueue(startLocation);
        var repos = new List<string>();

        while (dirs.Count > 0)
        {
            var currentDir = dirs.Dequeue();

            try
            {
                var potentialGitFolder = Path.Combine(currentDir, ".git");
                if (Directory.Exists(potentialGitFolder))
                {
                    repos.Add(potentialGitFolder);
                    continue;
                }

                foreach (var subDir in Directory.GetDirectories(currentDir))
                {
                    var dirName = new DirectoryInfo(subDir).Name;
                    if (IsIgnoredDirectory(dirName)) continue;
                    dirs.Enqueue(subDir);
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        return repos;
    }

    private bool IsIgnoredDirectory(string name)
    {
        name = name.ToLower();
        return name is "node_modules" or "bin" or "obj" or ".vs" or ".git" or "packages" or "dist" or "build" or ".cache" or "__pycache__" or ".venv" or "venv";
    }

    private string DetectPrimaryStack(string projectPath)
    {
        if (File.Exists(Path.Combine(projectPath, "Gemfile"))) return "Ruby/Rails";
        if (Directory.GetFiles(projectPath, "*.csproj").Any()) return ".NET";
        if (File.Exists(Path.Combine(projectPath, "go.mod"))) return "Go";
        if (File.Exists(Path.Combine(projectPath, "pom.xml"))) return "Java";
        if (File.Exists(Path.Combine(projectPath, "Cargo.toml"))) return "Rust";
        if (File.Exists(Path.Combine(projectPath, "requirements.txt")) || File.Exists(Path.Combine(projectPath, "pyproject.toml"))) return "Python";
        if (File.Exists(Path.Combine(projectPath, "package.json"))) return "Node/JS";
        return "Unknown";
    }

    private List<string> DetectAllTechs(string projectPath)
    {
        var techs = new List<string>();

        if (File.Exists(Path.Combine(projectPath, "Gemfile")))
        {
            techs.Add("Ruby");
            try
            {
                var gemContent = File.ReadAllText(Path.Combine(projectPath, "Gemfile"));
                if (gemContent.Contains("rails")) techs.Add("Rails");
            }
            catch { }
        }
        if (Directory.GetFiles(projectPath, "*.csproj").Any()) techs.Add(".NET");
        if (File.Exists(Path.Combine(projectPath, "go.mod"))) techs.Add("Go");
        if (File.Exists(Path.Combine(projectPath, "pom.xml"))) techs.Add("Java");
        if (File.Exists(Path.Combine(projectPath, "Cargo.toml"))) techs.Add("Rust");
        if (File.Exists(Path.Combine(projectPath, "requirements.txt")) || File.Exists(Path.Combine(projectPath, "pyproject.toml"))) techs.Add("Python");

        if (File.Exists(Path.Combine(projectPath, "package.json")))
        {
            try
            {
                var pkgContent = File.ReadAllText(Path.Combine(projectPath, "package.json"));
                if (pkgContent.Contains("\"react\"")) techs.Add("React");
                else if (pkgContent.Contains("\"vue\"")) techs.Add("Vue");
                else if (pkgContent.Contains("\"svelte\"")) techs.Add("Svelte");
                else if (pkgContent.Contains("\"next\"")) techs.Add("Next.js");
                else if (pkgContent.Contains("\"angular\"")) techs.Add("Angular");

                if (pkgContent.Contains("\"typescript\"") || File.Exists(Path.Combine(projectPath, "tsconfig.json"))) techs.Add("TypeScript");
                else techs.Add("JavaScript");

                if (pkgContent.Contains("\"electron\"")) techs.Add("Electron");
                if (pkgContent.Contains("\"tailwindcss\"")) techs.Add("Tailwind");
            }
            catch { if (!techs.Contains("JavaScript")) techs.Add("JavaScript"); }
        }

        if (File.Exists(Path.Combine(projectPath, "Dockerfile")) || File.Exists(Path.Combine(projectPath, "docker-compose.yml")) || File.Exists(Path.Combine(projectPath, "docker-compose.yaml")))
            techs.Add("Docker");

        return techs;
    }

    private List<string> ListDocs(string projectPath)
    {
        var docs = new List<string>();
        var dirs = new Queue<string>();
        dirs.Enqueue(projectPath);

        while (dirs.Count > 0)
        {
            var current = dirs.Dequeue();
            try
            {
                foreach (var file in Directory.GetFiles(current, "*.md"))
                {
                    docs.Add(System.IO.Path.GetRelativePath(projectPath, file));
                }

                foreach (var dir in Directory.GetDirectories(current))
                {
                    var name = new DirectoryInfo(dir).Name.ToLower();
                    if (name is "node_modules" or "bin" or "obj" or ".git" or "packages" or "dist" or "build" or "venv" or ".venv") continue;
                    dirs.Enqueue(dir);
                }
            }
            catch { }
        }
        return docs.OrderBy(f => f).ToList();
    }
}

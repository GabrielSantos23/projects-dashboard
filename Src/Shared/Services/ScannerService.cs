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

        // Load existing projects completely into memory to ensure flawless path alignment
        var allDbProjects = await dbContext.Projects.ToListAsync();

        foreach (var gitDir in gitFolders)
        {
            try
            {
                using var repo = new Repository(gitDir);

                if (!repo.Commits.Any()) continue;

                var lastCommit = repo.Head.Tip;
                var lastCommitDate = lastCommit?.Author.When.UtcDateTime;
                if (lastCommitDate < cutoffDate) continue;

                var realDir = Directory.GetParent(gitDir)?.FullName ?? gitDir;
                var normalizedPath = Path.GetFullPath(realDir).Replace('\\', '/').ToLowerInvariant();
                var projectName = new DirectoryInfo(realDir).Name;

                System.IO.File.AppendAllText("scan_log.txt", $"Processing: {normalizedPath}\n");

                foundPaths.Add(normalizedPath);

                // Safe, normalized in-memory matching to upgrade legacy SQLite paths
                var project = allDbProjects.FirstOrDefault(p => p.Path.Replace('\\', '/').ToLowerInvariant() == normalizedPath);
                
                if (project == null)
                {
                    project = new Project { Path = normalizedPath };
                    dbContext.Projects.Add(project);
                    allDbProjects.Add(project);
                }
                else
                {
                    // Upgrade legacy path format natively
                    project.Path = normalizedPath;
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
                metadata["inferred_type"] = DetectPrimaryStack(realDir);

                var techs = DetectAllTechs(realDir);
                metadata["techs"] = string.Join(",", techs);

                
                var contributors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var commitCount = 0;
                // Faster counting without full enumeration
                try { commitCount = repo.Commits.Count(); } catch { }
                foreach (var c in repo.Commits.Take(500))
                {
                    contributors.Add(c.Author.Name);
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

                
                var docs = ListDocs(realDir);
                metadata["doc_count"] = docs.Count.ToString();
                metadata["doc_files"] = string.Join(",", docs.Take(50));

                
                metadata["has_dockerfile"] = (File.Exists(Path.Combine(realDir, "Dockerfile")) ||
                    File.Exists(Path.Combine(realDir, "docker-compose.yml")) ||
                    File.Exists(Path.Combine(realDir, "docker-compose.yaml"))).ToString();
                metadata["has_ci"] = (Directory.Exists(Path.Combine(realDir, ".github", "workflows")) ||
                    File.Exists(Path.Combine(realDir, ".gitlab-ci.yml")) ||
                    File.Exists(Path.Combine(realDir, "Jenkinsfile"))).ToString();
                metadata["has_readme"] = File.Exists(Path.Combine(realDir, "README.md")).ToString();
                metadata["has_license"] = (File.Exists(Path.Combine(realDir, "LICENSE")) ||
                    File.Exists(Path.Combine(realDir, "LICENSE.md"))).ToString();

                project.Metadata = metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process repository at {GitDir}", gitDir);
                System.IO.File.AppendAllText("scan_log.txt", $"ERROR {gitDir}: {ex.Message}\n{ex.StackTrace}\n");
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
        var options = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = false };

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

                foreach (var subDir in Directory.EnumerateDirectories(currentDir, "*", options))
                {
                    var dirName = Path.GetFileName(subDir);
                    if (IsIgnoredDirectory(dirName)) continue;
                    dirs.Enqueue(subDir);
                }
            }
            catch { }
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
        var allFiles = new List<string>();

        try
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = "ls-files --cached --others --exclude-standard";
            process.StartInfo.WorkingDirectory = projectPath;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            allFiles = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(f => f.Trim()).ToList();
        }
        catch
        {
            allFiles = new List<string>();
        }

        var extensions = allFiles.Select(f => Path.GetExtension(f).ToLowerInvariant()).Distinct().ToList();
        var basenames = allFiles.Select(f => Path.GetFileName(f).ToLowerInvariant()).Distinct().ToList();

        if (basenames.Contains("gemfile"))
        {
            techs.Add("Ruby");
            try
            {
                var pkgPath = Path.Combine(projectPath, allFiles.First(f => Path.GetFileName(f).ToLowerInvariant() == "gemfile"));
                if (File.ReadAllText(pkgPath).Contains("rails")) techs.Add("Rails");
            }
            catch { }
        }

        if (extensions.Contains(".rb")) techs.Add("Ruby");
        if (extensions.Contains(".cs") || basenames.Any(f => f.EndsWith(".csproj"))) techs.Add("C#");
        if (basenames.Any(f => f.EndsWith(".csproj") || f.EndsWith(".sln"))) techs.Add(".NET");
        if (extensions.Contains(".go") || basenames.Contains("go.mod")) techs.Add("Go");
        if (extensions.Contains(".java") || basenames.Contains("pom.xml") || basenames.Contains("build.gradle")) techs.Add("Java");
        if (extensions.Contains(".rs") || basenames.Contains("cargo.toml")) techs.Add("Rust");
        if (extensions.Contains(".py") || basenames.Contains("requirements.txt") || basenames.Contains("pyproject.toml")) techs.Add("Python");
        
        if (extensions.Contains(".html") || extensions.Contains(".htm")) techs.Add("HTML");
        if (extensions.Contains(".css") || extensions.Contains(".scss") || extensions.Contains(".sass")) techs.Add("CSS");
        if (extensions.Contains(".php")) techs.Add("PHP");
        if (extensions.Contains(".lua")) techs.Add("Lua");
        if (extensions.Contains(".c") || (extensions.Contains(".h") && !extensions.Contains(".cpp") && !extensions.Contains(".cxx"))) techs.Add("C");
        if (extensions.Contains(".cpp") || extensions.Contains(".hpp") || extensions.Contains(".cxx") || extensions.Contains(".cc")) techs.Add("C++");

        if (basenames.Contains("package.json"))
        {
            try
            {
                var pkgPath = Path.Combine(projectPath, allFiles.First(f => Path.GetFileName(f).ToLowerInvariant() == "package.json"));
                var pkgContent = File.ReadAllText(pkgPath);
                if (pkgContent.Contains("\"react\"")) techs.Add("React");
                if (pkgContent.Contains("\"vue\"")) techs.Add("Vue");
                if (pkgContent.Contains("\"svelte\"")) techs.Add("Svelte");
                if (pkgContent.Contains("\"next\"")) techs.Add("Next.js");
                if (pkgContent.Contains("\"angular\"")) techs.Add("Angular");

                if (pkgContent.Contains("\"typescript\"") || basenames.Contains("tsconfig.json")) techs.Add("TypeScript");

                if (pkgContent.Contains("\"electron\"")) techs.Add("Electron");
                if (pkgContent.Contains("\"tailwindcss\"")) techs.Add("Tailwind");
            }
            catch { }
        }

        if (extensions.Contains(".ts") || extensions.Contains(".tsx")) techs.Add("TypeScript");
        if (extensions.Contains(".js") || extensions.Contains(".jsx")) techs.Add("JavaScript");

        if (basenames.Contains("dockerfile") || basenames.Contains("docker-compose.yml") || basenames.Contains("docker-compose.yaml"))
            techs.Add("Docker");

        return techs.Distinct().ToList();
    }

    private List<string> ListDocs(string projectPath)
    {
        var docs = new List<string>();
        var dirs = new Queue<string>();
        dirs.Enqueue(projectPath);
        var options = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = false };

        while (dirs.Count > 0)
        {
            var current = dirs.Dequeue();
            try
            {
                foreach (var file in Directory.EnumerateFiles(current, "*.md", options))
                {
                    docs.Add(System.IO.Path.GetRelativePath(projectPath, file));
                }

                foreach (var dir in Directory.EnumerateDirectories(current, "*", options))
                {
                    var name = Path.GetFileName(dir).ToLowerInvariant();
                    if (name is "node_modules" or "bin" or "obj" or ".git" or "packages" or "dist" or "build" or "venv" or ".venv" or ".vs" or "target" or "out") continue;
                    dirs.Enqueue(dir);
                }
            }
            catch { }
        }
        return docs.OrderBy(f => f).ToList();
    }
}

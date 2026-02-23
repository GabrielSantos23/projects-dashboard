using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Shared.Data;
using ProjectDashboard.Shared.Models;
using ProjectDashboard.Shared.Services;
using LibGit2Sharp;

namespace ProjectDashboard.Avalonia.ViewModels;

public class ProjectDetailViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext>? _dbFactory;
    private readonly ScannerService? _scanner;
    private readonly AppStateService? _appState;

    private Project? _project;
    public Project? Project
    {
        get => _project;
        set => SetProperty(ref _project, value);
    }

    public ObservableCollection<CommitInfo> RecentCommits { get; } = new();
    public ObservableCollection<string> ContributorNames { get; } = new();
    public ObservableCollection<TechPillDisplay> TechPills { get; } = new();

    public string EditorName => _appState?.EditorName ?? "Cursor";

    public string EditorCommand => EditorName switch
    {
        "VS Code" => "code .",
        "Rider" => "rider64 .",
        "Zed" => "zed .",
        "Antigravity" => "antigravity .",
        _ => "cursor ."
    };

    private bool _isRescanning;
    public bool IsRescanning
    {
        get => _isRescanning;
        set => SetProperty(ref _isRescanning, value);
    }

    private bool _showDeleteModal;
    public bool ShowDeleteModal
    {
        get => _showDeleteModal;
        set => SetProperty(ref _showDeleteModal, value);
    }

    private bool _showTagsModal;
    public bool ShowTagsModal
    {
        get => _showTagsModal;
        set => SetProperty(ref _showTagsModal, value);
    }

    private string _editingTags = "";
    public string EditingTags
    {
        get => _editingTags;
        set => SetProperty(ref _editingTags, value);
    }

    private bool _showCommitModal;
    public bool ShowCommitModal
    {
        get => _showCommitModal;
        set => SetProperty(ref _showCommitModal, value);
    }

    private string _commitMessage = "";
    public string CommitMessage
    {
        get => _commitMessage;
        set => SetProperty(ref _commitMessage, value);
    }

    private string _commitError = "";
    public string CommitError
    {
        get => _commitError;
        set => SetProperty(ref _commitError, value);
    }

    private bool _isCommitting;
    public bool IsCommitting
    {
        get => _isCommitting;
        set
        {
            SetProperty(ref _isCommitting, value);
            OnPropertyChanged(nameof(CommitButtonText));
        }
    }

    private bool _showGitTagModal;
    public bool ShowGitTagModal
    {
        get => _showGitTagModal;
        set => SetProperty(ref _showGitTagModal, value);
    }

    private string _gitTagName = "";
    public string GitTagName
    {
        get => _gitTagName;
        set => SetProperty(ref _gitTagName, value);
    }

    private string _gitTagMessage = "";
    public string GitTagMessage
    {
        get => _gitTagMessage;
        set => SetProperty(ref _gitTagMessage, value);
    }

    private string _gitTagError = "";
    public string GitTagError
    {
        get => _gitTagError;
        set => SetProperty(ref _gitTagError, value);
    }

    private bool _isDeleting;
    public bool IsDeleting
    {
        get => _isDeleting;
        set => SetProperty(ref _isDeleting, value);
    }

    private string _deleteError = "";
    public string DeleteError
    {
        get => _deleteError;
        set => SetProperty(ref _deleteError, value);
    }

    public string StallRisk => Project?.Status switch
    {
        "Active" => "Low",
        "Recent" => "Normal",
        "Stalled" => "High",
        _ => "Archived"
    };

    public string StallRiskColor => Project?.Status switch
    {
        "Active" => "#34c759",
        "Recent" => "#0088ff",
        "Stalled" => "#ffcc00",
        _ => "#5b5b60"
    };

    public int StallRiskWidth => Project?.Status switch
    {
        "Active" => 15,
        "Recent" => 50,
        "Stalled" => 85,
        _ => 100
    };

    public string RescanText => IsRescanning ? "Scanning..." : "Rescan Project";
    public string PinText => Project?.IsPinned == true ? "Unpin Project" : "Pin Project";
    public string DeleteButtonText => IsDeleting ? "Deleting..." : "Yes, Delete Completely";
    public string CommitButtonText => IsCommitting ? "Committing..." : "Commit";

    public ProjectDetailViewModel() { }

    public ProjectDetailViewModel(IDbContextFactory<AppDbContext> dbFactory, ScannerService scanner, AppStateService appState)
    {
        _dbFactory = dbFactory;
        _scanner = scanner;
        _appState = appState;
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(Project) || propertyName == nameof(IsRescanning) || propertyName == nameof(IsDeleting))
        {
            base.OnPropertyChanged(nameof(StallRisk));
            base.OnPropertyChanged(nameof(StallRiskColor));
            base.OnPropertyChanged(nameof(StallRiskWidth));
            base.OnPropertyChanged(nameof(RescanText));
            base.OnPropertyChanged(nameof(PinText));
            base.OnPropertyChanged(nameof(DeleteButtonText));
        }
    }

    public async Task LoadProject(int projectId)
    {
        if (_dbFactory == null) return;
        
        using var db = await _dbFactory.CreateDbContextAsync();
        Project = await db.Projects.FindAsync(projectId);
        
        if (Project == null) return;

        RecentCommits.Clear();
        ContributorNames.Clear();

        var meta = Project.Metadata;

        if (meta.TryGetValue("recent_commits", out var rc) && !string.IsNullOrWhiteSpace(rc))
        {
            var commits = rc.Split(";;", StringSplitOptions.RemoveEmptyEntries)
                .Select(entry =>
                {
                    var parts = entry.Split('|', 3);
                    return new CommitInfo
                    {
                        Date = parts.Length > 0 && DateTime.TryParse(parts[0], out var d) ? d : DateTime.MinValue,
                        Author = parts.Length > 1 ? parts[1] : "unknown",
                        Message = parts.Length > 2 ? parts[2] : ""
                    };
                })
                .Where(c => c.Date != DateTime.MinValue)
                .ToList();

            foreach (var c in commits)
                RecentCommits.Add(c);
        }

        if (meta.TryGetValue("contributor_names", out var cn) && !string.IsNullOrWhiteSpace(cn))
        {
            foreach (var name in cn.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                ContributorNames.Add(name);
        }

        TechPills.Clear();
        if (Project.TechPills != null)
        {
            var colors = new[] { "#e53935", "#e65100", "#2e7d32", "#1565c0", "#6a1b9a", "#00838f", "#c62828", "#4527a0" };
            var techPillsList = Project.TechPills.ToList();
            for (int i = 0; i < techPillsList.Count; i++)
            {
                var pt = techPillsList[i];
                var fileName = pt.ToLowerInvariant() + ".svg";
                var uri = new Uri($"avares://ProjectDashboard.Avalonia/Assets/svgs/{fileName}");
                var svgPath = global::Avalonia.Platform.AssetLoader.Exists(uri) ? uri.ToString() : null;

                TechPills.Add(new TechPillDisplay
                {
                    Name = pt,
                    Color = colors[i % colors.Length],
                    SvgPath = svgPath
                });
            }
        }

        ShowDeleteModal = false;
    }

    public async Task TogglePin()
    {
        if (Project == null || _dbFactory == null) return;
        Project.IsPinned = !Project.IsPinned;
        await SaveProject();
        OnPropertyChanged(nameof(PinText));
    }

    public async Task RescanProject()
    {
        if (Project == null || _scanner == null) return;
        IsRescanning = true;
        OnPropertyChanged(nameof(IsRescanning));
        OnPropertyChanged(nameof(RescanText));

        await _scanner.ScanAsync(Project.Path, 9999);
        await LoadProject(Project.Id);

        IsRescanning = false;
        OnPropertyChanged(nameof(IsRescanning));
        OnPropertyChanged(nameof(RescanText));
    }

    public void OpenTagsModal()
    {
        EditingTags = Project?.Tags ?? "";
        ShowTagsModal = true;
    }

    public async Task SaveTags()
    {
        if (Project != null && _dbFactory != null)
        {
            Project.Tags = EditingTags;
            using var db = await _dbFactory.CreateDbContextAsync();
            db.Projects.Update(Project);
            await db.SaveChangesAsync();
            OnPropertyChanged(nameof(Project));
        }
        ShowTagsModal = false;
    }

    public void OpenCommitModal()
    {
        CommitMessage = "";
        CommitError = "";
        ShowCommitModal = true;
    }

    public async Task CreateCommit()
    {
        if (Project == null || string.IsNullOrWhiteSpace(CommitMessage)) return;
        
        IsCommitting = true;
        CommitError = "";
        
        try
        {
            await Task.Run(() => 
            {
                using var repo = new Repository(Project.Path);
                Commands.Stage(repo, "*");
                
                Signature signature;
                try
                {
                    signature = repo.Config.BuildSignature(DateTimeOffset.Now);
                }
                catch
                {
                    signature = new Signature("User", "user@example.com", DateTimeOffset.Now);
                }

                repo.Commit(CommitMessage, signature, signature);
            });
            
            ShowCommitModal = false;
            await RescanProject();
        }
        catch (Exception ex)
        {
            CommitError = $"Error: {ex.Message}";
        }
        finally
        {
            IsCommitting = false;
        }
    }

    public void OpenGitTagModal()
    {
        GitTagName = "";
        GitTagMessage = "";
        GitTagError = "";
        ShowGitTagModal = true;
    }

    public void CreateGitTag()
    {
        if (Project == null || string.IsNullOrWhiteSpace(GitTagName)) return;
        
        try
        {
            using var repo = new Repository(Project.Path);
            
            Signature signature;
            try
            {
                signature = repo.Config.BuildSignature(DateTimeOffset.Now);
            }
            catch
            {
                signature = new Signature("User", "user@example.com", DateTimeOffset.Now);
            }

            if (string.IsNullOrWhiteSpace(GitTagMessage))
            {
                repo.ApplyTag(GitTagName);
            }
            else
            {
                repo.ApplyTag(GitTagName, repo.Head.Tip.Sha, signature, GitTagMessage);
            }
            
            ShowGitTagModal = false;
        }
        catch (Exception ex)
        {
            GitTagError = $"Error: {ex.Message}";
        }
    }

    public async Task DeleteProject()
    {
        if (Project == null) return;
        IsDeleting = true;
        DeleteError = "";
        OnPropertyChanged(nameof(IsDeleting));
        OnPropertyChanged(nameof(DeleteButtonText));

        try
        {
            if (Directory.Exists(Project.Path))
            {
                ForceDeleteDirectory(Project.Path);
            }

            if (_dbFactory != null)
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                db.Projects.Remove(Project);
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            DeleteError = $"Error deleting files: {ex.Message}";
        }
        finally
        {
            IsDeleting = false;
            OnPropertyChanged(nameof(IsDeleting));
            OnPropertyChanged(nameof(DeleteButtonText));
        }
    }

    public void OpenTerminal()
    {
        if (Project == null || !Directory.Exists(Project.Path)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" /d \"{Project.Path}\" cmd",
                UseShellExecute = true
            });
        }
        catch { }
    }

    public void OpenInEditor()
    {
        if (Project == null || !Directory.Exists(Project.Path)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {EditorCommand}",
                WorkingDirectory = Project.Path,
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
        catch { }
    }

    public void SetEditor(string editor) { } 

    private async Task SaveProject()
    {
        if (Project == null || _dbFactory == null) return;
        using var db = await _dbFactory.CreateDbContextAsync();
        db.Projects.Update(Project);
        await db.SaveChangesAsync();
    }

    private void ForceDeleteDirectory(string targetDir)
    {
        var files = Directory.GetFiles(targetDir);
        var dirs = Directory.GetDirectories(targetDir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            ForceDeleteDirectory(dir);
        }

        File.SetAttributes(targetDir, FileAttributes.Normal);
        Directory.Delete(targetDir, false);
    }
}

public class CommitInfo
{
    public DateTime Date { get; init; }
    public string Author { get; init; } = "";
    public string Message { get; init; } = "";
    public string AuthorInitial => Author.Length > 0 ? Author[..1].ToUpper() : "?";
    public string TimeAgo
    {
        get
        {
            var span = DateTime.UtcNow - Date;
            if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return Date.ToString("MMM dd, yyyy");
        }
    }
}

public static class StringExtensions
{
    public static string Initial(this string? value) => 
        !string.IsNullOrEmpty(value) && value.Length > 0 ? value[..1].ToUpper() : "?";
}

public class TechPillDisplay
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#0088ff";
    public string Initial => Name.Length > 0 ? Name[..1].ToUpper() : "?";
    
    public string? SvgPath { get; set; }
    public bool HasSvg => !string.IsNullOrEmpty(SvgPath);
    public bool ShowFallback => !HasSvg;
}

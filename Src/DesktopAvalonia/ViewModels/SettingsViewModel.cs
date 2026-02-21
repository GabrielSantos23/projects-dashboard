using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Shared.Data;
using ProjectDashboard.Shared.Models;
using ProjectDashboard.Shared.Services;

namespace ProjectDashboard.Avalonia.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext>? _dbFactory;
    private readonly AppStateService? _appState;

    public ObservableCollection<ScanFolder> Folders { get; } = new();

    private string _newFolderPath = "";
    public string NewFolderPath
    {
        get => _newFolderPath;
        set => SetProperty(ref _newFolderPath, value);
    }

    private string _errorMessage = "";
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string OpenInEditor
    {
        get => _appState?.EditorName ?? "Cursor";
        set 
        {
            if (_appState != null)
            {
                _appState.EditorName = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _watcherEnabled = true;
    public bool WatcherEnabled
    {
        get => _watcherEnabled;
        set => SetProperty(ref _watcherEnabled, value);
    }

    private int _selectedTheme = 1;
    public int SelectedTheme
    {
        get => _selectedTheme;
        set => SetProperty(ref _selectedTheme, value);
    }

    public string[] EditorOptions { get; } = new[] { "Cursor", "VS Code", "Rider", "Zed", "Antigravity" };

    public SettingsViewModel() { }

    public SettingsViewModel(IDbContextFactory<AppDbContext> dbFactory, AppStateService appState)
    {
        _dbFactory = dbFactory;
        _appState = appState;
    }

    public async Task LoadFolders()
    {
        if (_dbFactory == null) return;
        
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var folders = await db.ScanFolders.OrderBy(f => f.CreatedAt).ToListAsync();

            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Folders.Clear();
                foreach (var f in folders)
                    Folders.Add(f);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading folders: {ex}");
            ErrorMessage = $"Failed to load folders: {ex.Message}";
        }
    }

    public async Task AddFolder()
    {
        if (_dbFactory == null) return;
        
        ErrorMessage = "";
        var path = NewFolderPath.Trim();

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!Directory.Exists(path))
        {
            ErrorMessage = $"Folder does not exist: {path}";
            return;
        }

        if (Folders.Any(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = "This folder is already in the list.";
            return;
        }

        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var folder = new ScanFolder { Path = path };
            db.ScanFolders.Add(folder);
            await db.SaveChangesAsync();

            NewFolderPath = "";
            await LoadFolders();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding folder: {ex}");
            ErrorMessage = $"Failed to add folder: {ex.Message}";
        }
    }

    public async Task RemoveFolder(ScanFolder folder)
    {
        if (_dbFactory == null) return;
        
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.ScanFolders.FindAsync(folder.Id);
            if (entity != null)
            {
                db.ScanFolders.Remove(entity);
                await db.SaveChangesAsync();
            }
            await LoadFolders();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing folder: {ex}");
            ErrorMessage = $"Failed to remove folder: {ex.Message}";
        }
    }

    public void ToggleWatcher()
    {
        WatcherEnabled = !WatcherEnabled;
    }

    public void SetTheme(int themeId)
    {
        SelectedTheme = themeId;
    }

    public string GetEditorCommand()
    {
        return OpenInEditor switch
        {
            "VS Code" => "code .",
            "Rider" => "rider64 .",
            "Zed" => "zed .",
            "Antigravity" => "antigravity .",
            _ => "cursor ."
        };
    }
}

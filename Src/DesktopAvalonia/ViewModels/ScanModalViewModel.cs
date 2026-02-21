using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Shared.Data;
using ProjectDashboard.Shared.Models;
using ProjectDashboard.Shared.Services;

namespace ProjectDashboard.Avalonia.ViewModels;

public class ScanModalViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext>? _dbFactory;
    private readonly ScannerService? _scanner;

    public ObservableCollection<ScanFolder> Folders { get; } = new();

    private bool _isVisible;
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

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

    private bool _isScanning;
    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    private string _scanProgressMessage = "";
    public string ScanProgressMessage
    {
        get => _scanProgressMessage;
        set => SetProperty(ref _scanProgressMessage, value);
    }

    public string FolderCountText => $"{Folders.Count} folder{(Folders.Count == 1 ? "" : "s")} configured";
    public bool CanStartScan => Folders.Any() && !IsScanning;

    public event Action? ScanComplete;

    public ScanModalViewModel() { }

    public ScanModalViewModel(IDbContextFactory<AppDbContext> dbFactory, ScannerService scanner)
    {
        _dbFactory = dbFactory;
        _scanner = scanner;
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

                OnPropertyChanged(nameof(FolderCountText));
                OnPropertyChanged(nameof(CanStartScan));
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

    public async Task StartScan()
    {
        if (!Folders.Any() || IsScanning || _scanner == null) return;

        IsScanning = true;
        ErrorMessage = "";
        OnPropertyChanged(nameof(IsScanning));
        OnPropertyChanged(nameof(CanStartScan));

        try
        {
            for (int i = 0; i < Folders.Count; i++)
            {
                var folder = Folders[i];
                ScanProgressMessage = $"Scanning {folder.Path} ({i + 1}/{Folders.Count})...";
                OnPropertyChanged(nameof(ScanProgressMessage));
                await Task.Delay(50);

                await Task.Run(async () => {
                    await _scanner.ScanAsync(folder.Path, 36500);
                });
            }

            ScanProgressMessage = "Scan complete!";
            OnPropertyChanged(nameof(ScanProgressMessage));
            await Task.Delay(500);

            ScanComplete?.Invoke();

            IsVisible = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during scan: {ex}");
            ErrorMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            OnPropertyChanged(nameof(IsScanning));
            OnPropertyChanged(nameof(CanStartScan));
        }
    }
}

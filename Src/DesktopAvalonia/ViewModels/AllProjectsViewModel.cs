using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Shared.Data;
using ProjectDashboard.Shared.Models;
using ProjectDashboard.Shared.Services;

namespace ProjectDashboard.Avalonia.ViewModels;

public class AllProjectsViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext>? _dbFactory;
    private readonly ScannerService? _scannerService;

    public ObservableCollection<Project> Projects { get; } = new();

    private string _searchQuery = "";
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                OnPropertyChanged(nameof(FilteredProjects));
            }
        }
    }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public IEnumerable<Project> FilteredProjects => Projects
        .Where(p => string.IsNullOrEmpty(SearchQuery) ||
                    p.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    p.Path.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));

    public ICommand RefreshCommand { get; }

    public AllProjectsViewModel() 
    {
        RefreshCommand = new RelayCommand(async () => await HandleRefresh());
    }

    public AllProjectsViewModel(IDbContextFactory<AppDbContext> dbFactory, ScannerService scannerService) : this()
    {
        _dbFactory = dbFactory;
        _scannerService = scannerService;
    }

    public async Task LoadProjects()
    {
        if (_dbFactory == null) return;
        
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var projects = await db.Projects
                .OrderBy(p => p.Name)
                .ToListAsync();

            Projects.Clear();
            foreach (var p in projects)
                Projects.Add(p);

            OnPropertyChanged(nameof(FilteredProjects));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading projects: {ex}");
        }
    }

    public async Task HandleRefresh()
    {
        IsRefreshing = true;
        OnPropertyChanged(nameof(IsRefreshing));

        try
        {
            if (_dbFactory != null && _scannerService != null)
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                var folders = await db.ScanFolders.ToListAsync();
                foreach (var folder in folders)
                {
                    await Task.Run(async () => await _scannerService.ScanAsync(folder.Path, 9999));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error scanning in HandleRefresh: {ex}");
        }

        await LoadProjects();
        
        IsRefreshing = false;
        OnPropertyChanged(nameof(IsRefreshing));
    }

    public async Task OnScanComplete()
    {
        await LoadProjects();
    }
}

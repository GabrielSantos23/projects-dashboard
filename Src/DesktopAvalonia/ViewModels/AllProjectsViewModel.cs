using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Shared.Data;
using ProjectDashboard.Shared.Models;

namespace ProjectDashboard.Avalonia.ViewModels;

public class AllProjectsViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext>? _dbFactory;

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

    public AllProjectsViewModel(IDbContextFactory<AppDbContext> dbFactory) : this()
    {
        _dbFactory = dbFactory;
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
        await Task.Delay(400);
        await LoadProjects();
        IsRefreshing = false;
        OnPropertyChanged(nameof(IsRefreshing));
    }

    public async Task OnScanComplete()
    {
        await LoadProjects();
    }
}

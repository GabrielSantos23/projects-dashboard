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

public class DashboardViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext>? _dbFactory;

    public ObservableCollection<Project> Projects { get; } = new();
    public ObservableCollection<TechStatDisplay> TopTechs { get; } = new();

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    private int _activeCount;
    public int ActiveCount
    {
        get => _activeCount;
        set => SetProperty(ref _activeCount, value);
    }

    private int _stalledCount;
    public int StalledCount
    {
        get => _stalledCount;
        set => SetProperty(ref _stalledCount, value);
    }

    private int _archivedCount;
    public int ArchivedCount
    {
        get => _archivedCount;
        set => SetProperty(ref _archivedCount, value);
    }

    private int _totalTechCount;
    public int TotalTechCount
    {
        get => _totalTechCount;
        set => SetProperty(ref _totalTechCount, value);
    }

    public string TopTechName => TopTechs.Count > 0 ? $"#1 {TopTechs[0].Name}" : "No technologies found";

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

    public DashboardViewModel() 
    {
        RefreshCommand = new RelayCommand(async () => await HandleRefresh());
    }

    public DashboardViewModel(IDbContextFactory<AppDbContext> dbFactory) : this()
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
                .OrderByDescending(p => p.LastCommit)
                .ToListAsync();

            Projects.Clear();
            foreach (var p in projects)
                Projects.Add(p);

            TotalCount = projects.Count;

            var now = DateTime.UtcNow;
            ActiveCount = projects.Count(p => p.Status == "Active" || (p.LastCommit.HasValue && (now - p.LastCommit.Value).TotalDays <= 7));
            StalledCount = projects.Count(p => p.Status == "Stalled" || (p.LastCommit.HasValue && (now - p.LastCommit.Value).TotalDays > 30));
            ArchivedCount = projects.Count(p => p.Status == "Archived");

            var techGroups = projects
                .SelectMany(p => p.TechPills)
                .GroupBy(t => t)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            TopTechs.Clear();
            var colors = new[] { "#0088ff", "#ff3b30", "#ffcc00", "#34c759", "#00E5FF" };
            var totalTech = techGroups.Sum(x => x.Count);
            
            for (int i = 0; i < Math.Min(5, techGroups.Count); i++)
            {
                var tech = techGroups[i];
                var share = totalTech > 0 ? (double)tech.Count / totalTech * 100 : 0;
                var fileName = tech.Name.ToLowerInvariant() + ".svg";
                var uri = new Uri($"avares://ProjectDashboard.Avalonia/Assets/svgs/{fileName}");
                var svgPath = global::Avalonia.Platform.AssetLoader.Exists(uri) ? uri.ToString() : null;

                TopTechs.Add(new TechStatDisplay
                {
                    Name = tech.Name,
                    Count = tech.Count,
                    Color = colors[i % colors.Length],
                    Share = $"{share:F1}%",
                    Initial = tech.Name.Length > 0 ? tech.Name[..1].ToUpper() : "?",
                    Width = share,
                    SvgPath = svgPath
                });
            }

            TotalTechCount = totalTech;

            OnPropertyChanged(nameof(FilteredProjects));
            OnPropertyChanged(nameof(TopTechName));
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

public class TechStatDisplay
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public string Color { get; set; } = "#0088ff";
    public string Share { get; set; } = "0%";
    public string Initial { get; set; } = "?";
    public double Width { get; set; }

    public string? SvgPath { get; set; }
    public bool HasSvg => !string.IsNullOrEmpty(SvgPath);
    public bool ShowFallback => !HasSvg;
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

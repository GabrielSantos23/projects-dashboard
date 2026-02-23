using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectDashboard.Avalonia.Views;
using ProjectDashboard.Avalonia.ViewModels;
using ProjectDashboard.Shared.Data;
using ProjectDashboard.Shared.Models;
using ProjectDashboard.Shared.Services;

namespace ProjectDashboard.Avalonia;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly IServiceProvider? _services;
    private readonly IDbContextFactory<AppDbContext>? _dbFactory;
    
    private bool isSidebarCollapsed = false;
    private UserControl? _currentView;
    private string _currentPage = "Dashboard";

    private string CurrentVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
    private string _latestReleaseUrl = "";
    private string _exeDownloadUrl = "";

    public ObservableCollection<Project> PinnedProjects { get; } = new();
    
    private int _totalProjects;
    public int TotalProjects
    {
        get => _totalProjects;
        set { _totalProjects = value; OnPropertyChanged(); }
    }

    private bool _hasPinnedProjects = true;
    public bool HasPinnedProjects
    {
        get => _hasPinnedProjects;
        set { _hasPinnedProjects = value; OnPropertyChanged(); }
    }

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            DataContext = this;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing MainWindow: {ex}");
            throw;
        }
    }

    public MainWindow(IServiceProvider services) : this()
    {
        _services = services;
        _dbFactory = services.GetService<IDbContextFactory<AppDbContext>>();
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        try
        {
            EnsureStartMenuShortcut();
            await LoadSidebarData();
            NavigateToDashboard();
            
            _ = CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during load: {ex}");
            System.IO.File.AppendAllText("debug_log.txt", $"Error during load: {ex.Message}\n");
        }
    }

    private void EnsureStartMenuShortcut()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath) || !exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return;
            
            // Only create shortcut if we are running the published exe, not inside 'bin/Debug'
            if (exePath.Contains(@"\bin\Debug") || exePath.Contains(@"\bin\Release")) return;

            var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            var shortcutDir = Path.Combine(startMenu, "Programs", "Project Dashboard");
            if (!Directory.Exists(shortcutDir)) Directory.CreateDirectory(shortcutDir);
            
            var shortcutPath = Path.Combine(shortcutDir, "Project Dashboard.lnk");
            
            if (!File.Exists(shortcutPath))
            {
                var script = $"$s=(New-Object -COM WScript.Shell).CreateShortcut('{shortcutPath}');$s.TargetPath='{exePath}';$s.Save()";
                var psInfo = new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psInfo);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create shortcut: {ex}");
        }
    }

    public async Task LoadSidebarData()
    {
        if (_dbFactory == null) return;
        
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var allProjects = await db.Projects
                .OrderByDescending(p => p.LastCommit)
                .ToListAsync();

            TotalProjects = allProjects.Count;

            PinnedProjects.Clear();
            var pinned = allProjects.Where(p => p.IsPinned);
            foreach (var p in pinned)
                PinnedProjects.Add(p);

            HasPinnedProjects = PinnedProjects.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading sidebar data: {ex}");
        }
    }

    private void SidebarToggle_Click(object? sender, RoutedEventArgs e)
    {
        isSidebarCollapsed = !isSidebarCollapsed;
        SidebarBorder.Width = isSidebarCollapsed ? 56 : 220;
        BrandText.IsVisible = !isSidebarCollapsed;
        BrandPanel.IsVisible = !isSidebarCollapsed;
        GeneralText.IsVisible = !isSidebarCollapsed;
        NavDashboardText.IsVisible = !isSidebarCollapsed;
        NavProjectsText.IsVisible = !isSidebarCollapsed;
        NavProjectsBadge.IsVisible = !isSidebarCollapsed;
        NavActivityText.IsVisible = !isSidebarCollapsed;
        FavoritesText.IsVisible = !isSidebarCollapsed;
        NoFavoritesText.IsVisible = !isSidebarCollapsed && !HasPinnedProjects;
        NavSettingsText.IsVisible = !isSidebarCollapsed;

        if (isSidebarCollapsed)
        {
            Grid.SetColumn(SidebarToggleIcon, 0);
            Grid.SetColumnSpan(SidebarToggleIcon, 3);
            SidebarToggleIcon.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center;
            BrandButton.Padding = new global::Avalonia.Thickness(0, 20);
        }
        else
        {
            Grid.SetColumn(SidebarToggleIcon, 2);
            Grid.SetColumnSpan(SidebarToggleIcon, 1);
            SidebarToggleIcon.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right;
            BrandButton.Padding = new global::Avalonia.Thickness(16, 20);
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void TitleBar_DoubleTapped(object? sender, TappedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Minimize_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SetActiveButton(Button activeBtn)
    {
        NavDashboard.Classes.Remove("active");
        NavProjects.Classes.Remove("active");
        NavActivity.Classes.Remove("active");
        NavSettings.Classes.Remove("active");

        activeBtn.Classes.Add("active");
    }
    
    protected override void OnKeyDown(global::Avalonia.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == global::Avalonia.Input.Key.B && e.KeyModifiers == global::Avalonia.Input.KeyModifiers.Control)
        {
            SidebarToggle_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void Dashboard_Click(object? sender, RoutedEventArgs e)
    {
        SetActiveButton(NavDashboard);
        NavigateToDashboard();
    }
    
    private void Projects_Click(object? sender, RoutedEventArgs e)
    {
        SetActiveButton(NavProjects);
        NavigateToAllProjects();
    }

    private void Activity_Click(object? sender, RoutedEventArgs e)
    {
        SetActiveButton(NavActivity);
        NavigateToRecentActivity();
    }

    private void Settings_Click(object? sender, RoutedEventArgs e)
    {
        SetActiveButton(NavSettings);
        NavigateToSettings();
    }

    private void Favorite_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int projectId)
        {
            NavigateToProjectDetail(projectId);
        }
    }

    private ScanModalView? _currentScanModal;

    public void NavigateToDashboard()
    {
        _currentPage = "Dashboard";
        var vm = _services?.GetService<DashboardViewModel>();
        var view = new DashboardView { DataContext = vm };
        
        view.ScanRequested += OnScanRequested;
        view.ProjectSelected += (s, id) => NavigateToProjectDetail(id);
        
        MainContentHost.Content = view;
        _currentView = view;
        
        vm?.LoadProjects();
    }

    public void NavigateToAllProjects()
    {
        _currentPage = "AllProjects";
        var vm = _services?.GetService<AllProjectsViewModel>();
        var view = new AllProjectsView { DataContext = vm };
        
        view.ProjectSelected += (s, id) => NavigateToProjectDetail(id);
        view.ScanRequested += OnScanRequested;
        
        MainContentHost.Content = view;
        _currentView = view;
        
        vm?.LoadProjects();
    }

    public void NavigateToRecentActivity()
    {
        _currentPage = "RecentActivity";
        var vm = _services?.GetService<RecentActivityViewModel>();
        var view = new RecentActivityView { DataContext = vm };
        
        view.ProjectSelected += (s, id) => NavigateToProjectDetail(id);
        view.ScanRequested += OnScanRequested;
        
        MainContentHost.Content = view;
        _currentView = view;
        
        vm?.LoadProjects();
    }

    public void NavigateToSettings()
    {
        _currentPage = "Settings";
        var vm = _services?.GetService<SettingsViewModel>();
        var view = new SettingsView { DataContext = vm };
        
        view.BrowseFolderRequested += async (s, e) =>
        {
            try
            {
                var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select folder to scan",
                    AllowMultiple = false
                });

                if (folders.Count > 0 && vm != null)
                {
                    vm.NewFolderPath = folders[0].Path.LocalPath;
                    await vm.AddFolder();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error browsing folder: {ex}");
            }
        };
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        MainContentHost.Content = view;
        _currentView = view;
        
        vm?.LoadFolders();
    }

    public void NavigateToProjectDetail(int projectId)
    {
        var vm = _services?.GetService<ProjectDetailViewModel>();
        var view = new ProjectDetailView { DataContext = vm };
        
        view.BackRequested += (s, e) =>
        {
            switch (_currentPage)
            {
                case "AllProjects": NavigateToAllProjects(); break;
                case "RecentActivity": NavigateToRecentActivity(); break;
                default: NavigateToDashboard(); break;
            }
        };
        
        view.SettingsRequested += (s, e) => NavigateToSettings();
        
        MainContentHost.Content = view;
        
        vm?.LoadProject(projectId);
    }

    private void OnScanRequested(object? sender, EventArgs e)
    {
        if (_currentScanModal != null) return;
        
        global::Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            await ShowScanModal();
        });
    }

    private async Task ShowScanModal()
    {
        try
        {
            System.IO.File.AppendAllText("debug_log.txt", "MainWindow - ShowScanModal entered\n");
            
            var vm = _services?.GetService<ScanModalViewModel>();
            if (vm == null)
            {
                System.IO.File.AppendAllText("debug_log.txt", "MainWindow - Failed to get ScanModalViewModel from DI\n");
                return;
            }

            System.IO.File.AppendAllText("debug_log.txt", "MainWindow - ScanModalViewModel obtained, creating view\n");
            
            _currentScanModal = new ScanModalView { DataContext = vm };
            
            _currentScanModal.ScanComplete += async (s, e) =>
            {
                try
                {
                    await LoadSidebarData();
                    
                    if (_currentView?.DataContext is DashboardViewModel dashboardVm)
                        await dashboardVm.OnScanComplete();
                    else if (_currentView?.DataContext is AllProjectsViewModel allProjectsVm)
                        await allProjectsVm.OnScanComplete();
                    else if (_currentView?.DataContext is RecentActivityViewModel recentActivityVm)
                        await recentActivityVm.OnScanComplete();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling ScanComplete: {ex}");
                }
            };

            _currentScanModal.Closed += (s, e) =>
            {
                _currentScanModal = null;
            };

            System.IO.File.AppendAllText("debug_log.txt", "MainWindow - Loading folders into modal...\n");
            await vm.LoadFolders();
            System.IO.File.AppendAllText("debug_log.txt", "MainWindow - Loaded folders, calling ShowDialog\n");
            await _currentScanModal.ShowDialog(this);
            System.IO.File.AppendAllText("debug_log.txt", "MainWindow - ShowDialog exited\n");
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText("debug_log.txt", $"MainWindow - Error showing scan modal: {ex.Message}\n{ex.StackTrace}\n");
            _currentScanModal = null;
        }
    }
    
    // UI Event Handler for checking updates / dismissing 
    private void CloseUpdateToast_Click(object? sender, RoutedEventArgs e)
    {
        UpdateToastContainer.IsVisible = false;
    }

    private async void DownloadUpdate_Click(object? sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        if (!string.IsNullOrEmpty(_exeDownloadUrl))
        {
            if (btn != null) 
            {
                btn.Content = "Downloading...";
                btn.IsEnabled = false;
            }

            try
            {
                var tempFile = Path.Combine(Path.GetTempPath(), $"ProjectDashboard_Update_{Guid.NewGuid():N}.exe");
                
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "ProjectDashboard-App");
                
                var response = await client.GetAsync(_exeDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                
                using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
                fileStream.Close(); // ensure stream is completely flushed

                Process.Start(new ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                });

                // Exit app so the update installer can apply its changes
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading update: {ex}");
                System.IO.File.AppendAllText("debug_log.txt", $"Error downloading update: {ex.Message}\n{ex.StackTrace}\n");
                
                if (btn != null) 
                {
                    btn.Content = $"Download Failed: {ex.Message}";
                    ToolTip.SetTip(btn, ex.ToString());
                    btn.IsEnabled = true;
                }
            }
        }
        else if (!string.IsNullOrEmpty(_latestReleaseUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _latestReleaseUrl,
                    UseShellExecute = true
                });
            }
            catch { }
            UpdateToastContainer.IsVisible = false;
        }
        else
        {
            UpdateToastContainer.IsVisible = false;
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "ProjectDashboard-App");
            
            var response = await client.GetAsync("https://api.github.com/repos/GabrielSantos23/projects-dashboard/releases/latest");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("tag_name", out var tagElement) && root.TryGetProperty("html_url", out var urlElement))
                {
                    var latestVersionStr = tagElement.GetString()?.TrimStart('v', 'V') ?? "";
                    
                    if (Version.TryParse(latestVersionStr, out var latest) && Version.TryParse(CurrentVersion.TrimStart('v', 'V'), out var current))
                    {
                        if (latest > current)
                        {
                            var body = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() : "";
                            _latestReleaseUrl = urlElement.GetString() ?? "";
                            _exeDownloadUrl = "";

                            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var asset in assets.EnumerateArray())
                                {
                                    if (asset.TryGetProperty("name", out var nameEl) && nameEl.GetString()?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
                                    {
                                        if (asset.TryGetProperty("browser_download_url", out var dlUrlEl))
                                        {
                                            _exeDownloadUrl = dlUrlEl.GetString() ?? "";
                                            break;
                                        }
                                    }
                                }
                            }
                            
                            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                var updateVersionText = this.FindControl<TextBlock>("UpdateVersionText");
                                if (updateVersionText != null) updateVersionText.Text = $"Version {latestVersionStr} is ready";
                                
                                var updateBodyText = this.FindControl<TextBlock>("UpdateBodyText");
                                if (updateBodyText != null) 
                                {
                                    updateBodyText.Text = string.IsNullOrWhiteSpace(body) ? "A new version of Project Dashboard is available." : body;
                                }
                                
                                UpdateToastContainer.IsVisible = true;
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking for updates: {ex}");
        }
    }
}

using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectDashboard.Shared.Data;
using ProjectDashboard.Shared.Services;
using ProjectDashboard.Avalonia.ViewModels;

namespace ProjectDashboard.Avalonia;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        try
        {
            AvaloniaXamlLoader.Load(this);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading XAML: {ex}");
            throw;
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            var services = new ServiceCollection();
            
            ConfigureServices(services);
            
            Services = services.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow(Services);
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during framework initialization: {ex}");
            throw;
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var dbPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ProjectsHub",
            "projects.db"
        );
        
        var dbDir = System.IO.Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDir) && !System.IO.Directory.Exists(dbDir))
        {
            System.IO.Directory.CreateDirectory(dbDir);
        }

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<AppStateService>();
        services.AddScoped<ScannerService>();
        
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<AllProjectsViewModel>();
        services.AddTransient<RecentActivityViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ProjectDetailViewModel>();
        services.AddTransient<ScanModalViewModel>();

        EnsureDatabaseCreated(dbPath);
    }

    private void EnsureDatabaseCreated(string dbPath)
    {
        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            
            using var context = new AppDbContext(optionsBuilder.Options);
            context.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating database: {ex}");
        }
    }
}

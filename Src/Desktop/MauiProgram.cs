using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Shared.Data;
using ProjectDashboard.Shared.Services;
using Microsoft.Maui.LifecycleEvents;

#if WINDOWS
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;
#endif

namespace ProjectDashboard.Desktop;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		
		var appDataFolder = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"ProjectDashboard");
		Directory.CreateDirectory(appDataFolder);
		var dbPath = Path.Combine(appDataFolder, "dashboard.db");

		builder.Services.AddDbContextFactory<AppDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"));

		
		builder.Services.AddTransient<ScannerService>();

		
		builder.Services.AddSingleton<IFolderPickerService, ProjectDashboard.Desktop.Services.WindowsFolderPickerService>();
		
		
		builder.Services.AddSingleton<AppStateService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		builder.ConfigureLifecycleEvents(events =>
		{
#if WINDOWS
			events.AddWindows(windows => windows
				.OnWindowCreated(window =>
				{
					try
					{
						var hWnd = WindowNative.GetWindowHandle(window);
						var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
						var appWindow = AppWindow.GetFromWindowId(windowId);
						
						if (appWindow != null)
						{
							appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
							appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
						}
						
						if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
						{
							window.SystemBackdrop = new MicaBackdrop();
						}
						else if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
						{
							window.SystemBackdrop = new DesktopAcrylicBackdrop();
						}
					}
					catch
					{
					}
				}));
#endif
		});

		var app = builder.Build();

		
		using (var scope = app.Services.CreateScope())
		{
			var factory = scope.ServiceProvider
				.GetRequiredService<IDbContextFactory<AppDbContext>>();
			using var db = factory.CreateDbContext();
			db.Database.EnsureCreated();
		}

		return app;
	}
}

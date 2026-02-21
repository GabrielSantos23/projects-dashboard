using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Web.Components;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProjectDashboard");
Directory.CreateDirectory(appDataFolder);
var dbPath = Path.Combine(appDataFolder, "dashboard.db");

builder.Services.AddDbContextFactory<ProjectDashboard.Shared.Data.AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddTransient<ProjectDashboard.Shared.Services.ScannerService>();
builder.Services.AddSingleton<ProjectDashboard.Shared.Services.AppStateService>();
builder.Services.AddSingleton<ProjectDashboard.Shared.Services.IFolderPickerService, ProjectDashboard.Web.Services.DummyFolderPickerService>();


var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ProjectDashboard.Shared.Data.AppDbContext>>();
    using var db = await factory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ProjectDashboard.Shared.Components.Pages.Dashboard).Assembly);

app.Run();

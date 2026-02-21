using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using WinRT.Interop;




namespace ProjectDashboard.Desktop.WinUI;




public partial class App : MauiWinUIApplication
{
	
	
	
	
	public App()
	{
		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

namespace ProjectDashboard.Desktop;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage()) 
		{ 
			Title = "Project Dashboard",
			MinimumWidth = 800,
			MinimumHeight = 600,
			Width = 1200,
			Height = 800
		};
		return window;
	}
}

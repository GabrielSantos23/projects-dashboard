namespace ProjectDashboard.Desktop;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

#if WINDOWS
		blazorWebView.BlazorWebViewInitialized += (s, e) =>
		{
			
			e.WebView.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;
		};
#endif
	}
}

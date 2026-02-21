using System.Threading.Tasks;
using ProjectDashboard.Shared.Services;

namespace ProjectDashboard.Desktop.Services;

public class WindowsFolderPickerService : IFolderPickerService
{
#if WINDOWS
    public bool IsSupported => true;

    public async Task<string?> PickFolderAsync()
    {
        var folderPicker = new Windows.Storage.Pickers.FolderPicker();
        folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
        folderPicker.FileTypeFilter.Add("*");

        var window = Microsoft.Maui.Controls.Application.Current?.Windows[0]?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (window != null)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var result = await folderPicker.PickSingleFolderAsync();
            return result?.Path;
        }

        return null;
    }
#else
    public bool IsSupported => false;
    public Task<string?> PickFolderAsync() => Task.FromResult<string?>(null);
#endif
}

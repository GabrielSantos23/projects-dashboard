using System.Threading.Tasks;

namespace ProjectDashboard.Shared.Services;

public interface IFolderPickerService
{
    bool IsSupported { get; }
    Task<string?> PickFolderAsync();
}

using System.Threading.Tasks;
using ProjectDashboard.Shared.Services;

namespace ProjectDashboard.Web.Services;

public class DummyFolderPickerService : IFolderPickerService
{
    public bool IsSupported => false;
    public Task<string?> PickFolderAsync() => Task.FromResult<string?>(null);
}

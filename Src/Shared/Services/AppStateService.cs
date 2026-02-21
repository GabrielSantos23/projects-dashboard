using System;

namespace ProjectDashboard.Shared.Services;

public class AppStateService
{
    public event Action? OnChange;
    public string EditorName { get; set; } = "Cursor";

    public void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProjectDashboard.Shared.Models;

namespace ProjectDashboard.Avalonia.Views;

public partial class RecentActivityView : UserControl
{
    public RecentActivityView()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing RecentActivityView: {ex}");
            throw;
        }
    }

    public event EventHandler<int>? ProjectSelected;
    public event EventHandler? ScanRequested;

    private void ProjectRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Project project)
        {
            ProjectSelected?.Invoke(this, project.Id);
        }
    }

    private void ScanBtn_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("RecentActivityView - Scan button clicked!");
            ScanRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ScanRequested handler: {ex}");
        }
    }
}

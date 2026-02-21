using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProjectDashboard.Shared.Models;

namespace ProjectDashboard.Avalonia.Views;

public partial class AllProjectsView : UserControl
{
    public AllProjectsView()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing AllProjectsView: {ex}");
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
            System.Diagnostics.Debug.WriteLine("AllProjectsView - Scan button clicked!");
            ScanRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ScanRequested handler: {ex}");
        }
    }
}

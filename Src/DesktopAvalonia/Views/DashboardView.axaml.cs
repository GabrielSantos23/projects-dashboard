using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Layout;
using System.Collections.Specialized;
using ProjectDashboard.Avalonia.ViewModels;
using System.IO;

namespace ProjectDashboard.Avalonia.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing DashboardView: {ex}");
            throw;
        }

        DataContextChanged += OnDataContextChanged;
    }

    private DashboardViewModel? _vm;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm != null)
        {
            _vm.TopTechs.CollectionChanged -= OnTopTechsChanged;
        }

        if (DataContext is DashboardViewModel vm)
        {
            _vm = vm;
            _vm.TopTechs.CollectionChanged += OnTopTechsChanged;
            RebuildTechBar();
        }
    }

    private void OnTopTechsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildTechBar();
    }

    private void RebuildTechBar()
    {
        if (_vm == null || TechBarContainer == null) return;

        TechBarContainer.ColumnDefinitions.Clear();
        TechBarContainer.Children.Clear();

        int col = 0;
        foreach (var tech in _vm.TopTechs)
        {
            TechBarContainer.ColumnDefinitions.Add(new ColumnDefinition 
            { 
                Width = new GridLength(tech.Width, GridUnitType.Star) 
            });

            var border = new Border
            {
                Background = Brush.Parse(tech.Color),
                CornerRadius = new global::Avalonia.CornerRadius(1),
                Margin = new global::Avalonia.Thickness(0, 0, col < _vm.TopTechs.Count - 1 ? 2 : 0, 0)
            };

            Grid.SetColumn(border, col);
            TechBarContainer.Children.Add(border);
            col++;
        }
    }

    public event EventHandler<int>? ProjectSelected;
    public event EventHandler? ScanRequested;

    private void ScanBtn_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("DashboardView - Scan button clicked!");
            System.IO.File.AppendAllText("debug_log.txt", "DashboardView - Scan button clicked!\n");
            ScanRequested?.Invoke(this, EventArgs.Empty);
            System.IO.File.AppendAllText("debug_log.txt", "DashboardView - ScanRequested event invoked!\n"); 
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ScanRequested handler: {ex}");
        }
    }

    private void ProjectRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is int projectId)
        {
            ProjectSelected?.Invoke(this, projectId);
        }
    }
}

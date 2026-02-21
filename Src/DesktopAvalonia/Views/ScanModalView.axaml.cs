using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using ProjectDashboard.Avalonia.ViewModels;
using ProjectDashboard.Shared.Models;

namespace ProjectDashboard.Avalonia.Views;

public partial class ScanModalView : Window
{
    public ScanModalView()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing ScanModalView: {ex}");
            throw;
        }
    }

    public event EventHandler? ScanComplete;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        try
        {
            var closeBtn = this.FindControl<Button>("CloseBtn");
            if (closeBtn != null)
            {
                closeBtn.Click += (s, args) => Close();
            }

            var cancelBtn = this.FindControl<Button>("CancelBtn");
            if (cancelBtn != null)
            {
                cancelBtn.Click += (s, args) => Close();
            }

            var browseBtn = this.FindControl<Button>("BrowseBtn");
            if (browseBtn != null)
            {
                browseBtn.Click += OnBrowseClick;
            }

            var addBtn = this.FindControl<Button>("AddBtn");
            if (addBtn != null)
            {
                addBtn.Click += OnAddClick;
            }

            var startScanBtn = this.FindControl<Button>("StartScanBtn");
            if (startScanBtn != null)
            {
                startScanBtn.Click += OnStartScanClick;
            }

            var folderPathInput = this.FindControl<TextBox>("FolderPathInput");
            if (folderPathInput != null)
            {
                folderPathInput.KeyDown += OnFolderInputKeyDown;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ScanModalView.OnLoaded: {ex}");
        }
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select folder to scan",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var path = folders[0].Path.LocalPath;
                if (DataContext is ScanModalViewModel vm)
                {
                    vm.NewFolderPath = path;
                    await vm.AddFolder();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error browsing folder: {ex}");
        }
    }

    private async void OnAddClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ScanModalViewModel vm)
            {
                await vm.AddFolder();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding folder: {ex}");
        }
    }

    private void OnStartScanClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ScanModalViewModel vm)
            {
                vm.ScanComplete += () => OnScanCompleteHandler();
                _ = vm.StartScan();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting scan: {ex}");
        }
    }

    private void OnFolderInputKeyDown(object? sender, KeyEventArgs e)
    {
        try
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is ScanModalViewModel vm)
                {
                    _ = vm.AddFolder();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in folder input key down: {ex}");
        }
    }

    private void OnScanCompleteHandler()
    {
        try
        {
            ScanComplete?.Invoke(this, EventArgs.Empty);
            Close();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in scan complete handler: {ex}");
        }
    }

    private void FolderItem_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Button btn)
        {
            var removeBtn = btn.FindControl<Button>("RemoveBtn");
            if (removeBtn != null)
                removeBtn.IsVisible = true;
        }
    }

    private void FolderItem_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Button btn)
        {
            var removeBtn = btn.FindControl<Button>("RemoveBtn");
            if (removeBtn != null)
                removeBtn.IsVisible = false;
        }
    }

    private async void RemoveFolder_Click(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Button btn && btn.FindAncestorOfType<Button>()?.DataContext is ScanFolder folder)
        {
            if (DataContext is ScanModalViewModel vm)
            {
                await vm.RemoveFolder(folder);
            }
        }
    }
}

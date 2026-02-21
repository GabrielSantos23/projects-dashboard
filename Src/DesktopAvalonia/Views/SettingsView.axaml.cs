using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using ProjectDashboard.Avalonia.ViewModels;
using ProjectDashboard.Shared.Models;

namespace ProjectDashboard.Avalonia.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing SettingsView: {ex}");
            throw;
        }
    }

    public event EventHandler? BrowseFolderRequested;
    
    public event EventHandler? SaveRequested;

    private void FolderItem_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Button btn)
        {
            var removeBtn = btn.FindControl<Button>("RemoveFolderBtn");
            if (removeBtn != null)
                removeBtn.IsVisible = true;
        }
    }

    private void FolderItem_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Button btn)
        {
            var removeBtn = btn.FindControl<Button>("RemoveFolderBtn");
            if (removeBtn != null)
                removeBtn.IsVisible = false;
        }
    }

    private async void RemoveFolder_Click(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.DataContext is ScanFolder folder)
            {
                if (DataContext is SettingsViewModel vm)
                {
                    await vm.RemoveFolder(folder);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing folder: {ex}");
        }
    }


    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        try
        {
            var browseBtn = this.FindControl<Button>("BrowseBtn");
            if (browseBtn != null)
            {
                browseBtn.Click += (s, args) => BrowseFolderRequested?.Invoke(this, EventArgs.Empty);
            }

            var addFolderBtn = this.FindControl<Button>("AddFolderBtn");
            if (addFolderBtn != null)
            {
                addFolderBtn.Click += async (s, args) =>
                {
                    try
                    {
                        if (DataContext is SettingsViewModel vm)
                        {
                            await vm.AddFolder();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error adding folder: {ex}");
                    }
                };
            }

            var saveBtn = this.FindControl<Button>("SaveBtn");
            if (saveBtn != null)
            {
                saveBtn.Click += (s, args) => SaveRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SettingsView.OnLoaded: {ex}");
        }
    }
}

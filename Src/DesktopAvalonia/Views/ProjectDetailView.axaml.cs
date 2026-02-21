using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProjectDashboard.Avalonia.ViewModels;

namespace ProjectDashboard.Avalonia.Views;

public partial class ProjectDetailView : UserControl
{
    public ProjectDetailView()
    {
        InitializeComponent();
    }

    public event EventHandler? BackRequested;
    public event EventHandler? SettingsRequested;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var backBtn = this.FindControl<Button>("BackBtn");
        if (backBtn != null)
        {
            backBtn.Click += (s, args) => BackRequested?.Invoke(this, EventArgs.Empty);
        }

        var terminalBtn = this.FindControl<Button>("TerminalBtn");
        if (terminalBtn != null)
        {
            terminalBtn.Click += (s, args) =>
            {
                if (DataContext is ProjectDetailViewModel vm)
                    vm.OpenTerminal();
            };
        }

        var openEditorBtn = this.FindControl<Button>("OpenEditorBtn");
        if (openEditorBtn != null)
        {
            openEditorBtn.Click += (s, args) =>
            {
                if (DataContext is ProjectDetailViewModel vm)
                    vm.OpenInEditor();
            };
        }

        var copyPathBtn = this.FindControl<Button>("CopyPathBtn");
        if (copyPathBtn != null)
        {
            copyPathBtn.Click += async (s, args) =>
            {
                if (DataContext is ProjectDetailViewModel vm && vm.Project != null)
                {
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(vm.Project.Path);
                    }
                }
            };
        }

        var rescanBtn = this.FindControl<Button>("RescanBtn");
        if (rescanBtn != null)
        {
            rescanBtn.Click += async (s, args) =>
            {
                if (DataContext is ProjectDetailViewModel vm)
                    await vm.RescanProject();
            };
        }

        var pinBtn = this.FindControl<Button>("PinBtn");
        if (pinBtn != null)
        {
            pinBtn.Click += async (s, args) =>
            {
                if (DataContext is ProjectDetailViewModel vm)
                    await vm.TogglePin();
            };
        }

        var settingsNavBtn = this.FindControl<Button>("SettingsNavBtn");
        if (settingsNavBtn != null)
        {
            settingsNavBtn.Click += (s, args) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        var deleteBtn = this.FindControl<Button>("DeleteBtn");
        if (deleteBtn != null)
        {
            deleteBtn.Click += (s, args) =>
            {
                if (DataContext is ProjectDetailViewModel vm)
                {
                    vm.ShowDeleteModal = true;
                }
            };
        }

        var cancelDeleteBtn = this.FindControl<Button>("CancelDeleteBtn");
        if (cancelDeleteBtn != null)
        {
            cancelDeleteBtn.Click += (s, args) =>
            {
                if (DataContext is ProjectDetailViewModel vm)
                {
                    vm.ShowDeleteModal = false;
                    vm.DeleteError = "";
                }
            };
        }

        var confirmDeleteBtn = this.FindControl<Button>("ConfirmDeleteBtn");
        if (confirmDeleteBtn != null)
        {
            confirmDeleteBtn.Click += async (s, args) =>
            {
                if (DataContext is ProjectDetailViewModel vm)
                {
                    await vm.DeleteProject();
                    if (string.IsNullOrEmpty(vm.DeleteError))
                    {
                        BackRequested?.Invoke(this, EventArgs.Empty);
                    }
                }
            };
        }
    }
}

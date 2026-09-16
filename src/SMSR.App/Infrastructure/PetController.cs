using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using SMSR.App.Services;
using SMSR.App.ViewModels;
using SMSR.App.Views;

namespace SMSR.App.Infrastructure;

internal sealed class PetController : IDisposable
{
    private readonly AppSettingsService _settings;
    private readonly WorkflowWorkspaceViewModel _workspace;
    private readonly Action _showMainWindow;
    private PetWindow? _window;
    private bool _refreshQueued;

    public PetController(AppSettingsService settings, WorkflowWorkspaceViewModel workspace, Action showMainWindow)
    {
        _settings = settings;
        _workspace = workspace;
        _showMainWindow = showMainWindow;
        _settings.Changed += OnChanged;
        _workspace.Selection.PropertyChanged += OnSelectionChanged;
        _workspace.Monitor.Nodes.CollectionChanged += OnNodesChanged;
        _workspace.Monitor.PlanNodes.CollectionChanged += OnNodesChanged;
        Refresh();
    }

    public bool CanShow => PetMediaSelector.Rules(_settings.Current).Count > 0;
    public bool IsVisible => _window?.IsVisible == true;

    public void Toggle()
    {
        if (!CanShow) return;
        _settings.Save(_settings.Current with { PetEnabled = !IsVisible });
    }

    public void Refresh()
    {
        if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(Refresh);
            return;
        }
        var value = _settings.Current;
        var presentation = PetPresentation.From(_workspace.Monitor.Nodes, _workspace.Monitor.PlanNodes);
        var workflowKey = CurrentWorkflowKey();
        var acknowledged = value.PetAcknowledgedWorkflowKey == workflowKey && workflowKey.Length > 0;
        if (acknowledged && presentation.Status is not ("SUCCESS" or "PENDING"))
        {
            _settings.Save(value with { PetAcknowledgedWorkflowKey = "" });
            value = _settings.Current;
            acknowledged = false;
        }
        if (acknowledged) presentation = presentation.AsIdle();
        var mediaPath = presentation.Status == "IDLE" && File.Exists(value.PetIdleMediaPath)
            ? value.PetIdleMediaPath : PetMediaSelector.Select(value, presentation.Progress);
        if (!value.PetEnabled || mediaPath is null)
        {
            _window?.Hide();
            return;
        }
        try
        {
            _window ??= CreateWindow();
            _window.UpdatePet(mediaPath, presentation, value.PetSizePercent);
            if (!_window.IsVisible) _window.Show();
        }
        catch { _window?.Hide(); }
    }

    public void Dispose()
    {
        _settings.Changed -= OnChanged;
        _workspace.Selection.PropertyChanged -= OnSelectionChanged;
        _workspace.Monitor.Nodes.CollectionChanged -= OnNodesChanged;
        _workspace.Monitor.PlanNodes.CollectionChanged -= OnNodesChanged;
        _window?.Close();
    }

    private void OnChanged(object? sender, EventArgs e) => Refresh();
    private PetWindow CreateWindow()
    {
        var window = new PetWindow();
        window.CompletionAcknowledged += OnCompletionAcknowledged;
        window.OpenRequested += (_, _) => _showMainWindow();
        return window;
    }

    private void OnCompletionAcknowledged(object? sender, EventArgs e)
    {
        var key = CurrentWorkflowKey();
        if (key.Length > 0)
            _settings.Save(_settings.Current with { PetAcknowledgedWorkflowKey = key });
    }

    private string CurrentWorkflowKey()
        => string.IsNullOrWhiteSpace(_workspace.Selection.ProjectId)
            || string.IsNullOrWhiteSpace(_workspace.Selection.WorkflowId)
            ? "" : $"{_workspace.Selection.ProjectId}\n{_workspace.Selection.WorkflowId}";
    private void OnNodesChanged(object? sender, NotifyCollectionChangedEventArgs e) => QueueRefresh();
    private void OnSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WorkflowSelectionViewModel.WorkflowId)
            or nameof(WorkflowSelectionViewModel.SelectedWorkflow)) QueueRefresh();
    }

    private void QueueRefresh()
    {
        if (_refreshQueued) return;
        _refreshQueued = true;
        _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            _refreshQueued = false;
            Refresh();
        });
    }
}

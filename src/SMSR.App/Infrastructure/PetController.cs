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
    private PetWindow? _window;
    private bool _refreshQueued;

    public PetController(AppSettingsService settings, WorkflowWorkspaceViewModel workspace)
    {
        _settings = settings;
        _workspace = workspace;
        _settings.Changed += OnChanged;
        _workspace.Selection.PropertyChanged += OnSelectionChanged;
        _workspace.Monitor.Nodes.CollectionChanged += OnNodesChanged;
        _workspace.Monitor.PlanNodes.CollectionChanged += OnNodesChanged;
        Refresh();
    }

    public bool CanShow => File.Exists(_settings.Current.PetImagePath);
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
        if (!value.PetEnabled || !File.Exists(value.PetImagePath))
        {
            _window?.Hide();
            return;
        }
        try
        {
            _window ??= new PetWindow();
            var graphTitle = _workspace.Selection.SelectedWorkflow?.Title
                ?? _workspace.Selection.WorkflowId;
            if (string.IsNullOrWhiteSpace(graphTitle)) graphTitle = "그래프를 선택하세요";
            _window.UpdatePet(value.PetImagePath, value.PetName, graphTitle,
                PetPresentation.From(_workspace.Monitor.Nodes, _workspace.Monitor.PlanNodes));
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

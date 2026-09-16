using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SMSR.App.ViewModels;

namespace SMSR.App.Views;

public partial class SettingsPetPanel : System.Windows.Controls.UserControl
{
    private SettingsViewModel? _viewModel;
    private int? _draggedBoundary;

    public SettingsPetPanel()
    {
        InitializeComponent();
        Loaded += (_, _) => Attach(DataContext as SettingsViewModel);
        Unloaded += (_, _) => Attach(null);
        DataContextChanged += (_, args) => Attach(args.NewValue as SettingsViewModel);
    }

    private void Attach(SettingsViewModel? viewModel)
    {
        if (_viewModel == viewModel) return;
        if (_viewModel is not null) _viewModel.PetBoundaries.CollectionChanged -= BoundariesChanged;
        _viewModel = viewModel;
        if (_viewModel is not null) _viewModel.PetBoundaries.CollectionChanged += BoundariesChanged;
        DrawBoundaries();
    }

    private void BoundariesChanged(object? sender, NotifyCollectionChangedEventArgs args) => DrawBoundaries();
    private void BoundaryCanvas_SizeChanged(object sender, SizeChangedEventArgs args) => DrawBoundaries();

    private void BoundaryCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs args)
    {
        if (_viewModel?.PetBoundaries.Count is not > 0 || BoundaryCanvas.ActualWidth <= 0) return;
        var x = args.GetPosition(BoundaryCanvas).X;
        var nearest = _viewModel.PetBoundaries.MinBy(item => Math.Abs(Position(item.Value) - x))!;
        if (Math.Abs(Position(nearest.Value) - x) > 18) return;
        _draggedBoundary = nearest.Index;
        BoundaryCanvas.CaptureMouse();
        args.Handled = true;
    }

    private void BoundaryCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs args)
    {
        if (_draggedBoundary is not int index || args.LeftButton != MouseButtonState.Pressed) return;
        DrawBoundaries(index, BoundaryValue(index, args.GetPosition(BoundaryCanvas).X));
    }

    private void BoundaryCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs args)
    {
        if (_draggedBoundary is not int index || _viewModel is null) return;
        var value = BoundaryValue(index, args.GetPosition(BoundaryCanvas).X);
        _draggedBoundary = null;
        BoundaryCanvas.ReleaseMouseCapture();
        _viewModel.SetPetBoundary(index, value);
        args.Handled = true;
    }

    private int BoundaryValue(int index, double x)
    {
        var boundary = _viewModel!.PetBoundaries.First(item => item.Index == index);
        var value = (int)Math.Round(Math.Clamp(x / BoundaryCanvas.ActualWidth, 0, 1) * 100);
        return Math.Clamp(value, boundary.Minimum, boundary.Maximum);
    }

    private double Position(int value) => BoundaryCanvas.ActualWidth * value / 100d;

    private void DrawBoundaries(int movingIndex = -1, int movingValue = 0)
    {
        BoundaryCanvas.Children.Clear();
        if (_viewModel is null || BoundaryCanvas.ActualWidth <= 0) return;
        var accent = (System.Windows.Media.Brush)FindResource("AccentBrush");
        foreach (var boundary in _viewModel.PetBoundaries)
        {
            var value = boundary.Index == movingIndex ? movingValue : boundary.Value;
            var x = Position(value);
            var line = new Border
            {
                Width = 5, Height = 28, Background = accent, CornerRadius = new(2), IsHitTestVisible = false
            };
            var label = new TextBlock
            {
                Text = $"{value}%", Foreground = accent, FontSize = 11, IsHitTestVisible = false
            };
            Canvas.SetLeft(line, x - 2.5);
            Canvas.SetTop(line, 5);
            Canvas.SetLeft(label, Math.Clamp(x - 13, 0, Math.Max(0, BoundaryCanvas.ActualWidth - 30)));
            Canvas.SetTop(label, -2);
            BoundaryCanvas.Children.Add(line);
            BoundaryCanvas.Children.Add(label);
        }
    }
}

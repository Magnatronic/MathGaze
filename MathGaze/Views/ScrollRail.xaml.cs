using MathGaze.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace MathGaze.Views;

public partial class ScrollRail : UserControl
{
    public ScrollRail()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private MainViewModel? _vm;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = e.NewValue as MainViewModel;

        if (_vm is not null)
            _vm.PropertyChanged += OnVmPropertyChanged;

        UpdateThumbPosition();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.ScrollThumbTopRatio)
                           or nameof(MainViewModel.ScrollThumbSizeRatio)
                           or nameof(MainViewModel.IsScrollable))
            UpdateThumbPosition();
    }

    internal void OnTrackSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateThumbPosition();
    }

    private void UpdateThumbPosition()
    {
        if (ScrollThumb is null || ThumbCanvas is null) return;

        double trackH = ThumbCanvas.ActualHeight;
        double trackW = ThumbCanvas.ActualWidth;
        double ratio  = _vm?.ScrollThumbTopRatio ?? 0.0;

        // Bug 2 fix: hide the thumb when there is nothing to scroll.
        // ScrollThumbTopRatio returns 0 both when at the top AND when maxScroll==0,
        // so we detect the "nothing to scroll" case by asking the VM directly.
        bool canScroll = _vm?.IsScrollable ?? false;
        ScrollThumb.Visibility = canScroll
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;

        if (!canScroll) return;

        // Proportional thumb: size = visible fraction, position = scroll position
        double sizeRatio = _vm?.ScrollThumbSizeRatio ?? 1.0;
        double thumbH    = Math.Max(20, trackH * sizeRatio);
        double maxTop    = Math.Max(0, trackH - thumbH);
        ScrollThumb.Width  = Math.Max(1, trackW);
        ScrollThumb.Height = thumbH;
        System.Windows.Controls.Canvas.SetTop(ScrollThumb, ratio * maxTop);
        System.Windows.Controls.Canvas.SetLeft(ScrollThumb, 0);
    }
}

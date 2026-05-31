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
        if (e.PropertyName is nameof(MainViewModel.ScrollThumbTopRatio))
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
        double thumbH = ScrollThumb.Height;
        double ratio  = _vm?.ScrollThumbTopRatio ?? 0.0;

        ScrollThumb.Width = Math.Max(1, trackW);

        double maxTop = Math.Max(0, trackH - thumbH);
        double top    = Math.Clamp(ratio * maxTop, 0, maxTop);

        System.Windows.Controls.Canvas.SetTop(ScrollThumb, top);
        System.Windows.Controls.Canvas.SetLeft(ScrollThumb, 0);
    }
}

using MathGaze.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MathGaze.Views;

public partial class PdfCanvas : UserControl
{
    private PdfCanvasViewModel? _vm;

    public PdfCanvas()
    {
        InitializeComponent();
        // Wire PaintSurface in code-behind to avoid XAML temp-project type resolution
        // issues with the SkiaSharp.Views.WPF compat shim on net9.0-windows.
        SkCanvas.PaintSurface += OnPaintSurface;

        // Wire mouse events for tool interaction
        SkCanvas.MouseDown += OnMouseDown;
        SkCanvas.MouseMove += OnMouseMove;

        // Attach SizeChanged to the UserControl (this), not to the inner SKElement.
        // WPF fires SizeChanged on the element whose RenderSize has changed; attaching
        // to the UserControl ensures the event fires whenever the column that hosts
        // PdfCanvas is resized (e.g. window maximize), and e.NewSize gives the definitive
        // new logical size before ActualWidth/ActualHeight are queried asynchronously.
        SizeChanged += OnCanvasSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Wire DataContextChanged so we capture the ViewModel even if DataContext arrives
        // after Loaded (e.g. binding expression resolves after initial layout).
        DataContextChanged += OnDataContextChanged;

        WireViewModel(DataContext as PdfCanvasViewModel);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        WireViewModel(e.NewValue as PdfCanvasViewModel);
    }

    private void WireViewModel(PdfCanvasViewModel? newVm)
    {
        // Unsubscribe from old ViewModel
        if (_vm is not null)
            _vm.InvalidationRequested -= OnInvalidationRequested;

        _vm = newVm;

        if (_vm is null) return;

        _vm.InvalidationRequested += OnInvalidationRequested;

        // Report canvas size to the new ViewModel if the UserControl is already laid out.
        // SizeChanged fires during layout (before Loaded) when _vm is still null, so we
        // must push the size here once the ViewModel is available.
        if (ActualWidth > 0 && ActualHeight > 0)
            ReportCanvasSize();
    }

    private void OnCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // Use e.NewSize (the definitive new logical size at event time) rather than
        // ActualWidth/ActualHeight, which could still reflect the pre-resize value if
        // queried from a different code path before layout completes.
        ReportCanvasSize(e.NewSize.Width, e.NewSize.Height);

        // Immediately invalidate the SKElement so WPF redraws it at the new bounds
        // with the existing bitmap repositioned — without waiting for the async bitmap
        // reload.  The reload fires a second InvalidateVisual when the fresh bitmap
        // arrives, giving a two-step update: reposition now, refresh pixels later.
        SkCanvas.InvalidateVisual();
    }

    private void ReportCanvasSize(double logicalWidth = -1, double logicalHeight = -1)
    {
        if (_vm is null) return;

        // Fall back to ActualWidth/ActualHeight when called without explicit dimensions
        // (e.g. from WireViewModel after DataContext arrives).
        if (logicalWidth  < 0) logicalWidth  = ActualWidth;
        if (logicalHeight < 0) logicalHeight = ActualHeight;

        // Convert WPF logical DIUs to physical pixels using the per-monitor DPI scale.
        var dpiInfo  = VisualTreeHelper.GetDpi(this);
        double scale = dpiInfo.PixelsPerDip;
        int widthPx  = (int)Math.Round(logicalWidth  * scale);
        int heightPx = (int)Math.Round(logicalHeight * scale);
        if (widthPx > 0 && heightPx > 0)
        {
            _vm.SetCanvasSize(widthPx, heightPx);
            // D-11: forward real DPI to ViewModel so CoordinateMapper uses correct scale
            _vm.SetDpiScale(dpiInfo.PixelsPerDip);
        }
    }

    private void OnInvalidationRequested(object? sender, EventArgs e)
    {
        // Must marshal to UI thread — InvalidationRequested may fire from a background thread
        Dispatcher.Invoke(() => SkCanvas.InvalidateVisual());
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        if (_vm is null)
        {
            canvas.Clear(new SKColor(0xF5, 0xF3, 0xEE));
            canvas.Flush();
            return;
        }
        _vm.Paint(canvas, e.Info.Width, e.Info.Height);
    }

    // ── Mouse event handlers — DPI-correct physical pixel conversion (Pitfall 1) ──

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_vm is null) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var logicalPos = e.GetPosition(SkCanvas);
        var dpi        = VisualTreeHelper.GetDpi(this);
        var physPx     = new SKPoint(
            (float)(logicalPos.X * dpi.PixelsPerDip),
            (float)(logicalPos.Y * dpi.PixelsPerDip));
        _vm.HandleCanvasClick(physPx);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_vm is null) return;
        var logicalPos = e.GetPosition(SkCanvas);
        var dpi        = VisualTreeHelper.GetDpi(this);
        var physPx     = new SKPoint(
            (float)(logicalPos.X * dpi.PixelsPerDip),
            (float)(logicalPos.Y * dpi.PixelsPerDip));
        _vm.HandleMouseMove(physPx);
        UpdateStatusToast(_vm.ToolVmStatusMessage);
    }

    private void UpdateStatusToast(string msg)
    {
        if (StatusToast is null || StatusText is null) return;
        if (string.IsNullOrEmpty(msg))
        {
            StatusToast.Visibility = Visibility.Collapsed;
        }
        else
        {
            StatusText.Text = msg;
            StatusToast.Visibility = Visibility.Visible;
        }
    }
}

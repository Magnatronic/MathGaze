using MathGaze.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MathGaze.Views;

public partial class PdfCanvas : UserControl
{
    private PdfCanvasViewModel? _vm;

    public PdfCanvas()
    {
        InitializeComponent();
        // Wire PaintSurface and SizeChanged in code-behind to avoid XAML temp-project
        // type resolution issues with the SkiaSharp.Views.WPF compat shim on net9.0-windows.
        SkCanvas.PaintSurface += OnPaintSurface;
        SkCanvas.SizeChanged  += OnCanvasSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Obtain PdfCanvasViewModel from DataContext.
        // MainWindow sets PdfCanvas.DataContext = PdfCanvasViewModel instance (wired in App.xaml.cs).
        _vm = DataContext as PdfCanvasViewModel;
        if (_vm is null) return;

        _vm.InvalidationRequested += OnInvalidationRequested;

        // Report initial canvas size to ViewModel if already laid out
        if (SkCanvas.ActualWidth > 0 && SkCanvas.ActualHeight > 0)
        {
            ReportCanvasSize();
        }
    }

    private void OnCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        ReportCanvasSize();
    }

    private void ReportCanvasSize()
    {
        if (_vm is null) return;
        // Convert WPF logical size to physical pixels using DPI scale
        var dpiInfo  = VisualTreeHelper.GetDpi(this);
        double scale = dpiInfo.PixelsPerDip;
        int widthPx  = (int)Math.Round(SkCanvas.ActualWidth  * scale);
        int heightPx = (int)Math.Round(SkCanvas.ActualHeight * scale);
        if (widthPx > 0 && heightPx > 0)
            _vm.SetCanvasSize(widthPx, heightPx);
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
}

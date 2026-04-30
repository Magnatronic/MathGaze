using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Windows.Controls;

namespace MathGaze.Views;

public partial class PdfCanvas : UserControl
{
    public PdfCanvas()
    {
        InitializeComponent();
        // Wire PaintSurface in code-behind to avoid XAML temp-project type resolution issues
        // with the SkiaSharp.Views.WPF compat shim on net9.0-windows.
        SkCanvas.PaintSurface += OnPaintSurface;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(new SKColor(0xF5, 0xF3, 0xEE)); // BrushBg colour — placeholder until Plan 03
        canvas.Flush();
    }
}

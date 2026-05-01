using CommunityToolkit.Mvvm.ComponentModel;
using MathGaze.Core;
using MathGaze.Services;
using SkiaSharp;
using System.ComponentModel;

namespace MathGaze.ViewModels;

/// <summary>
/// Drives the SkiaSharp canvas. Owns the current page SKBitmap and CoordinateMapper.
/// Reacts to MainViewModel changes (zoom, page) to trigger re-renders.
///
/// Canvas invalidation: raises InvalidationRequested event → PdfCanvas.xaml.cs calls
/// SkCanvas.InvalidateVisual() on the UI thread.
/// </summary>
public sealed class PdfCanvasViewModel : ObservableObject, IDisposable
{
    private readonly IPdfService _pdfService;
    private readonly MainViewModel _mainVm;

    private SKBitmap? _pageBitmap;
    private bool _disposed;

    // Canvas physical dimensions — set by PdfCanvas when the SKElement reports its size
    private int _canvasWidthPx;
    private int _canvasHeightPx;

    // CoordinateMapper is created lazily on first render
    private CoordinateMapper? _coordinateMapper;

    /// <summary>
    /// Raised when the canvas needs to repaint. PdfCanvas.xaml.cs subscribes and calls
    /// SkCanvas.InvalidateVisual() on the UI thread.
    /// </summary>
    public event EventHandler? InvalidationRequested;

    public PdfCanvasViewModel(IPdfService pdfService, MainViewModel mainViewModel)
    {
        _pdfService = pdfService;
        _mainVm     = mainViewModel;

        // Observe MainViewModel for zoom/page changes that require re-render
        _mainVm.PropertyChanged += OnMainViewModelPropertyChanged;
    }

    private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.CurrentPage)
                           or nameof(MainViewModel.ZoomFactor)
                           or nameof(MainViewModel.ScrollOffsetY))
        {
            // Fire-and-forget page re-render; errors are swallowed (bitmap stays null = grey canvas)
            _ = LoadCurrentPageAsync();
        }
    }

    /// <summary>Last-known canvas height in physical pixels. Used by MainViewModel for fit-page and scroll clamping.</summary>
    public int CanvasHeightPx => _canvasHeightPx;

    /// <summary>
    /// Called by PdfCanvas.xaml.cs when the SKElement has reported its physical pixel dimensions.
    /// If a document is already open, triggers a full page reload at the new size so the bitmap
    /// matches the canvas dimensions (covers the case where canvas size arrives after document
    /// open, or after a window resize).
    /// </summary>
    public void SetCanvasSize(int widthPx, int heightPx)
    {
        _canvasWidthPx  = widthPx;
        _canvasHeightPx = heightPx;

        if (_pdfService.IsOpen)
            _ = LoadCurrentPageAsync();
        else
            InvalidationRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called by MainViewModel.CloseFile to dispose the current page bitmap and repaint
    /// the canvas blank so the closed PDF is no longer visible.
    /// </summary>
    public void ClearCanvas()
    {
        var old = Interlocked.Exchange(ref _pageBitmap, null);
        old?.Dispose();
        _coordinateMapper = null;
        InvalidationRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called by MainViewModel.OpenFileCommand (Plan 04) after a document is successfully opened.
    /// Loads page 0 at the current canvas size.
    /// </summary>
    public async Task OnDocumentOpenedAsync()
    {
        if (!_pdfService.IsOpen) return;
        await LoadCurrentPageAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Called by the PaintSurface handler — draws the current page bitmap onto the canvas.
    /// This method runs on the render thread; do NOT call IPdfService here.
    /// </summary>
    public void Paint(SKCanvas canvas, int canvasWidthPx, int canvasHeightPx)
    {
        canvas.Clear(new SKColor(0xF5, 0xF3, 0xEE)); // BrushBg

        if (_pageBitmap is null || !_pdfService.IsOpen)
        {
            canvas.Flush();
            return;
        }

        // Keep canvas size in sync (may have changed since last LoadCurrentPageAsync)
        _canvasWidthPx  = canvasWidthPx;
        _canvasHeightPx = canvasHeightPx;

        EnsureCoordinateMapper();

        if (_coordinateMapper is null)
        {
            canvas.Flush();
            return;
        }

        var destRect = _coordinateMapper.GetPageDestRect(canvasWidthPx, canvasHeightPx);
        canvas.DrawBitmap(_pageBitmap, destRect);
        canvas.Flush();
    }

    private void EnsureCoordinateMapper()
    {
        if (_canvasWidthPx == 0 || _canvasHeightPx == 0 || !_pdfService.IsOpen) return;

        var (widthPt, heightPt) = _pdfService.GetPageDimensionsPt(_mainVm.CurrentPage - 1);

        // Compute canvasOrigin to centre the page horizontally; vertical starts at top (scroll = 0 in Phase 1)
        double scale     = (_mainVm.ZoomFactor * 96.0) / 72.0; // points to physical pixels at 96dpi baseline
        // Note: dpiScale is 1.0 here — PdfCanvasViewModel does not have access to VisualTreeHelper.
        // The real dpiScale is passed in from PdfCanvas.xaml.cs via SetCanvasSize in Phase 1.
        // For Phase 1, dpiScale=1.0 produces correct rendering at 100% DPI. High-DPI support is
        // refined in Phase 2 when CoordinateMapper is fully integrated with WPF DPI context.
        double pageWidthPx = widthPt * scale;
        double originX = Math.Max(0, (_canvasWidthPx - pageWidthPx) / 2.0);

        if (_coordinateMapper is null)
        {
            _coordinateMapper = new CoordinateMapper(
                zoomFactor:    _mainVm.ZoomFactor,
                dpiScale:      1.0,
                pageWidthPt:   widthPt,
                pageHeightPt:  heightPt,
                canvasOriginX: originX,
                canvasOriginY: -_mainVm.ScrollOffsetY);   // negative: scrolling down increases offset, moves content up
        }
        else
        {
            _coordinateMapper.Update(
                zoomFactor:    _mainVm.ZoomFactor,
                dpiScale:      1.0,
                pageWidthPt:   widthPt,
                pageHeightPt:  heightPt,
                canvasOriginX: originX,
                canvasOriginY: -_mainVm.ScrollOffsetY);   // negative: scrolling down increases offset, moves content up
        }
    }

    private async Task LoadCurrentPageAsync()
    {
        if (!_pdfService.IsOpen || _canvasWidthPx == 0 || _canvasHeightPx == 0) return;

        int pageIndex = _mainVm.CurrentPage - 1; // MainViewModel is 1-based; IPdfService is 0-based
        if (pageIndex < 0 || pageIndex >= _pdfService.PageCount) return;

        // Compute target pixel dimensions from zoom and canvas size
        var (widthPt, heightPt) = _pdfService.GetPageDimensionsPt(pageIndex);
        double scale        = (_mainVm.ZoomFactor * 96.0) / 72.0;
        int targetWidthPx   = Math.Max(1, (int)Math.Round(widthPt  * scale));
        int targetHeightPx  = Math.Max(1, (int)Math.Round(heightPt * scale));

        var newBitmap = await _pdfService.GetPageBitmapAsync(pageIndex, targetWidthPx, targetHeightPx)
                                         .ConfigureAwait(false);

        if (newBitmap is null) return;

        // Dispose old bitmap and swap in new one
        var old = Interlocked.Exchange(ref _pageBitmap, newBitmap);
        old?.Dispose();

        // Request canvas repaint on the UI thread
        InvalidationRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _mainVm.PropertyChanged -= OnMainViewModelPropertyChanged;
        _pageBitmap?.Dispose();
        _pageBitmap = null;
    }
}

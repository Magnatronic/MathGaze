using CommunityToolkit.Mvvm.ComponentModel;
using MathGaze.Core;
using MathGaze.Core.Geometry;
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
    private readonly IPdfService             _pdfService;
    private readonly MainViewModel           _mainVm;
    private readonly IGeometryService        _geometryService;
    private readonly ToolViewModel           _toolVm;
    private readonly SnapEngine              _snapEngine;
    private readonly GeometryLayerViewModel  _geometryLayer;

    private SKBitmap? _pageBitmap;
    private bool _disposed;

    // Canvas physical dimensions — set by PdfCanvas when the SKElement reports its size
    private int _canvasWidthPx;
    private int _canvasHeightPx;

    // D-11: real DPI from VisualTreeHelper, not hardcoded 1.0
    private double _dpiScale = 1.0;

    // CoordinateMapper is created lazily on first render
    private CoordinateMapper? _coordinateMapper;

    /// <summary>
    /// Raised when the canvas needs to repaint. PdfCanvas.xaml.cs subscribes and calls
    /// SkCanvas.InvalidateVisual() on the UI thread.
    /// </summary>
    public event EventHandler? InvalidationRequested;

    public PdfCanvasViewModel(
        IPdfService pdfService,
        MainViewModel mainViewModel,
        IGeometryService geometryService,
        ToolViewModel toolViewModel,
        SnapEngine snapEngine,
        GeometryLayerViewModel geometryLayer)
    {
        _pdfService      = pdfService;
        _mainVm          = mainViewModel;
        _geometryService = geometryService;
        _toolVm          = toolViewModel;
        _snapEngine      = snapEngine;
        _geometryLayer   = geometryLayer;

        // Observe MainViewModel for zoom/page changes that require re-render
        _mainVm.PropertyChanged += OnMainViewModelPropertyChanged;

        // Subscribe to ghost changes so canvas repaints on every MouseMove.
        // Named methods (not lambdas) so they can be unsubscribed in Dispose().
        _toolVm.GhostChanged += OnGhostChanged;

        // Subscribe to geometry changes (objects placed/deleted/nudged)
        _geometryService.ObjectsChanged += OnObjectsChanged;
    }

    private void OnGhostChanged(object? sender, EventArgs e)
        => InvalidationRequested?.Invoke(this, EventArgs.Empty);

    private void OnObjectsChanged(object? sender, EventArgs e)
        => InvalidationRequested?.Invoke(this, EventArgs.Empty);

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

    /// <summary>StatusMessage from the active tool — exposed so PdfCanvas.xaml.cs can update the WPF status toast.</summary>
    public string ToolVmStatusMessage => _toolVm.StatusMessage;

    /// <summary>
    /// D-11: wire real DPI from VisualTreeHelper.GetDpi(this).PixelsPerDip.
    /// Called by PdfCanvas.xaml.cs from ReportCanvasSize() each time canvas size is reported.
    /// </summary>
    public void SetDpiScale(double pixelsPerDip)
    {
        _dpiScale = pixelsPerDip;
    }

    /// <summary>
    /// Forward a canvas click (in physical pixels) to ToolViewModel.
    /// Called by PdfCanvas.xaml.cs OnMouseDown handler.
    /// </summary>
    public void HandleCanvasClick(SKPoint physPx)
    {
        EnsureCoordinateMapper();
        if (_coordinateMapper is null) return;
        _toolVm.HandleCanvasClick(physPx, _coordinateMapper, _snapEngine);
    }

    /// <summary>
    /// Forward a mouse move (in physical pixels) to ToolViewModel.
    /// Called by PdfCanvas.xaml.cs OnMouseMove handler.
    /// </summary>
    public void HandleMouseMove(SKPoint physPx)
    {
        EnsureCoordinateMapper();
        if (_coordinateMapper is null) return;
        _toolVm.HandleMouseMove(physPx, _coordinateMapper, _snapEngine);
    }

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
        {
            // Let MainViewModel re-apply fit-page zoom if that mode is active.
            // This must run before LoadCurrentPageAsync so the zoom update triggers
            // its own reload; our unconditional reload below is then redundant but
            // harmless — it serves as the fallback for non-fit-page zoom.
            _mainVm.OnCanvasSizeChanged();
            _ = LoadCurrentPageAsync();
        }
        else
        {
            InvalidationRequested?.Invoke(this, EventArgs.Empty);
        }
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
    /// Called by the PaintSurface handler — draws the current page bitmap onto the canvas,
    /// then draws the ghost preview overlay.
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

        // Do NOT overwrite _canvasWidthPx/_canvasHeightPx from the Skia surface info
        // here.  SetCanvasSize() is the authoritative source (driven by the UserControl's
        // SizeChanged event using e.NewSize * PixelsPerDip).  Overwriting from
        // e.Info.Width/Height would revert the correct new dimensions to potentially
        // stale surface dimensions if SKElement has not yet recreated its internal
        // WriteableBitmap at the new size when this Paint call arrives.

        EnsureCoordinateMapper();

        if (_coordinateMapper is null)
        {
            canvas.Flush();
            return;
        }

        var destRect = _coordinateMapper.GetPageDestRect(canvasWidthPx, canvasHeightPx);
        canvas.DrawBitmap(_pageBitmap, destRect);

        // Draw committed geometry objects (vector layer above PDF bitmap, below ghost preview)
        _geometryLayer.Draw(canvas, _coordinateMapper);

        // Draw ghost preview (dashed line/circle between click 1 and click 2) — D-01/D-02
        DrawGhostPreview(canvas);

        canvas.Flush();
    }

    private void DrawGhostPreview(SKCanvas canvas)
    {
        if (_coordinateMapper is null) return;
        if (_toolVm.DrawState != DrawState.AnchorPlaced) return;
        if (_toolVm.AnchorPt is null) return;

        var anchorPx    = _coordinateMapper.PageToScreen(_toolVm.AnchorPt.Value.xPt, _toolVm.AnchorPt.Value.yPt);
        var ghostCursor = _toolVm.GhostCursorPx;

        // Ghost dashed line/arc paint (D-01)
        using var ghostPaint = new SKPaint
        {
            Style       = SKPaintStyle.Stroke,
            Color       = new SKColor(0x3B, 0x6F, 0xD4, 180), // BrushAccent + 70% alpha
            StrokeWidth = 2f,
            IsAntialias = true,
            PathEffect  = SKPathEffect.CreateDash(new float[] { 8f, 5f }, 0f),
        };
        // Anchor dot paint
        using var anchorPaint = new SKPaint
        {
            Style       = SKPaintStyle.Fill,
            Color       = new SKColor(0x3B, 0x6F, 0xD4, 255),
            IsAntialias = true,
        };
        // Anchor ring paint
        using var ringPaint = new SKPaint
        {
            Style       = SKPaintStyle.Stroke,
            Color       = new SKColor(0x3B, 0x6F, 0xD4, 200),
            StrokeWidth = 2f,
            IsAntialias = true,
        };

        // Draw anchor dot + ring (D-01)
        canvas.DrawCircle(anchorPx, 5f, anchorPaint);
        canvas.DrawCircle(anchorPx, 12f, ringPaint);

        if (_toolVm.ActiveTool == ToolMode.Line)
        {
            // Dashed ghost line from anchor to cursor
            canvas.DrawLine(anchorPx, ghostCursor, ghostPaint);
        }
        else if (_toolVm.ActiveTool == ToolMode.Circle)
        {
            // Ghost circle whose radius = distance from center to cursor (D-02)
            float ghostRadius = SKPoint.Distance(anchorPx, ghostCursor);
            if (ghostRadius > 2f)
                canvas.DrawCircle(anchorPx, ghostRadius, ghostPaint);
        }

        // Draw snap ring indicator if a snap candidate is active
        if (_toolVm.LastSnap?.Label is not null)
        {
            using var snapRingPaint = new SKPaint
            {
                Style       = SKPaintStyle.Stroke,
                Color       = new SKColor(0x3B, 0x6F, 0xD4, 200),
                StrokeWidth = 2f,
                IsAntialias = true,
                PathEffect  = SKPathEffect.CreateDash(new float[] { 3f, 3f }, 0f),
            };
            using var snapDotPaint = new SKPaint
            {
                Style       = SKPaintStyle.Fill,
                Color       = new SKColor(0x3B, 0x6F, 0xD4, 255),
                IsAntialias = true,
            };
            var snapPos = _toolVm.LastSnap.Value.Position;
            canvas.DrawCircle(snapPos, 18f, snapRingPaint);
            canvas.DrawCircle(snapPos, 5f,  snapDotPaint);
        }
    }

    private void EnsureCoordinateMapper()
    {
        if (_canvasWidthPx == 0 || _canvasHeightPx == 0 || !_pdfService.IsOpen) return;

        var (widthPt, heightPt) = _pdfService.GetPageDimensionsPt(_mainVm.CurrentPage - 1);

        // Compute canvasOrigin to centre the page horizontally; vertical starts at top (scroll = 0 in Phase 1)
        double scale      = (_dpiScale * 96.0 / 72.0) * _mainVm.ZoomFactor;
        double pageWidthPx = widthPt * scale;
        double originX    = Math.Max(0, (_canvasWidthPx - pageWidthPx) / 2.0);

        if (_coordinateMapper is null)
        {
            _coordinateMapper = new CoordinateMapper(
                zoomFactor:    _mainVm.ZoomFactor,
                dpiScale:      _dpiScale,   // D-11 fix: was hardcoded 1.0
                pageWidthPt:   widthPt,
                pageHeightPt:  heightPt,
                canvasOriginX: originX,
                canvasOriginY: -_mainVm.ScrollOffsetY);   // negative: scrolling down increases offset, moves content up
        }
        else
        {
            _coordinateMapper.Update(
                zoomFactor:    _mainVm.ZoomFactor,
                dpiScale:      _dpiScale,   // D-11 fix: was hardcoded 1.0
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
        // Match EnsureCoordinateMapper scale: include _dpiScale so bitmap px-dimensions align
        // with the physical-pixel coordinate space (GAP-1/GAP-2 fix).
        double scale        = (_dpiScale * _mainVm.ZoomFactor * 96.0 / 72.0);
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
        // Unsubscribe named event handlers to prevent memory leaks
        _geometryService.ObjectsChanged -= OnObjectsChanged;
        _toolVm.GhostChanged -= OnGhostChanged;
        _geometryLayer.Dispose();
        _pageBitmap?.Dispose();
        _pageBitmap = null;
    }
}

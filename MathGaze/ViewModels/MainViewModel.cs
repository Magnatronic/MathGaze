using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathGaze.Services;
using System.IO;
using System.Linq;

namespace MathGaze.ViewModels;

/// <summary>
/// Top-level ViewModel: file state, mode, zoom, page navigation, scroll.
/// All commands are RelayCommand/AsyncRelayCommand — safe to bind directly in XAML.
///
/// PdfCanvasViewModel is wired via SetPdfCanvasViewModel() after both singletons are
/// resolved by DI (avoids a circular constructor dependency).
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IPdfService        _pdfService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IGeometryService   _geometryService;
    private readonly ISessionService    _sessionService;
    private readonly IExportService     _exportService;
    private readonly ToolViewModel      _toolVm;
    private readonly SettingsViewModel  _settingsVm;
    private PdfCanvasViewModel?         _pdfCanvasVm;

    // Tracks the file path of the currently open PDF so page-nav save (D-14) and
    // CloseFile can reference it without re-scanning the dialog result.
    private string? _currentPdfPath;

    // Tracks the page number of the page we are LEAVING during navigation, so
    // TrySaveAsync can record the correct page index before CurrentPage is updated.
    private int _lastSavedPage = 1;

    // In-memory per-page object cache (Bug 2 fix).
    // Key = 1-based page number. Value = snapshot of geometry objects on that page.
    // Populated in OnCurrentPageChanged when leaving a page; restored when returning.
    // Cleared on CloseFile and OpenFileAsync so it never bleeds across documents.
    private readonly Dictionary<int, List<Core.Geometry.GeometryObject>> _pageObjectCache = new();

    public MainViewModel(
        IPdfService pdfService,
        IFileDialogService fileDialogService,
        IGeometryService geometryService,
        ISessionService sessionService,
        IExportService exportService,
        ToolViewModel toolViewModel,
        SettingsViewModel settingsViewModel)
    {
        _pdfService        = pdfService;
        _fileDialogService = fileDialogService;
        _geometryService   = geometryService;
        _sessionService    = sessionService;
        _exportService     = exportService;
        _toolVm            = toolViewModel;
        _settingsVm        = settingsViewModel;

        _toolVm.PropertyChanged += OnToolPropertyChanged;
    }

    private void OnToolPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ToolViewModel.ActiveTool) && _settingsVm.IsSettingsPanelOpen)
            _settingsVm.IsSettingsPanelOpen = false;
    }

    /// <summary>Exposed for TopBar and MainWindow DataContext binding.</summary>
    public SettingsViewModel SettingsVm => _settingsVm;

    /// <summary>
    /// Called from App.xaml.cs after both singletons are resolved to break the circular
    /// constructor dependency (MainViewModel ↔ PdfCanvasViewModel).
    /// </summary>
    public void SetPdfCanvasViewModel(PdfCanvasViewModel pdfCanvasViewModel)
    {
        _pdfCanvasVm = pdfCanvasViewModel;
    }

    // ── File state ──────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportPdfCommand))]
    private bool _isPdfOpen;

    [ObservableProperty]
    private string _fileName = string.Empty;

    // ── Zoom ────────────────────────────────────────────────────────────────────
    private static readonly double[] ZoomSteps =
        { 0.25, 0.50, 0.75, 1.00, 1.25, 1.50, 1.75, 2.00, 2.25, 2.50, 2.75, 3.00, 3.25, 3.50, 3.75, 4.00 };

    [ObservableProperty]
    private double _zoomFactor = 1.0;

    // True while the user is in "fit-page" mode (zoom was set via FitPage, not manually).
    // Re-applying fit-page on resize keeps the whole page visible after maximize/restore.
    private bool _isFitPageMode;

    public string ZoomLabel => $"{(int)Math.Round(ZoomFactor * 100)}%";

    partial void OnZoomFactorChanged(double value) => OnPropertyChanged(nameof(ZoomLabel));

    [RelayCommand]
    private void ZoomIn()
    {
        _isFitPageMode = false;         // user has taken manual control of zoom
        var next = ZoomSteps.FirstOrDefault(z => z > ZoomFactor + 0.001);
        if (next <= 0) return;

        // Cap at the zoom where page width exactly fills the canvas width (no H-overflow)
        if (IsPdfOpen && _pdfCanvasVm is not null)
        {
            var (widthPt, _) = _pdfService.GetPageDimensionsPt(CurrentPage - 1);
            double dpiScale = _pdfCanvasVm.DpiScale;
            int canvasW = _pdfCanvasVm.CanvasWidthPx;
            if (canvasW > 0 && widthPt > 0)
            {
                double maxZoom = canvasW / (widthPt * dpiScale * 96.0 / 72.0);
                next = Math.Min(next, maxZoom);
            }
        }

        if (next > ZoomFactor + 0.001)
            ZoomFactor = next;
    }

    [RelayCommand]
    private void ZoomOut()
    {
        _isFitPageMode = false;         // user has taken manual control of zoom
        var prev = ZoomSteps.LastOrDefault(z => z < ZoomFactor - 0.001);
        if (prev > 0) ZoomFactor = prev;
    }

    [RelayCommand]
    private void FitPage()
    {
        ApplyFitPage();
        _isFitPageMode = true;          // entering fit-page mode
    }

    /// <summary>
    /// Called by PdfCanvasViewModel.SetCanvasSize when the canvas is resized.
    /// Re-runs the fit-page calculation if the user is in fit-page mode so the
    /// page continues to fill the viewport after a window resize or maximize.
    /// </summary>
    public void OnCanvasSizeChanged()
    {
        if (_isFitPageMode && IsPdfOpen)
            ApplyFitPage();
    }

    private void ApplyFitPage()
    {
        // Set zoom so the full page height fits in the canvas viewport.
        if (!IsPdfOpen) return;

        var (_, heightPt) = _pdfService.GetPageDimensionsPt(CurrentPage - 1);
        if (heightPt <= 0) return;

        int canvasHeightPx = _pdfCanvasVm?.CanvasHeightPx ?? 0;
        if (canvasHeightPx <= 0)
        {
            ZoomFactor = 1.0;
            return;
        }

        double dpiScale = _pdfCanvasVm?.DpiScale ?? 1.0;
        // Physical canvas px = heightPt * (96/72) * dpiScale * zoom
        // Solve for zoom: zoom = canvasHeightPx / (heightPt * (96/72) * dpiScale)
        double zoom = canvasHeightPx / (heightPt * (96.0 / 72.0) * dpiScale);
        // Clamp to valid range
        zoom = Math.Clamp(zoom, ZoomSteps[0], ZoomSteps[^1]);
        ZoomFactor = zoom;
    }

    // ── Page navigation ─────────────────────────────────────────────────────────
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 0;

    public string PageLabel => TotalPages > 0 ? $"{CurrentPage} / {TotalPages}" : "— / —";

    partial void OnCurrentPageChanged(int value)
    {
        // Capture departing page objects into SessionService's all-pages store BEFORE Reset().
        // This is what makes cross-page sidecar persistence work: SyncPage records what was
        // on this page so TrySaveAsync (triggered by Reset's ObjectsChanged) can write all pages.
        if (_lastSavedPage > 0)
            _sessionService.SyncPage(_lastSavedPage, _geometryService.Objects.ToList());

        // Bug 2 fix: cache the objects from the page we are LEAVING so we can restore
        // them when the user navigates back within the same session.
        // Only cache when a document is open (IsPdfOpen) and _lastSavedPage is valid (> 0).
        // ToList() snapshots the current references — same live instances stored in cache.
        if (IsPdfOpen && _lastSavedPage > 0)
            _pageObjectCache[_lastSavedPage] = _geometryService.Objects.ToList();

        _lastSavedPage = value;

        // GAP-13 fix: each page has an independent geometry canvas.
        // Reset clears all objects and undo/redo stacks when the user navigates pages.
        // Also called in OpenFileAsync (GAP-10) — two calls on PDF open is harmless (Reset is idempotent).
        _geometryService.Reset();

        // Bug 2 fix: restore objects for the page we are ARRIVING at, if cached.
        // AddObject does not raise ObjectsChanged (safe per Pitfall 3 / SessionService pattern).
        // A single ForceRaise after all objects are loaded triggers one repaint.
        if (_pageObjectCache.TryGetValue(value, out var cachedObjects))
        {
            foreach (var obj in cachedObjects)
                _geometryService.AddObject(obj);
            _geometryService.ObjectsChanged_ForceRaise();
        }

        OnPropertyChanged(nameof(PageLabel));
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(PageLabel));
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void PreviousPage()
    {
        if (CurrentPage > 1) CurrentPage--;
    }
    private bool CanGoToPreviousPage() => IsPdfOpen && CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void NextPage()
    {
        if (CurrentPage < TotalPages) CurrentPage++;
    }
    private bool CanGoToNextPage() => IsPdfOpen && CurrentPage < TotalPages;

    partial void OnIsPdfOpenChanged(bool value)
    {
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    // ── Scroll ──────────────────────────────────────────────────────────────────
    // ScrollOffsetY is the logical Y offset in physical pixels (positive = scrolled down).
    // PdfCanvasViewModel reads this via MainViewModel.ScrollOffsetY to offset canvasOriginY.
    [ObservableProperty]
    private double _scrollOffsetY = 0;

    partial void OnScrollOffsetYChanged(double value) => OnPropertyChanged(nameof(ScrollThumbTopRatio));

    private const double SmallScrollPx = 120.0;

    [RelayCommand]
    private void ScrollUp()
    {
        ScrollOffsetY = Math.Max(0, ScrollOffsetY - SmallScrollPx);
    }

    [RelayCommand]
    private void ScrollDown()
    {
        ScrollOffsetY += SmallScrollPx;
        ClampScrollOffset();
    }

    [RelayCommand]
    private void ScrollPageUp()
    {
        double pageScroll = (_pdfCanvasVm?.CanvasHeightPx ?? 0) * 0.85;
        if (pageScroll <= 0) pageScroll = SmallScrollPx * 5;
        ScrollOffsetY = Math.Max(0, ScrollOffsetY - pageScroll);
    }

    [RelayCommand]
    private void ScrollPageDown()
    {
        double pageScroll = (_pdfCanvasVm?.CanvasHeightPx ?? 0) * 0.85;
        if (pageScroll <= 0) pageScroll = SmallScrollPx * 5;
        ScrollOffsetY += pageScroll;
        ClampScrollOffset();
    }

    private void ClampScrollOffset()
    {
        if (!IsPdfOpen) return;
        var (_, heightPt) = _pdfService.GetPageDimensionsPt(CurrentPage - 1);
        double dpiScale = _pdfCanvasVm?.DpiScale ?? 1.0;
        double pageHeightPx = heightPt * (ZoomFactor * dpiScale * 96.0 / 72.0);
        double canvasH = _pdfCanvasVm?.CanvasHeightPx ?? 0;
        double maxScroll = Math.Max(0, pageHeightPx - canvasH);
        ScrollOffsetY = Math.Min(ScrollOffsetY, maxScroll);
        OnPropertyChanged(nameof(ScrollThumbTopRatio));
    }

    /// <summary>
    /// The scroll position as a ratio 0–1 of (ScrollOffsetY / MaxScrollY).
    /// Used by ScrollRail to position the thumb indicator.
    /// Returns 0 when there is nothing to scroll or no document is open.
    /// </summary>
    public double ScrollThumbTopRatio
    {
        get
        {
            if (!IsPdfOpen) return 0;
            var (_, heightPt) = _pdfService.GetPageDimensionsPt(CurrentPage - 1);
            double dpiScale = _pdfCanvasVm?.DpiScale ?? 1.0;
            double pageHeightPx = heightPt * (ZoomFactor * dpiScale * 96.0 / 72.0);
            double canvasH = _pdfCanvasVm?.CanvasHeightPx ?? 0;
            double maxScroll = Math.Max(0, pageHeightPx - canvasH);
            if (maxScroll <= 0) return 0;
            return Math.Clamp(ScrollOffsetY / maxScroll, 0.0, 1.0);
        }
    }

    // ── File commands ───────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task OpenFileAsync()
    {
        // ShowOpenPdfDialog MUST run on the UI thread (WPF dialog requirement)
        string? filePath = _fileDialogService.ShowOpenPdfDialog();
        if (filePath is null) return;

        bool success = await _pdfService.OpenDocumentAsync(filePath).ConfigureAwait(false);
        if (!success) return;

        // Update ViewModel state on UI thread
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            FileName      = Path.GetFileName(filePath);
            TotalPages    = _pdfService.PageCount;
            CurrentPage   = 1;
            IsPdfOpen     = true;
            ScrollOffsetY = 0;
            _pageObjectCache.Clear();  // Bug 2 fix: discard any cached pages from previous document
            _geometryService.Reset();

            // D-13: Register new PDF path AFTER Reset so SessionService knows where to save.
            // Setting _currentPdfPath here (on UI thread) ensures OnCurrentPageChanged (which
            // fires synchronously when CurrentPage = 1 above) uses the correct path.
            _sessionService.SetPdfPath(filePath);
            _currentPdfPath = filePath;
            _lastSavedPage  = 1;
        });

        // D-13: Silently restore from sidecar if it exists (no prompt; corrupt = open clean).
        // TotalPages is now known so we can safely clamp restored.CurrentPage.
        var restored = await _sessionService.TryLoadAsync(filePath).ConfigureAwait(false);
        if (restored is not null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                int savedPage = Math.Max(1, Math.Min(restored.CurrentPage, TotalPages));

                // Restore all pages: populate the page cache AND tell SessionService about every
                // page so future saves include objects from pages not yet visited this session.
                foreach (var (page, objects) in restored.Pages)
                {
                    _pageObjectCache[page] = objects;
                    _sessionService.SyncPage(page, objects);
                }

                // Load the current page's objects into GeometryService now.
                // OnCurrentPageChanged (fired by CurrentPage = savedPage below) will:
                //   1. Re-cache them via _pageObjectCache[savedPage] = ...
                //   2. Reset() geometry
                //   3. Restore from _pageObjectCache[savedPage] — completing the round-trip.
                if (restored.Pages.TryGetValue(savedPage, out var currentPageObjs))
                {
                    foreach (var obj in currentPageObjs)
                        _geometryService.AddObject(obj);
                    _geometryService.ObjectsChanged_ForceRaise();
                }

                _lastSavedPage = savedPage;
                CurrentPage    = savedPage;
            });
        }

        // Trigger initial render
        if (_pdfCanvasVm is not null)
            await _pdfCanvasVm.OnDocumentOpenedAsync().ConfigureAwait(false);

        // Set fit-page zoom on first open (discretion: fit-page is sensible default)
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(FitPage);
    }

    [RelayCommand(CanExecute = nameof(CanCloseFile))]
    private void CloseFile()
    {
        // D-13: Clear PDF path so SessionService stops saving after close
        _sessionService.SetPdfPath(null);
        _currentPdfPath = null;
        _lastSavedPage  = 1;
        _pageObjectCache.Clear();  // Bug 2 fix: discard cached pages for the closed document

        _pdfService.CloseDocument();
        FileName       = string.Empty;
        TotalPages     = 0;
        CurrentPage    = 1;
        IsPdfOpen      = false;
        ZoomFactor     = 1.0;
        ScrollOffsetY  = 0;
        _isFitPageMode = false;
        // Clear the canvas so the last rendered page is not shown after close
        _pdfCanvasVm?.ClearCanvas();
    }
    private bool CanCloseFile() => IsPdfOpen;

    // ── Export command ──────────────────────────────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanExportPdf))]
    private async Task ExportPdfAsync()
    {
        if (_currentPdfPath is null) return;
        // Flush current page's in-memory objects into session store before export
        _sessionService.SyncPage(CurrentPage, _geometryService.Objects.ToList());
        string outputPath = Services.PdfExportService.BuildAnnotatedPath(_currentPdfPath);
        bool ok = await _exportService.ExportAsync(_currentPdfPath, outputPath).ConfigureAwait(false);
        // Show toast immediately (not on next MouseMove) via ToastRequested event
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _toolVm.StatusMessage = ok
                ? $"Saved: {Path.GetFileName(outputPath)}"
                : "Export failed — check folder permissions";
            _pdfCanvasVm?.RequestToastUpdate();
        });
    }
    private bool CanExportPdf() => IsPdfOpen;
}

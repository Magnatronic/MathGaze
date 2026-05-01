using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathGaze.Services;
using System.IO;

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
    private PdfCanvasViewModel?         _pdfCanvasVm;

    public MainViewModel(
        IPdfService pdfService,
        IFileDialogService fileDialogService)
    {
        _pdfService        = pdfService;
        _fileDialogService = fileDialogService;
    }

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
    private bool _isPdfOpen;

    [ObservableProperty]
    private string _fileName = string.Empty;

    // ── Mode ────────────────────────────────────────────────────────────────────
    [ObservableProperty]
    private bool _isPracticeMode = true;

    [RelayCommand]
    private void ToggleMode() => IsPracticeMode = !IsPracticeMode;

    // ── Zoom ────────────────────────────────────────────────────────────────────
    private static readonly double[] ZoomSteps =
        { 0.25, 0.50, 0.75, 1.00, 1.25, 1.50, 1.75, 2.00, 2.25, 2.50, 2.75, 3.00, 3.25, 3.50, 3.75, 4.00 };

    [ObservableProperty]
    private double _zoomFactor = 1.0;

    public string ZoomLabel => $"{(int)Math.Round(ZoomFactor * 100)}%";

    partial void OnZoomFactorChanged(double value) => OnPropertyChanged(nameof(ZoomLabel));

    [RelayCommand]
    private void ZoomIn()
    {
        // Find the next step above current zoom
        var next = ZoomSteps.FirstOrDefault(z => z > ZoomFactor + 0.001);
        if (next > 0) ZoomFactor = next;
    }

    [RelayCommand]
    private void ZoomOut()
    {
        // Find the next step below current zoom
        var prev = ZoomSteps.LastOrDefault(z => z < ZoomFactor - 0.001);
        if (prev > 0) ZoomFactor = prev;
    }

    [RelayCommand]
    private void FitPage()
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

        // Compute zoom that makes page height == canvas height (at 96 DPI baseline)
        double zoom = canvasHeightPx / (heightPt * 96.0 / 72.0);
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
        double pageHeightPx = heightPt * (ZoomFactor * 96.0 / 72.0);
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
            double pageHeightPx = heightPt * (ZoomFactor * 96.0 / 72.0);
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
        });

        // Trigger initial render
        if (_pdfCanvasVm is not null)
            await _pdfCanvasVm.OnDocumentOpenedAsync().ConfigureAwait(false);

        // Set fit-page zoom on first open (discretion: fit-page is sensible default)
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(FitPage);
    }

    [RelayCommand(CanExecute = nameof(CanCloseFile))]
    private void CloseFile()
    {
        _pdfService.CloseDocument();
        FileName      = string.Empty;
        TotalPages    = 0;
        CurrentPage   = 1;
        IsPdfOpen     = false;
        ZoomFactor    = 1.0;
        ScrollOffsetY = 0;
        // Clear the canvas so the last rendered page is not shown after close
        _pdfCanvasVm?.ClearCanvas();
    }
    private bool CanCloseFile() => IsPdfOpen;
}

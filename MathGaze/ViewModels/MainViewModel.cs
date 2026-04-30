using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MathGaze.ViewModels;

/// <summary>
/// Top-level ViewModel: file state, mode toggle, zoom, page navigation.
/// Commands for PDF open/close, zoom, page nav, and scroll are wired in Plan 04
/// after PDF service is available. This plan provides the observable state skeleton.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // ── File state ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private bool   _isPdfOpen = false;

    // ── Mode ────────────────────────────────────────────────────────────────────
    /// <summary>True = Practice Mode (angle readouts visible). False = Exam Mode (hidden).</summary>
    [ObservableProperty] private bool _isPracticeMode = true;

    // ── Zoom ────────────────────────────────────────────────────────────────────
    /// <summary>1.0 = 100%. Range: 0.25 to 4.0. Updated by zoom commands in Plan 04.</summary>
    [ObservableProperty] private double _zoomFactor = 1.0;

    /// <summary>Display string for TopBar zoom indicator, e.g. "100%".</summary>
    public string ZoomLabel => $"{(int)Math.Round(ZoomFactor * 100)}%";

    partial void OnZoomFactorChanged(double value) => OnPropertyChanged(nameof(ZoomLabel));

    // ── Page navigation ─────────────────────────────────────────────────────────
    /// <summary>1-based current page index.</summary>
    [ObservableProperty] private int _currentPage = 1;

    [ObservableProperty] private int _totalPages = 0;

    /// <summary>Display string for TopBar page counter, e.g. "7 / 22".</summary>
    public string PageLabel => TotalPages > 0 ? $"{CurrentPage} / {TotalPages}" : "— / —";

    partial void OnCurrentPageChanged(int value) => OnPropertyChanged(nameof(PageLabel));
    partial void OnTotalPagesChanged(int value)  => OnPropertyChanged(nameof(PageLabel));

    // ── Stub commands (Plan 04 wires real implementations) ──────────────────────
    [RelayCommand] private void ToggleMode()       => IsPracticeMode = !IsPracticeMode;
    [RelayCommand] private void OpenFile()         { /* wired in Plan 04 */ }
    [RelayCommand] private void CloseFile()        { /* wired in Plan 04 */ }
    [RelayCommand] private void ZoomIn()           { /* wired in Plan 04 */ }
    [RelayCommand] private void ZoomOut()          { /* wired in Plan 04 */ }
    [RelayCommand] private void FitPage()          { /* wired in Plan 04 */ }
    [RelayCommand] private void PreviousPage()     { /* wired in Plan 04 */ }
    [RelayCommand] private void NextPage()         { /* wired in Plan 04 */ }
    [RelayCommand] private void ScrollUp()         { /* wired in Plan 04 */ }
    [RelayCommand] private void ScrollDown()       { /* wired in Plan 04 */ }
    [RelayCommand] private void ScrollPageUp()     { /* wired in Plan 04 */ }
    [RelayCommand] private void ScrollPageDown()   { /* wired in Plan 04 */ }
}

namespace MathGaze.Services;

/// <summary>
/// Manages JSON sidecar save and restore for the current PDF session.
/// SYS-02: auto-save after every geometry change.
/// SYS-03: restore on PDF open.
/// D-10: sidecar at {pdfPath}.mathgaze.json.
/// </summary>
public interface ISessionService
{
    /// <summary>Set the active PDF path so saves know the sidecar location. Call on PDF open and on PDF close (null).</summary>
    void SetPdfPath(string? pdfPath);

    /// <summary>
    /// Save all geometry objects + current page number to the sidecar.
    /// Silently swallows IOException/UnauthorizedAccessException (school machine read-only dir).
    /// Optional pageOverride: pass the page number you want recorded (used when navigating away
    /// from a page — CurrentPage has already moved to the new page but we want to record the old one).
    /// </summary>
    Task TrySaveAsync(string pdfPath, int? pageOverride = null);

    /// <summary>
    /// Load sidecar for pdfPath. Returns null if sidecar missing or corrupt (open clean).
    /// Caller is responsible for calling AddObject + ObjectsChanged_ForceRaise.
    /// </summary>
    Task<SidecarModel?> TryLoadAsync(string pdfPath);
}

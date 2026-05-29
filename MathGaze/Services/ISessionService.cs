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
    /// Record the objects for a specific page into the in-memory all-pages store.
    /// Call before navigating away from a page (before GeometryService.Reset()) so the
    /// departing page's objects are captured before geometry is cleared.
    /// </summary>
    void SyncPage(int pageNumber, IList<Core.Geometry.GeometryObject> objects);

    /// <summary>
    /// Save all pages + current page number to the sidecar.
    /// Silently swallows IOException/UnauthorizedAccessException (school machine read-only dir).
    /// </summary>
    Task TrySaveAsync(string pdfPath);

    /// <summary>
    /// Load sidecar for pdfPath. Returns null if sidecar missing or corrupt (open clean).
    /// Caller is responsible for restoring objects and populating _pageObjectCache.
    /// </summary>
    Task<SidecarModel?> TryLoadAsync(string pdfPath);

    /// <summary>
    /// Returns a snapshot of all pages' geometry objects, keyed by 1-based page number.
    /// Used by PdfExportService to draw annotations across all pages.
    /// </summary>
    IReadOnlyDictionary<int, IReadOnlyList<Core.Geometry.GeometryObject>> GetAllPages();
}

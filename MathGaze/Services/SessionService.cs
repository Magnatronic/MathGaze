using MathGaze.Core.Geometry;
using System.IO;
using System.Text.Json;

namespace MathGaze.Services;

/// <summary>
/// Sidecar data model. Serialized to {pdfPath}.mathgaze.json.
/// CurrentPage: the page the student was on (D-09).
/// Objects: all geometry objects; polymorphic via [JsonDerivedType] on GeometryObject (Plan 01).
/// IsSelected is NOT persisted (transient UI state — per RESEARCH.md Anti-Patterns).
/// </summary>
public sealed class SidecarModel
{
    public int CurrentPage { get; set; }
    /// <summary>All pages keyed by 1-based page number. Replaces the old flat Objects list.</summary>
    public Dictionary<int, List<GeometryObject>> Pages { get; set; } = new();
}

/// <summary>
/// Subscribes to IGeometryService.ObjectsChanged and writes the sidecar on every change (D-12).
/// Named method (OnObjectsChanged) enables clean unsubscription in Dispose() — Phase 2 pattern.
///
/// Save-on-restore loop prevention (RESEARCH.md Pitfall 3):
/// AddObject does NOT raise ObjectsChanged — so restoring objects via AddObject is safe.
/// ObjectsChanged_ForceRaise() is called once after all objects are loaded.
///
/// DI circular dependency resolution:
/// SessionService needs CurrentPage from MainViewModel, but cannot take MainViewModel directly
/// in constructor (forward DI dependency). A Func&lt;int&gt; lambda is injected instead; the lambda
/// captures MainViewModel via lazy resolution in App.xaml.cs (resolved after all singletons built).
/// </summary>
public sealed class SessionService : ISessionService, IDisposable
{
    private readonly IGeometryService _geometryService;
    private readonly Func<int> _getCurrentPage;
    private string? _pdfPath;
    private bool _disposed;

    // Full per-page store keyed by 1-based page number.
    // SyncPage() populates it from MainViewModel before each Reset().
    // OnObjectsChanged() updates the active page's entry on every geometry change.
    private readonly Dictionary<int, List<GeometryObject>> _allPages = new();

    // Compact JSON — no WriteIndented (sidecar files are small, indentation wastes bytes)
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

    public SessionService(IGeometryService geometryService, Func<int> getCurrentPage)
    {
        _geometryService = geometryService;
        _getCurrentPage  = getCurrentPage;
        // Named method for clean unsubscription (Phase 2 pattern: OnGhostChanged, OnObjectsChanged)
        _geometryService.ObjectsChanged += OnObjectsChanged;
    }

    public void SetPdfPath(string? pdfPath)
    {
        _pdfPath = pdfPath;
        if (pdfPath is null) _allPages.Clear();
    }

    public void SyncPage(int pageNumber, IList<GeometryObject> objects)
        => _allPages[pageNumber] = objects.ToList();

    // ── Save ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fires on every IGeometryService.ObjectsChanged. Uses fire-and-forget async void
    /// (acceptable here: save failure is logged+swallowed; no caller awaits this).
    /// </summary>
    private async void OnObjectsChanged(object? sender, EventArgs e)
    {
        if (_pdfPath is null) return;
        // Update the active page's entry before saving so the sidecar always reflects current state.
        _allPages[_getCurrentPage()] = _geometryService.Objects.ToList();
        await TrySaveAsync(_pdfPath).ConfigureAwait(false);
    }

    public async Task TrySaveAsync(string pdfPath)
    {
        string sidecarPath = pdfPath + ".mathgaze.json";
        try
        {
            var model = new SidecarModel
            {
                CurrentPage = _getCurrentPage(),
                Pages       = new Dictionary<int, List<GeometryObject>>(_allPages),
            };

            string json = JsonSerializer.Serialize(model, _jsonOptions);
            await File.WriteAllTextAsync(sidecarPath, json).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // School machines may have read-only PDF directories — swallow silently.
        }
    }

    // ── Load ────────────────────────────────────────────────────────────────────

    public async Task<SidecarModel?> TryLoadAsync(string pdfPath)
    {
        string sidecarPath = pdfPath + ".mathgaze.json";
        if (!File.Exists(sidecarPath)) return null;
        try
        {
            string json = await File.ReadAllTextAsync(sidecarPath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SidecarModel>(json, _jsonOptions);
        }
        catch
        {
            // Corrupt or unreadable sidecar (D-13: open clean, no prompt, no crash)
            return null;
        }
    }

    // ── Export ──────────────────────────────────────────────────────────────────

    public IReadOnlyDictionary<int, IReadOnlyList<GeometryObject>> GetAllPages()
        => _allPages.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<GeometryObject>)kvp.Value.AsReadOnly());

    // ── Dispose ─────────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _geometryService.ObjectsChanged -= OnObjectsChanged;  // named method — clean unsubscription
    }
}

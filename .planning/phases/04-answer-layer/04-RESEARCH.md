# Phase 4: Answer Layer - Research

**Researched:** 2026-05-26
**Domain:** WPF text placement via clipboard, System.Text.Json polymorphic serialization, SkiaSharp text rendering and hit-testing, JSON sidecar I/O
**Confidence:** HIGH — all primary claims verified against official docs or codebase inspection

## Summary

Phase 4 delivers three tightly scoped features on top of the established Phase 2/3 foundation: (1) a clipboard-paste text placement tool, (2) JSON sidecar auto-save and session restore, and (3) wiring the existing Text stub button in ToolRail. The MCQ answer-selection requirements (ANS-01/02/03) are fully deferred to v2 by user decision.

The most complex technical problem is polymorphic JSON serialization of the `GeometryObject` hierarchy. System.Text.Json has supported this cleanly since .NET 7 using `[JsonDerivedType]` attributes on the abstract base class — no custom converter needed. The serializer emits a `"$type"` discriminator property and reconstructs the correct concrete type on deserialization. `GeometryObject` must be annotated with one `[JsonDerivedType(...)]` entry per concrete subclass.

The clipboard text flow is straightforward in WPF because `HandleCanvasClick` is already called from code-behind on the UI (STA) thread. `System.Windows.Clipboard.GetText()` can be called synchronously within the existing click handler without any additional Dispatcher marshalling. Session save and restore plug into the existing `IGeometryService.ObjectsChanged` event and the `MainViewModel.OpenFileAsync` flow respectively.

**Primary recommendation:** Place the `[JsonDerivedType]` annotations on `GeometryObject`, introduce a thin `ISessionService` / `SessionService` singleton, subscribe it to `ObjectsChanged`, and wire restore into `OpenFileAsync` after `_geometryService.Reset()`.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Clipboard-paste-on-placement. Student composes text in Grid 3, copies it, activates Text tool, clicks canvas → the clipboard content becomes a `TextObject` at that PDF-space position. No in-app text editing state. No WPF TextBox needed.
- **D-02:** If clipboard is empty or contains non-text when the student clicks, show a brief status toast: "Copy text first, then click to place." No TextObject is created. Same toast pattern as the protractor parallel-lines error.
- **D-03:** Text is rendered by SkiaSharp alongside other geometry objects. Once placed, text is immutable — to change it, student deletes the TextObject and re-places with corrected clipboard content.
- **D-04:** `TextObject` stores: `ContentText` (string from clipboard), `XPt` / `YPt` (PDF-space coordinates). No lock state for text boxes.
- **D-05:** Placing a text box = one undo entry. Each nudge press = one undo entry. Deleting = one undo entry. Full undo/redo participation consistent with D-08 (Phase 2).
- **D-06:** When a TextObject is selected, the right rail shows the standard Nudge block (1/5/20px UDLR) + Delete button. No text-specific right-rail controls needed.
- **D-07:** `TextObject` extends `GeometryObject` and is stored in `IGeometryService.Objects` alongside Point/Line/Circle/Protractor. Placed via `PlaceObjectCommand`.
- **D-08:** ANS-01/02/03 are deferred to v2. No `AnswerObject` or `AnswerMode` in Phase 4.
- **D-09:** The JSON sidecar saves: all geometry objects (Point, Line, Circle, Protractor, TextObject) and the current page number. Zoom level and scroll position are NOT saved.
- **D-10:** Sidecar filename: `{pdf-filename}.mathgaze.json` in the same directory as the PDF.
- **D-11:** Use `System.Text.Json` (inbox, no NuGet dependency). Each geometry object type serializes its discriminated type name plus all fields.
- **D-12:** Auto-save triggers on every `IGeometryService.ObjectsChanged` event. No debounce.
- **D-13:** On PDF open: check for sidecar at `{pdf-path}.mathgaze.json`. If found, silently deserialize and load all geometry objects + navigate to the saved page number. If sidecar is missing or corrupt, open clean.
- **D-14:** Page navigation also triggers a sidecar save.
- **D-15:** PDF export is v2 scope.

### Claude's Discretion
- TextObject rendering: font family (T.mono from design tokens), font size, text colour (T.ink or accent), background/border treatment while selected
- SkiaSharp text rendering API: use `SKFont`-based `DrawText` overload (not deprecated `SKPaint.TextSize` API — per Phase 3 pattern)
- Hit-test tolerance for TextObject (recommend 8px around the rendered text bounding box)
- Exact JSON schema structure for sidecar (polymorphic type discriminator strategy for System.Text.Json)
- Error handling for corrupt/unreadable sidecar (log and open clean)

### Deferred Ideas (OUT OF SCOPE)
- ANS-01: MCQ click-to-select — deferred to v2
- ANS-02: MCQ toggle selection — deferred to v2
- ANS-03: MCQ lock answer — deferred to v2
- EXAM-V2-02: PDF export — deferred to v2
- PROT-04: Protractor lock toggle — carried forward, still v2
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| TEXT-01 | User can place a text box at a clicked canvas location; Grid 3 can type into it via standard Windows text input | Clipboard-paste model (D-01): `System.Windows.Clipboard.GetText()` on STA UI thread; TextObject via PlaceObjectCommand |
| TEXT-02 | A selected text box responds to nudge controls for repositioning | No new work: `NudgeObjectCommand` already handles any `GeometryObject` subclass via `GeometryService.NudgeObject()` — add `TextObject` case to that switch |
| SYS-02 | Work is auto-saved to a JSON sidecar file after every change | Subscribe `SessionService` to `IGeometryService.ObjectsChanged`; serialize to `{pdfPath}.mathgaze.json` with `File.WriteAllTextAsync` |
| SYS-03 | User can resume a previous session by opening the same PDF | After `_geometryService.Reset()` in `OpenFileAsync`, check for sidecar, deserialize polymorphically, call `AddObject` for each, restore `CurrentPage` |
</phase_requirements>

## Standard Stack

### Core (all inbox — no new NuGet dependencies)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.Text.Json | Inbox (.NET 9) | JSON sidecar serialization / deserialization | D-11: no NuGet needed; supports polymorphic hierarchies via [JsonDerivedType] since .NET 7 [VERIFIED: learn.microsoft.com/dotnet/standard/serialization/system-text-json/polymorphism] |
| System.Windows.Clipboard | Inbox (WPF) | Read clipboard text on canvas click | WPF STA-thread clipboard API; no additional package [VERIFIED: MS Docs] |
| System.IO.File | Inbox (.NET 9) | WriteAllTextAsync / ReadAllTextAsync for sidecar I/O | Standard async file I/O [VERIFIED: learn.microsoft.com/dotnet/api/system.io.file] |
| SkiaSharp 3.119.2 | Already installed | Text rendering (DrawText + SKFont) | Phase 3 established SKFont-based API; same pattern used for protractor labels [VERIFIED: codebase] |

**No new NuGet packages are required for Phase 4.**

## Architecture Patterns

### Recommended Project Structure additions
```
MathGaze/
├── Core/
│   └── Geometry/
│       └── TextObject.cs          # NEW: extends GeometryObject
├── Services/
│   ├── ISessionService.cs         # NEW: Save/Load interface
│   └── SessionService.cs          # NEW: IGeometryService.ObjectsChanged subscriber + file I/O
└── ViewModels/
    └── ToolViewModel.cs           # MODIFY: add ToolMode.Text, ActivateText command, Text click handler
```

### Pattern 1: System.Text.Json Polymorphic Serialization

**What:** `[JsonDerivedType]` attributes on `GeometryObject` plus a `"$type"` string discriminator. The serializer writes `"$type":"point"` (etc.) into every object's JSON and uses it to reconstruct the correct concrete type on deserialization.

**When to use:** Any JSON round-trip of a `GeometryObject` or `IReadOnlyList<GeometryObject>`.

**Critical requirement:** When serializing a collection, the declared element type must be `GeometryObject`, not a concrete type. `IReadOnlyList<GeometryObject>` qualifies automatically.

```csharp
// Source: learn.microsoft.com/dotnet/standard/serialization/system-text-json/polymorphism
// Apply to the abstract base class — NOT to concrete subclasses:
[JsonDerivedType(typeof(PointObject),      typeDiscriminator: "point")]
[JsonDerivedType(typeof(LineObject),       typeDiscriminator: "line")]
[JsonDerivedType(typeof(CircleObject),     typeDiscriminator: "circle")]
[JsonDerivedType(typeof(ProtractorObject), typeDiscriminator: "protractor")]
[JsonDerivedType(typeof(TextObject),       typeDiscriminator: "text")]
public abstract class GeometryObject
{
    public Guid Id { get; } = Guid.NewGuid();
    // ...
}
```

**Serialized output example:**
```json
[
  { "$type": "line", "X1Pt": 100.0, "Y1Pt": 200.0, "X2Pt": 300.0, "Y2Pt": 200.0, "Id": "..." },
  { "$type": "text", "ContentText": "5 cm", "XPt": 120.0, "YPt": 210.0, "Id": "..." }
]
```

**Deserialization:**
```csharp
// Deserializes as GeometryObject base type; runtime types are correct concrete types
List<GeometryObject>? objects = JsonSerializer.Deserialize<List<GeometryObject>>(json);
```

### Pattern 2: Sidecar Save/Load — SessionService

**What:** `SessionService` is a singleton that subscribes to `IGeometryService.ObjectsChanged` and writes the sidecar asynchronously on every change. It also exposes `TrySaveAsync` and `TryLoadAsync` for explicit calls.

**Sidecar schema:**
```csharp
public sealed class SidecarModel
{
    public int CurrentPage { get; set; }
    public List<GeometryObject> Objects { get; set; } = new();
}
```

**Save (on every ObjectsChanged):**
```csharp
// SessionService.OnObjectsChanged handler
private async void OnObjectsChanged(object? sender, EventArgs e)
{
    if (_pdfPath is null) return;
    string sidecarPath = _pdfPath + ".mathgaze.json";
    try
    {
        var model = new SidecarModel
        {
            CurrentPage = _mainVm.CurrentPage,
            Objects = _geometryService.Objects.ToList()
        };
        string json = JsonSerializer.Serialize(model, _jsonOptions);
        await File.WriteAllTextAsync(sidecarPath, json);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        // Log and swallow — read-only directory should not crash the app
    }
}
```

**Load (on PDF open, after Reset):**
```csharp
// Called from MainViewModel.OpenFileAsync after _geometryService.Reset()
public async Task<bool> TryLoadAsync(string pdfPath)
{
    string sidecarPath = pdfPath + ".mathgaze.json";
    if (!File.Exists(sidecarPath)) return false;
    try
    {
        string json = await File.ReadAllTextAsync(sidecarPath);
        var model = JsonSerializer.Deserialize<SidecarModel>(json, _jsonOptions);
        if (model is null) return false;
        foreach (var obj in model.Objects)
            _geometryService.AddObject(obj);   // bypasses undo stack — correct for restore
        // Navigate to saved page without triggering Reset
        return true; // caller sets CurrentPage = model.CurrentPage
    }
    catch
    {
        return false; // corrupt sidecar: open clean
    }
}
```

### Pattern 3: Clipboard Text Read in ToolViewModel

**What:** `System.Windows.Clipboard.GetText()` is called synchronously inside the existing `HandleCanvasClick` on the `(ToolMode.Text, DrawState.Idle)` case. No threading ceremony needed.

**Why safe:** `HandleCanvasClick` is called from `PdfCanvas.xaml.cs` `MouseDown` event handler, which runs on the WPF UI thread (an STA thread). Clipboard access on the STA UI thread is always safe.

```csharp
// In ToolViewModel.HandleCanvasClick, new switch case:
case (ToolMode.Text, DrawState.Idle):
{
    // Clipboard must be accessed on STA (UI) thread — HandleCanvasClick is always called from UI thread
    bool hasText = System.Windows.Clipboard.ContainsText();
    if (!hasText)
    {
        StatusMessage = "Copy text first, then click to place";
        break;
    }
    string text = System.Windows.Clipboard.GetText();
    if (string.IsNullOrWhiteSpace(text))
    {
        StatusMessage = "Copy text first, then click to place";
        break;
    }
    var (xPt, yPt) = mapper.ScreenToPage(screenPx);
    _geometryService.ExecuteCommand(new PlaceObjectCommand(new TextObject(text, xPt, yPt)));
    StatusMessage = "Text placed";
    break;
}
```

### Pattern 4: SkiaSharp Text Rendering and Hit-Test Bounds

**What:** `SKFont.MeasureText(string, out SKRect bounds)` returns the advance width and populates `bounds` with the tight ink bounding rect relative to the draw-point origin. This bounds rect is used for both rendering the selection highlight and hit-testing.

**Key coordinate note:** SkiaSharp `DrawText` places the baseline at the given Y coordinate. `bounds.Top` is negative (ascenders above baseline), `bounds.Bottom` is typically near zero or small positive (descenders). For hit-testing, translate the bounds rect to the draw-point position and expand by a tolerance.

```csharp
// Source: learn.microsoft.com/dotnet/api/skiasharp.skfont.measuretext (version skiasharp-3.119)
// In GeometryLayerViewModel, for TextObject rendering:

private readonly SKFont _textFont  = new(SKTypeface.Default, 14f);  // T.mono equivalent
private readonly SKPaint _textPaint = new()
{
    Style       = SKPaintStyle.Fill,
    Color       = new SKColor(0x1A, 0x1A, 0x2E, 220),  // T.ink
    IsAntialias = true,
};
private readonly SKPaint _textSelectedPaint = new()
{
    Style       = SKPaintStyle.Stroke,
    Color       = new SKColor(0x3B, 0x6F, 0xD4, 255),  // BrushAccent cobalt
    StrokeWidth = 1.5f,
    IsAntialias = true,
};

// Draw call (inside DrawObject switch case):
case TextObject text:
    var drawPx = mapper.PageToScreen(text.XPt, text.YPt);
    canvas.DrawText(text.ContentText, drawPx.X, drawPx.Y,
        SKTextAlign.Left, _textFont, selected ? _textAccentPaint : _textPaint);
    if (selected)
    {
        // Draw selection bounding rect
        float advance = _textFont.MeasureText(text.ContentText, out SKRect bounds);
        var selRect = new SKRect(
            drawPx.X + bounds.Left - 4f, drawPx.Y + bounds.Top - 4f,
            drawPx.X + bounds.Left + advance + 4f, drawPx.Y + bounds.Bottom + 4f);
        canvas.DrawRect(selRect, _textSelectedPaint);
    }
    break;
```

**Hit-test method in `TextObject.HitTest`:**
```csharp
// TextObject.HitTest — uses SKFont.MeasureText to derive the screen bounding rect
public override bool HitTest(SKPoint screenPx, CoordinateMapper mapper, float tolerancePx)
{
    // SKFont cannot be stored on the model; pass font size as a constant
    // HitTest is called with tolerancePx=8f from GeometryHitTester
    var drawPx = mapper.PageToScreen(XPt, YPt);
    using var font = new SKFont(SKTypeface.Default, 14f);
    float advance = font.MeasureText(ContentText, out SKRect bounds);
    var hitRect = new SKRect(
        drawPx.X + bounds.Left  - tolerancePx,
        drawPx.Y + bounds.Top   - tolerancePx,
        drawPx.X + bounds.Left  + advance + tolerancePx,
        drawPx.Y + bounds.Bottom + tolerancePx);
    return hitRect.Contains(screenPx);
}
```

**Important pitfall:** `SKFont` is a managed wrapper around a native object. Allocating it per hit-test call (as above) is acceptable because `HitTest` is not called per-frame — only on canvas clicks. However, if hot-path rendering needs the font, use the cached field on `GeometryLayerViewModel`.

### Pattern 5: GeometryService.NudgeObject — TextObject extension

The existing `NudgeObject` switch in `GeometryService.cs` needs a `TextObject` case:

```csharp
// In GeometryService.NudgeObject, add to existing switch:
case TextObject t:
    t.XPt += dxPt;
    t.YPt += dyPt;
    break;
```

### Anti-Patterns to Avoid

- **Do NOT call `Clipboard.GetText()` from a background thread or `AsyncRelayCommand` body.** The clipboard requires STA access. The existing synchronous click handler is already correct — no async needed.
- **Do NOT subscribe `SessionService` to `ObjectsChanged` using a `lambda` you cannot unsubscribe.** Use a named method so `Dispose()` can unsubscribe cleanly (established Phase 2 pattern: `OnGhostChanged`, `OnObjectsChanged`).
- **Do NOT call `File.WriteAllTextAsync` from the `ObjectsChanged` handler without a `try/catch`.** School machines may have read-only PDF directories (exam papers on a USB). Swallow `IOException` / `UnauthorizedAccessException` silently after logging.
- **Do NOT use `GeometryService.AddObject` from `SessionService` during restore and then call `ExecuteCommand`.** Use `AddObject` directly for restore (bypasses undo stack — correct, as you do not want "undo" to erase restored state).
- **Do NOT save `IsSelected` state to the sidecar.** Selection is transient UI state, not persisted. Objects restore in unselected state.
- **Do NOT annotate `GeometryObject` with `[JsonInclude]` on `Id { get; }`.** System.Text.Json in .NET 9 can serialize init-only and get-only properties from constructors but `Id` needs special handling — either mark it `{ get; init; }` or use a constructor parameter named `id`. The current `Guid Id { get; } = Guid.NewGuid()` is get-only with no setter, which System.Text.Json will NOT serialize by default. Solution: change to `{ get; init; }` and add `[JsonConstructor]` or handle via `JsonSerializerOptions.IncludeFields`.

### Pattern 6: Sidecar Save–Restore Feedback Loop Prevention

**Risk:** If `AddObject` in `SessionService.TryLoadAsync` triggers `ObjectsChanged`, and `ObjectsChanged` triggers `OnObjectsChanged` (the save handler), then every restore object write triggers a save mid-restore. This wastes I/O and risks writing a partial state.

**Prevention strategy:** `GeometryService.AddObject` does NOT raise `ObjectsChanged` (this is the existing established pattern — see `GeometryService.cs` line 24: "Do NOT raise ObjectsChanged here — ExecuteCommand raises it after the full command"). Therefore, restoring objects via `AddObject` + single final `ObjectsChanged_ForceRaise()` after all objects are loaded is both safe and consistent with the existing architecture.

**Correct restore sequence:**
```csharp
foreach (var obj in model.Objects)
    _geometryService.AddObject(obj);   // does NOT fire ObjectsChanged
// Single explicit repaint after all objects loaded:
_geometryService.ObjectsChanged_ForceRaise();
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Polymorphic JSON type dispatch | Custom switch + WriteRawValue per type | `[JsonDerivedType]` attributes on `GeometryObject` | System.Text.Json handles the `$type` property automatically in both directions; handles Guid, double, string, bool, enum correctly without custom code [VERIFIED: MS Docs] |
| Text measurement for hit-testing | Approximate character width × count | `SKFont.MeasureText(text, out SKRect bounds)` | Returns true ink bounds including kerning, diacritics, multi-byte chars; correct for all clipboard content including numbers, Greek letters, and math symbols [VERIFIED: skiasharp-3.119 docs] |
| Clipboard thread safety | Dispatcher.BeginInvoke wrappers | Call `Clipboard.GetText()` synchronously in the existing STA click handler | Already on UI thread; no ceremony needed [VERIFIED: MS Docs] |
| Session restore deserialization via reflection | Manual type dispatch + Activator.CreateInstance | `JsonSerializer.Deserialize<SidecarModel>(json, _jsonOptions)` with `[JsonDerivedType]` | System.Text.Json reconstructs concrete types from `$type` discriminator [VERIFIED: MS Docs] |

**Key insight:** The System.Text.Json polymorphic attribute approach eliminates the need for a custom `JsonConverter<GeometryObject>` — the most common hand-rolled mistake in this domain.

## Common Pitfalls

### Pitfall 1: GeometryObject.Id is get-only — not serializable by default
**What goes wrong:** `Guid Id { get; } = Guid.NewGuid()` has no setter and no `init`. System.Text.Json skips get-only auto-properties during serialization in .NET 9. After a round-trip, every object gets a new `Guid.NewGuid()` on deserialization, breaking the protractor's `Line1Id`/`Line2Id` references.
**Why it happens:** System.Text.Json only serializes public properties with a getter AND setter (or `init`-only setter). The current `Id` property has neither.
**How to avoid:** Change `GeometryObject.Id` from `public Guid Id { get; } = Guid.NewGuid()` to `public Guid Id { get; init; } = Guid.NewGuid()`. This preserves the immutable-after-construction semantics while making it serializable.
**Warning signs:** Restored `ProtractorObject` objects show `0°` readout or no readout (Line1Id/Line2Id reference objects that no longer exist because their Ids changed on deserialization).

### Pitfall 2: ProtractorObject.Line1Id / Line2Id are GUIDs stored in the sidecar
**What goes wrong:** If `Line1Id` and `Line2Id` are `Guid.Empty` after restore (because the LineObject Ids were not serialized), the angle readout in `GeometryLayerViewModel.ComputeMeasuredAngle` returns 0°.
**Why it happens:** Same root cause as Pitfall 1 — depends on Id serialization being correct.
**How to avoid:** Fix Pitfall 1 first. Also ensure `Line1Id` and `Line2Id` on `ProtractorObject` are serializable — they are declared `public Guid Line1Id { get; init; }` which is already init-only and will serialize correctly once the base `Id` is also init-only.
**Warning signs:** Protractors placed before save restore with no angle readout in Practice Mode.

### Pitfall 3: Save-on-restore feedback loop
**What goes wrong:** If `SessionService.OnObjectsChanged` fires during restore, it writes a partial sidecar (containing only the objects added so far) before all objects are loaded.
**Why it happens:** Hooking the wrong API — calling `ExecuteCommand` for restore (which raises `ObjectsChanged`) rather than `AddObject` (which does not).
**How to avoid:** Use `_geometryService.AddObject(obj)` for each restored object (does not raise `ObjectsChanged`), then call `_geometryService.ObjectsChanged_ForceRaise()` once after all objects are added.
**Warning signs:** After reopen, some objects are missing; or the sidecar file gets written many times in quick succession during the open sequence.

### Pitfall 4: Clipboard access on non-STA thread
**What goes wrong:** `COMException: CLIPBRD_E_CANT_OPEN (0x800401D0)` at runtime if clipboard is accessed from a `Task.Run` body or MTA `AsyncRelayCommand`.
**Why it happens:** Win32 clipboard API is apartment-threaded; only STA threads can call it.
**How to avoid:** In `ToolViewModel.HandleCanvasClick` the call is already on the UI thread (STA). Do NOT move clipboard access into an `async` method or `Task.Run`.
**Warning signs:** Unhandled `COMException` thrown during Text tool placement; only reproducible in Release builds or when timing is different.

### Pitfall 5: SKFont allocation per-HitTest
**What goes wrong:** `TryHitObject` is called on every canvas click. If `TextObject.HitTest` allocates a new `SKFont` and does not dispose it, GC pressure accumulates on school hardware.
**Why it happens:** SKFont wraps a native Skia font object.
**How to avoid:** Use a `using var font = new SKFont(...)` in `HitTest` (ensures disposal). The allocation is one per click, not per frame, so it is acceptable. Alternatively, store a static readonly `SKFont` in `TextObject` — but that leaks across app lifetime without a Dispose call. `using var` in HitTest is the safer choice.
**Warning signs:** Memory profiler shows SKFont handle count growing proportional to click count.

### Pitfall 6: Read-only sidecar directory (school machines)
**What goes wrong:** `UnauthorizedAccessException` or `IOException` from `File.WriteAllTextAsync` when the PDF is loaded from a read-only location (e.g., a network drive or locked exam directory).
**Why it happens:** School machines frequently have restricted directories; the PDF may be on a read-only share.
**How to avoid:** Wrap the save in `try/catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)`. Log the failure (debug builds) and silently no-op. On next successful save location, the sidecar will be written.
**Warning signs:** App crashes on save; user reports "can't save" on school machine.

### Pitfall 7: `$type` discriminator position in JSON
**What goes wrong:** `JsonSerializerOptions.AllowOutOfOrderMetadataProperties` is `false` by default. If the sidecar JSON is hand-edited and the `$type` field is not first in the object, deserialization throws.
**Why it happens:** Default System.Text.Json behavior requires `$type` to be the first property.
**How to avoid:** Never hand-edit sidecars. The serializer always writes `$type` first. If future versions need to tolerate external editors, set `AllowOutOfOrderMetadataProperties = true` on the `JsonSerializerOptions` instance.
**Warning signs:** `JsonException: Expected $type to be first property` when deserializing.

## Code Examples

### TextObject model
```csharp
// MathGaze/Core/Geometry/TextObject.cs
using MathGaze.Core;
using SkiaSharp;

namespace MathGaze.Core.Geometry;

public sealed class TextObject : GeometryObject
{
    public string ContentText { get; init; } = string.Empty;
    public double XPt { get; set; }
    public double YPt { get; set; }

    // Required for deserialization via [JsonConstructor] or parameterless constructor
    public TextObject() { }

    public TextObject(string contentText, double xPt, double yPt)
    {
        ContentText = contentText;
        XPt = xPt;
        YPt = yPt;
    }

    public override void Draw(SKCanvas canvas, CoordinateMapper mapper, SKPaint paint)
        => throw new NotImplementedException("Draw implemented in GeometryLayerViewModel");

    public override bool HitTest(SKPoint screenPx, CoordinateMapper mapper, float tolerancePx)
    {
        var drawPx = mapper.PageToScreen(XPt, YPt);
        using var font = new SKFont(SKTypeface.Default, 14f);
        float advance = font.MeasureText(ContentText, out SKRect bounds);
        var hitRect = new SKRect(
            drawPx.X + bounds.Left  - tolerancePx,
            drawPx.Y + bounds.Top   - tolerancePx,
            drawPx.X + bounds.Left  + advance + tolerancePx,
            drawPx.Y + bounds.Bottom + tolerancePx);
        return hitRect.Contains(screenPx);
    }

    public override IEnumerable<(SKPoint ScreenPx, string Label)> GetSnapPoints(CoordinateMapper mapper)
        => Enumerable.Empty<(SKPoint, string)>();
}
```

### GeometryObject Id change (required for serialization)
```csharp
// In GeometryObject.cs — change from:
public Guid Id { get; } = Guid.NewGuid();
// To:
public Guid Id { get; init; } = Guid.NewGuid();
```

### GeometryObject JsonDerivedType annotations
```csharp
// Add at top of GeometryObject class declaration:
using System.Text.Json.Serialization;

[JsonDerivedType(typeof(PointObject),      typeDiscriminator: "point")]
[JsonDerivedType(typeof(LineObject),       typeDiscriminator: "line")]
[JsonDerivedType(typeof(CircleObject),     typeDiscriminator: "circle")]
[JsonDerivedType(typeof(ProtractorObject), typeDiscriminator: "protractor")]
[JsonDerivedType(typeof(TextObject),       typeDiscriminator: "text")]
public abstract class GeometryObject { ... }
```

### SidecarModel and SessionService skeleton
```csharp
// MathGaze/Services/SessionService.cs
public sealed class SidecarModel
{
    public int CurrentPage { get; set; }
    public List<GeometryObject> Objects { get; set; } = new();
}

public sealed class SessionService : ISessionService, IDisposable
{
    private readonly IGeometryService _geometryService;
    private readonly MainViewModel    _mainVm;
    private string? _pdfPath;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

    public SessionService(IGeometryService geometryService, MainViewModel mainVm)
    {
        _geometryService = geometryService;
        _mainVm          = mainVm;
        _geometryService.ObjectsChanged += OnObjectsChanged;
    }

    public void SetPdfPath(string? pdfPath) => _pdfPath = pdfPath;

    private async void OnObjectsChanged(object? sender, EventArgs e)
    {
        if (_pdfPath is null) return;
        await TrySaveAsync(_pdfPath);
    }

    public async Task TrySaveAsync(string pdfPath)
    {
        string sidecarPath = pdfPath + ".mathgaze.json";
        try
        {
            var model = new SidecarModel
            {
                CurrentPage = _mainVm.CurrentPage,
                Objects = _geometryService.Objects.ToList()
            };
            string json = JsonSerializer.Serialize(model, _jsonOptions);
            await File.WriteAllTextAsync(sidecarPath, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Silent fail — read-only directory on school machine
        }
    }

    public async Task<SidecarModel?> TryLoadAsync(string pdfPath)
    {
        string sidecarPath = pdfPath + ".mathgaze.json";
        if (!File.Exists(sidecarPath)) return null;
        try
        {
            string json = await File.ReadAllTextAsync(sidecarPath);
            return JsonSerializer.Deserialize<SidecarModel>(json, _jsonOptions);
        }
        catch { return null; }
    }

    public void Dispose()
        => _geometryService.ObjectsChanged -= OnObjectsChanged;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `SKPaint.TextSize` + `MeasureText` on `SKPaint` | `SKFont` + `SKFont.MeasureText` | SkiaSharp 2.88 → 3.x deprecation; enforced in Phase 3 | Use `SKFont` API only — `SKPaint.TextSize` is CS0618 warning |
| Custom `JsonConverter<GeometryObject>` | `[JsonDerivedType]` attribute on base class | .NET 7 | No custom converter needed; 10× less boilerplate |
| `get;` only Guid for Id | `get; init;` Guid for Id | Phase 4 (required change) | Enables round-trip serialization |
| `Newtonsoft.Json` with `TypeNameHandling.Auto` | `System.Text.Json` with `[JsonDerivedType]` | .NET 6+ | No NuGet, ~3× faster, no `$type: "AssemblyQualifiedName"` leaking |

## Runtime State Inventory

> Runtime state audit for Phase 4 (not a rename/refactor phase, but sidecar files are new runtime state)

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | No existing sidecar files — Phase 4 creates the format for the first time | Define schema; no migration needed |
| Live service config | None — no external services | None |
| OS-registered state | None | None |
| Secrets/env vars | None | None |
| Build artifacts | None affected | None |

**Nothing found in any category requiring migration.** Phase 4 introduces the sidecar format; no existing files need updating.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `GeometryObject.Id { get; }` is not serialized by System.Text.Json (get-only property with no init setter) | Pitfall 1, Code Examples | If wrong (i.e. STJ serializes it anyway), `init` change is still harmless but the fix is unnecessary |
| A2 | `HandleCanvasClick` is always called from the UI (STA) thread via WPF MouseDown handler | Pattern 3 | If MathGaze ever moves to background thread event dispatch, clipboard calls would fail; verified by reading `PdfCanvas.xaml.cs` pattern from Phase 1/2 |
| A3 | `ProtractorStyle` enum serializes correctly with `System.Text.Json` without a custom converter | Pattern 1 | System.Text.Json serializes enums as integers by default; restore produces correct enum values; string-name serialization requires `JsonStringEnumConverter` — use default (int) unless readability of sidecar is a requirement |

**A3 note:** If human-readable sidecar JSON is desired, add `JsonStringEnumConverter` to `_jsonOptions`. Otherwise integer enum serialization is fine.

## Open Questions

1. **ProtractorStyle enum — int vs string in sidecar**
   - What we know: System.Text.Json serializes enums as integers by default; `ProtractorStyle.Classic180 = 0`, `Full360 = 1`
   - What's unclear: Whether the team wants the sidecar to be human-readable (strings: `"Classic180"`) or compact (integers: `0`)
   - Recommendation: Default to integer; add `new JsonStringEnumConverter()` to `_jsonOptions.Converters` if human-readable sidecar is a later requirement

2. **SKTypeface for T.mono font**
   - What we know: CONTEXT.md discretion area specifies T.mono for text rendering; `SKTypeface.Default` is the system default (not monospace)
   - What's unclear: Whether `SKTypeface.FromFamilyName("Consolas")` is available on all school machines (Consolas ships with Windows but may not render the same on all hardware)
   - Recommendation: Use `SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default` with a null fallback; planner can choose

3. **Page navigation save trigger (D-14)**
   - What we know: D-14 says page navigation triggers a save. `MainViewModel.OnCurrentPageChanged` currently calls `_geometryService.Reset()`. `Reset()` raises `ObjectsChanged`. If `SessionService` saves on `ObjectsChanged`, the D-14 requirement is automatically satisfied by Reset.
   - What's unclear: Should the save also write the new page number BEFORE Reset clears objects? Currently Reset fires ObjectsChanged with zero objects + old page; then CurrentPage changes to new page. This would save `{newPage=1, objects=[]}` — which is wrong.
   - Recommendation: Subscribe `SessionService` to `MainViewModel.PropertyChanged` for `CurrentPage` changes in addition to `ObjectsChanged`, and trigger save from `CurrentPage` change *before* Reset is called. Or: reorder `MainViewModel.OnCurrentPageChanged` to save first, then Reset. Planner should choose.

## Environment Availability

Step 2.6: SKIPPED — Phase 4 has no new external dependencies. All required capabilities (WPF, SkiaSharp, System.Text.Json, System.IO) are already available and verified in previous phases.

## Security Domain

> `security_enforcement` not explicitly set to `false` in config.json (absent = treated as enabled).

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No — single-user, offline, no login | N/A |
| V3 Session Management | No — no network session | N/A |
| V4 Access Control | No — single-user app | N/A |
| V5 Input Validation | Yes — clipboard text accepted as-is | Validate non-null, non-empty; truncate at reasonable length (e.g., 500 chars) to prevent absurdly large TextObject labels; no injection risk (text rendered by SkiaSharp, not executed) |
| V6 Cryptography | No — sidecar is local, unencrypted JSON; no sensitive data | N/A |
| V7 Error Handling | Yes — corrupt sidecar must not crash | Wrap all sidecar I/O in try/catch; log in debug; open clean |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Malicious sidecar JSON (hand-edited, e.g., embedded type escalation) | Tampering | `[JsonDerivedType]` explicitly whitelists concrete types; unknown `$type` values throw `JsonException` which is caught by TryLoadAsync's catch-all — open clean |
| Clipboard content injection (student pastes executable path or script) | Spoofing | Not a risk — clipboard text is rendered as a visual label by SkiaSharp, never executed or written to a command shell |
| Extremely long clipboard string (DoS via large TextObject) | DoS | Truncate `ContentText` at 500 characters in `TextObject` constructor |

## Sources

### Primary (HIGH confidence)
- [System.Text.Json polymorphism](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism) — JsonDerivedType attribute syntax, discriminator configuration, .NET 7+ behaviour confirmed
- [SKFont.MeasureText (skiasharp-3.119)](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skfont.measuretext?view=skiasharp-3.119) — MeasureText(string, out SKRect, SKPaint) signature and bounds semantics verified
- [File.WriteAllTextAsync (.NET 9)](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.writealltextasync?view=net-9.0) — exception types confirmed (IOException, UnauthorizedAccessException)
- MathGaze codebase (direct read): GeometryService.cs, GeometryObject.cs, ToolViewModel.cs, MainViewModel.cs, App.xaml.cs, GeometryLayerViewModel.cs, GeometryHitTester.cs, RightRailViewModel.cs — all integration points verified

### Secondary (MEDIUM confidence)
- [WPF Clipboard STA requirement](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.clipboard.gettext) — STA thread requirement confirmed; UI thread is STA in WPF apps
- [Handling I/O errors .NET](https://learn.microsoft.com/en-us/dotnet/standard/io/handling-io-errors) — IOException as base class confirmed

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all inbox; no new NuGet
- Architecture: HIGH — patterns derived from direct codebase read + verified official docs
- Pitfalls: HIGH for Pitfall 1-4 (verified via docs/code); MEDIUM for Pitfalls 5-7 (experience-based with code support)
- Security: MEDIUM — clipboard injection risk assessed as low; sidecar tampering covered by STJ type whitelist

**Research date:** 2026-05-26
**Valid until:** 2026-11-26 (System.Text.Json and SkiaSharp APIs are stable; no fast-moving dependencies)

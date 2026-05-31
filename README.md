# MathGaze

A native Windows desktop app that lets eye-gaze students work through GCSE maths exam papers using only gaze-driven clicks — no drag gestures, no mouse, no keyboard.

Built for students who use Grid 3 / Smartbox eye-gaze systems. All interaction is standard Windows click events, so no gaze SDK integration is required.

## Features

- **PDF viewer** — load any GCSE exam paper, navigate pages, zoom in/out
- **Geometry tools** — place Points, Lines, and Circles with 1–2 clicks; snap to endpoints and intersections
- **Protractor** — place by clicking two drawn lines (auto-aligns to intersection) or by clicking the vertex and an arm of a pre-drawn angle; rotate ±1°/±5°, flip scale, switch between 180° and 360° styles
- **Text labels** — paste text from Grid 3 clipboard and place it on the canvas
- **Select & adjust** — click any object to select it; nudge in 1 px / 5 px / 20 px steps from the right rail; undo/redo
- **Auto-save** — work saves automatically to a `.mathgaze.json` sidecar alongside the PDF; reopening the same PDF restores everything silently
- **PDF export** — export the annotated page as a 200 DPI PDF with all geometry baked in, ready for printing or submission
- **Light and dark mode** — toggle from the settings panel
- **Gaze-optimised UI** — all targets ≥56×56 px; object list panel for selecting placed items; mid-draw guidance card

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | WPF + .NET 10 |
| 2D canvas | SkiaSharp 3.x (`SKElement`) |
| PDF rendering | Docnet.Core (PDFium) |
| MVVM | CommunityToolkit.Mvvm 8.x |
| DI | Microsoft.Extensions.DependencyInjection |

## Building

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 or 11

### Run in development

```
dotnet run --project MathGaze/MathGaze.csproj
```

### Build a self-contained EXE

```
dotnet publish MathGaze/MathGaze.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o publish/
```

The output is a single `MathGaze.exe` in `publish/`. It bundles the .NET 10 runtime and all native dependencies — no install required on the target machine.

> **School machines:** The EXE runs without admin rights and without any pre-installed .NET or VC++ runtime. Copy it to a USB stick and launch directly.

## Project Structure

```
MathGaze/
├── Models/          # Geometry objects (PointObject, LineObject, etc.)
├── Services/        # PDF, geometry, session, export services
├── ViewModels/      # MVVM ViewModels (Main, Tool, Canvas, RightRail)
├── Views/           # XAML views and controls
└── Themes/          # Light.xaml and Dark.xaml resource dictionaries
```

## Licence

MIT — see [LICENSE](LICENSE).

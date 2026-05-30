Great—let’s turn this into something you could actually hand to a developer and start building.

I’ll break it into **3 parts**:

1. MVP scope (what to build first, realistically)
2. Week-by-week roadmap (practical, not idealised)
3. Core data models (geometry + interaction engine)

---

# 🧩 1. MVP Definition (What You Actually Build First)

You do **NOT** want to build everything from the spec initially.

### 🎯 MVP Goal

> A student can open a PDF and **accurately measure an angle using a protractor**, draw a **line of reflection**, and **select answers**

---

## ✅ MVP Feature Set

### Core

* Load + display PDF
* Page navigation
* Zoom

---

### Geometry (Minimum viable)

* Line tool (2-click)
* Selection tool
* Nudge controls (↑ ↓ ← →)

---

### 🔥 High Priority Tools

* Protractor (fully working, exam-safe)
* Reflection line (line placement + refinement)

---

### Answering

* Multiple choice selection (click to highlight)
* Basic text tool

---

### System

* Undo / redo
* Snap to:

  * endpoints
  * intersections (basic)

---

## ❌ NOT in MVP

* Transformations (rotation, enlargement)
* Advanced constructions (bisectors, loci)
* Full math keyboard
* Auto-detection AI features

👉 These come later

---

# 🗺️ 2. Development Roadmap (Realistic)

Assuming:

* 1–2 developers
* Part-time or focused sprint work

---

## 🟢 Phase 1 (Weeks 1–2): Foundations

### Goals:

* App skeleton working
* PDF rendering
* Basic UI layout

### Tasks:

* Set up WinUI 3 project
* Implement:

  * Top bar
  * Canvas
  * Tool dock
* Load PDF (Windows PDF API)
* Render page as bitmap

---

## 🟢 Phase 2 (Weeks 3–4): Rendering Engine

### Goals:

* Draw on top of PDF

### Tasks:

* Integrate **Win2D canvas**
* Implement:

  * Render loop
  * Basic shape drawing
* Coordinate system:

  * Map PDF → canvas space

---

## 🟢 Phase 3 (Weeks 5–6): Geometry Core

### Goals:

* Interactive objects

### Tasks:

* Create object model:

  * Point
  * Line
* Implement:

  * Line creation (2 clicks)
  * Selection system
* Basic hit-testing:

  * Click near line → select

---

## 🟢 Phase 4 (Weeks 7–8): Precision System

### Goals:

* Make it usable with eye gaze

### Tasks:

* Nudge controls (↑ ↓ ← →)
* Snap system:

  * Endpoints
  * Intersections (basic)
* Visual feedback:

  * Highlight selected object

---

## 🟢 Phase 5 (Weeks 9–10): Protractor Tool (CORE)

### Goals:

* Fully usable protractor

### Tasks:

* Render protractor overlay
* Implement:

  * Snap to vertex
  * Align to line
* Add controls:

  * Rotate ±1°, ±5°
  * Flip scale
* Add magnifier

👉 This is your **hardest and most important feature**

---

## 🟢 Phase 6 (Weeks 11–12): Reflection Tool

### Goals:

* Draw reflection line accurately

### Tasks:

* Line placement
* Snap assists
* Fine adjustment controls
* Optional preview (non-solving)

---

## 🟢 Phase 7 (Weeks 13–14): Answering Features

### Tasks:

* Multiple choice:

  * Click to highlight
  * Toggle selection
* Text tool:

  * Basic text boxes

---

## 🟢 Phase 8 (Weeks 15–16): Polish & Stability

### Tasks:

* Undo/redo system
* Performance optimisation
* UI scaling (large targets)
* Bug fixing

---

# 🧠 3. Core Data Models (IMPORTANT)

This is the backbone of everything.

---

## 3.1 Base Object

```csharp
abstract class GeometryObject
{
    Guid Id;
    bool IsSelected;
    bool IsLocked;

    abstract void Draw(CanvasDrawingSession ds);
    abstract bool HitTest(Point p);
}
```

---

## 3.2 Point

```csharp
class PointObject : GeometryObject
{
    Vector2 Position;

    override void Draw(...) { }
    override bool HitTest(...) { }
}
```

---

## 3.3 Line

```csharp
class LineObject : GeometryObject
{
    PointObject Start;
    PointObject End;

    override void Draw(...) { }
    override bool HitTest(...) { }
}
```

---

## 3.4 Protractor

```csharp
class ProtractorObject : GeometryObject
{
    Vector2 Center;
    float Rotation;
    bool IsFlipped;

    override void Draw(...) { }
}
```

---

## 3.5 Selection System

```csharp
class SelectionManager
{
    GeometryObject Selected;

    void Select(GeometryObject obj);
    void Clear();
}
```

---

## 3.6 Tool State Machine

```csharp
enum ToolType
{
    Select,
    Line,
    Protractor,
    Text
}
```

---

## 3.7 Interaction Controller

```csharp
class InputController
{
    ToolType CurrentTool;

    void OnClick(Point p)
    {
        switch(CurrentTool)
        {
            case ToolType.Line:
                HandleLineClick(p);
                break;
        }
    }
}
```

---

# ⚡ Key Systems You Must Get Right Early

## 1. Coordinate System

* PDF space vs canvas space
* Zoom scaling

---

## 2. Hit Testing

* Detect “near” objects
* Adjustable tolerance

---

## 3. Snap System

* Closest point
* Intersection detection

---

## 4. Rendering Separation

```id="layers"
PDF Layer (bitmap)
↓
Geometry Layer (vector)
↓
UI Overlay (controls)
```

---

# 🚨 Biggest Risks

## 1. Overbuilding Too Early

→ Stick to MVP

## 2. Precision Problems

→ Solve snapping early

## 3. Input Friction

→ Everything must work with simple clicks

---

# 🧠 Final Advice

If you get **just these right**, the project succeeds:

1. Protractor usability
2. Line placement + refinement
3. Selection + undo

Everything else builds on that.

---

# 🚀 If you want next

I can:

* Design the **protractor system in detail (math + UI)**
* Define the **snap algorithm**
* Or create a **click-by-click interaction flow for each tool**

This is now at the stage where you could realistically start building.

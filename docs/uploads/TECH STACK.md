You’ve got a few competing requirements here that matter more than usual:

* **Must run smoothly with eye-gaze (low latency, no jank)**
* **Needs precise rendering (geometry + overlays)**
* **PDF + interactive canvas layered together**
* **Possibly portable (no heavy install / admin rights)**
* **Accessible + stable (exam environment)**

That combination rules out a lot of otherwise “nice” stacks.

Let’s walk through it properly.

---

# 🧠 First: What This App Actually Is (Technically)

At its core, you are building:

> A **high-performance 2D graphics application**
> with **PDF rendering + interactive vector overlay + accessibility-first UI**

That puts it closer to:

* Figma / Whiteboard apps
* CAD-lite / geometry tools
* Not a typical “form-based app”

---

# ⚠️ What You Should NOT Use

### ❌ Electron (React + Chromium)

* Too heavy
* Memory usage high
* Input latency can be inconsistent
* Bad for exam environments

👉 Fine for dashboards, **not for precision assistive tools**

---

### ❌ Pure Web App

* Browser limitations (file access, performance)
* Harder to guarantee stability offline
* Less control over rendering/input

---

### ❌ WinForms

* Outdated rendering model
* Weak for custom graphics
* Will fight you on scaling + modern UI

---

# ✅ Recommended Stack (Best Fit)

## 🥇 Option 1: **.NET + WinUI 3 + Win2D (Strong Recommendation)**

### Why:

* Native Windows performance
* Modern UI framework
* Good balance of control + productivity
* Works well with accessibility APIs

---

### Stack Breakdown

#### 1. UI Layer

* **WinUI 3**

  * Modern Windows UI
  * Fluent design support
  * Good accessibility hooks

---

#### 2. Rendering Layer (CRITICAL)

* **Win2D** (or Direct2D underneath)

  * Hardware-accelerated 2D drawing
  * Perfect for:

    * Lines
    * Circles
    * Protractors
    * Overlays

👉 This is where your geometry engine lives visually

---

#### 3. PDF Rendering

* **PDFium (via wrapper)** or **Windows PDF APIs**

Options:

* Windows built-in `Windows.Data.Pdf`
* Or PDFium for more control

👉 You’ll render PDF as:

* Background bitmap per page
* Then overlay vector layer on top

---

#### 4. Interaction Layer

* Custom input abstraction:

  * Treat everything as “click events”
  * Let external gaze software drive input

---

#### 5. Geometry Engine (Core Logic)

Custom layer:

* Objects:

  * Points, lines, shapes
* Relationships:

  * Snap, intersection, constraints

👉 This is NOT a library—you’ll likely build it

---

#### 6. State Management

* MVVM pattern (standard in .NET)
* Keeps UI + logic clean

---

# 🥈 Option 2: **WPF + Direct2D/SkiaSharp (More Mature, Slightly Older)**

### Pros:

* Very stable ecosystem
* Easier tooling
* Large community

### Cons:

* Slightly older UI paradigm
* Less “modern” feel than WinUI

---

### When to choose this:

* You want faster development
* You’re more familiar with WPF
* You don’t need cutting-edge UI polish

---

# 🥉 Option 3: **C++ + DirectX / Direct2D (High Performance, High Cost)**

### Pros:

* Maximum performance
* Full control

### Cons:

* Much slower to build
* Higher complexity
* Harder accessibility integration

👉 Only worth it if scaling to very complex rendering later

---

# 📦 Portable Executable Considerations

## Option A: Self-Contained .NET App (Recommended)

* Publish as:

  * **Self-contained**
  * Single EXE

Pros:

* No install required
* Runs on exam machines
* Predictable environment

Cons:

* Larger file size (~100MB+)

---

## Option B: MSIX Installer

Pros:

* Clean install
* Sandboxed

Cons:

* May require admin policies (bad for exams)

---

👉 **Recommendation:**

> Start with **self-contained EXE**

---

# ⚡ Performance Considerations (Important)

## You MUST ensure:

### 1. Separate Layers

* PDF = bitmap layer
* Geometry = vector layer

---

### 2. Minimal Re-Renders

* Only redraw changed objects
* Use retained-mode rendering

---

### 3. Hit Testing Optimisation

* Spatial indexing (e.g. quadtree later)
* Needed for:

  * Snap detection
  * Hover highlighting

---

### 4. Frame Rate Target

* 60 FPS ideal
* More important: **consistent latency**

---

# 🧩 Suggested Architecture

```id="arch"
[ Input Layer (Eye Gaze / Clicks) ]
                ↓
[ Interaction Controller ]
                ↓
[ Geometry Engine ] ←→ [ State Store ]
                ↓
[ Rendering Layer (Win2D) ]
                ↓
[ PDF Background Layer ]
```

---

# 🧠 Key Technical Challenges (Be Aware)

## 1. Precision Without Drag

* Everything must be:

  * Click → adjust → confirm

---

## 2. Snap System

* Detect:

  * Closest point
  * Line intersections
* Must feel “magnetic”

---

## 3. Protractor Rendering

* Needs:

  * Rotation
  * Scaling
  * Clear markings

---

## 4. PDF Alignment

* Coordinate system must match:

  * PDF space
  * Canvas space

---

# 🧰 Suggested Libraries

| Purpose              | Option                        |
| -------------------- | ----------------------------- |
| PDF Rendering        | PDFium / Windows PDF API      |
| 2D Graphics          | Win2D                         |
| Math / Geometry      | Custom (or small helper libs) |
| Dependency Injection | Microsoft.Extensions          |
| MVVM                 | CommunityToolkit.Mvvm         |

---

# 🚀 MVP Stack (What I’d Actually Do)

If I were building this:

* **WinUI 3**
* **Win2D**
* **Windows PDF API (start simple)**
* **Custom geometry engine**
* **Self-contained EXE**

---

# 🔑 Final Recommendation

> Build this as a **native .NET Windows app with hardware-accelerated 2D rendering**

Avoid:

* Web tech
* Heavy frameworks
* Anything that introduces latency

---

# If you want next

I can:

* Break this into a **dev roadmap (week-by-week)**
* Design the **geometry engine structure**
* Or define **data models for shapes, snapping, and tools**

This is very buildable—but only if the rendering + interaction layers are done right early on.

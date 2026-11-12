# Third-party notices

The architecture viewer contains code that is adapted from Z.Blazor.Diagrams (MIT), and a copy of elkjs (EPL-2.0). The paths in the tables are relative to `Features/ArchitectureViewer/`.

| Item | Value |
|---|---|
| Project | Z.Blazor.Diagrams |
| Source | https://github.com/Blazor-Diagrams/Blazor.Diagrams |
| Commit | `fceb36fed10b3fcb1a4601dd199871326075d2ae` (2026-03-02) |
| License | MIT |

## Files from Z.Blazor.Diagrams

| Our file | Source file (`src/Blazor.Diagrams.Core/`) |
|---|---|
| `Presenters/Gestures/Behavior.cs` | `Behavior.cs` |
| `Presenters/Gestures/EventsBehavior.cs` | `Behaviors/EventsBehavior.cs` |
| `Presenters/Gestures/PanBehavior.cs` | `Behaviors/PanBehavior.cs` |
| `Presenters/Gestures/ZoomBehavior.cs` | `Behaviors/ZoomBehavior.cs` |
| `Presenters/Gestures/SelectionBehavior.cs` | `Behaviors/SelectionBehavior.cs` |
| `Presenters/Gestures/MouseButton.cs` | `MouseEventButton.cs` |
| `Presenters/Gestures/PointerInput.cs` | `Events/PointerEventArgs.cs`, `Events/WheelEventArgs.cs` (reduced) |
| `Presenters/Gestures/CanvasOptions.cs` | `Options/DiagramZoomOptions.cs` (as a record) |
| `Presenters/Gestures/Canvas.cs` | Event hub and behavior registry of `Diagram.cs` |
| `Presenters/Gestures/DragMovablesBehavior.cs` | `Behaviors/DragMovablesBehavior.cs` |
| `Presenters/Gestures/DragNewLinkBehavior.cs` | `Behaviors/DragNewLinkBehavior.cs` |
| `Presenters/Gestures/KeyboardShortcutsBehavior.cs` | `Behaviors/KeyboardShortcutsBehavior.cs`, `Utils/KeysUtils.cs` |
| `Presenters/Gestures/KeyboardShortcutsDefaults.cs` | `Behaviors/KeyboardShortcutsDefaults.cs` (`DeleteSelection` only) |
| `Presenters/Gestures/PointerInput.cs` (`KeyInput`) | `Events/KeyboardEventArgs.cs` (reduced) |

## MIT License

```
MIT License

Copyright (c) 2018 zHaytam

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## elkjs

| Item | Value |
|---|---|
| Project | elkjs (Eclipse Layout Kernel for JavaScript) |
| Source | https://github.com/kieler/elkjs |
| Version | 0.9.3 (npm package `elkjs`) |
| License | EPL-2.0 |
| Our file | `Infrastructure/wwwroot/elk.bundled.js`, an unchanged copy of `lib/elk.bundled.js` |
| License text | `LICENSE-EPL-2.0.md` |

The source code of elkjs is available at the source link above.

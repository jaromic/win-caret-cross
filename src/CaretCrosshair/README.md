# Caret Crosshair

A screen-wide crosshair that follows the visible text caret on Windows 11
x64. Pressing **Alt**, or switching the active window (Alt+Tab, taskbar,
etc.), briefly snaps the crosshair to the active window's top-left corner
before it resumes following the caret. See `specs/win-caret-cross_spec.md`
for the full spec and `specs/stories/win-caret-cross.md` for the story.

## Build

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
Building works from any OS (Windows target references are pulled in via
`EnableWindowsTargeting`), but the app itself only **runs** on Windows.

```
cd src/CaretCrosshair
dotnet publish -c Release
```

Output: `bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/CaretCrosshair.exe`
— a single self-contained portable executable. Copy it anywhere on a
Windows 11 x64 machine and run it directly; no installer, no separate
.NET runtime needed on the target machine.

`dotnet build -c Debug` also works, for a quick compile check without
publishing.

## Usage

- Runs from the system tray (no visible window). Right-click the tray
  icon for **Enabled** (checkable), **Crosshair Style**, and **Exit**.
- `Ctrl+Alt+X` toggles the crosshair on/off globally.
- The crosshair follows the caret automatically; it hides itself whenever
  no caret is reliably detected.
- Pressing Alt, or switching the active window, snaps the crosshair to
  that window's top-left corner for ~300ms.
- **Crosshair Style** (tray menu) switches the line rendering live:
  - *Dashed (1px)* — default. A single 1px line, alternating black/white
    dashes (4px each) along its length ("marching ants").
  - *Dotted (1px)* — same idea at the finest grain: alternates single
    black/white pixels along the line.
  - *Dotted Gray (1px)* — the same single-pixel alternation, but
    light-gray/dark-gray instead of black/white. Subtler and less
    eye-catching, still readable on any background.
  - *Solid (2px)* — one solid black 1px line directly next to one solid
    white 1px line.
  - *Outline (3px)* — 1px white core with a 1px black outline on each
    side.
  The black/white styles use the two luminance extremes, so no single
  background color can make the line fully disappear (worst case, a
  mid-gray background, still gives roughly even, moderate contrast
  against both). Dotted Gray trades some of that margin for subtlety —
  its two grays are chosen to stay apart enough from each other and from
  most backgrounds to remain recognizable, without full black/white
  contrast.

## Known limitations

- **No custom `app.manifest`.** An earlier version embedded a hand-written
  manifest via `<ApplicationManifest>`; combined with the self-contained
  single-file apphost, that produced "the application was unable to
  start... side-by-side configuration is incorrect" on real Windows. The
  SDK's default auto-generated manifest is used instead — it's guaranteed
  compatible with the `PublishSingleFile` pipeline. DPI awareness still
  comes from `ApplicationHighDpiMode` (an `Application.SetHighDpiMode()`
  call in generated startup code, not a manifest entry), so nothing was
  lost.
- **The crosshair can't render above the Start menu or Windows Search.**
  Windows reserves a z-order band above ordinary topmost windows for that
  shell chrome; there's no public API for a regular window to draw above
  it. Rather than show the crosshair incorrectly *underneath* those
  surfaces, `CrosshairEngine` detects when `StartMenuExperienceHost` or
  `SearchHost` is the foreground process and hides the crosshair until
  focus moves elsewhere.
- **Line style:** true inversion of whatever is on-screen underneath the
  crosshair isn't achievable via an overlay window — DWM composites each
  top-level window independently, with no blend mode that reaches into
  other windows' pixels. All three selectable styles (see Usage above)
  use black+white instead, for the same cross-background-legibility
  effect without needing inversion.
- **Caret detection, in priority order:**
  1. UI Automation `TextPattern.GetSelection()` on the focused element: a
     zero-length selection *is* the caret position, so its bounding
     rectangle is used. This is the standard workaround for the fact that
     .NET's managed UI Automation client (`System.Windows.Automation`)
     predates `TextPattern2`/`GetCaretRange` (added in Windows 8.1) —
     using that would require hand-written COM interop against
     `IUIAutomationTextPattern2`, which isn't practical to get right
     without a Windows machine to verify against.
  2. Win32 `GetGUIThreadInfo` (`rcCaret`/`hwndCaret`) as a fallback, for
     classic Win32 edit/rich-edit controls that don't expose
     `TextPattern`.
  3. If neither finds a caret, nothing is shown — per spec, the tool
     never estimates or guesses a caret position.
  - Consequence: if text is *actively selected* (not just a blinking
    caret), the crosshair follows the selection's bound rather than a
    literal caret line. Apps that implement neither UI Automation text
    patterns nor the classic Win32 caret API (e.g. many custom-rendered
    editors, some GPU-rendered terminals/games) won't show a crosshair —
    this is the intended "not reliably detected = not shown" behavior,
    not a bug.
- **Polling, not eventing:** the caret is polled every 30ms on the UI
  thread (there's no universal "caret moved" event across app
  frameworks). A hung/unresponsive foreground app could momentarily stall
  a UI Automation call and, with it, the crosshair update.
- **Multi-monitor:** the crosshair's two lines span only the monitor that
  contains the caret (or corner) point, not the full virtual desktop.
- **`Ctrl+Alt+X`:** registered as a system-wide hotkey via
  `RegisterHotKey`, reserving that exact combination while the tool runs
  (by design). Pressing it also triggers a harmless corner-flash, since
  flash-detection reacts to the physical Alt key-down that's part of the
  chord.
- Tray icons are drawn at runtime (a small red/gray crosshair glyph), so
  the portable build doesn't need to ship separate `.ico` assets.

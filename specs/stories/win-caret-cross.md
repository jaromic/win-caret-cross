# Caret Crosshair (win-caret-cross)

## Business Value
Users lose track of where the text caret or the active window is on large
/ multi-monitor setups. A screen-wide crosshair pinned to the caret (or
briefly to the active window's corner) gives instant visual orientation
without altering input. Without it, users keep hunting for a blinking
caret or guessing which window has focus.

## Epic
None — small standalone tool, no existing epic/goal fits.

## Scope
In scope (MVP, matches `specs/win-caret-cross_spec.md`):
- Crosshair follows the visible text caret; hidden when none is reliably
  detected.
- Alt key-down and any foreground-window change briefly flash the
  crosshair at the active window's top-left corner, then resume
  caret-following.
- Click-through, topmost, non-activating, DPI-aware overlay.
- `Ctrl+Alt+X` global toggle; tray icon with Enable/Disable + Exit.
- Portable self-contained win-x64 build (primary deliverable, no
  installation required — AC6).
- An optional per-user Inno Setup installer (`installer/`) on top of the
  portable exe, added post-MVP at user request — not a replacement for
  the portable build.

Out of scope for this pass: settings UI (color/thickness/hotkey
customization), literal caret-blink-phase tracking, tracking a caret
inside an active text *selection* as anything other than the selection's
bound.

## As-is / To-be
- As-is: no tool exists; this is a new standalone capability.
- To-be: behavior exactly as described in `specs/win-caret-cross_spec.md`.
- Gap / approach: single walking skeleton, built and expanded in one pass
  since the whole thing is one small vertical slice (tray app + overlay +
  caret detection + hooks). No incremental deploy stages needed for an
  MVP this size.

## Dependencies
- Windows 11 x64 target only; no other services.
- .NET 8 SDK (`EnableWindowsTargeting`) to build, incl. cross-compiling
  from a non-Windows machine.
- This repo's dev container is Linux, so implementation could be
  build/publish-verified (`dotnet build` / `dotnet publish -r win-x64`)
  but **not functionally exercised** — first real run needs to happen on
  actual Windows 11 hardware. See README "Known limitations".

## Decisions made during interview (2026-09-17)
- Compile-check in this (Linux) container via a cross-compiled
  `dotnet publish -r win-x64` rather than skipping local verification
  entirely.
- Corner-flash triggers on **both** physical Alt key-down and any
  foreground-window-change event (covers Alt+Tab, taskbar clicks,
  Win+Tab), not strictly the Alt key alone.
- Flash hold duration: 300 ms.
- Crosshair lines span only the monitor containing the point, not the
  full virtual desktop.

## Fixes from first real-device run (2026-09-17)
- Dropped the hand-written `app.manifest` — combined with
  `PublishSingleFile` self-contained deployment, it caused "the
  application was unable to start... side-by-side configuration is
  incorrect" on real Windows. DPI awareness still works via
  `ApplicationHighDpiMode` alone.
- Crosshair now anchors to the caret's **bottom edge**, not its vertical
  center (both `UiaCaretProvider` and `Win32CaretProvider`).
- Line color changed from solid red to a white line with a black
  outline. True inversion of on-screen content isn't achievable for an
  overlay window (DWM composites each top-level window independently),
  so the outline is the practical substitute for cross-background
  legibility.
- Crosshair now hides itself while Start Menu or Windows Search has
  focus (`StartMenuExperienceHost`/`SearchHost`), since no public API
  lets a normal window draw above that shell's z-order band.
- User confirmed core behavior ("works") on their own Windows 11 machine
  after these fixes — AC1-AC5 are informally confirmed there, though not
  against the Test Plan's specific scripted checks below. AC6 (clean
  machine, no install) and the new installer are still unverified.

## Acceptance Criteria
(verbatim from the spec's Akzeptanzkriterien)
1. Given Notepad has focus and a visible caret, when the user types or
   navigates with arrow keys, then the crosshair follows the caret.
2. Given no visible text caret is present, then no crosshair is shown.
3. Given any window is active, when the user presses Alt, then the
   crosshair briefly appears at that window's top-left corner, even if it
   has no text field.
4. Given the user switches windows via Alt+Tab, then the crosshair shows
   the top-left corner of the newly activated window.
5. Given the tool is running, then mouse, keyboard, and focus behave
   exactly as without it (no interference).
6. Given a clean Windows 11 x64 machine, then the published build runs
   without installation.

## Test Plan
- AC1/AC2: manual — Notepad, type and arrow-navigate; blur focus entirely
  (e.g. focus the desktop) and confirm crosshair disappears.
- AC3/AC4: manual — press Alt in various apps; Alt+Tab between two
  windows; verify corner flash then caret resumption.
- AC5: manual — confirm typing/clicking/window switching feel unchanged
  with the tool running (no dropped/altered input).
- AC6: manual — copy the single published `.exe` to a clean Windows 11
  x64 VM/machine and launch it directly.
- Build correctness (this environment, no Windows available): `dotnet
  build` and `dotnet publish -c Release` for `win-x64` succeed with 0
  errors/warnings, confirmed in this container.

Status: AC1-AC5 informally confirmed working by the user on their own
Windows 11 machine after the post-first-run fixes above; not yet run
against the scripted checks above one by one. AC6 (clean machine) is
still unverified.

## Installer verification (2026-09-18)
User confirmed the Inno Setup installer (`installer/CaretCrosshair.iss`)
builds and works on their Windows machine. Not yet separately verified:
a truly clean machine with no prior manual install, and the
uninstall/re-install path.

## Deploy & Monitoring
Not a service deploy — a portable executable handed directly to the user.
"Ops" here is: user copies `CaretCrosshair.exe` to the target Windows 11
x64 machine and runs it; nothing to monitor beyond the user confirming
AC1-AC6 hold on real hardware.

# Caret Crosshair

A Windows 11 x64 tray tool that draws a screen-wide crosshair pinned to
the visible text caret, so you never lose track of it. Press **Alt**, or
switch windows, and it briefly snaps to the active window's top-left
corner instead — instant confirmation of which window has focus. The
overlay is click-through and never touches mouse/keyboard input;
`Ctrl+Alt+X` or the tray icon turns it on/off.

- `src/CaretCrosshair/` — the app itself. See
  `src/CaretCrosshair/README.md` for build instructions and known
  limitations.
- `installer/` — optional per-user Inno Setup installer built on top of
  the portable exe. See `installer/README.md`.
- `scripts/build.ps1` — builds both the exe and the installer in one go
  (Windows only). See `scripts/README.md`.
- `specs/win-caret-cross_spec.md` / `specs/stories/win-caret-cross.md` —
  the spec and story behind this tool.

## Dev container

This repo also carries a Docker-based container for running Claude Code
against this project. `/workspace` inside the container is bind-mounted
from the repo root, so edits made in the container are immediately
visible on the host and vice versa.

### Start a session (build-if-needed, resume-if-stopped, attach-if-running)
```
./start-session.sh
```
This is the only command you normally need. It attaches to the
container if it's already running, starts it if it's stopped, or builds
the image via `build-image.sh` and runs a new container if it doesn't
exist yet.

### Build the image manually
```
./build-image.sh
```

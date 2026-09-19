# Installer

`CaretCrosshair.iss` is an [Inno Setup](https://jrsoftware.org/isinfo.php)
script that packages the portable exe from `src/CaretCrosshair` into a
proper per-user installer, for people who'd rather not manage the
portable exe manually. The portable exe itself (see
`src/CaretCrosshair/README.md`) remains the primary, always-available
deliverable — this installer is an optional convenience on top of it.

## What it does

- Installs to `%LOCALAPPDATA%\Programs\CaretCrosshair` — **no admin
  rights required** (`PrivilegesRequired=lowest`).
- Adds a Start Menu entry and a standard uninstaller listed in
  *Settings → Apps → Installed apps*.
- Offers a checked-by-default task to register the app to launch at
  sign-in (a per-user `HKCU\...\Run` registry value, cleanly removed on
  uninstall).
- Detects and closes a running instance before install/uninstall
  (`AppMutex` matching the app's own single-instance mutex, plus a
  `taskkill` fallback in `[UninstallRun]`).

## Build (Windows only)

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php).

Easiest: `scripts\build.ps1` from the repo root builds both the portable
exe and this installer in one go — see `scripts/README.md`.

Manually:

```
cd src\CaretCrosshair
dotnet publish -c Release
cd ..\..
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\CaretCrosshair.iss
```

Output: `installer\dist\CaretCrosshairSetup.exe`.

## Known limitations

- Not code-signed. Windows SmartScreen will likely warn on first run of
  an unsigned installer/exe from an unrecognized publisher; that's
  expected for a small unsigned tool.

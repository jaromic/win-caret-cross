# Scripts

`build.ps1` (Windows only — PowerShell) builds both deliverables in one
go: the portable exe (`dotnet publish -c Release`) and, if
[Inno Setup 6](https://jrsoftware.org/isinfo.php) is installed, the
installer on top of it.

```
.\scripts\build.ps1
```

Just the exe, skipping the installer step:

```
.\scripts\build.ps1 -SkipInstaller
```

Run from anywhere — it locates the repo root relative to its own script
path. See `src/CaretCrosshair/README.md` and `installer/README.md` for
what each build step produces and their known limitations.

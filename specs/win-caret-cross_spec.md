# Caret Crosshair -- Minimal Specification

## Anforderungen

Windows-Tool, das ein bildschirmweites Fadenkreuz zur Orientierung
anzeigt.

-   Ist ein **sichtbares Text-Caret** vorhanden, liegt der Schnittpunkt
    des Fadenkreuzes auf dessen Position.
-   Ist **kein sichtbares Text-Caret** vorhanden, ist **kein Fadenkreuz
    sichtbar**.
-   Bei jedem Druck auf **Alt** springt das Fadenkreuz kurzzeitig auf
    `(0,0)` des **aktiven Fensters** (linke obere Ecke). Damit wird
    unmittelbar sichtbar, welches Fenster aktiv ist.
-   Danach folgt das Fadenkreuz wieder dem Caret bzw. verschwindet, wenn
    keines sichtbar ist.
-   Das Overlay ist click-through und darf weder Maus- noch
    Tastatureingaben beeinflussen.
-   `Ctrl+Alt+X`: Tool ein/aus.
-   Tray-Icon mit Enable/Disable und Exit.
-   Windows 11 x64, portable.

## Akzeptanzkriterien

1.  **Caret:** In Notepad folgt das Fadenkreuz dem Caret beim Tippen und
    bei Navigation mit den Pfeiltasten.
2.  **Kein Caret:** Ist kein sichtbares Text-Caret vorhanden, ist kein
    Fadenkreuz sichtbar.
3.  **Alt:** Beim Drücken von Alt erscheint das Fadenkreuz kurz an der
    linken oberen Ecke des aktiven Fensters -- auch wenn dieses kein
    Textfeld enthält.
4.  **Fensterwechsel:** Beim Wechseln zwischen Fenstern per Alt+Tab
    zeigt das Fadenkreuz die linke obere Ecke des jeweils aktivierten
    Fensters.
5.  **Keine Interferenz:** Maus, Tastatur und Fokus funktionieren
    unverändert.
6.  **Portable:** Release läuft ohne Installation unter Windows 11 x64.

## Technische Überlegungen

Implementierung vorzugsweise C#/.NET.

Für Caret-Erkennung geeignete Windows-APIs verwenden (z. B. UI
Automation, Accessibility/Win32 mit Fallbacks). Keine Caret-Position
schätzen: **nicht zuverlässig erkannt = nicht anzeigen**.

Overlay muss transparent, topmost, non-activating, click-through und
DPI-/Multi-Monitor-tauglich sein.

## Deliverable

Source Code + Build-Dateien + portable Windows-x64-Version + kurze
README mit Build-Anleitung und bekannten Einschränkungen.

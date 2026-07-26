# File Management and Printing

This page covers the everyday mechanics of working with files in READYCode: the Folder Explorer, tabs, importing and exporting, and printing.

## The Folder Explorer

Opening a folder (**File > Open Folder...**, Ctrl+K Ctrl+O) populates the Explorer panel with a tree of its contents. Each item shows a color-coded badge and icon for its kind, folder, BASIC `.prg`, machine-language `.prg`, `.d64`/`.d81` disk image, or plain assembly/other file, so you can tell at a glance what you are looking at.

The tree supports the conventions you would expect from a modern editor: inline creation of new files, folders, and disk images; inline rename; drag-and-drop to move items; cut/copy/paste; delete; and Reveal in File Explorer to jump to the item in Windows Explorer. Right-click any item for its full context menu.

## Tabs

Every open file gets its own tab, showing the file name and a dot when there are unsaved changes.

- **Reopen Closed Tab** (Ctrl+Shift+T) keeps a short history of recently closed tabs.
- **Restore previous session**, a Preferences option, reopens exactly the tabs you had open (including which ones were in Hex Editor mode) the next time you launch READYCode.
- The tab context menu offers Close, Close Others, Close to the Right, Close Saved (Ctrl+K U), and Close All (Ctrl+K W).
- If enough tabs are open that they overflow the tab bar, a dropdown button appears to list and jump to any of them.

## Opening files by dragging them in

Drag one or more `.prg` files from Windows Explorer onto the READYCode window to open them as new tabs. A drag containing anything other than `.prg` files is rejected.

## Saving, versus importing and exporting

**Save** (Ctrl+S) and **Save As...** (Ctrl+Shift+S) always round-trip through the authentic file format for the type of file you have open: the real tokenized `.prg` binary for BASIC programs, or plain assembly text for `.asm`/`.s` files. This is what keeps a saved BASIC program byte-for-byte compatible with what a real C64, or VICE, would produce.

**File > Export...** and **File > Import...** are a separate, plain-text escape hatch: Export writes the active document out as a `.txt` file, and Import reads a `.txt` file into a new tab. Use these when you need a plain-text copy of your source, for example to share it somewhere that does not understand `.prg` files, rather than the authentic binary format.

## Printing

![Print Preview showing PETSCII-accurate rendering](../images/READYCode-Print-Preview.png)

**File > Print...** (Ctrl+P) and **Print Preview...** render the active document through the same PETSCII-accurate rendering used on screen, so BASIC listings print with the correct C64 graphics and control characters. Assembly files print in the same plain font used to edit them. **Page Setup...** opens the standard Windows dialog for paper size, margins, and orientation.

---

[Back to Documentation Home](README.md)

# The Hex Editor

Sometimes you need to look at or edit the raw bytes of a file rather than its interpreted contents, whether that is a machine-language program, a disk image's directory sectors, or a file type READYCode does not otherwise understand. The Hex Editor covers that case.

## Opening a file as hex

Right-click any file, in the local Folder Explorer, the C64U Explorer, or an entry inside a mounted `.d64`/`.d81` disk image, and choose **Open as Hex**. This works on any file, not just ones READYCode recognizes as a BASIC program, assembly source, or disk image. Machine-language `.prg` files and unrecognized C64U files open directly in hex mode by default when double-clicked, since there is no meaningful text representation to show instead.

## Layout

The Hex Editor shows a standard three-column grid: byte offset, hexadecimal byte values (sixteen per row), and their ASCII representation side by side, so you can correlate raw bytes with any readable text they contain.

## Editing bytes

Click a byte cell, or the corresponding character in the ASCII column, to open a small inline editor. Type the new hex value and press Enter to commit it, or Escape to cancel. Click and drag, or Shift-click, to select a range of bytes at once.

## Clipboard operations

The same Cut, Copy, Paste, and Delete commands you would use in the text editor work here too, adapted for raw bytes:

- **Copy** places the selected bytes on the clipboard as space-separated hex text.
- **Cut** copies the selection and then zero-fills it.
- **Paste** overwrites bytes starting at the cursor with hex text from the clipboard.
- **Delete** zero-fills the current selection.

## Undo and redo

Every byte-level edit is tracked in its own undo/redo history, separate from the text editor's, so Ctrl+Z and Ctrl+Y behave exactly as you would expect while editing hex.

## Tab state

Your selected offset and scroll position in a hex tab are remembered when you switch away and back, and whether a tab was left in hex mode is preserved across restarting READYCode (when session restore is enabled in Preferences).

---

[Back to Documentation Home](README.md)

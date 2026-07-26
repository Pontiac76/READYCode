# Find in Files

Find in Files searches across an entire open project folder at once, rather than a single document, which is useful once a program grows across multiple files or you are trying to track down every place a variable or label is used.

## Opening it

Click the Search icon in the left activity bar, or use **Edit > Find in Files** (Ctrl+Shift+F) or **Edit > Replace in Files** (Ctrl+Shift+H).

## What gets searched

Every file with a searchable extension under the current project's root folder is searched recursively: `.bas`, `.asm`, `.s`, `.txt`, and `.prg`. A `.prg` file is decoded to text for searching the same way it would be if you opened it in the editor, and if you replace a match inside one, it is re-tokenized back to a real `.prg` on write. A `.prg` that is not a valid tokenized BASIC program, for example a machine-language file, is skipped rather than treated as text. Folders that cannot be read are skipped rather than stopping the search.

## Search options

Next to the search box, three toggle buttons control how the search text is matched:

- **Match Case** - treats the search as case-sensitive.
- **Match Whole Word** - only matches the search text as a complete word, not as part of a longer one.
- **Use Regular Expression** - treats the search text as a .NET regular expression (multiline mode) instead of literal text. An invalid pattern simply produces no results rather than showing an error.

Expanding the Replace row adds a **Replace All in Project** button for applying a replacement everywhere at once.

## Working with results

Matches are shown as a tree in the left panel, grouped by file with a match count next to each one. Expand a file to see every matching line, and double-click a line to jump straight to it in the editor.

---

[Back to Documentation Home](README.md)

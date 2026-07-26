# The Assembly Editor

READYCode also supports writing 6502 assembly language directly, for programs that need more speed or control than BASIC allows. This page covers the assembly-specific editing features and what the built-in assembler produces.

## Creating an assembly file

Use **File > New > Assembly File** (Ctrl+Alt+N) to start a new `.asm` file. Assembly files are edited as plain text, unlike BASIC's tokenized `.prg` format, so the usual text-editing conventions apply: case matters, and there is no automatic line numbering.

## Syntax highlighting

Mnemonics, numeric literals, labels, and `;` comments are each highlighted separately, making the structure of a routine easy to follow.

![A BASIC listing with REM comments rendered as PETSCII graphics](../images/READYCode-Bouncing-Ball-Assembly.png)

## Supported instructions and directives

The built-in assembler supports the full standard 6502 instruction set, all 56 official mnemonics, with every legal addressing mode for each one: immediate, zero page (with X or Y indexing), absolute (with X or Y indexing), indirect, indexed indirect, indirect indexed, accumulator, relative, and implied.

A small set of directives is supported:

- `.org` sets the assembly origin (the memory address the code will load at). If used, it must be the first thing in the file.
- `.byte` / `.text` embeds literal bytes, including quoted strings.
- `.word` embeds 16-bit values, either literal or symbolic.
- `NAME = value` declares a named constant.

There is no macro support.

## Labels and constants

Labels are declared with a trailing colon (`label:`) and are case-sensitive, along with constants. Symbol expressions like `msgptr+1` are supported when referencing a label or constant with an offset. Assembly happens in two passes, so labels can be referenced before they are declared later in the file.

## What assembling produces

How you assemble a program depends on whether you use `.org`:

- **Without `.org`**, READYCode produces a complete, directly runnable `.prg`. It automatically prepends a tiny one-line BASIC loader stub (`10 SYS 2062`) ahead of your machine code, so the result can be loaded and run immediately without writing your own loader.
- **With `.org`**, READYCode writes a standard two-byte load-address header instead, and does not add the BASIC stub. This is the typical choice for code that needs to load at a specific address, such as sprite or character data, or a program meant to be called from elsewhere rather than run directly.

## Diagnostics

The editor runs the real assembler in the background as you type and reports any problem as a squiggle underline: duplicate labels or constants, undefined labels, invalid addressing modes, branches that are out of range, operand overflow, and malformed `.org` usage among them. Hover over a squiggle to see the specific error.

## The Symbols panel

Below the primary side bar, the Symbols panel lists every label and constant in the active assembly file, with occurrence counts. Click an occurrence to jump straight to it. This is the assembly-language counterpart to the Variables panel shown for BASIC files.

## Hover tooltips

Hovering over a mnemonic shows a short description and the addressing modes it supports. Hovering over a line flagged by a diagnostic shows the specific error instead.

## The ASM Mnemonics panel

A reference panel, available from the right activity bar, listing every supported mnemonic with a description, the assembly-language counterpart to the BASIC Keywords panel.

## Code folding

Runs of two or more consecutive full-line `;` comments can be collapsed to reduce clutter. Folding can be turned off in **Preferences > Settings... > Text Editor > Code Analysis**.

## Code Statistics

![The Code Statistics dialog](../images/READYCode-Code-Statistics-Assembly.png)

**View > Code Statistics** opens a dialog showing character, word, and line counts for the active document, along with the assembled byte count.

---

[Back to Documentation Home](README.md)

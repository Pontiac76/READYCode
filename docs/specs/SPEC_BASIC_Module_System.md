# DRAFT: READYCode BASIC Module System — Feature Specification

**Status:** Draft  
**Target:** READYCode (future release)  
**Scope:** C64 BASIC multi-file module support with build-time stitching, symbolic labels, variable aliasing, and module documentation

---

## 1. Overview

C64 BASIC programs are monolithic: a single flat sequence of numbered lines tokenized into one PRG file. This makes large programs difficult to organize, maintain, or share between projects.

The READYCode BASIC Module System introduces a build-time pre-processing layer that allows developers to author BASIC programs as multiple source files ("modules"), then stitch them into a single valid PRG at build time. The C64 and C64 Ultimate receive a standard PRG — they have no awareness of the module system.

All module features are expressed via `REM`-based directives. This ensures every source file remains valid C64 BASIC that can be loaded directly onto real hardware if needed, with the directives silently ignored as remarks.

---

## 2. Concepts

### 2.1 Module

A module is a single `.bas` source file that represents one logical unit of a program — for example, `graphics.bas`, `input.bas`, or `sound.bas`. Each module:

- Begins with a `@MODULE` declaration
- Declares its public labels and variables using `@LABEL` and `@VAR`
- Documents its dependencies via `@DEPENDS`
- May include other modules via `#INCLUDE`

### 2.2 Entry Point / Main File

One module serves as the entry point. It is the file opened or "built" in READYCode. It contains `#INCLUDE` directives referencing other modules. Included modules may themselves include further modules (nested includes).

### 2.3 Build / Stitch

The build process:
1. Resolves the include tree starting from the entry point
2. Detects circular dependencies and line number conflicts
3. Renumbers all lines across all modules to produce a non-colliding sequence
4. Replaces all `@LABEL` symbolic references with resolved line numbers
5. Replaces all `@VAR` friendly names with assigned two-character abbreviations
6. Produces a single merged PRG file with an optional BASIC loader stub

---

## 3. Directives Reference

All directives are written inside `REM` statements. READYCode recognizes them via the `@` or `#` prefix. They are case-insensitive.

### 3.1 `#INCLUDE`

Instructs READYCode to insert the contents of another module at this position during stitching.

**Syntax:**
```basic
10 REM #INCLUDE "filename.bas"
```

- Path is relative to the current file's location
- Nested includes are supported
- Circular dependency detection is enforced at build time
- Order matters: included modules are stitched in the order they appear

**Example:**
```basic
10 REM #INCLUDE "init.bas"
20 REM #INCLUDE "graphics.bas"
30 REM #INCLUDE "gameloop.bas"
40 GOSUB @INIT
50 GOTO @GAMELOOP
```

---

### 3.2 Module Header Tags

These tags appear at the top of a module file and describe the module as a whole.

| Tag | Description |
|---|---|
| `@MODULE name` | Declares the module name. Required. Must be unique across the project. |
| `@DESCRIPTION text` | Human-readable description of what the module does. |
| `@AUTHOR text` | Author name. |
| `@VERSION text` | Version string (free-form). |
| `@DEPENDS name` | Declares a dependency on another module by name. Can appear multiple times. READYCode warns if the dependency is not included before this module in the stitch order. |

**Example:**
```basic
10 REM @MODULE graphics
20 REM @DESCRIPTION Routines for direct screen and color RAM manipulation
30 REM @AUTHOR Jeff Bram
40 REM @VERSION 1.0
50 REM @DEPENDS init
```

---

### 3.3 Label Tags

Labels are symbolic names for line numbers. They replace hardcoded numeric targets in `GOTO`, `GOSUB`, `THEN`, and `ON ... GOTO/GOSUB` statements.

#### `@LABEL`

Declares a named entry point at the current line. Scope is specified inline as an optional qualifier. If omitted, `(PRIVATE)` is assumed.

**Syntax:**
```basic
100 REM @LABEL (PUBLIC) CLEARSCREEN
110 REM @LABEL (PRIVATE) CLRINNER
120 REM @LABEL HELPERROUTINE          ← no qualifier; treated as (PRIVATE)
```

- `(PUBLIC)` - label is visible to all modules. Appears in the Module Explorer panel.
- `(PRIVATE)` - label is internal to the module. READYCode warns if another module references it.

The label name must be unique within its scope.

#### `@DESC`

Short description of what this label's routine does. Shown in tooltip on hover over any `GOSUB @LABELNAME` or `GOTO @LABELNAME` in the editor.

```basic
100 REM @LABEL (PUBLIC) CLEARSCREEN
110 REM @DESC Fills screen RAM ($0400) with spaces and resets color RAM to white
```

#### `@PARAM`

Documents a variable that must be set before calling this label (the C64 BASIC equivalent of a function parameter). Can appear multiple times.

```basic
130 REM @PARAM Z1% - color value (0-15) to apply; set via POKE or assignment before GOSUB
140 REM @PARAM Z2% - character code to fill screen with (default 32 = space)
```

#### `@RETURNS`

Documents variables that contain output values after the routine returns.

```basic
150 REM @RETURNS Z9% - status code: 0=success, 1=out of bounds
```

**Full label block example:**
```basic
100 REM @LABEL (PUBLIC) PLOTCHAR
110 REM @DESC Writes a character to screen RAM at the given coordinates
120 REM @PARAM PX% - X position (0-39)
130 REM @PARAM PY% - Y position (0-24)
140 REM @PARAM PC% - PETSCII character code to write
150 REM @PARAM PK% - color value (0-15)
160 REM @RETURNS nothing
```

---

### 3.4 Variable Tags

#### `@VAR`

Declares a friendly-named variable and maps it to a two-character C64 BASIC abbreviation.

**Syntax:**
```basic
REM @VAR [(PUBLIC)|(PRIVATE)] FriendlyName[type] [abbreviation]
```

- **FriendlyName** - descriptive name used in source code, prefixed with `@` when referenced
- **type** - standard BASIC type suffix: none (float), `%` (integer), `$` (string)
- **abbreviation** - optional preferred two-character abbreviation; auto-generated if omitted
- **scope** - `(PUBLIC)` (shared across modules) or `(PRIVATE)` (local to this module); default is `(PRIVATE)`

**Examples:**
```basic
10 REM @VAR (PUBLIC) NumberOfLives% NL
20 REM @VAR (PUBLIC) PlayerScore SC
30 REM @VAR (PUBLIC) PlayerName$ PN
40 REM @VAR (PRIVATE) TempRow% TR
50 REM @VAR (PRIVATE) TempCol% TC
60 REM @VAR (PRIVATE) WorkBuffer$ WB
```

#### Arrays

Array variables are declared with empty parentheses after the type suffix to distinguish them from scalars (a scalar `NL` and an array `NL(` would collide in BASIC):

```basic
10 REM @VAR (PUBLIC) ScoreTable%() ST
20 REM @VAR (PRIVATE) EnemyX%() EX
```

The `()` suffix tells READYCode to treat this as an array variable. In stitched output, array accesses like `@ScoreTable%(I%)` become `ST%(I%)`.

#### Referencing Variables in Source

In source code, friendly variable names are prefixed with `@`:

```basic
100 LET @NumberOfLives% = 3
110 IF @NumberOfLives% = 0 THEN GOTO @GAMEOVER
120 @PlayerName$ = "JEFF"
130 FOR I% = 0 TO 9 : @ScoreTable%(I%) = 0 : NEXT I%
```

Stitched output:
```basic
100 LET NL% = 3
110 IF NL% = 0 THEN GOTO 5020
120 PN$ = "JEFF"
130 FOR I% = 0 TO 9 : ST%(I%) = 0 : NEXT I%
```

Note: loop variables (e.g., `I%`, `J%`) and other short-lived variables that don't need friendly names can be written directly without `@` and pass through unchanged.

---

## 4. Variable Resolution Rules

### 4.1 Abbreviation Assignment

1. If the `@VAR` declaration specifies an abbreviation, that is used.
2. If no abbreviation is specified, READYCode generates one from the first two significant characters of the friendly name (e.g., `NumberOfLives` → `NO`, `PlayerScore` → `PL`).
3. If the generated abbreviation collides with an existing one in the same scope, READYCode increments a suffix (`NO`, `N1`, `N2`, etc.) and emits a build warning.

### 4.2 Collision Detection

- Two `@PUBLIC` variables from different modules cannot share an abbreviation. This is a build error.
- Two `@PRIVATE` variables in the same module cannot share an abbreviation. This is a build error.
- A `@PRIVATE` variable in module A and a `@PRIVATE` variable in module B may share an abbreviation — they are in separate scopes. READYCode manages the mapping per-module during substitution.
- A `@PRIVATE` variable cannot share an abbreviation with any `@PUBLIC` variable. This is a build error.

### 4.3 Type Suffix Handling

The type suffix (none, `%`, `$`) is preserved through substitution. `@NumberOfLives%` resolves to `NL%`, not `NL`. The declaration type and all usage types must match; a mismatch is a build warning.

### 4.4 Variable Budget Reporting

The build output panel reports:

```
Variable budget: 14 public variables, 23 private variables (37 total)
Available: 26 single-char slots, 676 two-char slots
WARNING: @TempBuffer$ and @WorkBuffer$ both requested abbreviation WB - @WorkBuffer$ reassigned to WK$
```

---

## 5. Symbolic Label Resolution

### 5.1 Reference Syntax

Anywhere a line number is valid in C64 BASIC, a `@LABELNAME` reference may appear:

```basic
GOTO @GAMELOOP
GOSUB @CLEARSCREEN
IF @Score% > 100 THEN GOTO @HIGHSCORE
ON J% GOTO @MENU1, @MENU2, @MENU3
ON K% GOSUB @FIRE, @JUMP, @CROUCH
```

### 5.2 Resolution Process

During stitching, after all modules are concatenated and lines are renumbered, READYCode builds a label map:

```
GAMELOOP  → 2020
CLEARSCREEN → 1100
HIGHSCORE → 3500
```

All `@LABELNAME` references in the stitched source are replaced with the corresponding line number. Unresolved labels (referenced but never declared) are build errors.

### 5.3 Forward References

Forward references are fully supported — a module may `GOSUB @ROUTINE` where `@ROUTINE` is declared in a module included later in the stitch order.

---

## 6. Build Process

### 6.1 Steps

1. **Parse entry point** - read the main `.bas` file, identify all `#INCLUDE` directives
2. **Resolve include tree** - recursively parse all included modules; detect circular dependencies (error if found)
3. **Validate module declarations** - check all `@DEPENDS` are satisfied in include order; warn if not
4. **Collect variable declarations** - build the global variable map; detect and resolve abbreviation collisions
5. **Collect label declarations** - build the label map; detect duplicate public label names (error)
6. **Stitch** - concatenate all modules in include order, assigning non-overlapping line number ranges to each
7. **Renumber** - assign final line numbers. Default: step of 10, starting at 10. Configurable per project.
8. **Substitute variables** - replace all `@FriendlyName` references with their assigned abbreviations
9. **Substitute labels** - replace all `@LABELNAME` references with resolved line numbers
10. **Emit output** - write the merged `.bas` source and compile to `.prg`
11. **Report** - display build summary, warnings, and variable budget in the Output panel

### 6.2 Line Numbering

Each module is allocated a block of line numbers during stitching. Blocks are sized based on the module's actual line count plus a configurable headroom (default: 20% padding).

Project-level settings (stored in the READYCode project file):
- `lineNumberStart` - first line number (default: 10)
- `lineNumberStep` - increment between lines (default: 10)
- `moduleHeadroom` - padding factor per module block (default: 1.2)

### 6.3 Stitch Order

Modules are stitched in the order their `#INCLUDE` directives appear, depth-first. The entry point's own lines appear first (before any included content) unless `#INCLUDE` directives are interspersed, in which case includes are inserted at the point of the directive.

---

## 7. Editor Integration

### 7.1 Symbols Panel

The existing Symbols panel is extended to show the module structure:

```
▼ MODULES
  ▼ graphics.bas
      CLEARSCREEN  (public)
      PLOTCHAR     (public)
      CLRINNER     (private)
  ▼ gameloop.bas
      GAMELOOP     (public)
      UPDATEPOS    (private)
▼ VARIABLES (public)
    NL%  → @NumberOfLives%
    SC   → @PlayerScore
    PN$  → @PlayerName$
```

Clicking a label navigates to its declaration. Exported variables show their friendly name and abbreviation.

### 7.2 Tooltips

Hovering over a `GOSUB @LABELNAME` or `GOTO @LABELNAME` reference shows:

```
CLEARSCREEN (graphics.bas)
Fills screen RAM ($0400) with spaces and resets color RAM to white
Params:  Z1% - color value (0-15)
         Z2% - character code to fill (default 32)
Returns: nothing
```

Hovering over an `@FriendlyName` variable reference shows:

```
@NumberOfLives%  →  NL%
Integer. Exported. Declared in gameloop.bas.
```

### 7.3 Module Explorer Panel

A new panel (tab alongside the Folder Explorer) shows the project's module tree:

```
▼ main.bas  [entry point]
  ├── init.bas
  ├── graphics.bas
  │     └── util.bas
  ├── input.bas
  └── gameloop.bas
```

Clicking a module opens it for editing. A badge shows public label and variable counts per module.

### 7.4 Merged View

A read-only tab labelled `[project name] (Merged).bas` shows the final stitched source with resolved line numbers, abbreviations, and no directives. This is what gets compiled to PRG. It is regenerated on each build.

### 7.5 Source/Merged Toggle

In the toolbar, a toggle switches between **Source** (friendly names, `@` references) and **Merged** (raw C64 BASIC output) views of the current file. Source view is for editing; Merged view is read-only.

---

## 8. Build Warnings and Errors

### Errors (build fails)

| Code | Condition |
|---|---|
| E001 | Circular dependency detected (module A includes B which includes A) |
| E002 | `#INCLUDE` references a file that does not exist |
| E003 | Duplicate `@PUBLIC` label name across two or more modules |
| E004 | Unresolved `@LABELNAME` reference (label declared nowhere) |
| E005 | Duplicate `@PUBLIC` variable abbreviation after collision resolution |
| E006 | `@MODULE` name is missing or duplicated |
| E007 | `@VAR` friendly name referenced in code but never declared |

### Warnings (build succeeds)

| Code | Condition |
|---|---|
| W001 | `@DEPENDS` module is not included before this module in stitch order |
| W002 | Variable abbreviation collision — auto-reassigned |
| W003 | `@PRIVATE` label referenced from outside its module |
| W004 | `@VAR` declared but never referenced in source |
| W005 | `@LABEL` declared but never referenced (unreachable entry point) |
| W006 | Type suffix mismatch between `@VAR` declaration and usage |
| W007 | Module declared `@DEPENDS` on a module not present in the project |

---

## 9. Project File Integration

Module system settings are stored in the READYCode project file (`.readycode` or equivalent):

```json
{
  "entryPoint": "main.bas",
  "build": {
    "lineNumberStart": 10,
    "lineNumberStep": 10,
    "moduleHeadroom": 1.2,
    "outputPrg": "mygame.prg",
    "basicLoaderStub": true
  }
}
```

The entry point is the only required field. All other settings have the defaults listed above.

---

## 10. Constraints and Edge Cases

### 10.1 REM Lines in Output

`@MODULE`, `@LABEL`, `@VAR`, `@DESCRIPTION`, and other directive REM lines are stripped from the merged output by default. A build option `keepDirectiveRems: true` retains them as plain REM lines (useful for debugging the merged output).

### 10.2 Non-Module BASIC Files

`.bas` files that do not contain a `@MODULE` declaration are treated as plain BASIC files. The `#INCLUDE` directive can still include them, but no label or variable processing is applied to them — they are inserted verbatim (with line renumbering only).

### 10.3 Standalone Module Execution

Individual modules should be authored so that they can load and run standalone on a real C64 for testing where possible (e.g., a graphics module could have a small test harness at the top of the file). The directive REM lines will be silently ignored by the C64.

### 10.4 Variables Not Declared via `@VAR`

Two-character (or single-character) variables written directly in source without an `@` prefix pass through unchanged. This allows loop counters, temporary scratch variables, and other short-lived values to be written naturally without requiring declaration.

READYCode should warn (W008) if a raw variable name collides with a generated abbreviation from the `@VAR` system.

### 10.5 `ON ... GOTO/GOSUB` with Mixed References

Mixed numeric and symbolic targets are supported:

```basic
ON J% GOTO @MENU1, @MENU2, 9000
```

The `9000` passes through unchanged. `@MENU1` and `@MENU2` are resolved normally.

### 10.6 `RESTORE` and `DATA` Statements

`DATA` lines referenced by `RESTORE` with a line number should use symbolic labels:

```basic
RESTORE @SPRITEDATA
...
1000 REM @LABEL SPRITEDATA
1010 DATA 1,2,3,4,5
```

### 10.7 Maximum Include Depth

Circular dependency detection handles infinite recursion. A configurable maximum include depth (default: 10 levels) is enforced to catch near-circular cases. Exceeding it is error E001.

---

## 11. Future Considerations (Out of Scope for Initial Implementation)

- **`@CONST` directive** - compile-time constants replaced at build time (e.g., `@CONST SCREENWIDTH = 40`)
- **Conditional inclusion** - `#IFDEF` / `#ENDIF` for platform variants (PAL vs NTSC builds, debug vs release)
- **Module library/registry** - shared module repository accessible from READYCode for common routines (screen clear, SID player, sprite handler, etc.)
- **Linter rule: `@PARAM` validation** - warn if a `GOSUB @LABEL` is called without setting the documented `@PARAM` variables beforehand
- **Disassembler annotation** - when disassembling a PRG that was built by READYCode, use the stored symbol map to annotate the output with original friendly names

---

## 12. Example: Complete Two-Module Project

### `main.bas`
```basic
10  REM @MODULE main
20  REM @DESCRIPTION Entry point - bouncing ball demo
30  REM @DEPENDS graphics
40  REM #INCLUDE "graphics.bas"
50  REM
60  REM @VAR (PUBLIC) BallX% BX
70  REM @VAR (PUBLIC) BallY% BY
80  REM @VAR (PUBLIC) DeltaX% DX
90  REM @VAR (PUBLIC) DeltaY% DY
100 REM
110 LET @BallX% = 20
120 LET @BallY% = 12
130 LET @DeltaX% = 1
140 LET @DeltaY% = 1
150 GOTO @MAINLOOP
160 REM
170 REM @LABEL (PUBLIC) MAINLOOP
180 REM @DESC Main game loop - erase, update, draw
200 GOSUB @ERASEBALL
210 LET @BallX% = @BallX% + @DeltaX%
220 IF @BallX% < 0 OR @BallX% > 39 THEN @DeltaX% = -@DeltaX%
230 LET @BallY% = @BallY% + @DeltaY%
240 IF @BallY% < 0 OR @BallY% > 24 THEN @DeltaY% = -@DeltaY%
250 GOSUB @DRAWBALL
260 GOTO @MAINLOOP
```

### `graphics.bas`
```basic
10  REM @MODULE graphics
20  REM @DESCRIPTION Screen and color RAM drawing routines
30  REM @AUTHOR Jeff Bram
40  REM @VERSION 1.0
50  REM
60  REM @VAR (PRIVATE) BallChar% BC
70  REM @VAR (PRIVATE) BallColor% BK
80  REM
90  LET @BallChar% = 81
100 LET @BallColor% = 10
110 REM
120 REM @LABEL (PUBLIC) DRAWBALL
130 REM @DESC Writes ball character to screen RAM at (@BallX%, @BallY%)
150 REM @PARAM BallX% - X position (0-39), declared in main
160 REM @PARAM BallY% - Y position (0-24), declared in main
170 POKE 1024 + (@BallY% * 40) + @BallX%, @BallChar%
180 POKE 55296 + (@BallY% * 40) + @BallX%, @BallColor%
190 RETURN
200 REM
210 REM @LABEL (PUBLIC) ERASEBALL
220 REM @DESC Writes a space to screen RAM at (@BallX%, @BallY%)
240 POKE 1024 + (@BallY% * 40) + @BallX%, 32
250 RETURN
```

### Merged output (approximate)
```basic
10  LET BX% = 20
20  LET BY% = 12
30  LET DX% = 1
40  LET DY% = 1
50  GOTO 110
60  BC% = 81
70  BK% = 10
80  GOSUB 130
90  LET BX% = BX% + DX%
100 IF BX% < 0 OR BX% > 39 THEN DX% = -DX%
110 LET BY% = BY% + DY%
120 IF BY% < 0 OR BY% > 24 THEN DY% = -DY%
130 GOSUB 180
140 GOTO 80
150 POKE 1024 + (BY% * 40) + BX%, BC%
160 POKE 55296 + (BY% * 40) + BX%, BK%
170 RETURN
180 POKE 1024 + (BY% * 40) + BX%, 32
190 RETURN
```

---

*End of specification.*

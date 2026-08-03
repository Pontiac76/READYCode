# What Changed

`____/2026-08-03\_________`

## Summary

This update adds a complete disk-image build and run workflow for READYCode projects. Projects can now define D64/D81 contents with simple manifest files, generate disk images automatically on save, and send those images directly to VICE or an Ultimate/U2+ device for mounting, loading, and running.

Major additions:

- `*._64` and `*._81` disk image manifests.
- Automatic D64/D81 generation after saving project files.
- A configurable generated disk image output directory.
- Correct CBM DOS directory, file type, block count, BAM, and D81 header metadata.
- VICE right-click disk image actions: mount, load, and run.
- Ultimate/U2+ right-click disk image actions grouped by Drive A and Drive B.
- Ultimate/U2+ keyboard-buffer automation for disk-image load/run workflows.
- C128 native-mode detection and automatic switch to C64 mode when needed.
- Tests covering manifest-generated disk images and raw disk directory metadata.

## Disk Image Manifests

READYCode now recognizes `*._64` and `*._81` files as disk image manifests.

A manifest is a plain-text list of disk entries. Each non-comment line describes one file or directory entry to place into the generated disk image.

Example:

```text
HELLOWORLD.PRG
README|SEQ
CONTACT ME|SEQ
```

Manifest rules:

- `*._64` generates a `.d64` image.
- `*._81` generates a `.d81` image.
- Blank lines are ignored.
- Lines beginning with `;` or `#` are comments.
- `|TYPE` may be used to specify a CBM DOS file type.
- Supported manifest type suffixes include `|PRG`, `|SEQ`, and `|USR`.
- If no type is specified, entries default to PRG.
- Existing project files are resolved case-insensitively.
- `.bas` files are tokenized to PRG.
- `.asm` and `.s` files are assembled to PRG.
- `.prg` files are copied as-is.
- Missing entries intentionally become empty disk entries, which can be used for directory comments or art.

The pipe character is used as the manifest type delimiter so colon remains available in CBM-style names and directory text.

## Manifest Editing in the IDE

Manifest files are treated as plain text, not BASIC source.

This means `*._64` and `*._81` files:

- open directly in the editor,
- save as text,
- are not tokenized as BASIC programs,
- do not use BASIC keyword highlighting,
- do not show the BASIC variable explorer or ASM symbol explorer.

For C64-style readability, manifest typing and pasting are upper-cased in READYCode. This keeps authored manifest content aligned with the normal look of a C64 directory while still avoiding BASIC tokenization.

When building the disk image:

- entries backed by real source files use conventional uppercase CBM directory names,
- missing/comment entries preserve the authored text as disk entry names as much as the disk format allows,
- disk entry names are limited to the CBM DOS 16-character filename field.

## Automatic Image Generation on Save

After a successful save, READYCode scans the project tree for `*._64` and `*._81` manifests and rebuilds the corresponding disk images.

The save flow waits for the current file write to complete before manifest generation begins. This is especially important for virtual files edited from inside disk images, because the source bytes must be fully written before the manifest builder reads them.

After generation, the file tree refreshes so newly created or updated disk images are visible immediately.

## Generated Disk Image Output Directory

Generated images are written to a configurable output folder.

Setting:

```text
Code > Disk Images > Generated disk image directory
```

Default:

```text
generated
```

If the setting is blank, READYCode writes each generated disk image next to its manifest.

This keeps generated `.d64` and `.d81` files separate from user-authored disk images by default.

## Disk Image Format Fixes

The disk image writer now produces more accurate CBM DOS metadata for generated and edited disk images.

Updated behavior includes:

- directory entry block counts are written at bytes `28-29`,
- manifest-requested file types are written to directory entries,
- D64/D81 BAM bitmap bytes are ordered by sector number,
- D81 BAM/header fields match expected 1581-style formatting,
- empty entries keep READYCode's existing one-sector terminal-file behavior.

These changes make generated images report correctly in external tools such as VICE, DirMaster, and `c1541`.

## VICE Disk Image Actions

Local D64/D81 images now have right-click VICE actions:

```text
VICE
  Mount Disk
  Load Disk
  Run Disk
```

VICE integration uses the binary monitor protocol.

Behavior:

- `Mount Disk` starts or restarts VICE as needed, attaches the selected disk image, and resets.
- `Load Disk` sends the selected disk image to VICE autoload without running it.
- `Run Disk` sends the selected disk image to VICE autostart and runs it.

READYCode detects the correct VICE drive type for the disk image formats currently surfaced by the IDE:

- `.d64` -> 1541
- `.d81` -> 1581

D71/1571 image authoring and browsing are not part of this change set.

When possible, READYCode avoids restarting VICE. It tracks the last disk image path it sent to VICE and reuses the running emulator when the selected action can be performed safely without a restart.

Manifest rebuilds also cooperate with VICE. If READYCode previously loaded or ran a generated image in VICE, the next rebuild can temporarily release drive 8 so the image can be overwritten, then restore the drive type.

## Ultimate/U2+ Disk Image Actions

Local D64/D81 images now have right-click Ultimate actions grouped by drive:

```text
Ultimate
  Drive A
    Mount Disk
    Mount and Reset
    Load Disk
    Run Disk
  Drive B
    Mount Disk
    Mount and Reset
    Load Disk
    Run Disk
```

The Ultimate workflow uses both FTP and REST:

1. Upload the local disk image to the Ultimate device.
2. Mount the uploaded image to Drive A or Drive B through the REST API.
3. Optionally reset, load, or run the first program.

Uploaded files are drive-specific:

```text
/Temp/readycode-drive-a-<filename>
/Temp/readycode-drive-b-<filename>
```

This lets Drive A and Drive B mount independent copies of the same local image.

If the C64U Explorer FTP connection is already open, READYCode reuses it. Otherwise it creates a short-lived FTP connection for the upload.

## Ultimate Load/Run Automation

The Ultimate REST API can mount disk images, but loading from a mounted disk is performed by the C64 itself. READYCode automates that by writing commands into the machine keyboard buffer with the Ultimate memory API.

In C64 mode:

- keyboard buffer: `$0277`
- keyboard count: `$00C6`

READYCode stuffs short C64 BASIC abbreviations so the command fits in the 10-byte keyboard buffer:

```text
lO"*",8,1<RETURN>
rU<RETURN>
```

The device number is read from the Ultimate drive status API. If the API does not report one, READYCode uses the usual defaults:

- Drive A -> device 8
- Drive B -> device 9

## C128 Handling

READYCode is currently C64-oriented, but the Ultimate/U2+ workflow can be used with a C128.

Before load/run automation, READYCode checks whether the machine is in C128 native mode by reading `$D030`. If native C128 mode is detected, READYCode uses the C128 keyboard buffer:

- keyboard buffer: `$034A`
- keyboard count: `$00D0`

It then sends:

```text
GO64<RETURN>
Y<RETURN>
```

After the machine settles into C64 mode, READYCode continues with the normal C64 load/run commands.

`Mount and Reset` also performs this C128-to-C64 transition when needed, but does not load or run a program.

## C64UltimateClient API Support

The Ultimate REST client now includes the API calls needed for disk-image workflows:

- drive status parsing, including IEC device numbers,
- disk image mount/remove operations,
- machine reset/control operations,
- memory read for machine-mode detection,
- memory write for keyboard-buffer stuffing.

Memory write requests use hex addresses and `data` URL parameters, matching the Ultimate/U2+ API format.

HTTP errors include request details to make device/API troubleshooting easier.

## Context Menu Organization

Disk image context actions are grouped by target:

```text
VICE
Ultimate
```

Ultimate actions are further grouped by drive:

```text
Drive A
Drive B
```

Nested context menu handling was updated so submenu actions still operate on the selected disk image node.

## Tests

Tests were added and updated to cover:

- D64 manifest generation,
- D81 manifest generation,
- generated output directory behavior,
- blank output-directory fallback behavior,
- manifest file type suffixes,
- empty/missing manifest entries,
- raw CBM DOS directory type/start/block-count bytes.

These tests protect both the manifest builder and the lower-level disk image metadata fixes.

## Git Ignore Behavior

Generated/local development output is ignored without globally ignoring disk images.

Ignored paths include:

```text
Projects/
generated/
**/generated/
```

`.d64` and `.d81` files are intentionally not globally ignored, because authored disk images may be real project assets.

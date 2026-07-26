# Disk Images (.d64 and .d81)

READYCode can read, create, and edit Commodore disk images directly, so you can manage a floppy's worth of programs without ever leaving the editor. Both `.d64` (the standard 35-track 1541 format) and `.d81` (the 80-track 1581 format) are supported, using the same directory and file-chain logic in each case, so results load correctly on real hardware or in VICE.

This works identically whether you are browsing local files in the Folder Explorer or the C64 Ultimate's own storage in the C64U Explorer. See [Transferring to Hardware and Emulators](c64-ultimate-and-vice.md) for more on the C64U Explorer itself.

## Creating a disk image

Right-click a folder in either Explorer and choose **New .d64 Disk Image...** or **New .d81 Disk Image...** to create a new, blank, correctly formatted image in place.

## Browsing a disk image

Disk images appear in the tree with their own icon. Expand one to see the individual programs stored inside it, without needing to mount it first. Opening a program from inside a disk image loads it into a normal editor tab, exactly as if it were a standalone file.

## Editing the contents

From the tree, you can add, replace, rename, or delete individual programs inside an existing disk image. Adding a file accepts a `.prg` as-is, or tokenizes a `.bas` source file into one automatically. Every one of these operations maintains a valid directory and block-allocation map, the bookkeeping structures a real C64 uses to find files on a disk, so the image stays usable afterward.

When you save changes to a program that was opened from inside a disk image, the edit is written straight back into the image rather than creating a separate file.

## Inspecting raw disk sectors

If you need to look at the directory or block-allocation map directly, right-click a disk image and choose **Open as Hex**. See [The Hex Editor](hex-editor.md).

## Mounting to a real C64 Ultimate

Disk images on the C64 Ultimate's own storage can be mounted to a virtual drive so the machine can actually use them, covered in [Transferring to Hardware and Emulators](c64-ultimate-and-vice.md#mounting-disk-images).

---

[Back to Documentation Home](README.md)

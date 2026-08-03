// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using ReadyCode.C64U;
using ReadyCode.Models;
using ReadyCode.ViewModels;
using Xunit;

namespace ReadyCode.Tests;

/// <summary>
/// Tests manifest-driven disk image generation (<c>*._64</c> / <c>*._81</c>) through the same
/// view-model entry point used by the save flow.
/// </summary>
public class ManifestDiskImageBuildTests
{
    [Fact]
    public async Task BuildDiskImagesFromManifests_D64_WritesConfiguredGeneratedDirectoryAndEntries()
    {
        string root = CreateTempProject();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Main"));
            await File.WriteAllBytesAsync(Path.Combine(root, "Main", "hello.prg"), Encoding.ASCII.GetBytes("HELLO"));
            await File.WriteAllTextAsync(Path.Combine(root, "Main", "disk._64"), "hello.prg\nTEST|SEQ\nHI|USR\n");

            var vm = new MainViewModel();
            vm.Settings.GeneratedDiskImageDirectory = "generated";

            bool built = await vm.BuildDiskImagesFromManifestsAsync(root);

            Assert.True(built);
            string diskPath = Path.Combine(root, "generated", "disk.d64");
            Assert.True(File.Exists(diskPath));

            byte[] image = await File.ReadAllBytesAsync(diskPath);
            var disk = DiskImage.ForKind(C64UFileKind.D64);
            var entries = disk.ReadDirectory(image);
            Assert.Equal(["HELLO", "TEST", "HI"], entries.Select(e => e.Name).ToArray());
            Assert.Equal(Encoding.ASCII.GetBytes("HELLO"), entries.Single(e => e.Name == "HELLO").Content);
            Assert.Empty(entries.Single(e => e.Name == "TEST").Content);
            Assert.Empty(entries.Single(e => e.Name == "HI").Content);

            AssertDirectoryEntry(image, DiskGeometry.D64, 0, expectedType: 0x82, expectedStartTrack: 1, expectedStartSector: 0, expectedBlocks: 1);
            AssertDirectoryEntry(image, DiskGeometry.D64, 1, expectedType: 0x81, expectedStartTrack: 1, expectedStartSector: 1, expectedBlocks: 1);
            AssertDirectoryEntry(image, DiskGeometry.D64, 2, expectedType: 0x83, expectedStartTrack: 1, expectedStartSector: 2, expectedBlocks: 1);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task BuildDiskImagesFromManifests_D81_WritesConfiguredGeneratedDirectoryAndEntries()
    {
        string root = CreateTempProject();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Main"));
            await File.WriteAllBytesAsync(Path.Combine(root, "Main", "hello.prg"), Encoding.ASCII.GetBytes("HELLO"));
            await File.WriteAllTextAsync(Path.Combine(root, "Main", "disk._81"), "hello.prg\nTEST|SEQ\nHI|USR\n");

            var vm = new MainViewModel();
            vm.Settings.GeneratedDiskImageDirectory = "generated";

            bool built = await vm.BuildDiskImagesFromManifestsAsync(root);

            Assert.True(built);
            string diskPath = Path.Combine(root, "generated", "disk.d81");
            Assert.True(File.Exists(diskPath));

            byte[] image = await File.ReadAllBytesAsync(diskPath);
            var disk = DiskImage.ForKind(C64UFileKind.D81);
            var entries = disk.ReadDirectory(image);
            Assert.Equal(["HELLO", "TEST", "HI"], entries.Select(e => e.Name).ToArray());
            Assert.Equal(Encoding.ASCII.GetBytes("HELLO"), entries.Single(e => e.Name == "HELLO").Content);
            Assert.Empty(entries.Single(e => e.Name == "TEST").Content);
            Assert.Empty(entries.Single(e => e.Name == "HI").Content);

            AssertDirectoryEntry(image, DiskGeometry.D81, 0, expectedType: 0x82, expectedStartTrack: 1, expectedStartSector: 0, expectedBlocks: 1);
            AssertDirectoryEntry(image, DiskGeometry.D81, 1, expectedType: 0x81, expectedStartTrack: 1, expectedStartSector: 1, expectedBlocks: 1);
            AssertDirectoryEntry(image, DiskGeometry.D81, 2, expectedType: 0x83, expectedStartTrack: 1, expectedStartSector: 2, expectedBlocks: 1);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task BuildDiskImagesFromManifests_BlankGeneratedDirectory_WritesNextToManifest()
    {
        string root = CreateTempProject();
        try
        {
            string main = Path.Combine(root, "Main");
            Directory.CreateDirectory(main);
            await File.WriteAllBytesAsync(Path.Combine(main, "hello.prg"), Encoding.ASCII.GetBytes("HELLO"));
            await File.WriteAllTextAsync(Path.Combine(main, "disk._64"), "hello.prg\n");

            var vm = new MainViewModel();
            vm.Settings.GeneratedDiskImageDirectory = "";

            bool built = await vm.BuildDiskImagesFromManifestsAsync(root);

            Assert.True(built);
            Assert.True(File.Exists(Path.Combine(main, "disk.d64")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempProject()
    {
        string root = Path.Combine(Path.GetTempPath(), "READYCode.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void AssertDirectoryEntry(
        byte[] image,
        DiskGeometry geometry,
        int index,
        byte expectedType,
        byte expectedStartTrack,
        byte expectedStartSector,
        int expectedBlocks)
    {
        int entryOffset = SectorOffset(geometry, geometry.DirectoryTrack, geometry.DirectorySector) + 2 + index * 32;
        Assert.Equal(expectedType, image[entryOffset]);
        Assert.Equal(expectedStartTrack, image[entryOffset + 1]);
        Assert.Equal(expectedStartSector, image[entryOffset + 2]);
        Assert.Equal(expectedBlocks & 0xFF, image[entryOffset + 28]);
        Assert.Equal((expectedBlocks >> 8) & 0xFF, image[entryOffset + 29]);
    }

    private static int SectorOffset(DiskGeometry geometry, int track, int sector)
    {
        int sectorsBefore = 0;
        for (int t = 1; t < track; t++)
            sectorsBefore += geometry.SectorsPerTrack[t];

        return (sectorsBefore + sector) * 256;
    }
}

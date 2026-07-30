// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using ReadyCode.Assembler;
using Xunit;

namespace ReadyCode.Tests;

/// <summary>
/// Tests for <see cref="AsmListingWriter"/>.
/// </summary>
public class AsmListingWriterTests
{
    #region Public Methods

    [Fact]
    public void Generate_CodeLine_ShowsAddressAndBytesBeforeSource()
    {
        string source = ".org $0810\nNOP";
        var result = new Asm6502Assembler().Assemble(source);

        string listing = AsmListingWriter.Generate(source, result);
        string[] lines = listing.Split('\n');

        Assert.Contains("$0810", lines[1]);
        Assert.Contains("EA", lines[1]);
        Assert.EndsWith("NOP", lines[1]);
    }

    [Fact]
    public void Generate_NonCodeLine_HasNoAddressButKeepsSourceText()
    {
        string source = ".org $0810\nloop:\nNOP";
        var result = new Asm6502Assembler().Assemble(source);

        string listing = AsmListingWriter.Generate(source, result);
        string[] lines = listing.Split('\n');

        Assert.DoesNotContain("$", lines[1]);
        Assert.EndsWith("loop:", lines[1]);
    }

    [Fact]
    public void Generate_PreservesEveryOriginalSourceLine()
    {
        string source = ".org $0810\n; a comment\nloop:\nNOP\nBEQ loop";
        var result = new Asm6502Assembler().Assemble(source);

        string listing = AsmListingWriter.Generate(source, result);
        string[] lines = listing.Split('\n');

        Assert.Equal(6, lines.Length); // 5 source lines + trailing empty entry from the final '\n'
        Assert.EndsWith(".org $0810", lines[0]);
        Assert.EndsWith("; a comment", lines[1]);
        Assert.EndsWith("loop:", lines[2]);
        Assert.EndsWith("NOP", lines[3]);
        Assert.EndsWith("BEQ loop", lines[4]);
    }

    [Fact]
    public void Generate_ByteDirectiveWithManyBytes_DoesNotTruncateByteList()
    {
        string source = ".org $0810\n.byte $01,$02,$03,$04,$05,$06,$07,$08";
        var result = new Asm6502Assembler().Assemble(source);

        string listing = AsmListingWriter.Generate(source, result);
        string[] lines = listing.Split('\n');

        Assert.Contains("01 02 03 04 05 06 07 08", lines[1]);
    }

    #endregion
}

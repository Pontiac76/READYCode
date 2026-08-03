// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using ReadyCode.Assembler;
using Xunit;

namespace ReadyCode.Tests;

/// <summary>
/// Tests for <see cref="Asm6502Disassembler"/>.
/// </summary>
public class Asm6502DisassemblerTests
{
    #region Public Methods

    [Fact]
    public void Disassemble_StartsWithOrgDirectiveForStartAddress()
    {
        string source = new Asm6502Disassembler().Disassemble([0xEA], 0x0810).Source;

        Assert.StartsWith(".org $0810", source);
    }

    [Fact]
    public void Disassemble_Implied_HasNoOperand()
    {
        string source = new Asm6502Disassembler().Disassemble([0xEA], 0x0810).Source; // NOP

        Assert.Matches(@"NOP\s+;", source);
    }

    [Fact]
    public void Disassemble_Accumulator_OperandIsA()
    {
        string source = new Asm6502Disassembler().Disassemble([0x0A], 0x0810).Source; // ASL A

        Assert.Matches(@"ASL A\s+;", source);
    }

    [Fact]
    public void Disassemble_Immediate_FormatsHashHex()
    {
        string source = new Asm6502Disassembler().Disassemble([0xA9, 0x00], 0x0810).Source; // LDA #$00

        Assert.Matches(@"LDA #\$00\s+;", source);
    }

    [Fact]
    public void Disassemble_ZeroPage_FormatsTwoDigitHex()
    {
        string source = new Asm6502Disassembler().Disassemble([0xA5, 0xFB], 0x0810).Source; // LDA $FB

        Assert.Matches(@"LDA \$FB\s+;", source);
    }

    [Fact]
    public void Disassemble_ZeroPageX_FormatsWithIndex()
    {
        string source = new Asm6502Disassembler().Disassemble([0xB5, 0xFB], 0x0810).Source; // LDA $FB,X

        Assert.Matches(@"LDA \$FB,X\s+;", source);
    }

    [Fact]
    public void Disassemble_Absolute_FormatsFourDigitHex()
    {
        string source = new Asm6502Disassembler().Disassemble([0xAD, 0x00, 0xD0], 0x0810).Source; // LDA $D000

        Assert.Matches(@"LDA \$D000\s+;", source);
    }

    [Fact]
    public void Disassemble_AbsoluteX_FormatsWithIndex()
    {
        string source = new Asm6502Disassembler().Disassemble([0xBD, 0x00, 0xD0], 0x0810).Source; // LDA $D000,X

        Assert.Matches(@"LDA \$D000,X\s+;", source);
    }

    [Fact]
    public void Disassemble_Indirect_FormatsParenthesized()
    {
        string source = new Asm6502Disassembler().Disassemble([0x6C, 0x34, 0x12], 0x0810).Source; // JMP ($1234)

        Assert.Matches(@"JMP \(\$1234\)\s+;", source);
    }

    [Fact]
    public void Disassemble_IndirectX_FormatsParenthesizedWithIndex()
    {
        string source = new Asm6502Disassembler().Disassemble([0xA1, 0xFB], 0x0810).Source; // LDA ($FB,X)

        Assert.Matches(@"LDA \(\$FB,X\)\s+;", source);
    }

    [Fact]
    public void Disassemble_IndirectY_FormatsParenthesizedWithTrailingIndex()
    {
        string source = new Asm6502Disassembler().Disassemble([0xB1, 0xFB], 0x0810).Source; // LDA ($FB),Y

        Assert.Matches(@"LDA \(\$FB\),Y\s+;", source);
    }

    [Fact]
    public void Disassemble_Relative_ResolvesForwardBranchTarget()
    {
        // BEQ +2 at $0810 -> next instruction at $0812, so target is $0814.
        string source = new Asm6502Disassembler().Disassemble([0xF0, 0x02], 0x0810).Source;

        Assert.Matches(@"BEQ \$0814\s+;", source);
    }

    [Fact]
    public void Disassemble_Relative_ResolvesBackwardBranchTarget()
    {
        // BNE -2 at $0810 -> next instruction at $0812, so target is $0810 (branches to itself).
        string source = new Asm6502Disassembler().Disassemble([0xD0, 0xFE], 0x0810).Source;

        Assert.Matches(@"BNE \$0810\s+;", source);
    }

    [Fact]
    public void Disassemble_UnrecognizedOpcode_FallsBackToByteDirective()
    {
        // 0x02 is not one of the 56 official opcodes.
        string source = new Asm6502Disassembler().Disassemble([0x02], 0x0810).Source;

        Assert.Contains(".byte $02", source);
    }

    [Fact]
    public void Disassemble_TruncatedInstructionAtEndOfBuffer_FallsBackToByteDirective()
    {
        // LDA absolute (0xAD) needs 2 operand bytes, but the buffer ends right after the opcode.
        string source = new Asm6502Disassembler().Disassemble([0xAD], 0x0810).Source;

        Assert.Contains(".byte $AD", source);
        Assert.DoesNotContain("LDA", source);
    }

    [Fact]
    public void Disassemble_RawByteComment_ShowsAddressAndBytes()
    {
        string source = new Asm6502Disassembler().Disassemble([0xA9, 0x2A], 0x0810).Source; // LDA #$2A

        Assert.Contains("; $0810: A9 2A", source);
    }

    // ── Mnemonic indent column ────────────────────────────────────────────────────

    [Fact]
    public void Disassemble_DefaultMnemonicIndentColumn_Is8Spaces()
    {
        string source = new Asm6502Disassembler().Disassemble([0xEA], 0x0810).Source; // NOP, default column 9

        Assert.Contains("\n        NOP", source);
    }

    [Fact]
    public void Disassemble_CustomMnemonicIndentColumn_IndentsAccordingly()
    {
        // Column 5 -> 4 leading spaces.
        string source = new Asm6502Disassembler().Disassemble([0xEA], 0x0810, mnemonicIndentColumn: 5).Source;

        Assert.Contains("\n    NOP", source);
        Assert.DoesNotContain("\n     NOP", source); // not 5 spaces
    }

    [Fact]
    public void Disassemble_ByteDirective_UsesSameIndentAsMnemonics()
    {
        string source = new Asm6502Disassembler().Disassemble([0x02], 0x0810, mnemonicIndentColumn: 5).Source;

        Assert.Contains("\n    .byte $02", source);
    }

    // ── Comment alignment column ──────────────────────────────────────────────────

    [Fact]
    public void Disassemble_DefaultCommentAlignColumn_AlignsAtColumn32()
    {
        string source = new Asm6502Disassembler().Disassemble([0xEA], 0x0810).Source; // NOP
        string line = GetLineContaining(source, "NOP");

        Assert.Equal(31, line.IndexOf(';'));
    }

    [Fact]
    public void Disassemble_CustomCommentAlignColumn_AlignsAtThatColumn()
    {
        string source = new Asm6502Disassembler().Disassemble([0xEA], 0x0810, commentAlignColumn: 20).Source;
        string line = GetLineContaining(source, "NOP");

        Assert.Equal(19, line.IndexOf(';'));
    }

    [Fact]
    public void Disassemble_CodeLongerThanCommentAlignColumn_FallsBackToTwoSpaceGap()
    {
        // AbsoluteX operand text is long enough to blow past a column of 10.
        string source = new Asm6502Disassembler().Disassemble([0xBD, 0x00, 0xD0], 0x0810, commentAlignColumn: 10).Source;
        string line = GetLineContaining(source, "LDA");

        Assert.Matches(@"LDA \$D000,X {2};", line);
    }

    // ── Line addresses ─────────────────────────────────────────────────────────────

    [Fact]
    public void Disassemble_LineAddresses_SkipsOrgAndBlankLine()
    {
        var result = new Asm6502Disassembler().Disassemble([0xEA], 0x0810); // NOP

        Assert.False(result.LineAddresses.ContainsKey(1)); // ".org $0810"
        Assert.False(result.LineAddresses.ContainsKey(2)); // blank separator line
    }

    [Fact]
    public void Disassemble_LineAddresses_MapsEachInstructionLineToItsAddress()
    {
        // NOP ($0810, 1 byte) then LDA #$00 ($0811, 2 bytes) then RTS ($0813, 1 byte).
        var result = new Asm6502Disassembler().Disassemble([0xEA, 0xA9, 0x00, 0x60], 0x0810);

        Assert.Equal(0x0810, result.LineAddresses[3]);
        Assert.Equal(0x0811, result.LineAddresses[4]);
        Assert.Equal(0x0813, result.LineAddresses[5]);
        Assert.Equal(3, result.LineAddresses.Count);
    }

    [Fact]
    public void Disassemble_ThenReassemble_RoundTripsToOriginalBytes()
    {
        // A short but addressing-mode-diverse program: LDX #$00 / loop: LDA $D000,X / STA $0400,X
        // INX / BNE loop / RTS.
        byte[] original =
        [
            0xA2, 0x00,             // LDX #$00
            0xBD, 0x00, 0xD0,       // LDA $D000,X
            0x9D, 0x00, 0x04,       // STA $0400,X
            0xE8,                   // INX
            0xD0, 0xF7,             // BNE loop (back to $0812)
            0x60,                   // RTS
        ];
        ushort start = 0x0810;

        string source = new Asm6502Disassembler().Disassemble(original, start).Source;
        var result = new Asm6502Assembler().Assemble(source);

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        Assert.Equal(start, result.Origin);

        // PrgBytes is a 2-byte load-address header (matching Origin) followed by the code itself,
        // since an ".org" directive suppresses the BASIC loader stub - see Asm6502Assembler.
        Assert.Equal(original, result.PrgBytes![2..]);
    }

    #endregion

    #region Private Methods

    private static string GetLineContaining(string source, string text) =>
        source.Split('\n').Single(line => line.Contains(text));

    #endregion
}

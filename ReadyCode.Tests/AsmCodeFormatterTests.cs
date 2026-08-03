// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using ReadyCode.Formatting;
using Xunit;

namespace ReadyCode.Tests;

/// <summary>
/// Tests for <see cref="AsmCodeFormatter"/>.
/// </summary>
public class AsmCodeFormatterTests
{
    #region Public Methods

    // ── Format ────────────────────────────────────────────────────────────────

    [Fact]
    public void Format_ReindentsAndUppercasesBareMnemonicLine()
    {
        Assert.Equal("        LDA #$01", AsmCodeFormatter.Format("lda #$01", mnemonicIndentColumn: 9, commentAlignColumn: 32));
    }

    [Fact]
    public void Format_LeavesLabelOnlyLineUntouched()
    {
        Assert.Equal("start:", AsmCodeFormatter.Format("start:", mnemonicIndentColumn: 9, commentAlignColumn: 32));
    }

    [Fact]
    public void Format_LeavesLabelAndMnemonicOnSameLineUntouched()
    {
        // Not a "bare" mnemonic line (see TryParseAsmMnemonicLine) - the indent rule only applies
        // when nothing precedes the mnemonic.
        Assert.Equal("start: lda #$01", AsmCodeFormatter.Format("start: lda #$01", mnemonicIndentColumn: 9, commentAlignColumn: 32));
    }

    [Fact]
    public void Format_RealignsInlineComment()
    {
        string result = AsmCodeFormatter.Format("        LDA #$01 ; load it", mnemonicIndentColumn: 9, commentAlignColumn: 20);

        Assert.Equal("        LDA #$01   ; load it", result);
    }

    [Fact]
    public void Format_MovesWholeLineCommentToColumn1()
    {
        Assert.Equal("; a whole-line comment", AsmCodeFormatter.Format("        ; a whole-line comment", mnemonicIndentColumn: 9, commentAlignColumn: 32));
    }

    [Fact]
    public void Format_WholeLineCommentAlreadyAtColumn1IsUnchanged()
    {
        Assert.Equal("; a whole-line comment", AsmCodeFormatter.Format("; a whole-line comment", mnemonicIndentColumn: 9, commentAlignColumn: 32));
    }

    [Fact]
    public void Format_MovesDotOrgDirectiveToColumn1()
    {
        Assert.Equal(".org $c000", AsmCodeFormatter.Format("    .org $c000", mnemonicIndentColumn: 9, commentAlignColumn: 32));
    }

    [Fact]
    public void Format_MovesStarEqualsOriginDirectiveToColumn1()
    {
        Assert.Equal("* = $c000", AsmCodeFormatter.Format("        * = $c000", mnemonicIndentColumn: 9, commentAlignColumn: 32));
    }

    [Fact]
    public void Format_OrgDirectiveWithTrailingCommentIsMovedAndCommentStillRealigned()
    {
        string result = AsmCodeFormatter.Format("    .org $c000 ; program start", mnemonicIndentColumn: 9, commentAlignColumn: 5);

        Assert.Equal(".org $c000  ; program start", result);
    }

    [Fact]
    public void Format_DoesNotMistakeADottedDirectiveNamedOrganForOrg()
    {
        // ".org" must be followed by whitespace or end-of-line, not just be a prefix - same rule
        // AsmLineParser's own IsDirective helper applies.
        Assert.Equal("    .organ $01", AsmCodeFormatter.Format("    .organ $01", mnemonicIndentColumn: 9, commentAlignColumn: 32));
    }

    [Fact]
    public void Format_LeavesBlankLineUntouched()
    {
        Assert.Equal("", AsmCodeFormatter.Format("", mnemonicIndentColumn: 9, commentAlignColumn: 32));
    }

    [Fact]
    public void Format_CodeAlreadyPastCommentColumn_AddsTwoSpacesRatherThanTruncating()
    {
        string result = AsmCodeFormatter.Format("        LDA REALLYLONGCONSTANTNAME ; comment", mnemonicIndentColumn: 9, commentAlignColumn: 10);

        Assert.Equal("        LDA REALLYLONGCONSTANTNAME  ; comment", result);
    }

    [Fact]
    public void Format_UppercasesOnlyTheMnemonicNotTheOperand()
    {
        Assert.Equal("        LDA label", AsmCodeFormatter.Format("lda label", mnemonicIndentColumn: 9, commentAlignColumn: 32));
    }

    [Fact]
    public void Format_AppliesToEveryLineInTheDocument()
    {
        string source = "start:\nlda #$01\nsta $d020\n; done";
        string expected = "start:\n    LDA #$01\n    STA $d020\n; done";

        Assert.Equal(expected, AsmCodeFormatter.Format(source, mnemonicIndentColumn: 5, commentAlignColumn: 32));
    }

    [Fact]
    public void Format_AlreadyFormattedSourceIsUnchanged()
    {
        string source = "        LDA #$01";

        Assert.Equal(source, AsmCodeFormatter.Format(source, mnemonicIndentColumn: 9, commentAlignColumn: 32));
    }

    // ── TryParseAsmMnemonicLine ──────────────────────────────────────────────────

    [Fact]
    public void TryParseAsmMnemonicLine_RecognizesKnownMnemonic()
    {
        Assert.True(AsmCodeFormatter.TryParseAsmMnemonicLine("lda #$01", out string mnemonic, out string rest));
        Assert.Equal("lda", mnemonic);
        Assert.Equal(" #$01", rest);
    }

    [Fact]
    public void TryParseAsmMnemonicLine_RejectsUnknownWord()
    {
        Assert.False(AsmCodeFormatter.TryParseAsmMnemonicLine("notamnemonic $01", out _, out _));
    }

    [Fact]
    public void TryParseAsmMnemonicLine_RejectsLabelOnlyLine()
    {
        Assert.False(AsmCodeFormatter.TryParseAsmMnemonicLine("loop:", out _, out _));
    }

    [Fact]
    public void TryParseAsmMnemonicLine_RejectsBlankLine()
    {
        Assert.False(AsmCodeFormatter.TryParseAsmMnemonicLine("", out _, out _));
    }

    #endregion
}

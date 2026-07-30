// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using ReadyCode.Assembler;
using ReadyCode.Tokenizer;
using Xunit;
using Encoding = System.Text.Encoding;

namespace ReadyCode.Tests;

/// <summary>
/// Tests for <see cref="Asm6502Assembler"/> and its supporting types.
/// </summary>
public class Asm6502AssemblerTests
{
    #region Public Methods

    // Pins the exact byte layout the BASIC loader stub trick depends on: appending machine code
    // right after PrgConverter.ConvertToPrg("10 SYS 2062") must land the first code byte at
    // memory address $080E (decimal 2062), matching the SYS target. If PrgConverter's tokenized
    // output ever changes shape, this test - not a silently-wrong load address - must fail first.
    [Fact]
    public void StubLine_ProducesFifteenByteStubLandingCodeAt080E()
    {
        byte[] stub = new PrgConverter().ConvertToPrg("10 SYS 2062");

        Assert.Equal(15, stub.Length);
        Assert.Equal(new byte[]
        {
            0x01, 0x08,             // load address $0801
            0x0C, 0x08,             // next-line link -> $080C (end-of-program marker)
            0x0A, 0x00,             // line number 10
            0x9E,                   // SYS token
            0x20,                   // space
            0x32, 0x30, 0x36, 0x32, // "2062"
            0x00,                   // line terminator
            0x00, 0x00,             // end-of-program marker
        }, stub);
    }

    // ── Addressing-mode families ─────────────────────────────────────────────────

    [Fact]
    public void Assemble_ImmediateAddressing()
    {
        Assert.Equal(new byte[] { 0xA9, 0x41 }, AssembleCode("LDA #$41"));
    }

    [Fact]
    public void Assemble_ZeroPageAddressing()
    {
        Assert.Equal(new byte[] { 0xA5, 0x02 }, AssembleCode("LDA $02"));
    }

    [Fact]
    public void Assemble_AbsoluteAddressing()
    {
        Assert.Equal(new byte[] { 0xAD, 0x00, 0x02 }, AssembleCode("LDA $0200"));
    }

    [Fact]
    public void Assemble_ZeroPageIndexedXAddressing()
    {
        Assert.Equal(new byte[] { 0xB5, 0x10 }, AssembleCode("LDA $10,X"));
    }

    [Fact]
    public void Assemble_AbsoluteIndexedYAddressing()
    {
        Assert.Equal(new byte[] { 0xB9, 0x00, 0x02 }, AssembleCode("LDA $0200,Y"));
    }

    [Fact]
    public void Assemble_IndirectXAddressing()
    {
        Assert.Equal(new byte[] { 0x81, 0x20 }, AssembleCode("STA ($20,X)"));
    }

    [Fact]
    public void Assemble_IndirectYAddressing()
    {
        Assert.Equal(new byte[] { 0xB1, 0xFB }, AssembleCode("LDA ($FB),Y"));
    }

    [Fact]
    public void Assemble_JmpIndirectAbsolute()
    {
        Assert.Equal(new byte[] { 0x6C, 0x34, 0x12 }, AssembleCode("JMP ($1234)"));
    }

    [Fact]
    public void Assemble_AccumulatorAddressing()
    {
        Assert.Equal(new byte[] { 0x0A }, AssembleCode("ASL A"));
    }

    // ASL/LSR/ROL/ROR have no Implied form - only some real-world sources (Merlin among them)
    // write them with no operand at all to mean the accumulator, same as writing "A" explicitly.
    [Theory]
    [InlineData("ASL", 0x0A)]
    [InlineData("LSR", 0x4A)]
    [InlineData("ROL", 0x2A)]
    [InlineData("ROR", 0x6A)]
    public void Assemble_ShiftRotateMnemonicWithNoOperand_DefaultsToAccumulator(string mnemonic, byte expectedOpcode)
    {
        Assert.Equal(new byte[] { expectedOpcode }, AssembleCode(mnemonic));
    }

    [Fact]
    public void Assemble_ImpliedAddressing()
    {
        Assert.Equal(new byte[] { 0xEA }, AssembleCode("NOP"));
    }

    [Fact]
    public void Assemble_InvalidAddressingModeFails()
    {
        var result = new Asm6502Assembler().Assemble("STX #$00");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.LineNumber == 1 && e.Message.Contains("STX"));
    }

    // ── Label references ─────────────────────────────────────────────────────────

    [Fact]
    public void Assemble_BackwardBranchProducesNegativeOffset()
    {
        // LOOP: DEX (1 byte, at origin) ; BNE LOOP (2 bytes, at origin+1).
        // offset = target - (branchAddr + 2) = origin - (origin + 1 + 2) = -3.
        byte[] code = AssembleCode("LOOP: DEX\nBNE LOOP");

        Assert.Equal(new byte[] { 0xCA, 0xD0, unchecked((byte)-3) }, code);
    }

    [Fact]
    public void Assemble_ForwardJsrResolvesToLaterLabelAddress()
    {
        // JSR SUB (3 bytes, at origin $080E) ; RTS (1 byte, at $0811) ; SUB: RTS (1 byte, at $0812).
        byte[] code = AssembleCode("JSR SUB\nRTS\nSUB: RTS");

        Assert.Equal(new byte[] { 0x20, 0x12, 0x08, 0x60, 0x60 }, code);
    }

    [Fact]
    public void Assemble_LabelOnlyAndCommentOnlyLinesAddNoBytes()
    {
        // A standalone label line and a comment-only line must contribute zero bytes, so START
        // resolves to the very first real instruction's address (the origin).
        byte[] code = AssembleCode("; just a comment\nSTART:\nLDA #$01\n; trailing comment\nSTA START");

        Assert.Equal(new byte[] { 0xA9, 0x01, 0x8D, 0x0E, 0x08 }, code);
    }

    // ── Branch range ──────────────────────────────────────────────────────────────

    [Fact]
    public void Assemble_BranchWithinRangeSucceeds()
    {
        string nops = string.Concat(Enumerable.Repeat("NOP\n", 125));
        byte[] code = AssembleCode($"BNE TARGET\n{nops}TARGET: NOP");

        Assert.Equal(0xD0, code[0]);
        Assert.Equal(125, (sbyte)code[1]);
    }

    [Fact]
    public void Assemble_BranchOutOfRangeFails()
    {
        string nops = string.Concat(Enumerable.Repeat("NOP\n", 200));
        var result = new Asm6502Assembler().Assemble($"BNE TARGET\n{nops}TARGET: NOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("out of range"));
    }

    // ── Malformed input ───────────────────────────────────────────────────────────

    [Fact]
    public void Assemble_UnknownMnemonicFails()
    {
        var result = new Asm6502Assembler().Assemble("FOO $00");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.LineNumber == 1 && e.Message.Contains("FOO"));
    }

    [Fact]
    public void Assemble_UndefinedLabelFails()
    {
        var result = new Asm6502Assembler().Assemble("JMP MISSING");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("MISSING"));
    }

    [Fact]
    public void Assemble_DuplicateLabelFails()
    {
        var result = new Asm6502Assembler().Assemble("X: NOP\nX: NOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate"));
    }

    [Fact]
    public void Assemble_MalformedOperandFails()
    {
        var result = new Asm6502Assembler().Assemble("LDA $ZZ");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.LineNumber == 1);
    }

    // ── Empty source ──────────────────────────────────────────────────────────────

    [Fact]
    public void Assemble_EmptySourceProducesStubOnlyPrg()
    {
        var result = new Asm6502Assembler().Assemble("");

        Assert.True(result.Success);
        Assert.Equal(15, result.PrgBytes!.Length);
    }

    [Fact]
    public void Assemble_WhitespaceOnlySourceProducesStubOnlyPrg()
    {
        var result = new Asm6502Assembler().Assemble("   \n\n  \t\n");

        Assert.True(result.Success);
        Assert.Equal(15, result.PrgBytes!.Length);
    }

    // ── Constants ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Assemble_ZeroPageConstantAssemblesZeroPage()
    {
        // Unlike a label, a constant's value is known immediately, so it's zero-page-eligible
        // exactly like an equivalent bare literal would be.
        byte[] code = AssembleCode("PTR = $fb\nLDA PTR");

        Assert.Equal(new byte[] { 0xA5, 0xFB }, code);
    }

    [Fact]
    public void Assemble_AbsoluteConstantAssemblesAbsolute()
    {
        // The user's real-world KERNAL CHROUT scenario: a constant naming an address above the
        // zero-page range, used as a JSR target.
        byte[] code = AssembleCode("chrout = $ffd2\njsr chrout");

        Assert.Equal(new byte[] { 0x20, 0xD2, 0xFF }, code);
    }

    [Fact]
    public void Assemble_ConstantCanBeUsedBeforeItsDeclarationLine()
    {
        byte[] code = AssembleCode("LDA CHROUT\nCHROUT = $ffd2");

        Assert.Equal(new byte[] { 0xAD, 0xD2, 0xFF }, code);
    }

    [Fact]
    public void Assemble_DuplicateConstantFails()
    {
        var result = new Asm6502Assembler().Assemble("X = $01\nX = $02\nNOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate constant"));
    }

    [Fact]
    public void Assemble_ConstantCollidingWithLabelFails()
    {
        var result = new Asm6502Assembler().Assemble("X = $01\nX: NOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("already defined as a constant"));
    }

    [Fact]
    public void Assemble_ConstantAndLabelDifferingOnlyByCaseAreDistinctSymbols()
    {
        // Symbol names are case-sensitive (unlike mnemonics) - a common real-world style uses an
        // uppercase constant for a tunable value alongside a same-spelled lowercase label, e.g.
        // "DELAY" (a constant) and "delay:" (a subroutine), which must not collide.
        byte[] code = AssembleCode("DELAY = $30\ndelay:\nldx #DELAY\nrts");

        Assert.Equal(new byte[] { 0xA2, 0x30, 0x60 }, code);
    }

    [Fact]
    public void Assemble_ConstantReferencingUndefinedSymbolFails()
    {
        // "SOMETHING" is never defined anywhere - this is not "X = SOMETHING must be a numeric
        // literal" (a symbol reference is valid constant-value syntax - see the ".label" tests
        // below), it's specifically that the referenced symbol doesn't exist.
        var result = new Asm6502Assembler().Assemble("X = SOMETHING\nNOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.LineNumber == 1 && e.Message.Contains("Undefined"));
    }

    // ── ".label" / "*" / symbol-referencing constants ───────────────────────────────
    // KickAssembler-style syntax ported from real-world disassemblies (see astro.asm on GitHub -
    // MyDeveloperThoughts/astroPANICdissassembly).

    [Fact]
    public void Assemble_LabelDirective_PlainLiteral_SameAsBareConstant()
    {
        byte[] code = AssembleCode(".label ptr = $fb\nLDA ptr");

        Assert.Equal(new byte[] { 0xA5, 0xFB }, code);
    }

    [Fact]
    public void Assemble_LabelDirective_CurrentAddress_ResolvesToThatPoint()
    {
        var result = new Asm6502Assembler().Assemble(".org $0810\nNOP\n.label here = *\nLDA #<here");

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        Assert.Equal(0x11, result.PrgBytes![^1]); // low byte of $0811 (NOP is 1 byte, so "here" = $0811)
    }

    [Fact]
    public void Assemble_LabelDirective_SymbolPlusOffset_ResolvesRelativeToEarlierLabel()
    {
        // Mirrors astro.asm's ".label data = *" followed by ".label datsaucxlo = data+1".
        var result = new Asm6502Assembler().Assemble(
            ".org $0810\ndata:\n.byte $01,$02,$03\n.label second = data+1\nLDA second");

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        // "data" is at $0810 (right after .org), so "second" = $0811.
        Assert.Equal(new byte[] { 0xAD, 0x11, 0x08 }, result.PrgBytes![^3..]);
    }

    [Fact]
    public void Assemble_LabelDirective_ReferencingAnotherLabelDirective_ChainsCorrectly()
    {
        var result = new Asm6502Assembler().Assemble(
            ".org $0810\n.label a = *\n.byte $00\n.label b = a+1\nLDA b");

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        Assert.Equal(new byte[] { 0xAD, 0x11, 0x08 }, result.PrgBytes![^3..]);
    }

    [Fact]
    public void Assemble_LabelDirective_ForwardReferenceToLaterLabel_Fails()
    {
        // Order-dependent by design: a "*"/symbol-referencing constant can only see a label
        // already defined earlier in the file - unlike referencing that same label from a regular
        // instruction operand (always fine, resolved in pass 2 once every label is known), or a
        // plain-literal constant referencing another plain-literal constant (both fully resolved
        // up front in pass 0, regardless of order).
        var result = new Asm6502Assembler().Assemble(".label early = loop\nloop:\nNOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.LineNumber == 1 && e.Message.Contains("Undefined"));
    }

    [Fact]
    public void Assemble_LabelDirective_CollidingWithEarlierLabel_Fails()
    {
        // Must be a "*"/symbol-referencing value to exercise this specific check - a plain
        // literal like ".label loop = $10" is resolved in pass 0, before any label address is
        // known, so that collision is instead caught (with a different message) from the label
        // declaration's own side once pass 1 reaches "loop:" - see the test below.
        var result = new Asm6502Assembler().Assemble("loop:\nNOP\n.label loop = *\nNOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("already defined as a label"));
    }

    [Fact]
    public void Assemble_PlainLiteralLabelDirective_CollidingWithLabelAnywhereInFile_Fails()
    {
        // Unlike the symbol-referencing case above, a plain-literal ".label"/constant is resolved
        // in pass 0 before pass 1 runs, so this collision is caught from the label's own
        // declaration line regardless of whether the constant appears before or after it in the
        // file - the same pre-existing behavior a bare "NAME = value" constant already has.
        var result = new Asm6502Assembler().Assemble("loop:\nNOP\n.label loop = $10\nNOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("already defined as a constant"));
    }

    // ── "*" (current program counter) ───────────────────────────────────────────────

    [Fact]
    public void Assemble_StarEqualsOrigin_SameAsDotOrg()
    {
        var result = new Asm6502Assembler().Assemble("* = $c000\nNOP");

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        Assert.True(result.HasExplicitOrigin);
        Assert.Equal(0xC000, result.Origin);
        Assert.Equal(new byte[] { 0x00, 0xC0, 0xEA }, result.PrgBytes);
    }

    [Fact]
    public void Assemble_StarEqualsOrigin_CombinedWithDotOrgFailsAsDuplicate()
    {
        // "* =" sets the exact same OrgAddress field ".org" does, so Asm6502Assembler's existing
        // duplicate-origin check catches this combination for free, regardless of which spelling
        // is used where.
        var result = new Asm6502Assembler().Assemble(".org $c000\n* = $d000\nNOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate '.org' directive"));
    }

    [Fact]
    public void Assemble_StarEqualsOrigin_AfterCodeFails()
    {
        var result = new Asm6502Assembler().Assemble("NOP\n* = $c000");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("'.org' must appear before any code"));
    }

    [Fact]
    public void Assemble_StarEqualsOrigin_NonNumericValueFails()
    {
        var result = new Asm6502Assembler().Assemble("* = somewhere\nNOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.LineNumber == 1 && e.Message.Contains("must be a numeric literal"));
    }

    [Fact]
    public void Assemble_JmpStar_UsesTheJmpInstructionsOwnAddress()
    {
        // "JMP *" is a common self-loop idiom (e.g. a crash trap) - "*" must resolve to the
        // address of the JMP instruction itself, not wherever the assembler happens to be by the
        // time the operand is emitted, and not the file's origin if other code precedes it.
        var result = new Asm6502Assembler().Assemble(".org $c000\nNOP\nJMP *");

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        // NOP is 1 byte, so JMP starts at $c001 - that's what "*" must resolve to.
        Assert.Equal(new byte[] { 0x4C, 0x01, 0xC0 }, result.PrgBytes![3..]);
    }

    [Fact]
    public void Assemble_WordDirectiveWithStar_ResolvesToItsOwnAddress()
    {
        var result = new Asm6502Assembler().Assemble(".org $c000\n.word *");

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        Assert.Equal(new byte[] { 0x00, 0xC0 }, result.PrgBytes![2..]);
    }

    [Fact]
    public void Assemble_ConstantCurrentAddressMinusEarlierLabel_ComputesByteCount()
    {
        // The motivating real-world use case: "size = * - start" computes a data block's byte
        // count once its end address is known, without hand-counting the ".byte" list.
        var result = new Asm6502Assembler().Assemble(
            ".org $c000\nstart:\n.byte $01,$02,$03,$04,$05\nsize = * - start\nLDX #size");

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => $"L{e.LineNumber}: {e.Message}")));
        Assert.Equal(5, result.Constants["size"]);
        Assert.Equal(new byte[] { 0xA2, 0x05 }, result.PrgBytes![^2..]); // LDX #$05
    }

    [Fact]
    public void Assemble_ConstantSymbolMinusEarlierSymbol_ComputesDifference()
    {
        // Generalizes beyond the "*" case: a constant's offset term can be any earlier symbol,
        // not just the current address.
        var result = new Asm6502Assembler().Assemble(
            ".org $c000\nstart:\n.byte $01,$02,$03\nend:\ndiff = end - start\nLDX #diff");

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => $"L{e.LineNumber}: {e.Message}")));
        Assert.Equal(3, result.Constants["diff"]);
    }

    [Fact]
    public void Assemble_ConstantCurrentAddressMinusUndefinedSymbolFails()
    {
        var result = new Asm6502Assembler().Assemble(".org $c000\nsize = * - missing\nNOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.LineNumber == 2 && e.Message.Contains("Undefined label \"missing\""));
    }

    // ── .byte directive ───────────────────────────────────────────────────────────

    [Fact]
    public void Assemble_ByteDirectiveNumericList()
    {
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, AssembleCode(".byte $01, $02, $03"));
    }

    [Fact]
    public void Assemble_ByteDirectiveStringLiteral()
    {
        // Each character becomes its plain character code - no PETSCII remapping.
        Assert.Equal(new byte[] { 0x41, 0x42, 0x00 }, AssembleCode(".byte \"AB\", $00"));
    }

    [Fact]
    public void Assemble_ByteDirectiveStringWithInternalCommaIsNotSplit()
    {
        byte[] expected = [.. Encoding.ASCII.GetBytes("HELLO, WORLD!"), 0x0D, 0x00];

        Assert.Equal(expected, AssembleCode(".byte \"HELLO, WORLD!\", $0d, $00"));
    }

    [Fact]
    public void Assemble_IndexedAddressingOverLabelledByteData()
    {
        // message: (address $0812, 0 bytes) -> .byte "AB",$00 (3 bytes) - LDA message,X must
        // resolve to $0812 even though it labels data, not code.
        byte[] code = AssembleCode("lda message,x\nrts\nmessage:\n.byte \"AB\", $00");

        Assert.Equal(new byte[] { 0xBD, 0x12, 0x08, 0x60, 0x41, 0x42, 0x00 }, code);
    }

    [Fact]
    public void Assemble_UnterminatedByteStringFails()
    {
        var result = new Asm6502Assembler().Assemble(".byte \"AB");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("Unterminated"));
    }

    [Fact]
    public void Assemble_InvalidByteValueFails()
    {
        var result = new Asm6502Assembler().Assemble(".byte $100");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("Invalid .byte value"));
    }

    [Fact]
    public void Assemble_UserKernalChroutIndexedProgramAssembles()
    {
        // The user's real reported program: a KERNAL CHROUT constant, an indexed-addressing
        // print loop over a null-terminated .byte string, verified byte-for-byte end to end.
        string source = """
            ; Hello World using KERNAL chrout

            chrout = $ffd2

                    jsr dispmsg
                    rts

            dispmsg:
                    ldx #0

            loop:
                    lda message,x   ; load next character (indexed addressing)
                    beq done        ; null terminator = we're done
                    jsr chrout      ; print it
                    inx             ; advance index
                    bne loop        ; loop (bne safe here; string won't be 256 chars)

            done:
                    rts

            message:
                    .byte "HELLO, WORLD!", $0d, $00
            """;

        byte[] code = AssembleCode(source);

        byte[] expected =
        [
            0x20, 0x12, 0x08, // jsr dispmsg ($0812)
            0x60,             // rts
            0xA2, 0x00,       // ldx #0
            0xBD, 0x20, 0x08, // lda message,x ($0820)
            0xF0, 0x06,       // beq done (+6)
            0x20, 0xD2, 0xFF, // jsr chrout ($ffd2)
            0xE8,             // inx
            0xD0, 0xF5,       // bne loop (-11)
            0x60,             // rts
            .. Encoding.ASCII.GetBytes("HELLO, WORLD!"),
            0x0D, 0x00,
        ];

        Assert.Equal(expected, code);
    }

    // ── .encoding directive ─────────────────────────────────────────────────────────
    // KickAssembler-style syntax ported from real-world disassemblies (see astro.asm on GitHub -
    // MyDeveloperThoughts/astroPANICdissassembly), which uses ".encoding "petscii_mixed"" ahead
    // of its screen-text ".text" lines.

    [Fact]
    public void Assemble_NoEncodingDirective_ByteStringIsPlainAsciiAsBefore()
    {
        Assert.Equal(new byte[] { 0x53, 0x63, 0x6F, 0x72, 0x65 }, AssembleCode(".text \"Score\""));
    }

    [Fact]
    public void Assemble_EncodingPetsciiMixed_InvertsCaseIntoPetsciisShiftedRange()
    {
        // The user's real scenario: ".encoding "petscii_mixed"" then ".text "Score:"".
        byte[] code = AssembleCode(".encoding \"petscii_mixed\"\n.text \"Score:\"");

        Assert.Equal(new byte[] { 0xD3, 0x43, 0x4F, 0x52, 0x45, 0x3A }, code); // S,c,o,r,e,:
    }

    [Fact]
    public void Assemble_EncodingPetsciiUpper_FoldsToUppercaseRegardlessOfSourceCase()
    {
        byte[] code = AssembleCode(".encoding \"petscii_upper\"\n.text \"Score\"");

        Assert.Equal(new byte[] { 0x53, 0x43, 0x4F, 0x52, 0x45 }, code);
    }

    [Fact]
    public void Assemble_EncodingScreencodeMixed_ConvertsThroughToScreenCodes()
    {
        byte[] code = AssembleCode(".encoding \"screencode_mixed\"\n.text \"Aa\"");

        Assert.Equal(new byte[] { 0x41, 0x01 }, code);
    }

    [Fact]
    public void Assemble_EncodingAppliesOnlyFromItsPointOnward()
    {
        // The first .text (before ".encoding") stays plain ASCII; only the second one, after
        // ".encoding", gets remapped.
        byte[] code = AssembleCode(".text \"A\"\n.encoding \"petscii_mixed\"\n.text \"A\"");

        Assert.Equal(new byte[] { 0x41, 0xC1 }, code);
    }

    [Fact]
    public void Assemble_EncodingBackToAscii_RestoresPlainCharacterCodes()
    {
        byte[] code = AssembleCode(".encoding \"petscii_mixed\"\n.text \"A\"\n.encoding \"ascii\"\n.text \"A\"");

        Assert.Equal(new byte[] { 0xC1, 0x41 }, code);
    }

    [Fact]
    public void Assemble_EncodingDoesNotAffectNumericByteValues()
    {
        byte[] code = AssembleCode(".encoding \"petscii_mixed\"\n.byte $00, \"a\", $00");

        Assert.Equal(new byte[] { 0x00, 0x41, 0x00 }, code);
    }

    [Fact]
    public void Assemble_UnknownEncodingModeFails()
    {
        var result = new Asm6502Assembler().Assemble(".encoding \"bogus_mode\"\nNOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("Unknown '.encoding' mode"));
    }

    [Fact]
    public void Assemble_UnquotedEncodingValueFails()
    {
        var result = new Asm6502Assembler().Assemble(".encoding petscii_mixed\nNOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("must be a quoted string"));
    }

    [Fact]
    public void Assemble_AstroPanicStyleSnippet_CombinesLabelOffsetsShiftMnemonicsAndEncoding()
    {
        // A representative excerpt in the shape of the user's real Merlin/KickAssembler port
        // (astro.asm on GitHub - MyDeveloperThoughts/astroPANICdissassembly): a ".label"
        // current-address table, an implicit-accumulator shift, and ".encoding"-driven text,
        // all assembling together without errors.
        string source = """
            .org $c000

                    lda #$00
                    asl
                    rol
                    lsr

                    jmp start

            start:
            .label data      = *
            .label datplyrx8 = data
            .label datsaucy  = data+15

                    .byte $00,$01,$02,$03,$04,$05,$06,$07,$08,$09,$0a,$0b,$0c,$0d,$0e

                    lda datplyrx8
                    ldy datsaucy,x

                    .encoding "petscii_mixed"
            shstxt: .text "Score:"
            """;

        var result = new Asm6502Assembler().Assemble(source);

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => $"L{e.LineNumber}: {e.Message}")));

        // start: (a real label) sits after "lda #$00" (2 bytes) + "asl"/"rol"/"lsr" (1 byte each,
        // implicit accumulator) + "jmp start" (3 bytes) = $c000 + 8 = $c008. "data" (a ".label",
        // i.e. a constant) is declared right there via "*", so it matches; "datsaucy" is 15 past it.
        Assert.Equal(0xC008, result.Labels["start"]);
        Assert.Equal(0xC008, result.Constants["data"]);
        Assert.Equal(0xC008, result.Constants["datplyrx8"]);
        Assert.Equal(0xC017, result.Constants["datsaucy"]); // data ($c008) + 15
    }

    // ── Low/high byte immediates and symbol offsets ──────────────────────────────

    [Fact]
    public void Assemble_ImmediateLowAndHighByteOfLabel()
    {
        // TARGET resolves to $0812 (after the two 2-byte immediate loads before it).
        byte[] code = AssembleCode("lda #<TARGET\nlda #>TARGET\nTARGET:\nrts");

        Assert.Equal(new byte[] { 0xA9, 0x12, 0xA9, 0x08, 0x60 }, code);
    }

    [Fact]
    public void Assemble_ImmediateLowAndHighByteOfConstant()
    {
        byte[] code = AssembleCode("FOO = $1234\nlda #<FOO\nlda #>FOO");

        Assert.Equal(new byte[] { 0xA9, 0x34, 0xA9, 0x12 }, code);
    }

    [Fact]
    public void Assemble_ConstantPlusOffsetAssemblesZeroPage()
    {
        // msgptr+1 = $fc, still zero-page - a known constant's offset value is available
        // immediately, unlike a label's.
        byte[] code = AssembleCode("msgptr = $fb\nsta msgptr+1");

        Assert.Equal(new byte[] { 0x85, 0xFC }, code);
    }

    [Fact]
    public void Assemble_LabelPlusOffsetStillAssemblesAbsolute()
    {
        // LABEL resolves to $0812; LABEL+1 = $0813, but a label reference (deferred, unlike a
        // constant) always assembles absolute regardless of the offset - same rule as a bare
        // label reference.
        byte[] code = AssembleCode("sta LABEL+1\nrts\nLABEL:\nrts");

        Assert.Equal(new byte[] { 0x8D, 0x13, 0x08, 0x60, 0x60 }, code);
    }

    [Fact]
    public void Assemble_UserZeroPagePointerProgramAssembles()
    {
        // The user's real reported program: VIC-20-style color pokes, a zero-page pointer
        // (msgptr/msgptr+1) built from #<label/#>label, and two messages printed through it via
        // indirect-indexed addressing - verified byte-for-byte end to end.
        string source = """
            ; Hello World using KERNAL chrout
            chrout  = $ffd2
            msgptr  = $fb          ; zero page pointer (uses $fb and $fc)

                    ; Set VIC-20 style colors
                    lda #3
                    sta $d020
                    lda #1
                    sta $d021
                    lda #6
                    sta $0286

                    ; Print message 1
                    lda #<message1  ; low byte of address
                    sta msgptr
                    lda #>message1  ; high byte of address
                    sta msgptr+1
                    jsr printmsg

                    ; Print message 2
                    lda #<message2
                    sta msgptr
                    lda #>message2
                    sta msgptr+1
                    jsr printmsg

                    rts

            ; printmsg: prints null-terminated string pointed to by msgptr/msgptr+1
            printmsg:
                    ldy #0

            loop:
                    lda (msgptr),y  ; load byte from [msgptr + Y]
                    beq done        ; null terminator = done
                    jsr chrout
                    iny
                    bne loop        ; safe for strings < 256 chars
            done:
                    rts

            message1:
                    .byte $93, "HELLO, WORLD!", $0d, $0d, $00

            message2:
                    .byte "READYCODE ASSEMBLY SUPPORT COMING SOON!", $0d, $00
            """;

        byte[] code = AssembleCode(source);

        byte[] expected =
        [
            0xA9, 0x03,       // lda #3
            0x8D, 0x20, 0xD0, // sta $d020
            0xA9, 0x01,       // lda #1
            0x8D, 0x21, 0xD0, // sta $d021
            0xA9, 0x06,       // lda #6
            0x8D, 0x86, 0x02, // sta $0286

            0xA9, 0x41,       // lda #<message1 ($0841)
            0x85, 0xFB,       // sta msgptr
            0xA9, 0x08,       // lda #>message1
            0x85, 0xFC,       // sta msgptr+1
            0x20, 0x34, 0x08, // jsr printmsg ($0834)

            0xA9, 0x52,       // lda #<message2 ($0852)
            0x85, 0xFB,       // sta msgptr
            0xA9, 0x08,       // lda #>message2
            0x85, 0xFC,       // sta msgptr+1
            0x20, 0x34, 0x08, // jsr printmsg

            0x60,             // rts

            0xA0, 0x00,       // ldy #0 (printmsg:)

            0xB1, 0xFB,       // lda (msgptr),y (loop:)
            0xF0, 0x06,       // beq done (+6)
            0x20, 0xD2, 0xFF, // jsr chrout
            0xC8,             // iny
            0xD0, 0xF6,       // bne loop (-10)

            0x60,             // rts (done:)

            0x93,             // message1:
            .. Encoding.ASCII.GetBytes("HELLO, WORLD!"),
            0x0D, 0x0D, 0x00,

            .. Encoding.ASCII.GetBytes("READYCODE ASSEMBLY SUPPORT COMING SOON!"),
            0x0D, 0x00,
        ];

        Assert.Equal(expected, code);
    }

    // ── .text directive ───────────────────────────────────────────────────────────

    [Fact]
    public void Assemble_TextDirectiveMatchesByteDirectiveGrammar()
    {
        // ".text" is a pure alias of ".byte" - same grammar, same output.
        Assert.Equal(AssembleCode(".byte \"HI\""), AssembleCode(".text \"HI\""));
    }

    [Fact]
    public void Assemble_TextDirectiveMixedStringAndNumeric()
    {
        Assert.Equal(new byte[] { 0x48, 0x49, 0x00 }, AssembleCode(".text \"HI\", 0"));
    }

    // ── .org directive ────────────────────────────────────────────────────────────

    [Fact]
    public void Assemble_NoOrgDirectiveDefaultsToFixedOriginWithStub()
    {
        // Regression guard: a program with no ".org" must produce byte-identical output to
        // before ".org" support existed - the 15-byte BASIC stub followed by code at $080E.
        var result = new Asm6502Assembler().Assemble("LDA #$01");

        Assert.True(result.Success);
        Assert.Equal(0x080E, result.Origin);
        Assert.Equal(17, result.PrgBytes!.Length);
        Assert.Equal(new byte[] { 0xA9, 0x01 }, result.PrgBytes[15..]);
    }

    [Fact]
    public void Assemble_OrgDirectiveSetsOriginAndOmitsStub()
    {
        var result = new Asm6502Assembler().Assemble(".org $2000\nLDA #$01");

        Assert.True(result.Success);
        Assert.Equal(0x2000, result.Origin);
        Assert.Equal(new byte[] { 0x00, 0x20, 0xA9, 0x01 }, result.PrgBytes);
    }

    [Fact]
    public void Assemble_OrgDirectiveRetargetsLabelAddresses()
    {
        var result = new Asm6502Assembler().Assemble(".org $2000\nSTART:\nLDA #$01\nJMP START");

        Assert.True(result.Success);
        Assert.Equal(new byte[] { 0x00, 0x20, 0xA9, 0x01, 0x4C, 0x00, 0x20 }, result.PrgBytes);
    }

    [Fact]
    public void Assemble_OrgDirectiveAfterCodeFails()
    {
        var result = new Asm6502Assembler().Assemble("NOP\n.org $2000");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("must appear before any code"));
    }

    [Fact]
    public void Assemble_DuplicateOrgDirectiveFails()
    {
        var result = new Asm6502Assembler().Assemble(".org $2000\n.org $3000\nNOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate '.org'"));
    }

    [Fact]
    public void Assemble_OrgDirectiveWithSymbolValueFails()
    {
        var result = new Asm6502Assembler().Assemble(".org LABEL\nLABEL: NOP");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("numeric literal"));
    }

    // ── .word directive ───────────────────────────────────────────────────────────

    [Fact]
    public void Assemble_WordDirectiveNumericLiteral()
    {
        Assert.Equal(new byte[] { 0x34, 0x12 }, AssembleCode(".word $1234"));
    }

    [Fact]
    public void Assemble_WordDirectiveJumpTableOfForwardLabels()
    {
        // TABLE: (origin) -> .word ENTRY1, ENTRY2 (4 bytes) -> ENTRY1: NOP ($0812) -> ENTRY2: NOP ($0813).
        byte[] code = AssembleCode("TABLE:\n.word ENTRY1, ENTRY2\nENTRY1: NOP\nENTRY2: NOP");

        Assert.Equal(new byte[] { 0x12, 0x08, 0x13, 0x08, 0xEA, 0xEA }, code);
    }

    [Fact]
    public void Assemble_WordDirectiveLabelPlusOffset()
    {
        byte[] code = AssembleCode(".word LABEL+1\nLABEL:\nNOP");

        Assert.Equal(new byte[] { 0x11, 0x08, 0xEA }, code);
    }

    [Fact]
    public void Assemble_WordDirectiveUndefinedLabelFails()
    {
        var result = new Asm6502Assembler().Assemble(".word MISSING");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("MISSING"));
    }

    [Fact]
    public void Assemble_WordDirectiveRequiresAtLeastOneValue()
    {
        var result = new Asm6502Assembler().Assemble(".word");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains(".word requires at least one value"));
    }

    [Fact]
    public void Assemble_WordDirectiveInvalidValueFails()
    {
        var result = new Asm6502Assembler().Assemble(".word $ZZ");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("Invalid .word value"));
    }

    // ── Regression guard ──────────────────────────────────────────────────────────
    // OpcodeTable and AsmTokens are independent tables (encoding vs. reference metadata) that
    // must still describe the exact same 56 mnemonics - this catches either one drifting.

    [Fact]
    public void OpcodeTable_MnemonicSetMatchesAsmTokens()
    {
        var opcodeMnemonics = OpcodeTable.Modes.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tokenMnemonics = AsmTokens.Mnemonics.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(tokenMnemonics, opcodeMnemonics);
    }

    // A bare "#" with nothing after it (the normal, momentary state of typing "LDA #5" one
    // keystroke at a time, since diagnostics/symbol-indexing re-parse on every keystroke) used to
    // throw ArgumentOutOfRangeException out of AsmLineParser.TrySplitOffset instead of reporting a
    // malformed-operand error - crashing the app on ordinary mid-typing input.
    [Theory]
    [InlineData("LDA #")]
    [InlineData("LDA #<")]
    [InlineData("LDA #>")]
    public void Assemble_EmptyImmediateOperand_ReportsErrorRatherThanThrowing(string line)
    {
        var result = new Asm6502Assembler().Assemble(".org $C000\n" + line);

        Assert.False(result.Success);
        Assert.Single(result.Errors);
    }

    // ── Standalone output mode ────────────────────────────────────────────────────

    [Fact]
    public void Assemble_StandaloneOutput_NoOrg_UsesDefaultOriginNoStub()
    {
        var result = new Asm6502Assembler().Assemble("NOP", standaloneOutput: true, defaultOriginAddress: 0xC000);

        Assert.True(result.Success);
        Assert.Equal(0xC000, result.Origin);
        Assert.Equal(new byte[] { 0x00, 0xC0, 0xEA }, result.PrgBytes); // header + NOP, no BASIC stub
    }

    [Fact]
    public void Assemble_StandaloneOutput_ExplicitOrg_OverridesDefaultOrigin()
    {
        var result = new Asm6502Assembler().Assemble(".org $D000\nNOP", standaloneOutput: true, defaultOriginAddress: 0xC000);

        Assert.True(result.Success);
        Assert.Equal(0xD000, result.Origin);
        Assert.Equal(new byte[] { 0x00, 0xD0, 0xEA }, result.PrgBytes);
    }

    [Fact]
    public void Assemble_NonStandaloneOutput_NoOrg_StillUsesBasicStub()
    {
        var result = new Asm6502Assembler().Assemble("NOP", standaloneOutput: false, defaultOriginAddress: 0xC000);

        Assert.True(result.Success);
        Assert.Equal(0x080E, result.Origin);
        Assert.Equal(15, result.PrgBytes!.Length - 1); // 15-byte stub + 1-byte NOP
    }

    // ── HasExplicitOrigin ──────────────────────────────────────────────────────────

    [Fact]
    public void Assemble_ExplicitOrg_HasExplicitOriginIsTrue()
    {
        var result = new Asm6502Assembler().Assemble(".org $C000\nNOP");

        Assert.True(result.HasExplicitOrigin);
    }

    [Fact]
    public void Assemble_NoOrg_HasExplicitOriginIsFalse()
    {
        var result = new Asm6502Assembler().Assemble("NOP");
        Assert.False(result.HasExplicitOrigin);

        var standaloneResult = new Asm6502Assembler().Assemble("NOP", standaloneOutput: true, defaultOriginAddress: 0xC000);
        Assert.False(standaloneResult.HasExplicitOrigin);
    }

    [Fact]
    public void Assemble_ExplicitOrg_ButOtherErrors_HasExplicitOriginStillTrue()
    {
        var result = new Asm6502Assembler().Assemble(".org $C000\nBOGUS");

        Assert.False(result.Success);
        Assert.True(result.HasExplicitOrigin);
    }

    // ── Listing entries ───────────────────────────────────────────────────────────

    [Fact]
    public void Assemble_ListingEntries_OneEntryPerCodeLine_WithCorrectAddresses()
    {
        var result = new Asm6502Assembler().Assemble(".org $0810\nNOP\nLDA #$00", standaloneOutput: false);

        Assert.True(result.Success);
        Assert.Equal(2, result.ListingEntries.Count);
        Assert.Equal(2, result.ListingEntries[0].LineNumber);
        Assert.Equal(0x0810, result.ListingEntries[0].Address);
        Assert.Equal(new byte[] { 0xEA }, result.ListingEntries[0].Bytes);
        Assert.Equal(3, result.ListingEntries[1].LineNumber);
        Assert.Equal(0x0811, result.ListingEntries[1].Address);
        Assert.Equal(new byte[] { 0xA9, 0x00 }, result.ListingEntries[1].Bytes);
    }

    [Fact]
    public void Assemble_ListingEntries_SkipsLabelAndCommentLines()
    {
        var result = new Asm6502Assembler().Assemble(".org $0810\nloop:\n; a comment\nNOP", standaloneOutput: false);

        Assert.True(result.Success);
        Assert.Single(result.ListingEntries);
        Assert.Equal(4, result.ListingEntries[0].LineNumber);
    }

    [Fact]
    public void Assemble_ListingEntries_ByteDirective_RecordsAllBytesOnOneLine()
    {
        var result = new Asm6502Assembler().Assemble(".org $0810\n.byte $01,$02,$03", standaloneOutput: false);

        Assert.True(result.Success);
        Assert.Single(result.ListingEntries);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, result.ListingEntries[0].Bytes);
    }

    #endregion

    #region Private Methods

    // Assembles source expected to succeed and returns just the machine-code portion (the
    // 15-byte BASIC loader stub stripped off), so tests can assert on the assembled bytes alone.
    private static byte[] AssembleCode(string source)
    {
        var result = new Asm6502Assembler().Assemble(source);
        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => $"L{e.LineNumber}: {e.Message}")));
        return result.PrgBytes![15..];
    }

    #endregion
}

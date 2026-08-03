// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using ReadyCode.Assembler;
using Xunit;

namespace ReadyCode.Tests;

/// <summary>
/// Tests for <see cref="AsmTextEncoder"/>.
/// </summary>
public class AsmTextEncoderTests
{
    #region Public Methods

    [Theory]
    [InlineData('A', 0x41)]
    [InlineData('a', 0x61)]
    [InlineData(':', 0x3A)]
    [InlineData(' ', 0x20)]
    public void Encode_Ascii_IsPlainCharacterCode(char c, byte expected)
    {
        Assert.Equal(expected, AsmTextEncoder.Encode(c, AsmTextEncoding.Ascii));
    }

    [Theory]
    [InlineData('A', 0x41)] // already uppercase - unchanged
    [InlineData('a', 0x41)] // lowercase source folds to the same uppercase PETSCII code
    [InlineData(':', 0x3A)] // non-letters pass through unchanged - no case to fold
    public void Encode_PetsciiUpper_FoldsEveryLetterToUppercaseRange(char c, byte expected)
    {
        Assert.Equal(expected, AsmTextEncoder.Encode(c, AsmTextEncoding.PetsciiUpper));
    }

    [Theory]
    [InlineData('A', 0xC1)] // uppercase source -> shifted range (displays uppercase in charset 2)
    [InlineData('a', 0x41)] // lowercase source -> plain $41-$5A range (displays lowercase in charset 2)
    [InlineData(':', 0x3A)] // non-letters pass through unchanged
    public void Encode_PetsciiMixed_InvertsCaseIntoPetsciisShiftedRange(char c, byte expected)
    {
        Assert.Equal(expected, AsmTextEncoder.Encode(c, AsmTextEncoding.PetsciiMixed));
    }

    [Theory]
    [InlineData('A', 0x01)]
    [InlineData('a', 0x01)]
    public void Encode_ScreenCodeUpper_MatchesPetsciiUpperConvertedToScreenCode(char c, byte expected)
    {
        Assert.Equal(expected, AsmTextEncoder.Encode(c, AsmTextEncoding.ScreenCodeUpper));
    }

    [Theory]
    [InlineData('A', 0x41)] // uppercase source lands in charset 2's uppercase screen-code range
    [InlineData('a', 0x01)] // lowercase source lands in charset 2's lowercase screen-code range
    public void Encode_ScreenCodeMixed_MatchesPetsciiMixedConvertedToScreenCode(char c, byte expected)
    {
        Assert.Equal(expected, AsmTextEncoder.Encode(c, AsmTextEncoding.ScreenCodeMixed));
    }

    #endregion
}

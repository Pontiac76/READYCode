// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using ReadyCode.Assembler;
using ReadyCode.Diagnostics;
using Xunit;

namespace ReadyCode.Tests;

/// <summary>
/// Tests for <see cref="AsmSymbolIndex"/>.
/// </summary>
public class AsmSymbolIndexTests
{
    #region Public Methods

    [Fact]
    public void Analyze_FindsLabelDefinitionAndReference()
    {
        var occurrences = AsmSymbolIndex.Analyze("loop:\nJMP loop");

        Assert.Contains(occurrences, o => o.Name == "loop" && o.LineNumber == 1 && o.Kind == AsmSymbolKind.LabelDefinition);
        Assert.Contains(occurrences, o => o.Name == "loop" && o.LineNumber == 2 && o.Kind == AsmSymbolKind.Reference);
    }

    [Fact]
    public void Analyze_FindsConstantDefinition()
    {
        var occurrences = AsmSymbolIndex.Analyze("chrout = $ffd2");

        Assert.Contains(occurrences, o => o.Name == "chrout" && o.LineNumber == 1 && o.Kind == AsmSymbolKind.ConstantDefinition);
    }

    [Fact]
    public void Analyze_ExcludesTheCurrentAddressPseudoSymbol()
    {
        var occurrences = AsmSymbolIndex.Analyze(".org $c000\nJMP *\nsize = * - start\nstart:\nNOP");

        Assert.DoesNotContain(occurrences, o => o.Name == "*");
    }

    [Fact]
    public void Analyze_OverloadTakingParsedLines_MatchesTheStringOverload()
    {
        // The whole point of the IReadOnlyList<ParsedAsmLine> overload is to let a caller that
        // already parsed the source (e.g. via Asm6502Assembler.Assemble) skip re-parsing it just
        // to index it - the two must therefore report identical results for the same source.
        string source = ".org $c000\nstart:\ndata = start+1\nLDA data\nJMP start\n.word data";

        var parser = new AsmLineParser();
        string[] rawLines = source.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var parsedLines = new List<ParsedAsmLine>();
        for (int i = 0; i < rawLines.Length; i++)
            parsedLines.Add(parser.ParseLine(rawLines[i], i + 1));

        Assert.Equal(AsmSymbolIndex.Analyze(source), AsmSymbolIndex.Analyze(parsedLines));
    }

    [Fact]
    public void Analyze_UsingAssemblyResultParsedLines_MatchesAnalyzingTheSourceDirectly()
    {
        // Integration check for the RunAsmSymbolIndex reuse path: AssemblyResult.ParsedLines from
        // a real Assemble() call must index identically to re-parsing the same source from scratch.
        string source = "start:\nLDA #$01\nSTA target\ntarget:\nNOP";

        var result = new Asm6502Assembler().Assemble(source);

        Assert.Equal(AsmSymbolIndex.Analyze(source), AsmSymbolIndex.Analyze(result.ParsedLines));
    }

    #endregion
}

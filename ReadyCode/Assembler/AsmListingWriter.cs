// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;

namespace ReadyCode.Assembler;

/// <summary>
/// Generates a listing file (address, raw bytes, and original source side by side) from a
/// successful <see cref="Asm6502Assembler.Assemble"/> result - see
/// <c>AppSettings.AsmGenerateListingFile</c>.
/// </summary>
public static class AsmListingWriter
{
    #region Private Fields

    // Width of the "$XXXX  XX XX XX  " prefix column, before the original source line - lines
    // with no listing entry (labels, comments, blank lines, ".org") get this much blank padding
    // instead, so every source line still lines up in the same column regardless.
    private const int _prefixWidth = 20;

    #endregion

    #region Public Methods

    /// <summary>
    /// Builds the listing text for the given source and its assembly result.
    /// </summary>
    /// <param name="source">The original assembly source text.</param>
    /// <param name="result">A successful result from <see cref="Asm6502Assembler.Assemble"/>.</param>
    /// <returns>The listing text, one line per source line.</returns>
    public static string Generate(string source, AssemblyResult result)
    {
        var byLine = new Dictionary<int, AsmListingEntry>();
        foreach (var entry in result.ListingEntries)
            byLine[entry.LineNumber] = entry;

        string[] rawLines = source.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var sb = new StringBuilder();

        for (int i = 0; i < rawLines.Length; i++)
        {
            int lineNumber = i + 1;
            if (byLine.TryGetValue(lineNumber, out var listingEntry))
            {
                string bytesText = string.Join(' ', listingEntry.Bytes.Select(b => b.ToString("X2")));
                string prefix = $"${listingEntry.Address:X4}  {bytesText}";
                sb.Append(prefix.Length < _prefixWidth ? prefix.PadRight(_prefixWidth) : prefix + "  ");
            }
            else
            {
                sb.Append(' ', _prefixWidth);
            }

            sb.Append(rawLines[i]).Append('\n');
        }

        return sb.ToString();
    }

    #endregion
}

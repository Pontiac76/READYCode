// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace ReadyCode.Editor;

/// <summary>
/// A gutter margin showing sequential editor line numbers (1, 2, 3, ...) for assembly source,
/// which - unlike BASIC - has no line numbers embedded in the text itself. Numbers are always
/// right-aligned; when <see cref="ZeroPadWidth"/> is greater than zero, they're additionally
/// left-padded with zeros to that many digits, mirroring the BASIC line-number padding setting.
/// </summary>
public class AsmLineNumberMargin : AbstractMargin
{
    #region Private Fields

    private const double _rightPadding = 4;

    // Immutable, so built once rather than per glyph/render pass.
    private static readonly Typeface _typeface = new("Consolas");

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets or sets the brush used to draw line numbers.
    /// </summary>
    public Brush TextBrush { get; set; } = Brushes.Gray;

    /// <summary>
    /// Gets or sets the number of digits to zero-pad line numbers to, or 0 to show each number
    /// at its natural width instead.
    /// </summary>
    public int ZeroPadWidth { get; set; }

    /// <summary>
    /// Gets or sets the font size line numbers are drawn at, matching the editor's own font size.
    /// </summary>
    public double FontSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the memory address each document line represents, keyed by 1-based line
    /// number, or null to show ordinary sequential line numbers instead. Set for a disassembly
    /// tab (see <see cref="ReadyCode.Models.EditorTab.DisassemblyLineAddresses"/>) so the gutter
    /// shows real addresses; a line with no entry (e.g. the ".org" line) is left blank rather
    /// than falling back to a line number that would misleadingly imply a real address.
    /// </summary>
    public IReadOnlyDictionary<int, ushort>? LineAddresses { get; set; }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Measures the margin wide enough to fit the document's largest line number (or the
    /// zero-padded width, if that's wider).
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (LineAddresses != null)
        {
            double addressWidth = CreateFormattedText("$FFFF").Width + _rightPadding;
            return new Size(addressWidth, 0);
        }

        int lineCount = Document?.LineCount ?? 1;
        int digitCount = Math.Max(ZeroPadWidth, lineCount.ToString(CultureInfo.InvariantCulture).Length);
        double width = CreateFormattedText(new string('8', digitCount)).Width + _rightPadding;
        return new Size(width, 0);
    }

    /// <summary>
    /// Draws the line number for every currently visible line, right-aligned against the
    /// margin's right edge.
    /// </summary>
    /// <param name="drawingContext">The drawing context to render into.</param>
    protected override void OnRender(DrawingContext drawingContext)
    {
        var textView = TextView;
        if (textView == null || !textView.VisualLinesValid) return;

        foreach (VisualLine line in textView.VisualLines)
        {
            int lineNumber = line.FirstDocumentLine.LineNumber;

            string text;
            if (LineAddresses != null)
            {
                if (!LineAddresses.TryGetValue(lineNumber, out ushort address)) continue;
                text = "$" + address.ToString("X4", CultureInfo.InvariantCulture);
            }
            else
            {
                text = ZeroPadWidth > 0
                    ? lineNumber.ToString(CultureInfo.InvariantCulture).PadLeft(ZeroPadWidth, '0')
                    : lineNumber.ToString(CultureInfo.InvariantCulture);
            }

            var formattedText = CreateFormattedText(text);

            // Centered within the visual line's real height rather than anchored to its top -
            // FormattedText's own line-height metrics for a given font/size don't necessarily
            // match AvalonEdit's, so top-anchoring alone left the number visibly higher than the
            // code text beside it (most noticeable on a disassembly tab, where every single line
            // has a long trailing comment).
            double y = line.VisualTop - textView.VerticalOffset + (line.Height - formattedText.Height) / 2;
            drawingContext.DrawText(formattedText, new Point(ActualWidth - _rightPadding - formattedText.Width, y));
        }
    }

    /// <summary>
    /// Hooks/unhooks the text view's redraw-triggering events so the margin's size and content
    /// stay in sync as the document scrolls or is edited.
    /// </summary>
    /// <param name="oldTextView">The text view being detached, or null.</param>
    /// <param name="newTextView">The text view being attached, or null.</param>
    protected override void OnTextViewChanged(TextView? oldTextView, TextView? newTextView)
    {
        if (oldTextView != null)
        {
            oldTextView.VisualLinesChanged -= TextView_VisualLinesChanged;
            oldTextView.ScrollOffsetChanged -= TextView_ScrollOffsetChanged;
        }

        base.OnTextViewChanged(oldTextView, newTextView);

        if (newTextView != null)
        {
            newTextView.VisualLinesChanged += TextView_VisualLinesChanged;
            newTextView.ScrollOffsetChanged += TextView_ScrollOffsetChanged;
        }

        InvalidateMeasure();
        InvalidateVisual();
    }

    #endregion

    #region Private Methods

    private void TextView_VisualLinesChanged(object? sender, EventArgs e)
    {
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void TextView_ScrollOffsetChanged(object? sender, EventArgs e) => InvalidateVisual();

    private FormattedText CreateFormattedText(string text) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            _typeface, FontSize, TextBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);

    #endregion
}

// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReadyCode.Views;

/// <summary>
/// Address-range toolbar shown above a disassembly tab, raising an event for the host window to
/// read memory and disassemble it - mirrors <see cref="FindBarControl"/>'s division of labor,
/// where the control only owns its own input fields and the host does the actual work.
/// </summary>
public partial class DisassemblyToolbarControl : UserControl
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DisassemblyToolbarControl"/> class.
    /// </summary>
    public DisassemblyToolbarControl()
    {
        InitializeComponent();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Occurs when the user clicks Disassemble or presses Enter in either address box.
    /// </summary>
    public event EventHandler? DisassembleRequested;

    #endregion

    #region Public Methods

    /// <summary>
    /// Focuses and selects the contents of the Start address box, ready for the user to type
    /// over it immediately.
    /// </summary>
    public void FocusStartAddress()
    {
        StartAddressBox.Focus();
        StartAddressBox.SelectAll();
    }

    /// <summary>
    /// Parses and validates the Start/End address boxes.
    /// </summary>
    /// <param name="start">The parsed start address, if valid.</param>
    /// <param name="end">The parsed end address, if valid.</param>
    /// <param name="error">A user-facing message describing the problem, if invalid.</param>
    /// <returns>True if both addresses are valid and the range is non-empty.</returns>
    public bool TryGetAddressRange(out ushort start, out ushort end, out string? error)
    {
        end = 0;

        if (!TryParseAddress(StartAddressBox.Text, out start))
        {
            error = "Invalid start address - use hex, e.g. $0810.";
            return false;
        }

        if (!TryParseAddress(EndAddressBox.Text, out end))
        {
            error = "Invalid end address - use hex, e.g. $0810.";
            return false;
        }

        if (end < start)
        {
            error = "End address must be at or after the start address.";
            return false;
        }

        error = null;
        return true;
    }

    #endregion

    #region Private Methods

    private static bool TryParseAddress(string text, out ushort value)
    {
        string trimmed = text.Trim();
        if (trimmed.StartsWith('$')) trimmed = trimmed[1..];
        return ushort.TryParse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    private void DisassembleButton_Click(object sender, RoutedEventArgs e) => DisassembleRequested?.Invoke(this, EventArgs.Empty);

    private void AddressBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) DisassembleRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}

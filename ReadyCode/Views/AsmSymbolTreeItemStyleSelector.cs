// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using ReadyCode.Models;

namespace ReadyCode.Views;

/// <summary>
/// Picks the Symbol Explorer tree's <see cref="TreeViewItem"/> style by the bound data's type -
/// <see cref="AsmSymbolGroupInfo"/> and <see cref="AsmSymbolInfo"/> rows have an
/// <c>IsExpanded</c> property to bind for expand/collapse state, but the leaf
/// <see cref="AsmSymbolOccurrenceInfo"/> rows don't (they have no children), so they can't all
/// share one style with a single <c>IsExpanded</c> binding.
/// </summary>
public class AsmSymbolTreeItemStyleSelector : StyleSelector
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the style applied to an <see cref="AsmSymbolGroupInfo"/> or
    /// <see cref="AsmSymbolInfo"/> row.
    /// </summary>
    public Style? ExpandableStyle { get; set; }

    /// <summary>
    /// Gets or sets the style applied to an <see cref="AsmSymbolOccurrenceInfo"/> leaf row.
    /// </summary>
    public Style? LeafStyle { get; set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns <see cref="ExpandableStyle"/> or <see cref="LeafStyle"/> depending on the bound
    /// item's type.
    /// </summary>
    /// <param name="item">The bound data item.</param>
    /// <param name="container">The container the style will be applied to.</param>
    public override Style? SelectStyle(object item, DependencyObject container) => item switch
    {
        AsmSymbolGroupInfo or AsmSymbolInfo => ExpandableStyle,
        _ => LeafStyle,
    };

    #endregion
}

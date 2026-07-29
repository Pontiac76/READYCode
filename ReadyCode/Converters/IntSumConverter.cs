// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Windows.Data;

namespace ReadyCode.Converters;

/// <summary>
/// Sums every bound integer value - used to combine multiple collections' Count bindings into a
/// single total, such as the Symbol Explorer header's combined constant/label count.
/// </summary>
public class IntSumConverter : IMultiValueConverter
{
    #region Public Methods

    /// <inheritdoc/>
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        => values.OfType<int>().Sum();

    /// <inheritdoc/>
    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    #endregion
}

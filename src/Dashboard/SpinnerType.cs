// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.Benchmarks;

/// <summary>
/// Represents a spinner type.
/// </summary>
public enum SpinnerType
{
    /// <summary>
    /// A spinner that grows and shrinks.
    /// </summary>
    /// <remarks>
    /// See https://getbootstrap.com/docs/5.2/components/spinners/#growing-spinner.
    /// </remarks>
    Growing = 0,

    /// <summary>
    /// A spinner with a rotating border.
    /// </summary>
    /// <remarks>
    /// See https://getbootstrap.com/docs/5.2/components/spinners/#border-spinner.
    /// </remarks>
    Border,
}

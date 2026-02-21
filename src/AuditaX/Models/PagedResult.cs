using System.Collections.Generic;

namespace AuditaX.Models;

/// <summary>
/// Represents a paged result set with total count for pagination support.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public IEnumerable<T> Items { get; init; } = [];

    /// <summary>
    /// Gets the total number of records matching the query (across all pages).
    /// </summary>
    public int TotalCount { get; init; }
}

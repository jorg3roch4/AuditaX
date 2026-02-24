namespace AuditaX.Wrappers;

/// <summary>
/// Paged response wrapper that extends <see cref="Response{T}"/> with pagination metadata.
/// </summary>
/// <typeparam name="T">The type of the data payload (typically IEnumerable of a result type).</typeparam>
public class PagedResponse<T> : Response<T>
{
    /// <summary>Gets or sets the current page number (1-based).</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets or sets the number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets or sets the total number of records matching the query across all pages.</summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Initializes a successful paged response.
    /// </summary>
    /// <param name="data">The page data.</param>
    /// <param name="pageNumber">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalCount">The total number of matching records.</param>
    public PagedResponse(T data, int pageNumber, int pageSize, int totalCount)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        Succeeded = true;
        Data = data;
    }

    /// <summary>
    /// Initializes a failed paged response with an error message.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    public PagedResponse(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a failed paged response with an error message and error list.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="errors">The list of detailed error descriptions.</param>
    public PagedResponse(string message, List<string> errors) : base(message, errors)
    {
    }
}

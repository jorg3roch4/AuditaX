namespace AuditaX.Wrappers;

/// <summary>
/// Generic response wrapper for audit query results.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class Response<T>
{
    /// <summary>Gets or sets whether the operation succeeded.</summary>
    public bool Succeeded { get; set; }

    /// <summary>Gets or sets an optional message describing the result.</summary>
    public string? Message { get; set; }

    /// <summary>Gets or sets a list of error messages when the operation fails.</summary>
    public List<string>? Errors { get; set; }

    /// <summary>Gets or sets the data payload of the response.</summary>
    public T? Data { get; set; }

    /// <summary>Initializes a new empty instance of <see cref="Response{T}"/>.</summary>
    public Response()
    {
    }

    /// <summary>Initializes a successful response with data.</summary>
    /// <param name="data">The result data.</param>
    /// <param name="message">An optional success message.</param>
    public Response(T data, string? message = null)
    {
        Succeeded = true;
        Message = message;
        Data = data;
    }

    /// <summary>Initializes a failed response with a message.</summary>
    /// <param name="message">The failure message.</param>
    public Response(string message)
    {
        Succeeded = false;
        Message = message;
    }

    /// <summary>Initializes a failed response with a message and error list.</summary>
    /// <param name="message">The failure message.</param>
    /// <param name="errors">The list of error details.</param>
    public Response(string message, List<string> errors)
    {
        Succeeded = false;
        Message = message;
        Errors = errors;
    }
}

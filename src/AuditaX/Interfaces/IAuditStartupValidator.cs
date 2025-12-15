using System.Threading;
using System.Threading.Tasks;

namespace AuditaX.Interfaces;

/// <summary>
/// Validates the audit infrastructure at application startup.
/// </summary>
public interface IAuditStartupValidator
{
    /// <summary>
    /// Validates that the audit infrastructure is properly configured.
    /// This includes checking that the audit table exists and that the
    /// column type matches the configured ChangeLogFormat.
    /// Throws an exception if validation fails.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Exceptions.AuditTableNotFoundException">
    /// Thrown when the audit table does not exist and AutoCreateTable is false.
    /// </exception>
    /// <exception cref="Exceptions.AuditColumnFormatMismatchException">
    /// Thrown when the audit log column type does not match the configured ChangeLogFormat.
    /// </exception>
    Task ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the audit table if it doesn't exist.
    /// The table is created with the column type matching the configured ChangeLogFormat.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateTableIfNotExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the actual column type of the AuditLog column in the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The column data type as a string, or null if the table/column doesn't exist.</returns>
    Task<string?> GetAuditLogColumnTypeAsync(CancellationToken cancellationToken = default);
}

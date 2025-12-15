using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Exceptions;
using AuditaX.Interfaces;

namespace AuditaX.EntityFramework.Validators;

/// <summary>
/// Entity Framework Core implementation of the startup validator.
/// </summary>
public sealed class EfAuditStartupValidator : IAuditStartupValidator
{
    private readonly DbContext _dbContext;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly AuditaXOptions _options;

    /// <summary>
    /// Initializes a new instance of the EfAuditStartupValidator.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="databaseProvider">The database provider.</param>
    /// <param name="options">The audit options.</param>
    public EfAuditStartupValidator(
        DbContext dbContext,
        IDatabaseProvider databaseProvider,
        AuditaXOptions options)
    {
        _dbContext = dbContext;
        _databaseProvider = databaseProvider;
        _options = options;
    }

    /// <inheritdoc />
    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            // Step 1: Check if the table exists
            using (var command = connection.CreateCommand())
            {
                command.CommandText = _databaseProvider.CheckTableExistsSql;
                var result = await command.ExecuteScalarAsync(cancellationToken);
                var tableExists = result is not null && (int)result == 1;

                if (!tableExists)
                {
                    if (_options.AutoCreateTable)
                    {
                        await CreateTableIfNotExistsAsync(cancellationToken);
                        // Table was just created with the correct column type, no need to validate format
                        return;
                    }
                    else
                    {
                        throw new AuditTableNotFoundException(
                            _databaseProvider.FullTableName,
                            _databaseProvider.CreateTableSql);
                    }
                }
            }

            // Step 2: Validate the column type matches the configured format
            await ValidateColumnFormatAsync(connection, cancellationToken);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    /// <inheritdoc />
    public async Task CreateTableIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            _databaseProvider.CreateTableSql,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetAuditLogColumnTypeAsync(CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = _databaseProvider.GetAuditLogColumnTypeSql;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result as string;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    /// <summary>
    /// Validates that the column type in the database matches the configured ChangeLogFormat.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="AuditColumnFormatMismatchException">
    /// Thrown when the column type doesn't match the configured format.
    /// </exception>
    private async Task ValidateColumnFormatAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = _databaseProvider.GetAuditLogColumnTypeSql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        var actualColumnType = result as string;

        if (string.IsNullOrEmpty(actualColumnType))
        {
            // Column doesn't exist - this shouldn't happen if table exists, but handle it gracefully
            throw new AuditTableNotFoundException(
                _databaseProvider.FullTableName,
                _databaseProvider.CreateTableSql);
        }

        var isCompatible = _options.ChangeLogFormat == ChangeLogFormat.Xml
            ? _databaseProvider.IsXmlCompatibleColumnType(actualColumnType)
            : _databaseProvider.IsJsonCompatibleColumnType(actualColumnType);

        if (!isCompatible)
        {
            var expectedColumnType = _options.ChangeLogFormat == ChangeLogFormat.Xml
                ? _databaseProvider.ExpectedXmlColumnType
                : _databaseProvider.ExpectedJsonColumnType;

            throw new AuditColumnFormatMismatchException(
                _databaseProvider.FullTableName,
                _databaseProvider.AuditLogColumn,
                _options.ChangeLogFormat,
                expectedColumnType,
                actualColumnType);
        }
    }
}

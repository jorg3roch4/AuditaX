using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Exceptions;
using AuditaX.Interfaces;

namespace AuditaX.Dapper.Validators;

/// <summary>
/// Dapper-based implementation of the startup validator.
/// </summary>
public sealed class DapperAuditStartupValidator : IAuditStartupValidator
{
    private readonly IDbConnection _dbConnection;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly AuditaXOptions _options;

    /// <summary>
    /// Initializes a new instance of the DapperAuditStartupValidator.
    /// </summary>
    /// <param name="dbConnection">The database connection.</param>
    /// <param name="databaseProvider">The database provider.</param>
    /// <param name="options">The audit options.</param>
    public DapperAuditStartupValidator(
        IDbConnection dbConnection,
        IDatabaseProvider databaseProvider,
        AuditaXOptions options)
    {
        _dbConnection = dbConnection;
        _databaseProvider = databaseProvider;
        _options = options;
    }

    /// <inheritdoc />
    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnectionOpen();

        // Step 1: Check if the table exists
        var tableExistsCommand = new CommandDefinition(
            _databaseProvider.CheckTableExistsSql,
            cancellationToken: cancellationToken);

        var exists = await _dbConnection.ExecuteScalarAsync<int>(tableExistsCommand);

        if (exists != 1)
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

        // Step 2: Validate the column type matches the configured format
        await ValidateColumnFormatAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CreateTableIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnectionOpen();

        var command = new CommandDefinition(
            _databaseProvider.CreateTableSql,
            cancellationToken: cancellationToken);

        await _dbConnection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public async Task<string?> GetAuditLogColumnTypeAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnectionOpen();

        var command = new CommandDefinition(
            _databaseProvider.GetAuditLogColumnTypeSql,
            cancellationToken: cancellationToken);

        return await _dbConnection.ExecuteScalarAsync<string?>(command);
    }

    /// <summary>
    /// Validates that the column type in the database matches the configured ChangeLogFormat.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="AuditColumnFormatMismatchException">
    /// Thrown when the column type doesn't match the configured format.
    /// </exception>
    private async Task ValidateColumnFormatAsync(CancellationToken cancellationToken)
    {
        var actualColumnType = await GetAuditLogColumnTypeAsync(cancellationToken);

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

    private void EnsureConnectionOpen()
    {
        if (_dbConnection.State != ConnectionState.Open)
        {
            _dbConnection.Open();
        }
    }
}

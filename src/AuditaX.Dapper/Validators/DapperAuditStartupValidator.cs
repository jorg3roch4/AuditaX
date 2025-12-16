using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Exceptions;
using AuditaX.Interfaces;
using AuditaX.Models;

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
                // Table was just created with the correct structure, no need to validate
                return;
            }
            else
            {
                throw new AuditTableNotFoundException(
                    _databaseProvider.FullTableName,
                    _databaseProvider.CreateTableSql);
            }
        }

        // Step 2: Validate the complete table structure
        await ValidateTableStructureAsync(cancellationToken);
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
    /// Validates that the table structure matches the expected structure.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="AuditTableStructureMismatchException">
    /// Thrown when the table structure doesn't match the expected structure.
    /// </exception>
    /// <exception cref="AuditColumnFormatMismatchException">
    /// Thrown when the AuditLog column type doesn't match the configured format.
    /// </exception>
    private async Task ValidateTableStructureAsync(CancellationToken cancellationToken)
    {
        EnsureConnectionOpen();

        // Get actual table structure in a single query
        var command = new CommandDefinition(
            _databaseProvider.GetTableStructureSql,
            cancellationToken: cancellationToken);

        var actualColumns = (await _dbConnection.QueryAsync<TableColumnInfo>(command)).ToList();
        var expectedColumns = _databaseProvider.GetExpectedTableStructure();

        var missingColumns = new List<string>();
        var incorrectColumns = new List<(string ColumnName, string ExpectedType, string ActualType)>();

        foreach (var expected in expectedColumns)
        {
            var actual = actualColumns.FirstOrDefault(c =>
                c.ColumnName.Equals(expected.ColumnName, System.StringComparison.OrdinalIgnoreCase));

            if (actual is null)
            {
                missingColumns.Add(expected.ColumnName);
            }
            else if (!_databaseProvider.ValidateColumn(actual, expected))
            {
                var actualTypeDesc = actual.MaxLength.HasValue && actual.MaxLength.Value > 0
                    ? $"{actual.DataType}({actual.MaxLength})"
                    : actual.DataType;

                incorrectColumns.Add((expected.ColumnName, expected.ExpectedTypeDescription, actualTypeDesc));
            }
        }

        // Throw appropriate exception based on validation results
        if (missingColumns.Count > 0 || incorrectColumns.Count > 0)
        {
            // Check if the AuditLog column is the only issue and it's a format mismatch
            if (missingColumns.Count == 0 &&
                incorrectColumns.Count == 1 &&
                incorrectColumns[0].ColumnName.Equals(_databaseProvider.AuditLogColumn, System.StringComparison.OrdinalIgnoreCase))
            {
                var actualColumn = actualColumns.First(c =>
                    c.ColumnName.Equals(_databaseProvider.AuditLogColumn, System.StringComparison.OrdinalIgnoreCase));

                var expectedColumnType = _options.ChangeLogFormat == ChangeLogFormat.Xml
                    ? _databaseProvider.ExpectedXmlColumnType
                    : _databaseProvider.ExpectedJsonColumnType;

                throw new AuditColumnFormatMismatchException(
                    _databaseProvider.FullTableName,
                    _databaseProvider.AuditLogColumn,
                    _options.ChangeLogFormat,
                    expectedColumnType,
                    actualColumn.DataType);
            }

            throw new AuditTableStructureMismatchException(
                _databaseProvider.FullTableName,
                missingColumns,
                incorrectColumns,
                _databaseProvider.CreateTableSql);
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

using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Exceptions;
using AuditaX.Interfaces;
using AuditaX.Models;

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
            }

            // Step 2: Validate the complete table structure
            await ValidateTableStructureAsync(connection, cancellationToken);
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
    /// Validates that the table structure matches the expected structure.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="AuditTableStructureMismatchException">
    /// Thrown when the table structure doesn't match the expected structure.
    /// </exception>
    /// <exception cref="AuditColumnFormatMismatchException">
    /// Thrown when the AuditLog column type doesn't match the configured format.
    /// </exception>
    private async Task ValidateTableStructureAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        // Get actual table structure in a single query
        var actualColumns = new List<TableColumnInfo>();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = _databaseProvider.GetTableStructureSql;
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                // Handle IsNullable which may be returned as int (SQL Server) or boolean (PostgreSQL)
                var isNullableValue = reader.GetValue(3);
                var isNullable = isNullableValue switch
                {
                    bool b => b,
                    int i => i == 1,
                    long l => l == 1,
                    _ => false
                };

                actualColumns.Add(new TableColumnInfo
                {
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1),
                    MaxLength = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    IsNullable = isNullable
                });
            }
        }

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

                var expectedColumnType = _options.LogFormat == LogFormat.Xml
                    ? _databaseProvider.ExpectedXmlColumnType
                    : _databaseProvider.ExpectedJsonColumnType;

                throw new AuditColumnFormatMismatchException(
                    _databaseProvider.FullTableName,
                    _databaseProvider.AuditLogColumn,
                    _options.LogFormat,
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
}

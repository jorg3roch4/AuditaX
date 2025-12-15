using System;

namespace AuditaX.Exceptions;

/// <summary>
/// Exception thrown when the audit log table does not exist in the database.
/// </summary>
public class AuditTableNotFoundException : Exception
{
    /// <summary>
    /// Gets the SQL statement to create the audit table.
    /// </summary>
    public string CreateTableSql { get; }

    /// <summary>
    /// Gets the table name that was not found.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Initializes a new instance of the AuditTableNotFoundException.
    /// </summary>
    /// <param name="tableName">The name of the table that was not found.</param>
    /// <param name="createTableSql">The SQL statement to create the table.</param>
    public AuditTableNotFoundException(string tableName, string createTableSql)
        : base($"Audit table '{tableName}' does not exist. " +
               $"Please create it using the following SQL or enable AutoCreateTable option:\n{createTableSql}")
    {
        TableName = tableName;
        CreateTableSql = createTableSql;
    }

    /// <summary>
    /// Initializes a new instance of the AuditTableNotFoundException with an inner exception.
    /// </summary>
    /// <param name="tableName">The name of the table that was not found.</param>
    /// <param name="createTableSql">The SQL statement to create the table.</param>
    /// <param name="innerException">The inner exception.</param>
    public AuditTableNotFoundException(string tableName, string createTableSql, Exception innerException)
        : base($"Audit table '{tableName}' does not exist. " +
               $"Please create it using the following SQL or enable AutoCreateTable option:\n{createTableSql}",
               innerException)
    {
        TableName = tableName;
        CreateTableSql = createTableSql;
    }
}

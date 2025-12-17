using AuditaX.Enums;
using AuditaX.Interfaces;

namespace AuditaX.Samples.Common;

/// <summary>
/// Represents the configuration selected by the user for the sample.
/// </summary>
public class SampleConfiguration
{
    public DatabaseType Database { get; set; }
    public LogFormat Format { get; set; }
    public ConfigurationMode ConfigMode { get; set; }

    /// <summary>
    /// Gets the database name (single database per provider).
    /// SQL Server: AuditaX | PostgreSQL: auditax
    /// </summary>
    public string GetDatabaseName()
    {
        return Database == DatabaseType.SqlServer ? "AuditaX" : "auditax";
    }

    /// <summary>
    /// Gets the connection string for the database.
    /// </summary>
    public string GetConnectionString()
    {
        return Database == DatabaseType.SqlServer
            ? "Server=localhost;Database=AuditaX;User Id=sa;Password=sa;TrustServerCertificate=True;"
            : "Host=localhost;Port=5432;Database=auditax;Username=postgres;Password=postgres;";
    }

    /// <summary>
    /// Gets the AuditLog table name based on ORM and format.
    /// DJ = Dapper JSON, DX = Dapper XML, EFJ = EF JSON, EFX = EF XML
    /// </summary>
    public string GetAuditLogTableName(string ormPrefix)
    {
        // ormPrefix: "D" for Dapper, "EF" for Entity Framework
        var formatSuffix = Format == LogFormat.Json ? "J" : "X";
        var tableSuffix = $"{ormPrefix}{formatSuffix}";

        return Database == DatabaseType.SqlServer
            ? $"AuditLog{tableSuffix}"      // AuditLogDJ, AuditLogDX, AuditLogEFJ, AuditLogEFX
            : $"audit_log_{tableSuffix.ToLower()}"; // audit_log_dj, audit_log_dx, audit_log_efj, audit_log_efx
    }

    /// <summary>
    /// Gets the schema name for the database.
    /// </summary>
    public string GetSchema()
    {
        return Database == DatabaseType.SqlServer ? "dbo" : "public";
    }

    public string GetDisplayName()
    {
        var db = Database == DatabaseType.SqlServer ? "SQL Server" : "PostgreSQL";
        var format = Format == LogFormat.Json ? "JSON" : "XML";
        var config = ConfigMode == ConfigurationMode.FluentApi ? "FluentApi" : "AppSettings";
        return $"{db} + {format} + {config}";
    }
}

public enum ConfigurationMode
{
    FluentApi,
    AppSettings
}

using AuditaX.Enums;
using AuditaX.Interfaces;

namespace AuditaX.Samples.Common;

/// <summary>
/// Represents the configuration selected by the user for the sample.
/// </summary>
public class SampleConfiguration
{
    public DatabaseType Database { get; set; }
    public ChangeLogFormat Format { get; set; }
    public ConfigurationMode ConfigMode { get; set; }

    public string GetDatabaseName(string ormPrefix)
    {
        var dbPrefix = Database == DatabaseType.SqlServer ? "SqlServer" : "PostgreSql";
        var formatSuffix = Format == ChangeLogFormat.Json ? "Json" : "Xml";

        return Database == DatabaseType.SqlServer
            ? $"AuditaX{ormPrefix}{dbPrefix}{formatSuffix}"
            : $"auditax_{ormPrefix.ToLower()}_{dbPrefix.ToLower()}_{formatSuffix.ToLower()}";
    }

    public string GetConnectionString(string ormPrefix)
    {
        var dbName = GetDatabaseName(ormPrefix);

        return Database == DatabaseType.SqlServer
            ? $"Server=localhost;Database={dbName};User Id=sa;Password=sa;TrustServerCertificate=True;"
            : $"Host=localhost;Port=5432;Database={dbName};Username=postgres;Password=postgres;";
    }

    public string GetDisplayName()
    {
        var db = Database == DatabaseType.SqlServer ? "SQL Server" : "PostgreSQL";
        var format = Format == ChangeLogFormat.Json ? "JSON" : "XML";
        var config = ConfigMode == ConfigurationMode.FluentApi ? "FluentApi" : "AppSettings";
        return $"{db} + {format} + {config}";
    }
}

public enum ConfigurationMode
{
    FluentApi,
    AppSettings
}

namespace AuditaX.Configuration;

/// <summary>
/// Configuration settings for AuditaX loaded from appsettings.json.
/// </summary>
public sealed class AuditaXSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "AuditaX";

    /// <summary>
    /// Enables logging for audit operations. Default is false.
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Minimum log level for audit operations. Default is "Information".
    /// Valid values: Trace, Debug, Information, Warning, Error, Critical.
    /// </summary>
    public string MinimumLogLevel { get; set; } = "Information";

    /// <summary>
    /// Name of the audit log table. Default is "AuditLog".
    /// </summary>
    public string TableName { get; set; } = "AuditLog";

    /// <summary>
    /// Database schema for the audit table. Default is "dbo".
    /// </summary>
    public string Schema { get; set; } = "dbo";

    /// <summary>
    /// Whether to automatically create the audit table if it doesn't exist.
    /// Default is false.
    /// </summary>
    public bool AutoCreateTable { get; set; } = false;

    /// <summary>
    /// Format for storing ChangeLog field data.
    /// Valid values: "Xml" (default), "Json".
    /// </summary>
    public string ChangeLogFormat { get; set; } = "Xml";

    /// <summary>
    /// Entity configurations loaded from appsettings.
    /// Key is the entity name, value is the entity settings.
    /// </summary>
    public Dictionary<string, EntitySettings> Entities { get; set; } = [];
}

/// <summary>
/// Entity-specific audit settings loaded from appsettings.json.
/// </summary>
public sealed class EntitySettings
{
    /// <summary>
    /// The name used for this entity in audit logs.
    /// If not specified, the entity key name from configuration will be used.
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// Property name used as the entity key.
    /// Corresponds to FluentAPI: .WithKey()
    /// </summary>
    public string Key { get; set; } = "Id";

    /// <summary>
    /// List of property names to audit.
    /// Corresponds to FluentAPI: .AuditProperties()
    /// </summary>
    public List<string> AuditProperties { get; set; } = [];

    /// <summary>
    /// Related entities configuration.
    /// Key is the related entity name, value is the related entity settings.
    /// Corresponds to FluentAPI: .WithRelatedEntity()
    /// </summary>
    public Dictionary<string, RelatedEntitySettings> RelatedEntities { get; set; } = [];
}

/// <summary>
/// Related entity audit settings loaded from appsettings.json.
/// </summary>
public sealed class RelatedEntitySettings
{
    /// <summary>
    /// Property name that references the parent entity.
    /// Corresponds to FluentAPI: .WithParentKey()
    /// </summary>
    public string ParentKey { get; set; } = string.Empty;

    /// <summary>
    /// Properties to capture when the related entity is added/removed.
    /// Used for both OnAdded and OnRemoved scenarios.
    /// </summary>
    public List<string> CaptureProperties { get; set; } = [];
}

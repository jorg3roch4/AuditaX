namespace AuditaX.Enums;

/// <summary>
/// Specifies the format used to store audit log entries in the ChangeLog field.
/// </summary>
public enum ChangeLogFormat
{
    /// <summary>
    /// XML format. Default for backward compatibility.
    /// </summary>
    Xml,

    /// <summary>
    /// JSON format. More compact and easier to query in modern databases.
    /// </summary>
    Json
}

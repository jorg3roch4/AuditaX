namespace AuditaX.Enums;

/// <summary>
/// Specifies the format used to store audit log entries.
/// </summary>
public enum LogFormat
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

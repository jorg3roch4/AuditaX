using System.Collections.Generic;
using AuditaX.Enums;
using AuditaX.Models;

namespace AuditaX.Interfaces;

/// <summary>
/// Service for creating and parsing XML-based audit log entries.
/// </summary>
public interface IChangeLogService
{
    /// <summary>
    /// Creates a new audit log entry for entity creation.
    /// </summary>
    /// <param name="existingAuditLog">The existing audit log XML, or null for new entries.</param>
    /// <param name="user">The user who created the entity.</param>
    /// <returns>The updated audit log XML.</returns>
    string CreateEntry(string? existingAuditLog, string user);

    /// <summary>
    /// Creates an audit log entry for entity update with field changes.
    /// </summary>
    /// <param name="existingAuditLog">The existing audit log XML, or null for new entries.</param>
    /// <param name="changes">The list of field changes.</param>
    /// <param name="user">The user who made the changes.</param>
    /// <returns>The updated audit log XML.</returns>
    string UpdateEntry(string? existingAuditLog, List<FieldChange> changes, string user);

    /// <summary>
    /// Creates an audit log entry for entity deletion.
    /// </summary>
    /// <param name="existingAuditLog">The existing audit log XML, or null for new entries.</param>
    /// <param name="user">The user who deleted the entity.</param>
    /// <returns>The updated audit log XML.</returns>
    string DeleteEntry(string? existingAuditLog, string user);

    /// <summary>
    /// Creates an audit log entry for related entity changes.
    /// </summary>
    /// <param name="existingAuditLog">The existing audit log XML, or null for new entries.</param>
    /// <param name="action">The action performed (Added or Removed).</param>
    /// <param name="relatedName">The name of the related entity.</param>
    /// <param name="fields">The field values of the related entity.</param>
    /// <param name="user">The user who made the change.</param>
    /// <returns>The updated audit log XML.</returns>
    string RelatedEntry(
        string? existingAuditLog,
        AuditAction action,
        string relatedName,
        List<FieldChange> fields,
        string user);

    /// <summary>
    /// Parses an audit log XML string into a list of entries.
    /// </summary>
    /// <param name="auditLogXml">The audit log XML to parse.</param>
    /// <returns>A list of parsed audit log entries.</returns>
    List<AuditLogEntry> ParseAuditLog(string? auditLogXml);

    /// <summary>
    /// Determines if two values are different.
    /// </summary>
    /// <param name="originalValue">The original value.</param>
    /// <param name="currentValue">The current value.</param>
    /// <returns>True if the values are different, otherwise false.</returns>
    bool HasChanged(object? originalValue, object? currentValue);

    /// <summary>
    /// Converts a value to its string representation for audit logging.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string representation of the value.</returns>
    string? ConvertToString(object? value);
}

using System;
using System.Collections.Generic;
using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;

namespace AuditaX.Services;

/// <summary>
/// Facade service that delegates to XML or JSON change log services based on configuration.
/// Automatically detects format when parsing existing audit logs.
/// </summary>
/// <param name="options">The AuditaX options containing format configuration.</param>
public sealed class ChangeLogService(AuditaXOptions options) : IChangeLogService
{
    private readonly XmlChangeLogService _xmlService = new();
    private readonly JsonChangeLogService _jsonService = new();

    private readonly IChangeLogService _writeService = options.LogFormat switch
    {
        LogFormat.Json => new JsonChangeLogService(),
        _ => new XmlChangeLogService()
    };

    /// <inheritdoc />
    public string CreateEntry(string? existingAuditLog, string user)
    {
        // If there's existing data, detect its format and use the same service
        var service = DetectServiceForExistingData(existingAuditLog) ?? _writeService;
        return service.CreateEntry(existingAuditLog, user);
    }

    /// <inheritdoc />
    public string UpdateEntry(string? existingAuditLog, List<FieldChange> changes, string user)
    {
        var service = DetectServiceForExistingData(existingAuditLog) ?? _writeService;
        return service.UpdateEntry(existingAuditLog, changes, user);
    }

    /// <inheritdoc />
    public string DeleteEntry(string? existingAuditLog, string user)
    {
        var service = DetectServiceForExistingData(existingAuditLog) ?? _writeService;
        return service.DeleteEntry(existingAuditLog, user);
    }

    /// <inheritdoc />
    public string RelatedEntry(
        string? existingAuditLog,
        AuditAction action,
        string relatedName,
        List<FieldChange> fields,
        string user)
    {
        var service = DetectServiceForExistingData(existingAuditLog) ?? _writeService;
        return service.RelatedEntry(existingAuditLog, action, relatedName, fields, user);
    }

    /// <inheritdoc />
    public List<AuditLogEntry> ParseAuditLog(string? auditLog)
    {
        if (string.IsNullOrWhiteSpace(auditLog))
        {
            return [];
        }

        // Auto-detect format and parse with appropriate service
        var service = DetectFormat(auditLog);
        return service.ParseAuditLog(auditLog);
    }

    /// <inheritdoc />
    public bool HasChanged(object? originalValue, object? currentValue)
    {
        return _writeService.HasChanged(originalValue, currentValue);
    }

    /// <inheritdoc />
    public string? ConvertToString(object? value)
    {
        return _writeService.ConvertToString(value);
    }

    /// <summary>
    /// Detects the format of an audit log string.
    /// </summary>
    /// <param name="auditLog">The audit log string to analyze.</param>
    /// <returns>The detected format.</returns>
    public static LogFormat DetectFormatType(string? auditLog)
    {
        if (string.IsNullOrWhiteSpace(auditLog))
        {
            return LogFormat.Xml; // Default
        }

        var trimmed = auditLog.TrimStart();

        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            return LogFormat.Json;
        }

        if (trimmed.StartsWith('<'))
        {
            return LogFormat.Xml;
        }

        // Default to XML for backward compatibility
        return LogFormat.Xml;
    }

    private IChangeLogService DetectFormat(string? auditLog)
    {
        var format = DetectFormatType(auditLog);
        return format == LogFormat.Json ? _jsonService : _xmlService;
    }

    private IChangeLogService? DetectServiceForExistingData(string? existingAuditLog)
    {
        if (string.IsNullOrWhiteSpace(existingAuditLog))
        {
            return null; // Use default write service for new entries
        }

        // Preserve format of existing data
        return DetectFormat(existingAuditLog);
    }
}

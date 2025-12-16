using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;

namespace AuditaX.Services;

/// <summary>
/// Service for creating and parsing JSON-based audit log entries.
/// </summary>
public sealed class JsonChangeLogService : IChangeLogService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <inheritdoc />
    public string CreateEntry(string? existingAuditLog, string user)
    {
        return GenerateJson(existingAuditLog, AuditAction.Created, null, [], user);
    }

    /// <inheritdoc />
    public string UpdateEntry(string? existingAuditLog, List<FieldChange> changes, string user)
    {
        return GenerateJson(existingAuditLog, AuditAction.Updated, null, changes, user);
    }

    /// <inheritdoc />
    public string DeleteEntry(string? existingAuditLog, string user)
    {
        return GenerateJson(existingAuditLog, AuditAction.Deleted, null, [], user);
    }

    /// <inheritdoc />
    public string RelatedEntry(
        string? existingAuditLog,
        AuditAction action,
        string relatedName,
        List<FieldChange> fields,
        string user)
    {
        return GenerateJson(existingAuditLog, action, relatedName, fields, user);
    }

    /// <inheritdoc />
    public List<AuditLogEntry> ParseAuditLog(string? auditLogJson)
    {
        if (string.IsNullOrWhiteSpace(auditLogJson))
        {
            return [];
        }

        List<AuditLogEntry> entries = [];

        try
        {
            var container = JsonSerializer.Deserialize<AuditLogContainer>(auditLogJson, SerializerOptions);
            if (container?.AuditLog is not null)
            {
                foreach (var jsonEntry in container.AuditLog)
                {
                    var entry = new AuditLogEntry
                    {
                        User = jsonEntry.User ?? string.Empty,
                        Timestamp = jsonEntry.Timestamp,
                        Related = jsonEntry.Related
                    };

                    if (Enum.TryParse<AuditAction>(jsonEntry.Action, out var action))
                    {
                        entry.Action = action;
                    }

                    if (jsonEntry.Fields is not null)
                    {
                        foreach (var jsonField in jsonEntry.Fields)
                        {
                            entry.Fields.Add(new FieldChange
                            {
                                Name = jsonField.Name ?? string.Empty,
                                Before = jsonField.Before,
                                After = jsonField.After,
                                Value = jsonField.Value
                            });
                        }
                    }

                    entries.Add(entry);
                }
            }
        }
        catch
        {
            // Return empty list on parse errors (fail-safe)
        }

        return entries;
    }

    /// <inheritdoc />
    public bool HasChanged(object? originalValue, object? currentValue)
    {
        var originalStr = ConvertToString(originalValue);
        var currentStr = ConvertToString(currentValue);

        return !string.Equals(originalStr, currentStr, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public string? ConvertToString(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return value switch
        {
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            bool boolean => boolean.ToString().ToLowerInvariant(),
            decimal decimalValue => decimalValue.ToString("G", CultureInfo.InvariantCulture),
            float floatValue => floatValue.ToString("G", CultureInfo.InvariantCulture),
            double doubleValue => doubleValue.ToString("G", CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    private string GenerateJson(
        string? existingAuditLog,
        AuditAction action,
        string? relatedName,
        List<FieldChange> fields,
        string user)
    {
        AuditLogContainer container;

        if (string.IsNullOrWhiteSpace(existingAuditLog))
        {
            container = new AuditLogContainer { AuditLog = [] };
        }
        else
        {
            try
            {
                container = JsonSerializer.Deserialize<AuditLogContainer>(existingAuditLog, SerializerOptions)
                            ?? new AuditLogContainer { AuditLog = [] };
                container.AuditLog ??= [];
            }
            catch
            {
                container = new AuditLogContainer { AuditLog = [] };
            }
        }

        var entry = new JsonAuditEntry
        {
            Action = action.ToString(),
            User = user,
            Timestamp = DateTime.UtcNow,
            Related = relatedName
        };

        if (fields.Count > 0)
        {
            entry.Fields = [];
            foreach (var field in fields)
            {
                // For Added/Removed actions, use Value only
                // For Updated action, use Before/After
                if (action == AuditAction.Added || action == AuditAction.Removed)
                {
                    entry.Fields.Add(new JsonFieldChange
                    {
                        Name = field.Name,
                        Value = field.Value ?? field.After ?? field.Before
                    });
                }
                else
                {
                    entry.Fields.Add(new JsonFieldChange
                    {
                        Name = field.Name,
                        Before = field.Before,
                        After = field.After
                    });
                }
            }
        }

        container.AuditLog.Add(entry);

        return JsonSerializer.Serialize(container, SerializerOptions);
    }

    #region Internal JSON Models

    private sealed class AuditLogContainer
    {
        public List<JsonAuditEntry>? AuditLog { get; set; }
    }

    private sealed class JsonAuditEntry
    {
        public string? Action { get; set; }
        public string? User { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Related { get; set; }
        public List<JsonFieldChange>? Fields { get; set; }
    }

    private sealed class JsonFieldChange
    {
        public string? Name { get; set; }
        public string? Before { get; set; }
        public string? After { get; set; }
        public string? Value { get; set; }
    }

    #endregion
}

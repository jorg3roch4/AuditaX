using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;

namespace AuditaX.Services;

/// <summary>
/// Service for creating and parsing XML-based audit log entries.
/// </summary>
public sealed class XmlChangeLogService : IChangeLogService
{
    private const string RootElement = "AuditLog";
    private const string EntryElement = "Entry";
    private const string FieldElement = "Field";
    private const string ActionAttribute = "Action";
    private const string UserAttribute = "User";
    private const string TimestampAttribute = "Timestamp";
    private const string RelatedAttribute = "Related";
    private const string NameAttribute = "Name";
    private const string BeforeAttribute = "Before";
    private const string AfterAttribute = "After";
    private const string ValueAttribute = "Value";

    /// <inheritdoc />
    public string CreateEntry(string? existingAuditLog, string user)
    {
        return GenerateXml(existingAuditLog, AuditAction.Created, null, [], user);
    }

    /// <inheritdoc />
    public string UpdateEntry(string? existingAuditLog, List<FieldChange> changes, string user)
    {
        return GenerateXml(existingAuditLog, AuditAction.Updated, null, changes, user);
    }

    /// <inheritdoc />
    public string DeleteEntry(string? existingAuditLog, string user)
    {
        return GenerateXml(existingAuditLog, AuditAction.Deleted, null, [], user);
    }

    /// <inheritdoc />
    public string RelatedEntry(
        string? existingAuditLog,
        AuditAction action,
        string relatedName,
        List<FieldChange> fields,
        string user)
    {
        return GenerateXml(existingAuditLog, action, relatedName, fields, user);
    }

    /// <inheritdoc />
    public List<AuditLogEntry> ParseAuditLog(string? auditLogXml)
    {
        if (string.IsNullOrWhiteSpace(auditLogXml))
        {
            return [];
        }

        List<AuditLogEntry> entries = [];

        try
        {
            var doc = XDocument.Parse(auditLogXml);
            var root = doc.Element(RootElement);

            if (root is null)
            {
                return entries;
            }

            foreach (var entryElement in root.Elements(EntryElement))
            {
                var entry = new AuditLogEntry();

                var actionAttr = entryElement.Attribute(ActionAttribute);
                if (actionAttr is not null && Enum.TryParse<AuditAction>(actionAttr.Value, out var action))
                {
                    entry.Action = action;
                }

                var userAttr = entryElement.Attribute(UserAttribute);
                if (userAttr is not null)
                {
                    entry.User = userAttr.Value;
                }

                var timestampAttr = entryElement.Attribute(TimestampAttribute);
                if (timestampAttr is not null && DateTime.TryParse(timestampAttr.Value, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var timestamp))
                {
                    entry.Timestamp = timestamp;
                }

                var relatedAttr = entryElement.Attribute(RelatedAttribute);
                if (relatedAttr is not null)
                {
                    entry.Related = relatedAttr.Value;
                }

                foreach (var fieldElement in entryElement.Elements(FieldElement))
                {
                    var field = new FieldChange();

                    var nameAttr = fieldElement.Attribute(NameAttribute);
                    if (nameAttr is not null)
                    {
                        field.Name = nameAttr.Value;
                    }

                    var beforeAttr = fieldElement.Attribute(BeforeAttribute);
                    if (beforeAttr is not null)
                    {
                        field.Before = beforeAttr.Value;
                    }

                    var afterAttr = fieldElement.Attribute(AfterAttribute);
                    if (afterAttr is not null)
                    {
                        field.After = afterAttr.Value;
                    }

                    var valueAttr = fieldElement.Attribute(ValueAttribute);
                    if (valueAttr is not null)
                    {
                        field.Value = valueAttr.Value;
                    }

                    entry.Fields.Add(field);
                }

                entries.Add(entry);
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

    private string GenerateXml(
        string? existingAuditLog,
        AuditAction action,
        string? relatedName,
        List<FieldChange> fields,
        string user)
    {
        XDocument doc;
        XElement root;

        if (string.IsNullOrWhiteSpace(existingAuditLog))
        {
            doc = new XDocument();
            root = new XElement(RootElement);
            doc.Add(root);
        }
        else
        {
            try
            {
                doc = XDocument.Parse(existingAuditLog);
                root = doc.Element(RootElement) ?? new XElement(RootElement);
                if (doc.Root is null)
                {
                    doc.Add(root);
                }
            }
            catch
            {
                doc = new XDocument();
                root = new XElement(RootElement);
                doc.Add(root);
            }
        }

        var entry = new XElement(EntryElement,
            new XAttribute(ActionAttribute, action.ToString()),
            new XAttribute(UserAttribute, user),
            new XAttribute(TimestampAttribute, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)));

        if (!string.IsNullOrEmpty(relatedName))
        {
            entry.Add(new XAttribute(RelatedAttribute, relatedName));
        }

        foreach (var field in fields)
        {
            var fieldElement = new XElement(FieldElement,
                new XAttribute(NameAttribute, field.Name));

            // For Added/Removed actions, use Value only
            // For Updated action, use Before/After
            if (action == AuditAction.Added || action == AuditAction.Removed)
            {
                var value = field.Value ?? field.After ?? field.Before;
                if (value is not null)
                {
                    fieldElement.Add(new XAttribute(ValueAttribute, value));
                }
            }
            else
            {
                if (field.Before is not null)
                {
                    fieldElement.Add(new XAttribute(BeforeAttribute, field.Before));
                }

                if (field.After is not null)
                {
                    fieldElement.Add(new XAttribute(AfterAttribute, field.After));
                }
            }

            entry.Add(fieldElement);
        }

        root.Add(entry);

        return doc.ToString(SaveOptions.DisableFormatting);
    }
}

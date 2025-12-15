using Microsoft.Extensions.Logging;
using AuditaX.Configuration;
using AuditaX.Entities;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;

namespace AuditaX.Services;

/// <summary>
/// Default implementation of the audit service.
/// Handles all audit logging operations to the centralized AuditLog table.
/// </summary>
/// <param name="repository">The audit repository.</param>
/// <param name="changeLogService">The change log service.</param>
/// <param name="userProvider">The user provider.</param>
/// <param name="options">The audit options.</param>
/// <param name="logger">Optional logger.</param>
public sealed class AuditService(
    IAuditRepository repository,
    IChangeLogService changeLogService,
    IAuditUserProvider userProvider,
    AuditaXOptions options,
    ILogger<AuditService>? logger = null) : IAuditService
{
    private readonly ILogger<AuditService>? _logger = options.EnableLogging ? logger : null;

    /// <inheritdoc />
    public async Task LogCreateAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        var user = userProvider.GetCurrentUser();
        await LogCreateAsync(sourceName, sourceKey, user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogCreateAsync(
        string sourceName,
        string sourceKey,
        string user,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug(
            "Logging create event for {SourceName}:{SourceKey} by user {User}",
            sourceName,
            sourceKey,
            user);

        var auditLog = await GetOrCreateAuditLogAsync(sourceName, sourceKey, cancellationToken);

        auditLog.AuditLogXml = changeLogService.CreateEntry(auditLog.AuditLogXml, user);

        await repository.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation(
            "Successfully logged create event for {SourceName}:{SourceKey}",
            sourceName,
            sourceKey);
    }

    /// <inheritdoc />
    public async Task LogUpdateAsync(
        string sourceName,
        string sourceKey,
        List<FieldChange> changes,
        CancellationToken cancellationToken = default)
    {
        var user = userProvider.GetCurrentUser();
        await LogUpdateAsync(sourceName, sourceKey, changes, user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogUpdateAsync(
        string sourceName,
        string sourceKey,
        List<FieldChange> changes,
        string user,
        CancellationToken cancellationToken = default)
    {
        if (changes.Count == 0)
        {
            _logger?.LogDebug(
                "No changes to log for {SourceName}:{SourceKey}",
                sourceName,
                sourceKey);
            return;
        }

        _logger?.LogDebug(
            "Logging {ChangeCount} changes for {SourceName}:{SourceKey} by user {User}",
            changes.Count,
            sourceName,
            sourceKey,
            user);

        var auditLog = await GetOrCreateAuditLogAsync(sourceName, sourceKey, cancellationToken);

        auditLog.AuditLogXml = changeLogService.UpdateEntry(
            auditLog.AuditLogXml,
            changes,
            user);

        await repository.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation(
            "Successfully logged update event for {SourceName}:{SourceKey}",
            sourceName,
            sourceKey);
    }

    /// <inheritdoc />
    public async Task LogDeleteAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        var user = userProvider.GetCurrentUser();
        await LogDeleteAsync(sourceName, sourceKey, user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogDeleteAsync(
        string sourceName,
        string sourceKey,
        string user,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug(
            "Logging delete event for {SourceName}:{SourceKey} by user {User}",
            sourceName,
            sourceKey,
            user);

        var auditLog = await GetOrCreateAuditLogAsync(sourceName, sourceKey, cancellationToken);

        auditLog.AuditLogXml = changeLogService.DeleteEntry(auditLog.AuditLogXml, user);

        await repository.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation(
            "Successfully logged delete event for {SourceName}:{SourceKey}",
            sourceName,
            sourceKey);
    }

    /// <inheritdoc />
    public async Task LogRelatedAsync(
        string sourceName,
        string sourceKey,
        AuditAction action,
        string relatedName,
        List<FieldChange> fields,
        CancellationToken cancellationToken = default)
    {
        var user = userProvider.GetCurrentUser();
        await LogRelatedAsync(sourceName, sourceKey, action, relatedName, fields, user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogRelatedAsync(
        string sourceName,
        string sourceKey,
        AuditAction action,
        string relatedName,
        List<FieldChange> fields,
        string user,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug(
            "Logging {Action} event for related {RelatedName} on {SourceName}:{SourceKey} by user {User}",
            action,
            relatedName,
            sourceName,
            sourceKey,
            user);

        var auditLog = await GetOrCreateAuditLogAsync(sourceName, sourceKey, cancellationToken);

        auditLog.AuditLogXml = changeLogService.RelatedEntry(
            auditLog.AuditLogXml,
            action,
            relatedName,
            fields,
            user);

        await repository.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation(
            "Successfully logged {Action} event for related {RelatedName} on {SourceName}:{SourceKey}",
            action,
            relatedName,
            sourceName,
            sourceKey);
    }

    /// <inheritdoc />
    public async Task<List<AuditLogEntry>?> GetAuditHistoryAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug(
            "Retrieving audit history for {SourceName}:{SourceKey}",
            sourceName,
            sourceKey);

        var auditLog = await repository.GetByEntityAsync(sourceName, sourceKey, cancellationToken);

        if (auditLog is null)
        {
            _logger?.LogDebug(
                "No audit history found for {SourceName}:{SourceKey}",
                sourceName,
                sourceKey);
            return null;
        }

        var entries = changeLogService.ParseAuditLog(auditLog.AuditLogXml);

        _logger?.LogDebug(
            "Retrieved {EntryCount} audit entries for {SourceName}:{SourceKey}",
            entries.Count,
            sourceName,
            sourceKey);

        return entries;
    }

    private async Task<AuditLog> GetOrCreateAuditLogAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken)
    {
        var auditLog = await repository.GetByEntityTrackingAsync(
            sourceName,
            sourceKey,
            cancellationToken);

        if (auditLog is not null)
        {
            return auditLog;
        }

        auditLog = new AuditLog
        {
            SourceName = sourceName,
            SourceKey = sourceKey,
            AuditLogXml = string.Empty
        };

        await repository.AddAsync(auditLog, cancellationToken);

        return auditLog;
    }
}

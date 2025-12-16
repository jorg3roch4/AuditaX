using AuditaX.Configuration;
using AuditaX.Dapper.Interfaces;
using AuditaX.Interfaces;
using AuditaX.Models;

namespace AuditaX.Dapper.Services;

/// <summary>
/// Implementation of IAuditUnitOfWork that provides audit logging for Dapper repositories.
/// </summary>
public class DapperAuditUnitOfWork(
    IAuditService auditService,
    IChangeLogService changeLogService,
    AuditaXOptions options,
    IAuditUserProvider userProvider) : IAuditUnitOfWork
{
    /// <inheritdoc />
    public async Task LogCreateAsync<T>(T entity) where T : class
    {
        var (sourceName, sourceKey) = GetEntityInfo(entity);
        var user = userProvider.GetCurrentUser();

        await auditService.LogCreateAsync(sourceName, sourceKey, user);
    }

    /// <inheritdoc />
    public async Task LogUpdateAsync<T>(T original, T modified) where T : class
    {
        var (sourceName, sourceKey) = GetEntityInfo(modified);
        var changes = GetFieldChanges(original, modified);

        if (changes.Count > 0)
        {
            var user = userProvider.GetCurrentUser();
            await auditService.LogUpdateAsync(sourceName, sourceKey, changes, user);
        }
    }

    /// <inheritdoc />
    public async Task LogDeleteAsync<T>(T entity) where T : class
    {
        var (sourceName, sourceKey) = GetEntityInfo(entity);
        var user = userProvider.GetCurrentUser();

        await auditService.LogDeleteAsync(sourceName, sourceKey, user);
    }

    private (string SourceName, string SourceKey) GetEntityInfo<T>(T entity) where T : class
    {
        var entityType = typeof(T);
        var config = options.GetEntity(entityType)
            ?? throw new InvalidOperationException(
                $"Entity type '{entityType.Name}' is not configured for auditing. " +
                $"Configure it using AuditaX options in appsettings.json or fluent API.");

        var sourceKey = config.GetKey(entity);

        return (config.DisplayName ?? entityType.Name, sourceKey);
    }

    private List<FieldChange> GetFieldChanges<T>(T original, T modified) where T : class
    {
        var changes = new List<FieldChange>();
        var entityType = typeof(T);
        var config = options.GetEntity(entityType);

        if (config is null)
        {
            return changes;
        }

        foreach (var propertyName in config.Properties)
        {
            var property = entityType.GetProperty(propertyName);
            if (property is null)
            {
                continue;
            }

            var originalValue = property.GetValue(original);
            var modifiedValue = property.GetValue(modified);

            if (changeLogService.HasChanged(originalValue, modifiedValue))
            {
                changes.Add(new FieldChange
                {
                    Name = propertyName,
                    Before = changeLogService.ConvertToString(originalValue),
                    After = changeLogService.ConvertToString(modifiedValue)
                });
            }
        }

        return changes;
    }
}

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AuditaX.Entities;
using AuditaX.Interfaces;

namespace AuditaX.EntityFramework.Repositories;

/// <summary>
/// Entity Framework Core implementation of the audit repository.
/// </summary>
/// <param name="dbContext">The database context.</param>
public sealed class EfAuditRepository(DbContext dbContext) : IAuditRepository
{
    private readonly DbSet<AuditLog> _auditLogs = dbContext.Set<AuditLog>();

    /// <inheritdoc />
    public async Task<AuditLog?> GetByEntityAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        return await _auditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.SourceName == sourceName && a.SourceKey == sourceKey,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuditLog?> GetByEntityTrackingAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        return await _auditLogs
            .FirstOrDefaultAsync(
                a => a.SourceName == sourceName && a.SourceKey == sourceKey,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _auditLogs.AddAsync(auditLog, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

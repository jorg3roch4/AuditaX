namespace AuditaX.Dapper.Interfaces;

/// <summary>
/// Provides audit logging operations for Dapper repositories.
/// Inject this interface in your repositories to log entity changes.
/// </summary>
/// <remarks>
/// Usage example:
/// <code>
/// public class ProductRepository(DapperContext context, IAuditUnitOfWork audit) : IProductRepository
/// {
///     public async Task&lt;int&gt; UpdateAsync(Product entity)
///     {
///         var original = await GetByIdAsync(entity.Id);
///
///         using var connection = context.CreateConnection();
///         var result = await connection.ExecuteAsync(sql, entity);
///
///         if (result > 0 &amp;&amp; original != null)
///         {
///             await audit.LogUpdateAsync(original, entity);
///         }
///
///         return result;
///     }
/// }
/// </code>
/// </remarks>
public interface IAuditUnitOfWork
{
    /// <summary>
    /// Logs a create operation for the specified entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The created entity.</param>
    Task LogCreateAsync<T>(T entity) where T : class;

    /// <summary>
    /// Logs an update operation, capturing changes between original and modified states.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="original">The entity state before the update.</param>
    /// <param name="modified">The entity state after the update.</param>
    Task LogUpdateAsync<T>(T original, T modified) where T : class;

    /// <summary>
    /// Logs a delete operation for the specified entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The deleted entity.</param>
    Task LogDeleteAsync<T>(T entity) where T : class;
}

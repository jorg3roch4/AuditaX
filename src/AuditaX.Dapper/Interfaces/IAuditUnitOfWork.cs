namespace AuditaX.Dapper.Interfaces;

/// <summary>
/// Provides audit logging operations for Dapper repositories.
/// Inject this interface in your repositories to log entity changes.
/// </summary>
/// <remarks>
/// <para><b>Basic Usage:</b></para>
/// <code>
/// public class ProductRepository(DapperContext context, IAuditUnitOfWork audit) : IProductRepository
/// {
///     public async Task&lt;int&gt; UpdateAsync(Product entity)
///     {
///         var original = await GetByIdAsync(entity.Id);
///         using var connection = context.CreateConnection();
///         var result = await connection.ExecuteAsync(sql, entity);
///
///         if (result > 0 &amp;&amp; original != null)
///             await audit.LogUpdateAsync(original, entity);
///
///         return result;
///     }
///
///     public async Task AddTagAsync(Product product, ProductTag tag)
///     {
///         using var connection = context.CreateConnection();
///         await connection.ExecuteAsync(sql, tag);
///         await audit.LogRelatedAddedAsync(product, tag);
///     }
/// }
/// </code>
///
/// <para><b>With Lookups (resolve FK to display values):</b></para>
/// <code>
/// public async Task AssignRoleAsync(User user, UserRole userRole)
/// {
///     // 1. Insert the UserRole
///     using var connection = context.CreateConnection();
///     await connection.ExecuteAsync(sql, userRole);
///
///     // 2. Resolve the lookup (get Role to capture RoleName)
///     var role = await GetRoleByIdAsync(userRole.RoleId);
///
///     // 3. Log with lookup - audit will show "RoleName: Administrator" instead of RoleId GUID
///     await audit.LogRelatedAddedAsync(user, userRole, role);
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

    /// <summary>
    /// Logs an addition of a related entity to the parent entity.
    /// Captures properties configured in <c>.Properties()</c> from the related entity.
    /// </summary>
    /// <typeparam name="TParent">The parent entity type.</typeparam>
    /// <typeparam name="TRelated">The related entity type.</typeparam>
    /// <param name="parent">The parent entity.</param>
    /// <param name="related">The related entity being added.</param>
    Task LogRelatedAddedAsync<TParent, TRelated>(TParent parent, TRelated related)
        where TParent : class
        where TRelated : class;

    /// <summary>
    /// Logs an addition of a related entity with lookup resolution.
    /// Captures properties from lookup entities instead of foreign key values.
    /// </summary>
    /// <typeparam name="TParent">The parent entity type.</typeparam>
    /// <typeparam name="TRelated">The related entity type.</typeparam>
    /// <param name="parent">The parent entity.</param>
    /// <param name="related">The related entity being added.</param>
    /// <param name="lookups">The resolved lookup entities (e.g., Role entity to capture RoleName).</param>
    /// <example>
    /// <code>
    /// var role = await GetRoleByIdAsync(userRole.RoleId);
    /// await audit.LogRelatedAddedAsync(user, userRole, role);
    /// // Audit log shows: "RoleName: Administrator" instead of "RoleId: guid..."
    /// </code>
    /// </example>
    Task LogRelatedAddedAsync<TParent, TRelated>(TParent parent, TRelated related, params object[] lookups)
        where TParent : class
        where TRelated : class;

    /// <summary>
    /// Logs an update of a related entity, capturing changes between original and modified states.
    /// </summary>
    /// <typeparam name="TParent">The parent entity type.</typeparam>
    /// <typeparam name="TRelated">The related entity type.</typeparam>
    /// <param name="parent">The parent entity.</param>
    /// <param name="original">The related entity state before the update.</param>
    /// <param name="modified">The related entity state after the update.</param>
    Task LogRelatedUpdatedAsync<TParent, TRelated>(TParent parent, TRelated original, TRelated modified)
        where TParent : class
        where TRelated : class;

    /// <summary>
    /// Logs an update of a related entity with lookup resolution.
    /// Captures changes from lookup entities for before/after comparison.
    /// </summary>
    /// <typeparam name="TParent">The parent entity type.</typeparam>
    /// <typeparam name="TRelated">The related entity type.</typeparam>
    /// <param name="parent">The parent entity.</param>
    /// <param name="original">The related entity state before the update.</param>
    /// <param name="modified">The related entity state after the update.</param>
    /// <param name="originalLookups">The resolved lookup entities for the original state.</param>
    /// <param name="modifiedLookups">The resolved lookup entities for the modified state.</param>
    Task LogRelatedUpdatedAsync<TParent, TRelated>(
        TParent parent,
        TRelated original,
        TRelated modified,
        object[] originalLookups,
        object[] modifiedLookups)
        where TParent : class
        where TRelated : class;

    /// <summary>
    /// Logs a removal of a related entity from the parent entity.
    /// Captures properties configured in <c>.Properties()</c> from the related entity.
    /// </summary>
    /// <typeparam name="TParent">The parent entity type.</typeparam>
    /// <typeparam name="TRelated">The related entity type.</typeparam>
    /// <param name="parent">The parent entity.</param>
    /// <param name="related">The related entity being removed.</param>
    Task LogRelatedRemovedAsync<TParent, TRelated>(TParent parent, TRelated related)
        where TParent : class
        where TRelated : class;

    /// <summary>
    /// Logs a removal of a related entity with lookup resolution.
    /// Captures properties from lookup entities instead of foreign key values.
    /// </summary>
    /// <typeparam name="TParent">The parent entity type.</typeparam>
    /// <typeparam name="TRelated">The related entity type.</typeparam>
    /// <param name="parent">The parent entity.</param>
    /// <param name="related">The related entity being removed.</param>
    /// <param name="lookups">The resolved lookup entities (e.g., Role entity to capture RoleName).</param>
    Task LogRelatedRemovedAsync<TParent, TRelated>(TParent parent, TRelated related, params object[] lookups)
        where TParent : class
        where TRelated : class;
}

using AuditaX.Samples.Common.Entities;

namespace AuditaX.Samples.Common.Demo;

/// <summary>
/// Interface for data operations used in demos.
/// Each sample (Dapper, EF) implements this with their specific data access logic.
/// </summary>
public interface IDemoDataOperations
{
    /// <summary>
    /// Creates the required tables (Products, ProductTags) if they don't exist.
    /// </summary>
    Task EnsureTablesCreatedAsync();

    /// <summary>
    /// Creates a new product and returns it with the generated ID.
    /// </summary>
    Task<Product> CreateProductAsync(Product product);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    Task UpdateProductAsync(Product product);

    /// <summary>
    /// Creates a new product tag and returns it with the generated ID.
    /// </summary>
    Task<ProductTag> CreateProductTagAsync(ProductTag tag);

    /// <summary>
    /// Deletes a product tag by ID.
    /// </summary>
    Task DeleteProductTagAsync(int tagId);
}

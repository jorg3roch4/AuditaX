namespace AuditaX.EntityFramework.Tests.TestEntities;

/// <summary>
/// Test entity simulating a parent with a separate display identifier (Sku) vs primary key (Id).
/// </summary>
public class ParentProduct
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Test entity simulating a related child entity with FK back to <see cref="ParentProduct.Id"/>.
/// </summary>
public class ProductTag
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string TagName { get; set; } = string.Empty;
}

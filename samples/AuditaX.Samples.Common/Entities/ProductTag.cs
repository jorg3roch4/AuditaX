namespace AuditaX.Samples.Common.Entities;

/// <summary>
/// Sample related entity for demonstrating LogRelatedAsync functionality.
/// Represents tags associated with a product.
/// </summary>
public class ProductTag
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Tag { get; set; } = string.Empty;
}

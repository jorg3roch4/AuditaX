namespace AuditaX.Samples.Common.Entities;

/// <summary>
/// Sample product entity for AuditaX demos.
/// Note: No audit columns - audit data is stored in centralized AuditLog table.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
}

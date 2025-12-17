namespace AuditaX.Samples.Common.Entities;

/// <summary>
/// Sample role entity for AuditaX demos.
/// Simplified version of ASP.NET Identity's IdentityRole.
/// </summary>
public class Role
{
    public string RoleId { get; set; } = Guid.NewGuid().ToString();
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

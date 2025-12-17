namespace AuditaX.Samples.Common.Entities;

/// <summary>
/// Sample user-role mapping entity for AuditaX demos.
/// Simplified version of ASP.NET Identity's IdentityUserRole.
/// This is a junction table with a composite key (UserId, RoleId).
/// </summary>
public class UserRole
{
    public string UserId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
}

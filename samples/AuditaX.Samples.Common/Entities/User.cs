namespace AuditaX.Samples.Common.Entities;

/// <summary>
/// Sample user entity for AuditaX demos.
/// Simplified version of ASP.NET Identity's IdentityUser.
/// </summary>
public class User
{
    public string UserId { get; set; } = Guid.NewGuid().ToString();
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

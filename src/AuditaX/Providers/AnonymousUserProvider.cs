using AuditaX.Interfaces;

namespace AuditaX.Providers;

/// <summary>
/// Default user provider that returns "Anonymous".
/// Replace this with a custom implementation for proper user tracking.
/// </summary>
public sealed class AnonymousUserProvider : IAuditUserProvider
{
    /// <inheritdoc />
    public string GetCurrentUser() => "Anonymous";
}

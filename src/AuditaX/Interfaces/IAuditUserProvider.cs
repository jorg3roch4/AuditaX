namespace AuditaX.Interfaces;

/// <summary>
/// Provides the current user identity for audit logging.
/// Implement this interface to capture user context from your application.
/// </summary>
public interface IAuditUserProvider
{
    /// <summary>
    /// Gets the current user identifier.
    /// </summary>
    /// <returns>The username or identifier of the current user. Returns "Anonymous" if no user context is available.</returns>
    string GetCurrentUser();
}

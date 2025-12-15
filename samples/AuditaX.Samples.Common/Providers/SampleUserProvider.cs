using AuditaX.Interfaces;

namespace AuditaX.Samples.Common.Providers;

/// <summary>
/// Sample user provider that returns a hardcoded user for demo purposes.
/// In production, implement this to get the current user from your authentication system.
/// </summary>
public class SampleUserProvider : IAuditUserProvider
{
    private readonly string _userName;

    public SampleUserProvider(string userName = "demo@auditax.sample")
    {
        _userName = userName;
    }

    public string GetCurrentUser()
    {
        return _userName;
    }
}

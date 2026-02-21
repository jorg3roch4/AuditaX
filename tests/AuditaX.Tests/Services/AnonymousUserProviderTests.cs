using AuditaX.Providers;

namespace AuditaX.Tests.Services;

public class AnonymousUserProviderTests
{
    [Fact]
    public void GetCurrentUser_ShouldReturnAnonymous()
    {
        var provider = new AnonymousUserProvider();

        var user = provider.GetCurrentUser();

        user.Should().Be("Anonymous");
    }
}

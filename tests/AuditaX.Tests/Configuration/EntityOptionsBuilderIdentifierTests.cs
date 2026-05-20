using AuditaX.Configuration;

namespace AuditaX.Tests.Configuration;

public class EntityOptionsBuilderIdentifierTests
{
    [Fact]
    public void WithIdentifier_Compiles_Selector_Like_WithKey()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntity<FakeUser>("User")
            .WithKey(u => u.Id)
            .WithIdentifier(u => u.UserName);

        // Assert
        var entityOptions = options.GetEntity(typeof(FakeUser));
        entityOptions.Should().NotBeNull();
        entityOptions!.IdentifierSelector.Should().NotBeNull();

        var user = new FakeUser { Id = 1, UserName = "alice" };
        entityOptions.IdentifierSelector!(user).Should().Be("alice");
    }

    [Fact]
    public void WithIdentifier_Returns_This_For_Chaining()
    {
        // Arrange
        var options = new AuditaXOptions();
        var builder = options.ConfigureEntity<FakeUser>("User");

        // Act
        var returned = builder.WithIdentifier(u => u.UserName);

        // Assert
        returned.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithIdentifier_With_Null_Property_Returns_Empty_String()
    {
        // Arrange
        var options = new AuditaXOptions();
        options.ConfigureEntity<FakeUser>("User")
            .WithIdentifier(u => u.UserName);

        var entityOptions = options.GetEntity(typeof(FakeUser))!;
        var user = new FakeUser { Id = 1, UserName = null! };

        // Act
        var result = entityOptions.IdentifierSelector!(user);

        // Assert
        result.Should().Be(string.Empty);
    }

    private sealed class FakeUser
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}

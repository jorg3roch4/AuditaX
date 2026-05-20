using AuditaX.Configuration;

namespace AuditaX.Tests.Configuration;

public class EntityOptionsIdentifierTests
{
    [Fact]
    public void GetIdentifier_Returns_Identifier_When_Set()
    {
        // Arrange
        var options = new EntityOptions
        {
            EntityType = typeof(FakeUser),
            IdentifierSelector = _ => "display-x"
        };

        var entity = new FakeUser { Id = 1, UserName = "alice" };

        // Act
        var result = options.GetIdentifier(entity);

        // Assert
        result.Should().Be("display-x");
    }

    [Fact]
    public void GetIdentifier_Falls_Back_To_Key_When_Not_Set()
    {
        // Arrange
        var options = new EntityOptions
        {
            EntityType = typeof(FakeUser),
            KeySelector = e => ((FakeUser)e).Id.ToString()
            // IdentifierSelector intentionally null
        };

        var entity = new FakeUser { Id = 42, UserName = "alice" };

        // Act
        var result = options.GetIdentifier(entity);

        // Assert
        result.Should().Be("42");
        result.Should().Be(options.GetKey(entity));
    }

    [Fact]
    public void ResolveIdentifierSelector_Resolves_From_Property_Name()
    {
        // Arrange
        var options = new EntityOptions
        {
            EntityType = typeof(FakeUser),
            Identifier = "UserName"
        };

        // Act
        options.ResolveIdentifierSelector();

        // Assert
        options.IdentifierSelector.Should().NotBeNull();

        var entity = new FakeUser { Id = 1, UserName = "alice" };
        options.IdentifierSelector!(entity).Should().Be("alice");
    }

    [Fact]
    public void ResolveIdentifierSelector_NoOp_When_Identifier_Null_Or_Empty()
    {
        // Arrange
        var options = new EntityOptions
        {
            EntityType = typeof(FakeUser),
            Identifier = null
        };

        // Act
        options.ResolveIdentifierSelector();

        // Assert
        options.IdentifierSelector.Should().BeNull();
    }

    [Fact]
    public void ResolveIdentifierSelector_NoOp_When_Selector_Already_Set()
    {
        // Arrange — Fluent API takes precedence over JSON
        Func<object, string> existing = _ => "fluent-set";
        var options = new EntityOptions
        {
            EntityType = typeof(FakeUser),
            Identifier = "UserName",
            IdentifierSelector = existing
        };

        // Act
        options.ResolveIdentifierSelector();

        // Assert
        options.IdentifierSelector.Should().BeSameAs(existing);
    }

    private sealed class FakeUser
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}

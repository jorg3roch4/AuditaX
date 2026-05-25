using AuditaX.Configuration;

namespace AuditaX.Tests.Configuration;

public class EntityOptionsReferenceTests
{
    [Fact]
    public void GetReference_Returns_Null_When_Selector_Not_Configured()
    {
        var options = new EntityOptions
        {
            EntityType = typeof(FakeUser)
        };

        var entity = new FakeUser { Id = 1, UserName = "alice" };

        var result = options.GetReference(entity);

        result.Should().BeNull();
    }

    [Fact]
    public void GetReference_Returns_Selector_Output_When_Within_Limit()
    {
        var options = new EntityOptions
        {
            EntityType = typeof(FakeUser),
            ReferenceSelector = e => ((FakeUser)e).UserName
        };

        var entity = new FakeUser { Id = 1, UserName = "alice@example.com" };

        var result = options.GetReference(entity);

        result.Should().Be("alice@example.com");
    }

    [Fact]
    public void GetReference_Truncates_Silently_When_Selector_Exceeds_MaxReferenceLength()
    {
        var longValue = new string('a', EntityOptions.MaxReferenceLength + 50);
        var options = new EntityOptions
        {
            EntityType = typeof(FakeUser),
            ReferenceSelector = _ => longValue
        };

        var entity = new FakeUser { Id = 1, UserName = "alice" };

        var result = options.GetReference(entity);

        result.Should().NotBeNull();
        result!.Length.Should().Be(EntityOptions.MaxReferenceLength);
        result.Should().Be(longValue.Substring(0, EntityOptions.MaxReferenceLength));
    }

    [Fact]
    public void GetReference_Preserves_Exact_Length_At_Limit()
    {
        var exactValue = new string('b', EntityOptions.MaxReferenceLength);
        var options = new EntityOptions
        {
            EntityType = typeof(FakeUser),
            ReferenceSelector = _ => exactValue
        };

        var entity = new FakeUser { Id = 1, UserName = "alice" };

        var result = options.GetReference(entity);

        result.Should().Be(exactValue);
        result!.Length.Should().Be(EntityOptions.MaxReferenceLength);
    }

    private sealed class FakeUser
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}

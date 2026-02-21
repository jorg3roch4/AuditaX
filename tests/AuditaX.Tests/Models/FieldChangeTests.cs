using AuditaX.Models;

namespace AuditaX.Tests.Models;

public class FieldChangeTests
{
    [Fact]
    public void DefaultValues_ShouldHaveEmptyNameAndNullValues()
    {
        var change = new FieldChange();

        change.Name.Should().Be(string.Empty);
        change.Before.Should().BeNull();
        change.After.Should().BeNull();
        change.Value.Should().BeNull();
    }

    [Fact]
    public void UpdateChange_ShouldUseBeforeAndAfter()
    {
        var change = new FieldChange
        {
            Name = "Price",
            Before = "10.00",
            After = "20.00"
        };

        change.Name.Should().Be("Price");
        change.Before.Should().Be("10.00");
        change.After.Should().Be("20.00");
        change.Value.Should().BeNull();
    }

    [Fact]
    public void AddChange_ShouldUseValue()
    {
        var change = new FieldChange
        {
            Name = "TagName",
            Value = "Electronics"
        };

        change.Name.Should().Be("TagName");
        change.Value.Should().Be("Electronics");
        change.Before.Should().BeNull();
        change.After.Should().BeNull();
    }

    [Fact]
    public void IsRecord_ShouldSupportValueEquality()
    {
        var change1 = new FieldChange { Name = "Price", Before = "10", After = "20" };
        var change2 = new FieldChange { Name = "Price", Before = "10", After = "20" };

        change1.Should().Be(change2);
    }

    [Fact]
    public void IsRecord_DifferentValues_ShouldNotBeEqual()
    {
        var change1 = new FieldChange { Name = "Price", Before = "10", After = "20" };
        var change2 = new FieldChange { Name = "Price", Before = "10", After = "30" };

        change1.Should().NotBe(change2);
    }
}

using AuditaX.Models;

namespace AuditaX.Tests.Models;

public class ExpectedColumnDefinitionTests
{
    [Fact]
    public void DefaultValues_ShouldHaveDefaults()
    {
        var def = new ExpectedColumnDefinition();

        def.ColumnName.Should().Be(string.Empty);
        def.AcceptableDataTypes.Should().BeEmpty();
        def.MinLength.Should().BeNull();
        def.RequireNotNull.Should().BeFalse();
    }

    [Fact]
    public void ExpectedTypeDescription_SingleType_ShouldReturnTypeName()
    {
        var def = new ExpectedColumnDefinition
        {
            AcceptableDataTypes = ["uniqueidentifier"]
        };

        def.ExpectedTypeDescription.Should().Be("uniqueidentifier");
    }

    [Fact]
    public void ExpectedTypeDescription_MultipleTypes_ShouldJoinWithOr()
    {
        var def = new ExpectedColumnDefinition
        {
            AcceptableDataTypes = ["nvarchar", "varchar"]
        };

        def.ExpectedTypeDescription.Should().Be("nvarchar or varchar");
    }

    [Fact]
    public void ExpectedTypeDescription_WithMaxMinLength_ShouldAppendMax()
    {
        var def = new ExpectedColumnDefinition
        {
            AcceptableDataTypes = ["nvarchar", "varchar"],
            MinLength = -1
        };

        def.ExpectedTypeDescription.Should().Be("nvarchar or varchar(MAX)");
    }

    [Fact]
    public void ExpectedTypeDescription_WithPositiveMinLength_ShouldAppendLengthPlus()
    {
        var def = new ExpectedColumnDefinition
        {
            AcceptableDataTypes = ["nvarchar", "varchar"],
            MinLength = 50
        };

        def.ExpectedTypeDescription.Should().Be("nvarchar or varchar(50+)");
    }

    [Fact]
    public void ExpectedTypeDescription_WithNullMinLength_ShouldNotAppendLength()
    {
        var def = new ExpectedColumnDefinition
        {
            AcceptableDataTypes = ["xml"],
            MinLength = null
        };

        def.ExpectedTypeDescription.Should().Be("xml");
    }

    [Fact]
    public void WithAllProperties_ShouldRetainValues()
    {
        var def = new ExpectedColumnDefinition
        {
            ColumnName = "SourceName",
            AcceptableDataTypes = ["nvarchar", "varchar"],
            MinLength = 50,
            RequireNotNull = true
        };

        def.ColumnName.Should().Be("SourceName");
        def.AcceptableDataTypes.Should().HaveCount(2);
        def.MinLength.Should().Be(50);
        def.RequireNotNull.Should().BeTrue();
    }

    [Fact]
    public void ExpectedTypeDescription_ThreeTypes_ShouldJoinAllWithOr()
    {
        var def = new ExpectedColumnDefinition
        {
            AcceptableDataTypes = ["character varying", "varchar", "text"]
        };

        def.ExpectedTypeDescription.Should().Be("character varying or varchar or text");
    }
}

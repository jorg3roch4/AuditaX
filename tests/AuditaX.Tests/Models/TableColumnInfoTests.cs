using AuditaX.Models;

namespace AuditaX.Tests.Models;

public class TableColumnInfoTests
{
    [Fact]
    public void DefaultValues_ShouldHaveDefaults()
    {
        var info = new TableColumnInfo();

        info.ColumnName.Should().Be(string.Empty);
        info.DataType.Should().Be(string.Empty);
        info.MaxLength.Should().BeNull();
        info.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void WithAllProperties_ShouldRetainValues()
    {
        var info = new TableColumnInfo
        {
            ColumnName = "SourceName",
            DataType = "nvarchar",
            MaxLength = 50,
            IsNullable = false
        };

        info.ColumnName.Should().Be("SourceName");
        info.DataType.Should().Be("nvarchar");
        info.MaxLength.Should().Be(50);
        info.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void MaxLength_ShouldBeNullable()
    {
        var infoWithLength = new TableColumnInfo { MaxLength = 900 };
        var infoWithoutLength = new TableColumnInfo { MaxLength = null };

        infoWithLength.MaxLength.Should().Be(900);
        infoWithoutLength.MaxLength.Should().BeNull();
    }

    [Fact]
    public void MaxLength_NegativeOne_ShouldIndicateMax()
    {
        var info = new TableColumnInfo
        {
            ColumnName = "AuditLog",
            DataType = "nvarchar",
            MaxLength = -1
        };

        info.MaxLength.Should().Be(-1);
    }
}

using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.SqlServer.Providers;

namespace AuditaX.Dapper.Tests.Providers;

public class SqlServerProviderPaginationTests
{
    private readonly AuditaXOptions _jsonOptions;
    private readonly AuditaXOptions _xmlOptions;

    public SqlServerProviderPaginationTests()
    {
        _jsonOptions = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            LogFormat = LogFormat.Json
        };

        _xmlOptions = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            LogFormat = LogFormat.Xml
        };
    }

    #region GetSelectBySourceNameSql Tests

    [Fact]
    public void GetSelectBySourceNameSql_ShouldContainOffsetFetch()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectBySourceNameSql(0, 100);

        // Assert
        sql.Should().Contain("OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");
    }

    [Fact]
    public void GetSelectBySourceNameSql_ShouldContainOrderBy()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectBySourceNameSql(0, 100);

        // Assert
        sql.Should().Contain("ORDER BY");
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 20)]
    [InlineData(100, 50)]
    public void GetSelectBySourceNameSql_WithDifferentParameters_ShouldGenerateValidSql(int skip, int take)
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectBySourceNameSql(skip, take);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("@SourceName");
        sql.Should().Contain("@Skip");
        sql.Should().Contain("@Take");
    }

    #endregion

    #region GetSelectBySourceNameAndDateSql Tests

    [Fact]
    public void GetSelectBySourceNameAndDateSql_Json_ShouldContainOffsetFetch()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectBySourceNameAndDateSql(0, 100);

        // Assert
        sql.Should().Contain("OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");
    }

    [Fact]
    public void GetSelectBySourceNameAndDateSql_Xml_ShouldContainOffsetFetch()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        // Act
        var sql = provider.GetSelectBySourceNameAndDateSql(0, 100);

        // Assert
        sql.Should().Contain("OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");
    }

    [Fact]
    public void GetSelectBySourceNameAndDateSql_ShouldContainDateParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectBySourceNameAndDateSql(0, 100);

        // Assert
        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(50, 100)]
    public void GetSelectBySourceNameAndDateSql_WithDifferentParameters_ShouldGenerateValidSql(int skip, int take)
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectBySourceNameAndDateSql(skip, take);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("@Skip");
        sql.Should().Contain("@Take");
    }

    #endregion

    #region GetSelectSummaryBySourceNameSql Tests

    [Fact]
    public void GetSelectSummaryBySourceNameSql_Json_ShouldContainOffsetFetch()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectSummaryBySourceNameSql(0, 100);

        // Assert
        sql.Should().Contain("OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");
    }

    [Fact]
    public void GetSelectSummaryBySourceNameSql_Xml_ShouldContainOffsetFetch()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        // Act
        var sql = provider.GetSelectSummaryBySourceNameSql(0, 100);

        // Assert
        sql.Should().Contain("OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");
    }

    [Fact]
    public void GetSelectSummaryBySourceNameSql_ShouldReturnSummaryColumns()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectSummaryBySourceNameSql(0, 100);

        // Assert
        sql.Should().Contain("SourceName");
        sql.Should().Contain("SourceKey");
        sql.Should().Contain("LastAction");
        sql.Should().Contain("LastTimestamp");
        sql.Should().Contain("LastUser");
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(20, 30)]
    public void GetSelectSummaryBySourceNameSql_WithDifferentParameters_ShouldGenerateValidSql(int skip, int take)
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectSummaryBySourceNameSql(skip, take);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("@Skip");
        sql.Should().Contain("@Take");
    }

    #endregion

    #region Non-Paginated Queries Should Not Change

    [Fact]
    public void SelectBySourceNameAndActionSql_ShouldNotHavePagination()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.SelectBySourceNameAndActionSql;

        // Assert - These queries don't support pagination
        sql.Should().NotContain("OFFSET");
        sql.Should().NotContain("FETCH");
    }

    [Fact]
    public void SelectBySourceNameActionAndDateSql_ShouldNotHavePagination()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.SelectBySourceNameActionAndDateSql;

        // Assert - These queries don't support pagination
        sql.Should().NotContain("OFFSET");
        sql.Should().NotContain("FETCH");
    }

    [Fact]
    public void SelectByEntitySql_ShouldNotHavePagination()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.SelectByEntitySql;

        // Assert - Single entity lookup doesn't need pagination
        sql.Should().NotContain("OFFSET");
        sql.Should().NotContain("FETCH");
    }

    #endregion
}

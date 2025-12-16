using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.PostgreSql.Providers;

namespace AuditaX.Dapper.Tests.Providers;

public class PostgreSqlProviderPaginationTests
{
    private readonly AuditaXOptions _jsonOptions;
    private readonly AuditaXOptions _xmlOptions;

    public PostgreSqlProviderPaginationTests()
    {
        _jsonOptions = new AuditaXOptions
        {
            TableName = "audit_log",
            Schema = "public",
            LogFormat = LogFormat.Json
        };

        _xmlOptions = new AuditaXOptions
        {
            TableName = "audit_log",
            Schema = "public",
            LogFormat = LogFormat.Xml
        };
    }

    #region GetSelectBySourceNameSql Tests

    [Fact]
    public void GetSelectBySourceNameSql_ShouldContainLimitOffset()
    {
        // Arrange
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectBySourceNameSql(0, 100);

        // Assert
        sql.Should().Contain("LIMIT @Take OFFSET @Skip");
    }

    [Fact]
    public void GetSelectBySourceNameSql_ShouldContainOrderBy()
    {
        // Arrange
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

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
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

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
    public void GetSelectBySourceNameAndDateSql_Json_ShouldContainLimitOffset()
    {
        // Arrange
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectBySourceNameAndDateSql(0, 100);

        // Assert
        sql.Should().Contain("LIMIT @Take OFFSET @Skip");
    }

    [Fact]
    public void GetSelectBySourceNameAndDateSql_Xml_ShouldContainLimitOffset()
    {
        // Arrange
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        // Act
        var sql = provider.GetSelectBySourceNameAndDateSql(0, 100);

        // Assert
        sql.Should().Contain("LIMIT @Take OFFSET @Skip");
    }

    [Fact]
    public void GetSelectBySourceNameAndDateSql_ShouldContainDateParameters()
    {
        // Arrange
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

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
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

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
    public void GetSelectSummaryBySourceNameSql_Json_ShouldContainLimitOffset()
    {
        // Arrange
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectSummaryBySourceNameSql(0, 100);

        // Assert
        sql.Should().Contain("LIMIT @Take OFFSET @Skip");
    }

    [Fact]
    public void GetSelectSummaryBySourceNameSql_Xml_ShouldContainLimitOffset()
    {
        // Arrange
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        // Act
        var sql = provider.GetSelectSummaryBySourceNameSql(0, 100);

        // Assert
        sql.Should().Contain("LIMIT @Take OFFSET @Skip");
    }

    [Fact]
    public void GetSelectSummaryBySourceNameSql_ShouldReturnSummaryColumns()
    {
        // Arrange
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

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
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

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
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.SelectBySourceNameAndActionSql;

        // Assert - These queries don't support pagination
        sql.Should().NotContain("LIMIT");
        sql.Should().NotContain("OFFSET");
    }

    [Fact]
    public void SelectBySourceNameActionAndDateSql_ShouldNotHavePagination()
    {
        // Arrange
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.SelectBySourceNameActionAndDateSql;

        // Assert - These queries don't support pagination
        sql.Should().NotContain("LIMIT");
        sql.Should().NotContain("OFFSET");
    }

    [Fact]
    public void SelectByEntitySql_ShouldNotHavePagination()
    {
        // Arrange
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.SelectByEntitySql;

        // Assert - Single entity lookup doesn't need pagination
        sql.Should().NotContain("LIMIT");
        sql.Should().NotContain("OFFSET");
    }

    #endregion

    #region PostgreSQL vs SQL Server Syntax Comparison

    [Fact]
    public void PostgreSql_ShouldUseLimitOffset_NotOffsetFetch()
    {
        // Arrange
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        // Act
        var sql = provider.GetSelectBySourceNameSql(0, 100);

        // Assert - PostgreSQL uses LIMIT/OFFSET, not OFFSET/FETCH
        sql.Should().Contain("LIMIT");
        sql.Should().Contain("OFFSET");
        sql.Should().NotContain("FETCH");
        sql.Should().NotContain("ROWS ONLY");
    }

    #endregion
}

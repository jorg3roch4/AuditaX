using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.PostgreSql.Providers;

namespace AuditaX.Dapper.Tests.Providers;

public class PostgreSqlProviderQueryTests
{
    private readonly AuditaXOptions _jsonOptions;
    private readonly AuditaXOptions _xmlOptions;

    public PostgreSqlProviderQueryTests()
    {
        _jsonOptions = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "public",
            LogFormat = LogFormat.Json
        };

        _xmlOptions = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "public",
            LogFormat = LogFormat.Xml
        };
    }

    #region PostgreSQL-specific Column Naming

    [Fact]
    public void ColumnNames_ShouldUseSnakeCase()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        provider.LogIdColumn.Should().Be("log_id");
        provider.SourceNameColumn.Should().Be("source_name");
        provider.SourceKeyColumn.Should().Be("source_key");
        provider.AuditLogColumn.Should().Be("audit_log");
    }

    [Fact]
    public void FullTableName_ShouldUseDoubleQuotesAndSnakeCase()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        provider.FullTableName.Should().Be("\"public\".\"audit_log\"");
    }

    [Fact]
    public void FullTableName_WithCustomSchema_ShouldUseLowercaseSchema()
    {
        var options = new AuditaXOptions { TableName = "AuditLog", Schema = "MySchema" };
        var provider = new PostgreSqlDatabaseProvider(options);

        provider.FullTableName.Should().Be("\"myschema\".\"audit_log\"");
    }

    [Fact]
    public void ToSnakeCase_ShouldConvertPascalCase()
    {
        var options = new AuditaXOptions { TableName = "MyAuditLogTable", Schema = "public" };
        var provider = new PostgreSqlDatabaseProvider(options);

        provider.FullTableName.Should().Contain("my_audit_log_table");
    }

    #endregion

    #region Pagination - LIMIT/OFFSET

    [Fact]
    public void GetSelectBySourceNameSql_ShouldUseLimitOffset()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectBySourceNameSql(0, 100);

        sql.Should().Contain("LIMIT @Take OFFSET @Skip");
        sql.Should().NotContain("OFFSET @Skip ROWS FETCH NEXT");
    }

    [Fact]
    public void GetSelectBySourceNameAndDateSql_ShouldUseLimitOffset()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectBySourceNameAndDateSql(0, 50);

        sql.Should().Contain("LIMIT @Take OFFSET @Skip");
    }

    [Fact]
    public void GetSelectSummaryBySourceNameSql_ShouldUseLimitOffset()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectSummaryBySourceNameSql(0, 50);

        sql.Should().Contain("LIMIT @Take OFFSET @Skip");
    }

    #endregion

    #region Count SQL Properties

    [Fact]
    public void CountBySourceNameSql_ShouldContainCountAndDoubleQuotes()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.CountBySourceNameSql;

        sql.Should().Contain("COUNT(*)");
        sql.Should().Contain("@SourceName");
        sql.Should().Contain("\"source_name\"");
    }

    [Fact]
    public void CountBySourceNameAndDateSql_Json_ShouldUseJsonbArrayElements()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.CountBySourceNameAndDateSql;

        sql.Should().Contain("jsonb_array_elements");
        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Fact]
    public void CountBySourceNameAndDateSql_Xml_ShouldUseXpath()
    {
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        var sql = provider.CountBySourceNameAndDateSql;

        sql.Should().Contain("xpath(");
        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Fact]
    public void CountBySourceNameAndActionSql_Json_ShouldUseJsonbArrayElements()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.CountBySourceNameAndActionSql;

        sql.Should().Contain("jsonb_array_elements");
        sql.Should().Contain("@Action");
    }

    [Fact]
    public void CountBySourceNameAndActionSql_Xml_ShouldUseXpath()
    {
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        var sql = provider.CountBySourceNameAndActionSql;

        sql.Should().Contain("xpath(");
        sql.Should().Contain("@Action");
    }

    [Fact]
    public void CountBySourceNameActionAndDateSql_Json_ShouldContainAllFilters()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.CountBySourceNameActionAndDateSql;

        sql.Should().Contain("jsonb_array_elements");
        sql.Should().Contain("@Action");
        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Fact]
    public void CountBySourceNameActionAndDateSql_Xml_ShouldContainAllFilters()
    {
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        var sql = provider.CountBySourceNameActionAndDateSql;

        sql.Should().Contain("xpath(");
        sql.Should().Contain("@Action");
        sql.Should().Contain("@FromDate");
    }

    [Fact]
    public void CountSummaryBySourceNameSql_ShouldEqualCountBySourceNameSql()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        provider.CountSummaryBySourceNameSql.Should().Be(provider.CountBySourceNameSql);
    }

    #endregion

    #region Action Paginated SQL

    [Fact]
    public void GetSelectBySourceNameAndActionSql_Json_ShouldContainLimitAndJsonb()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectBySourceNameAndActionSql(0, 50);

        sql.Should().Contain("LIMIT @Take OFFSET @Skip");
        sql.Should().Contain("jsonb_array_elements");
        sql.Should().Contain("@Action");
    }

    [Fact]
    public void GetSelectBySourceNameAndActionSql_Xml_ShouldContainLimitAndXpath()
    {
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        var sql = provider.GetSelectBySourceNameAndActionSql(0, 50);

        sql.Should().Contain("LIMIT @Take OFFSET @Skip");
        sql.Should().Contain("xpath(");
        sql.Should().Contain("@Action");
    }

    [Fact]
    public void GetSelectBySourceNameActionAndDateSql_Json_ShouldContainLimitAndJsonb()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectBySourceNameActionAndDateSql(10, 20);

        sql.Should().Contain("LIMIT @Take OFFSET @Skip");
        sql.Should().Contain("jsonb_array_elements");
        sql.Should().Contain("@Action");
        sql.Should().Contain("@FromDate");
    }

    [Fact]
    public void GetSelectBySourceNameActionAndDateSql_Xml_ShouldContainLimitAndXpath()
    {
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        var sql = provider.GetSelectBySourceNameActionAndDateSql(10, 20);

        sql.Should().Contain("LIMIT @Take OFFSET @Skip");
        sql.Should().Contain("xpath(");
        sql.Should().Contain("@Action");
        sql.Should().Contain("@FromDate");
    }

    #endregion

    #region Filtered Summary SQL

    [Fact]
    public void GetSelectFilteredSummaryBySourceNameSql_NoFilters_Json_ShouldUseJsonbOperators()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectFilteredSummaryBySourceNameSql(0, 10, null, false);

        sql.Should().Contain("->>");
        sql.Should().Contain("LastAction");
        sql.Should().Contain("LastTimestamp");
        sql.Should().Contain("LastUser");
        sql.Should().NotContain("@SourceKey");
        sql.Should().NotContain("@FromDate");
    }

    [Fact]
    public void GetSelectFilteredSummaryBySourceNameSql_NoFilters_Xml_ShouldUseXpath()
    {
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        var sql = provider.GetSelectFilteredSummaryBySourceNameSql(0, 10, null, false);

        sql.Should().Contain("xpath(");
        sql.Should().Contain("LastAction");
        sql.Should().NotContain("@SourceKey");
        sql.Should().NotContain("@FromDate");
    }

    [Fact]
    public void GetSelectFilteredSummaryBySourceNameSql_WithSourceKey_ShouldAddSourceKeyFilter()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectFilteredSummaryBySourceNameSql(0, 10, "42", false);

        sql.Should().Contain("@SourceKey");
    }

    [Fact]
    public void GetSelectFilteredSummaryBySourceNameSql_WithDateFilter_ShouldAddDateFilter()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectFilteredSummaryBySourceNameSql(0, 10, null, true);

        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Fact]
    public void GetSelectFilteredSummaryBySourceNameSql_WithBothFilters_ShouldAddBothFilters()
    {
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        var sql = provider.GetSelectFilteredSummaryBySourceNameSql(0, 10, "42", true);

        sql.Should().Contain("@SourceKey");
        sql.Should().Contain("@FromDate");
    }

    #endregion

    #region Count Filtered Summary SQL

    [Fact]
    public void GetCountFilteredSummaryBySourceNameSql_NoFilters_ShouldContainBasicCount()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.GetCountFilteredSummaryBySourceNameSql(null, false);

        sql.Should().Contain("COUNT(*)");
        sql.Should().Contain("@SourceName");
        sql.Should().NotContain("@SourceKey");
        sql.Should().NotContain("@FromDate");
    }

    [Fact]
    public void GetCountFilteredSummaryBySourceNameSql_WithSourceKey_ShouldAddSourceKeyFilter()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.GetCountFilteredSummaryBySourceNameSql("42", false);

        sql.Should().Contain("@SourceKey");
    }

    [Fact]
    public void GetCountFilteredSummaryBySourceNameSql_WithDateFilter_ShouldAddDateFilter()
    {
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        var sql = provider.GetCountFilteredSummaryBySourceNameSql(null, true);

        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Fact]
    public void GetCountFilteredSummaryBySourceNameSql_WithBothFilters_ShouldAddBothFilters()
    {
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        var sql = provider.GetCountFilteredSummaryBySourceNameSql("42", true);

        sql.Should().Contain("@SourceKey");
        sql.Should().Contain("@FromDate");
    }

    #endregion

    #region CreateSourceNameIndexSql

    [Fact]
    public void CreateSourceNameIndexSql_ShouldContainPostgreSqlSyntax()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.CreateSourceNameIndexSql;

        sql.Should().Contain("CREATE INDEX IF NOT EXISTS");
        sql.Should().Contain("ix_audit_log_source_name");
        sql.Should().Contain("\"source_name\"");
    }

    #endregion

    #region JSON vs XML SQL Format Differences

    [Fact]
    public void SelectBySourceNameAndActionSql_Json_ShouldUseJsonbFunctions()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.SelectBySourceNameAndActionSql;

        sql.Should().Contain("jsonb_array_elements");
        sql.Should().Contain("->>");
    }

    [Fact]
    public void SelectBySourceNameAndActionSql_Xml_ShouldUseXpathFunctions()
    {
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        var sql = provider.SelectBySourceNameAndActionSql;

        sql.Should().Contain("xpath(");
        sql.Should().Contain("unnest");
    }

    [Fact]
    public void GetSelectSummaryBySourceNameSql_Json_ShouldUseJsonbOperators()
    {
        var provider = new PostgreSqlDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectSummaryBySourceNameSql(0, 10);

        sql.Should().Contain("->>");
        sql.Should().Contain("->-1");
    }

    [Fact]
    public void GetSelectSummaryBySourceNameSql_Xml_ShouldUseXpath()
    {
        var provider = new PostgreSqlDatabaseProvider(_xmlOptions);

        var sql = provider.GetSelectSummaryBySourceNameSql(0, 10);

        sql.Should().Contain("xpath(");
        sql.Should().Contain("Entry[last()]");
    }

    #endregion
}

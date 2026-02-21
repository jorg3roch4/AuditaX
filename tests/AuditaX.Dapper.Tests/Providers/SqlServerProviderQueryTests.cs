using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.SqlServer.Providers;

namespace AuditaX.Dapper.Tests.Providers;

public class SqlServerProviderQueryTests
{
    private readonly AuditaXOptions _jsonOptions;
    private readonly AuditaXOptions _xmlOptions;

    public SqlServerProviderQueryTests()
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

    #region Count SQL Properties

    [Fact]
    public void CountBySourceNameSql_ShouldContainCountAndSourceNameParam()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.CountBySourceNameSql;

        sql.Should().Contain("COUNT(*)");
        sql.Should().Contain("@SourceName");
        sql.Should().Contain("[dbo].[AuditLog]");
    }

    [Fact]
    public void CountBySourceNameAndDateSql_Json_ShouldUseOpenjson()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.CountBySourceNameAndDateSql;

        sql.Should().Contain("OPENJSON");
        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Fact]
    public void CountBySourceNameAndDateSql_Xml_ShouldUseXmlExist()
    {
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        var sql = provider.CountBySourceNameAndDateSql;

        sql.Should().Contain(".exist(");
        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Fact]
    public void CountBySourceNameAndActionSql_Json_ShouldUseOpenjson()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.CountBySourceNameAndActionSql;

        sql.Should().Contain("OPENJSON");
        sql.Should().Contain("JSON_VALUE");
        sql.Should().Contain("@Action");
    }

    [Fact]
    public void CountBySourceNameAndActionSql_Xml_ShouldUseXmlExist()
    {
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        var sql = provider.CountBySourceNameAndActionSql;

        sql.Should().Contain(".exist(");
        sql.Should().Contain("@Action");
    }

    [Fact]
    public void CountBySourceNameActionAndDateSql_Json_ShouldUseOpenjsonWithBothFilters()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.CountBySourceNameActionAndDateSql;

        sql.Should().Contain("OPENJSON");
        sql.Should().Contain("@Action");
        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Fact]
    public void CountBySourceNameActionAndDateSql_Xml_ShouldUseXmlExistWithBothFilters()
    {
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        var sql = provider.CountBySourceNameActionAndDateSql;

        sql.Should().Contain(".exist(");
        sql.Should().Contain("@Action");
        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Fact]
    public void CountSummaryBySourceNameSql_ShouldEqualCountBySourceNameSql()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        provider.CountSummaryBySourceNameSql.Should().Be(provider.CountBySourceNameSql);
    }

    #endregion

    #region Action Paginated SQL

    [Fact]
    public void GetSelectBySourceNameAndActionSql_Json_ShouldContainPaginationAndOpenjson()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectBySourceNameAndActionSql(0, 50);

        sql.Should().Contain("OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");
        sql.Should().Contain("OPENJSON");
        sql.Should().Contain("@Action");
    }

    [Fact]
    public void GetSelectBySourceNameAndActionSql_Xml_ShouldContainPaginationAndXmlExist()
    {
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        var sql = provider.GetSelectBySourceNameAndActionSql(0, 50);

        sql.Should().Contain("OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");
        sql.Should().Contain(".exist(");
        sql.Should().Contain("@Action");
    }

    [Fact]
    public void GetSelectBySourceNameActionAndDateSql_Json_ShouldContainPaginationAndOpenjson()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectBySourceNameActionAndDateSql(10, 20);

        sql.Should().Contain("OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");
        sql.Should().Contain("OPENJSON");
        sql.Should().Contain("@Action");
        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Fact]
    public void GetSelectBySourceNameActionAndDateSql_Xml_ShouldContainPaginationAndXmlExist()
    {
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        var sql = provider.GetSelectBySourceNameActionAndDateSql(10, 20);

        sql.Should().Contain("OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");
        sql.Should().Contain(".exist(");
        sql.Should().Contain("@Action");
        sql.Should().Contain("@FromDate");
    }

    #endregion

    #region Filtered Summary SQL

    [Fact]
    public void GetSelectFilteredSummaryBySourceNameSql_NoFilters_Json_ShouldUseJsonCrossApply()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectFilteredSummaryBySourceNameSql(0, 10, null, false);

        sql.Should().Contain("CROSS APPLY");
        sql.Should().Contain("OPENJSON");
        sql.Should().Contain("LastAction");
        sql.Should().Contain("LastTimestamp");
        sql.Should().Contain("LastUser");
        sql.Should().NotContain("@SourceKey");
        sql.Should().NotContain("@FromDate");
    }

    [Fact]
    public void GetSelectFilteredSummaryBySourceNameSql_NoFilters_Xml_ShouldUseXmlValue()
    {
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        var sql = provider.GetSelectFilteredSummaryBySourceNameSql(0, 10, null, false);

        sql.Should().Contain(".value(");
        sql.Should().Contain("LastAction");
        sql.Should().NotContain("@SourceKey");
        sql.Should().NotContain("@FromDate");
    }

    [Fact]
    public void GetSelectFilteredSummaryBySourceNameSql_WithSourceKey_ShouldAddSourceKeyFilter()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectFilteredSummaryBySourceNameSql(0, 10, "42", false);

        sql.Should().Contain("@SourceKey");
    }

    [Fact]
    public void GetSelectFilteredSummaryBySourceNameSql_WithDateFilter_ShouldAddDateFilter()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.GetSelectFilteredSummaryBySourceNameSql(0, 10, null, true);

        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Fact]
    public void GetSelectFilteredSummaryBySourceNameSql_WithBothFilters_ShouldAddBothFilters()
    {
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        var sql = provider.GetSelectFilteredSummaryBySourceNameSql(0, 10, "42", true);

        sql.Should().Contain("@SourceKey");
        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    #endregion

    #region Count Filtered Summary SQL

    [Fact]
    public void GetCountFilteredSummaryBySourceNameSql_NoFilters_ShouldContainBasicCount()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.GetCountFilteredSummaryBySourceNameSql(null, false);

        sql.Should().Contain("COUNT(*)");
        sql.Should().Contain("@SourceName");
        sql.Should().NotContain("@SourceKey");
        sql.Should().NotContain("@FromDate");
    }

    [Fact]
    public void GetCountFilteredSummaryBySourceNameSql_WithSourceKey_ShouldAddSourceKeyFilter()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.GetCountFilteredSummaryBySourceNameSql("42", false);

        sql.Should().Contain("@SourceKey");
    }

    [Fact]
    public void GetCountFilteredSummaryBySourceNameSql_WithDateFilter_ShouldAddDateFilter()
    {
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        var sql = provider.GetCountFilteredSummaryBySourceNameSql(null, true);

        sql.Should().Contain("@FromDate");
        sql.Should().Contain("@ToDate");
    }

    [Fact]
    public void GetCountFilteredSummaryBySourceNameSql_WithBothFilters_ShouldAddBothFilters()
    {
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        var sql = provider.GetCountFilteredSummaryBySourceNameSql("42", true);

        sql.Should().Contain("@SourceKey");
        sql.Should().Contain("@FromDate");
    }

    #endregion

    #region CreateSourceNameIndexSql

    [Fact]
    public void CreateSourceNameIndexSql_ShouldContainIndexCreation()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.CreateSourceNameIndexSql;

        sql.Should().Contain("CREATE NONCLUSTERED INDEX");
        sql.Should().Contain("IX_AuditLog_SourceName");
        sql.Should().Contain("[SourceName]");
        sql.Should().Contain("IF NOT EXISTS");
    }

    #endregion

    #region JSON vs XML SQL Format Differences

    [Fact]
    public void SelectBySourceNameAndActionSql_Json_ShouldUseJsonFunctions()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.SelectBySourceNameAndActionSql;

        sql.Should().Contain("OPENJSON");
        sql.Should().Contain("JSON_VALUE");
    }

    [Fact]
    public void SelectBySourceNameAndActionSql_Xml_ShouldUseXmlFunctions()
    {
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        var sql = provider.SelectBySourceNameAndActionSql;

        sql.Should().Contain(".exist(");
        sql.Should().Contain("/AuditLog/Entry");
    }

    [Fact]
    public void SelectBySourceNameActionAndDateSql_Json_ShouldUseJsonFunctions()
    {
        var provider = new SqlServerDatabaseProvider(_jsonOptions);

        var sql = provider.SelectBySourceNameActionAndDateSql;

        sql.Should().Contain("OPENJSON");
        sql.Should().Contain("JSON_VALUE");
        sql.Should().Contain("TRY_CAST");
    }

    [Fact]
    public void SelectBySourceNameActionAndDateSql_Xml_ShouldUseXmlFunctions()
    {
        var provider = new SqlServerDatabaseProvider(_xmlOptions);

        var sql = provider.SelectBySourceNameActionAndDateSql;

        sql.Should().Contain(".exist(");
        sql.Should().Contain("sql:variable");
    }

    #endregion
}

using Microsoft.EntityFrameworkCore;
using Moq;
using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.EntityFramework.Validators;

namespace AuditaX.EntityFramework.Tests.Validators;

/// <summary>
/// Tests for EfAuditStartupValidator construction.
/// Note: ValidateAsync tests require a real database connection since
/// DatabaseFacade.GetDbConnection() is a non-mockable extension method.
/// </summary>
public class EfAuditStartupValidatorTests
{
    private readonly Mock<IDatabaseProvider> _providerMock;
    private readonly AuditaXOptions _options;

    public EfAuditStartupValidatorTests()
    {
        _providerMock = new Mock<IDatabaseProvider>();
        _options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            LogFormat = LogFormat.Xml
        };

        SetupDefaultProviderMock();
    }

    private void SetupDefaultProviderMock()
    {
        _providerMock.Setup(p => p.FullTableName).Returns("[dbo].[AuditLog]");
        _providerMock.Setup(p => p.AuditLogColumn).Returns("AuditLog");
        _providerMock.Setup(p => p.CheckTableExistsSql).Returns("SELECT 1");
        _providerMock.Setup(p => p.CreateTableSql).Returns("CREATE TABLE ...");
        _providerMock.Setup(p => p.GetTableStructureSql).Returns("SELECT ...");
        _providerMock.Setup(p => p.ExpectedXmlColumnType).Returns("xml");
        _providerMock.Setup(p => p.ExpectedJsonColumnType).Returns("nvarchar");
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        var dbContext = new Mock<DbContext>();

        var act = () => new EfAuditStartupValidator(
            dbContext.Object,
            _providerMock.Object,
            _options);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ShouldImplementIAuditStartupValidator()
    {
        var dbContext = new Mock<DbContext>();

        var validator = new EfAuditStartupValidator(
            dbContext.Object,
            _providerMock.Object,
            _options);

        validator.Should().BeAssignableTo<IAuditStartupValidator>();
    }

    [Fact]
    public void Constructor_WithJsonFormat_ShouldNotThrow()
    {
        var dbContext = new Mock<DbContext>();
        var jsonOptions = new AuditaXOptions { LogFormat = LogFormat.Json };

        var act = () => new EfAuditStartupValidator(
            dbContext.Object,
            _providerMock.Object,
            jsonOptions);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithAutoCreateTable_ShouldNotThrow()
    {
        var dbContext = new Mock<DbContext>();
        var autoCreateOptions = new AuditaXOptions { AutoCreateTable = true };

        var act = () => new EfAuditStartupValidator(
            dbContext.Object,
            _providerMock.Object,
            autoCreateOptions);

        act.Should().NotThrow();
    }
}

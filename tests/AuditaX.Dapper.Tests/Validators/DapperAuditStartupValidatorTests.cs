using System.Data;
using Moq;
using AuditaX.Configuration;
using AuditaX.Dapper.Validators;
using AuditaX.Enums;
using AuditaX.Interfaces;

namespace AuditaX.Dapper.Tests.Validators;

public class DapperAuditStartupValidatorTests
{
    private readonly Mock<IDbConnection> _connectionMock;
    private readonly Mock<IDatabaseProvider> _providerMock;
    private readonly AuditaXOptions _options;

    public DapperAuditStartupValidatorTests()
    {
        _connectionMock = new Mock<IDbConnection>();
        _providerMock = new Mock<IDatabaseProvider>();
        _options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            LogFormat = LogFormat.Xml
        };
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        var act = () => new DapperAuditStartupValidator(
            _connectionMock.Object,
            _providerMock.Object,
            _options);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ShouldAcceptAllParameters()
    {
        var validator = new DapperAuditStartupValidator(
            _connectionMock.Object,
            _providerMock.Object,
            _options);

        validator.Should().NotBeNull();
        validator.Should().BeAssignableTo<IAuditStartupValidator>();
    }

    [Fact]
    public void Constructor_WithJsonFormat_ShouldNotThrow()
    {
        var jsonOptions = new AuditaXOptions { LogFormat = LogFormat.Json };

        var act = () => new DapperAuditStartupValidator(
            _connectionMock.Object,
            _providerMock.Object,
            jsonOptions);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithAutoCreateTable_ShouldNotThrow()
    {
        var autoCreateOptions = new AuditaXOptions { AutoCreateTable = true };

        var act = () => new DapperAuditStartupValidator(
            _connectionMock.Object,
            _providerMock.Object,
            autoCreateOptions);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validator_ShouldImplementIAuditStartupValidator()
    {
        var validator = new DapperAuditStartupValidator(
            _connectionMock.Object,
            _providerMock.Object,
            _options);

        validator.Should().BeAssignableTo<IAuditStartupValidator>();
    }

    [Fact]
    public void Constructor_WithOpenConnection_ShouldNotThrow()
    {
        _connectionMock.Setup(c => c.State).Returns(ConnectionState.Open);

        var act = () => new DapperAuditStartupValidator(
            _connectionMock.Object,
            _providerMock.Object,
            _options);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithClosedConnection_ShouldNotThrow()
    {
        _connectionMock.Setup(c => c.State).Returns(ConnectionState.Closed);

        var act = () => new DapperAuditStartupValidator(
            _connectionMock.Object,
            _providerMock.Object,
            _options);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithCustomTableName_ShouldNotThrow()
    {
        var customOptions = new AuditaXOptions
        {
            TableName = "CustomAuditLog",
            Schema = "audit"
        };

        var act = () => new DapperAuditStartupValidator(
            _connectionMock.Object,
            _providerMock.Object,
            customOptions);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithDefaultOptions_ShouldNotThrow()
    {
        var act = () => new DapperAuditStartupValidator(
            _connectionMock.Object,
            _providerMock.Object,
            new AuditaXOptions());

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithLoggingEnabled_ShouldNotThrow()
    {
        var loggingOptions = new AuditaXOptions { EnableLogging = true };

        var act = () => new DapperAuditStartupValidator(
            _connectionMock.Object,
            _providerMock.Object,
            loggingOptions);

        act.Should().NotThrow();
    }
}

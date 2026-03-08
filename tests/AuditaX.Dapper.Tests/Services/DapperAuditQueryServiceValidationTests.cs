using System.Data;
using AuditaX.Configuration;
using AuditaX.Dapper.Services;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.SqlServer.Providers;
using AuditaX.Validation;
using Moq;

namespace AuditaX.Dapper.Tests.Services;

public class DapperAuditQueryServiceValidationTests
{
    private readonly Mock<IDbConnection> _mockConnection;
    private readonly Mock<IChangeLogService> _mockChangeLogService;
    private readonly DapperAuditQueryService _service;

    public DapperAuditQueryServiceValidationTests()
    {
        _mockConnection = new Mock<IDbConnection>();
        _mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        _mockChangeLogService = new Mock<IChangeLogService>();

        var options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            LogFormat = LogFormat.Json
        };

        _service = new DapperAuditQueryService(
            _mockConnection.Object,
            new SqlServerDatabaseProvider(options),
            _mockChangeLogService.Object);
    }

    #region skip validation

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetBySourceNameAsync_WhenSkipNegative_ReturnsError(int skip)
    {
        var result = await _service.GetBySourceNameAsync("Product", skip: skip);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SkipNegative);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetBySourceNameAndDateAsync_WhenSkipNegative_ReturnsError(int skip)
    {
        var result = await _service.GetBySourceNameAndDateAsync("Product", DateTime.UtcNow.AddDays(-1), skip: skip);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SkipNegative);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetPagedBySourceNameAsync_WhenSkipNegative_ReturnsError(int skip)
    {
        var result = await _service.GetPagedBySourceNameAsync("Product", skip: skip);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SkipNegative);
    }

    #endregion

    #region take validation

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetBySourceNameAsync_WhenTakeInvalid_ReturnsError(int take)
    {
        var result = await _service.GetBySourceNameAsync("Product", take: take);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.TakeOutOfRange(AuditQueryValidator.MaxTake));
    }

    [Fact]
    public async Task GetBySourceNameAsync_WhenTakeExceedsMax_ReturnsError()
    {
        var result = await _service.GetBySourceNameAsync("Product", take: AuditQueryValidator.MaxTake + 1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.TakeOutOfRange(AuditQueryValidator.MaxTake));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPagedBySourceNameAndActionAsync_WhenTakeInvalid_ReturnsError(int take)
    {
        var result = await _service.GetPagedBySourceNameAndActionAsync("Product", AuditAction.Created, take: take);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.TakeOutOfRange(AuditQueryValidator.MaxTake));
    }

    [Fact]
    public async Task GetPagedBySourceNameActionAndDateAsync_WhenTakeExceedsMax_ReturnsError()
    {
        var result = await _service.GetPagedBySourceNameActionAndDateAsync(
            "Product", AuditAction.Created, DateTime.UtcNow.AddDays(-1), take: AuditQueryValidator.MaxTake + 1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.TakeOutOfRange(AuditQueryValidator.MaxTake));
    }

    #endregion

    #region date range validation

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WhenFromDateNotUtc_ReturnsError()
    {
        var fromDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);

        var result = await _service.GetBySourceNameAndDateAsync("Product", fromDate);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.DateKindInvalid("fromDate"));
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WhenToDateNotUtc_ReturnsError()
    {
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);

        var result = await _service.GetBySourceNameAndDateAsync("Product", fromDate, toDate);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.DateKindInvalid("toDate"));
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WhenFromDateAfterToDate_ReturnsError()
    {
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-1);

        var result = await _service.GetBySourceNameAndDateAsync("Product", fromDate, toDate);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.DateRangeInverted);
    }

    [Fact]
    public async Task GetBySourceNameActionAndDateAsync_WhenFromDateAfterToDate_ReturnsError()
    {
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-1);

        var result = await _service.GetBySourceNameActionAndDateAsync("Product", AuditAction.Created, fromDate, toDate);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.DateRangeInverted);
    }

    [Fact]
    public async Task GetPagedBySourceNameAndDateAsync_WhenFromDateNotUtc_ReturnsError()
    {
        var fromDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var result = await _service.GetPagedBySourceNameAndDateAsync("Product", fromDate);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.DateKindInvalid("fromDate"));
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_WhenFromDateAfterToDate_ReturnsError()
    {
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-1);

        var result = await _service.GetPagedSummaryBySourceNameAsync("Product", null, fromDate, toDate);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.DateRangeInverted);
    }

    #endregion

    #region optional sourceKey validation

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPagedSummaryBySourceNameAsync_WhenSourceKeyEmpty_ReturnsError(string sourceKey)
    {
        var result = await _service.GetPagedSummaryBySourceNameAsync("Product", sourceKey, null, null);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SourceKeyEmpty);
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_WhenSourceKeyTooLong_ReturnsError()
    {
        var sourceKey = new string('1', AuditQueryValidator.MaxSourceKeyLength + 1);

        var result = await _service.GetPagedSummaryBySourceNameAsync("Product", sourceKey, null, null);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SourceKeyTooLong(AuditQueryValidator.MaxSourceKeyLength));
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_WhenSourceKeyIsNull_SkipsKeyValidation()
    {
        // Null sourceKey is valid — key validation is skipped and the DB call is attempted.
        // Mock connection throws at that point (expected in unit tests).
        var action = async () => await _service.GetPagedSummaryBySourceNameAsync("Product", null, null, null);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region action enum validation

    [Fact]
    public async Task GetBySourceNameAndActionAsync_WhenInvalidAction_ReturnsError()
    {
        var invalidAction = (AuditAction)999;

        var result = await _service.GetBySourceNameAndActionAsync("Product", invalidAction);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.InvalidAction(999));
    }

    [Fact]
    public async Task GetBySourceNameActionAndDateAsync_WhenInvalidAction_ReturnsError()
    {
        var invalidAction = (AuditAction)999;

        var result = await _service.GetBySourceNameActionAndDateAsync("Product", invalidAction, DateTime.UtcNow.AddDays(-1));

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.InvalidAction(999));
    }

    [Fact]
    public async Task GetPagedBySourceNameAndActionAsync_WhenInvalidAction_ReturnsError()
    {
        var invalidAction = (AuditAction)999;

        var result = await _service.GetPagedBySourceNameAndActionAsync("Product", invalidAction);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.InvalidAction(999));
    }

    [Fact]
    public async Task GetPagedBySourceNameActionAndDateAsync_WhenInvalidAction_ReturnsError()
    {
        var invalidAction = (AuditAction)999;

        var result = await _service.GetPagedBySourceNameActionAndDateAsync(
            "Product", invalidAction, DateTime.UtcNow.AddDays(-1));

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.InvalidAction(999));
    }

    #endregion

    #region validation order — static before DB

    [Fact]
    public async Task GetBySourceNameAsync_WhenSkipNegativeAndSourceNameEmpty_ReturnsSkipError()
    {
        // Static validation runs before DB — skip error takes priority over sourceName error
        var result = await _service.GetBySourceNameAsync("", skip: -1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SkipNegative);
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WhenSkipNegativeAndFromDateNotUtc_ReturnsSkipError()
    {
        // Pagination is validated first
        var result = await _service.GetBySourceNameAndDateAsync(
            "Product",
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local),
            skip: -1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SkipNegative);
    }

    [Fact]
    public async Task GetPagedBySourceNameAndActionAsync_WhenTakeInvalidAndActionInvalid_ReturnsTakeError()
    {
        // Pagination is validated first, then action
        var result = await _service.GetPagedBySourceNameAndActionAsync(
            "Product", (AuditAction)999, take: 0);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.TakeOutOfRange(AuditQueryValidator.MaxTake));
    }

    #endregion
}

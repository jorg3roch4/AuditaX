using AuditaX.Configuration;
using AuditaX.EntityFramework.Services;
using AuditaX.EntityFramework.Tests.TestEntities;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.SqlServer.Providers;
using AuditaX.Validation;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AuditaX.EntityFramework.Tests.Services;

public class EfAuditQueryServiceValidationTests
{
    private readonly Mock<IChangeLogService> _mockChangeLogService;
    private readonly AuditaXOptions _options;

    public EfAuditQueryServiceValidationTests()
    {
        _mockChangeLogService = new Mock<IChangeLogService>();
        _options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            LogFormat = LogFormat.Json
        };
    }

    private EfAuditQueryService CreateService(string dbName)
    {
        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new EfAuditQueryService(
            new TestDbContext(dbOptions),
            new SqlServerDatabaseProvider(_options),
            _mockChangeLogService.Object);
    }

    #region skip validation

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetBySourceNameAsync_WhenSkipNegative_ReturnsError(int skip)
    {
        var service = CreateService($"EfVal_Skip_{skip}");

        var result = await service.GetBySourceNameAsync("Product", skip: skip);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SkipNegative);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetPagedBySourceNameAsync_WhenSkipNegative_ReturnsError(int skip)
    {
        var service = CreateService($"EfVal_PagedSkip_{skip}");

        var result = await service.GetPagedBySourceNameAsync("Product", skip: skip);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SkipNegative);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetSummaryBySourceNameAsync_WhenSkipNegative_ReturnsError(int skip)
    {
        var service = CreateService($"EfVal_SummarySkip_{skip}");

        var result = await service.GetSummaryBySourceNameAsync("Product", skip: skip);

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
        var service = CreateService($"EfVal_Take_{take}");

        var result = await service.GetBySourceNameAsync("Product", take: take);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.TakeOutOfRange(AuditQueryValidator.MaxTake));
    }

    [Fact]
    public async Task GetBySourceNameAsync_WhenTakeExceedsMax_ReturnsError()
    {
        var service = CreateService("EfVal_TakeMax");

        var result = await service.GetBySourceNameAsync("Product", take: AuditQueryValidator.MaxTake + 1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.TakeOutOfRange(AuditQueryValidator.MaxTake));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPagedBySourceNameAndActionAsync_WhenTakeInvalid_ReturnsError(int take)
    {
        var service = CreateService($"EfVal_PagedActionTake_{take}");

        var result = await service.GetPagedBySourceNameAndActionAsync("Product", AuditAction.Created, take: take);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.TakeOutOfRange(AuditQueryValidator.MaxTake));
    }

    [Fact]
    public async Task GetPagedBySourceNameActionAndDateAsync_WhenTakeExceedsMax_ReturnsError()
    {
        var service = CreateService("EfVal_PagedActionDateTakeMax");

        var result = await service.GetPagedBySourceNameActionAndDateAsync(
            "Product", AuditAction.Created, DateTime.UtcNow.AddDays(-1), take: AuditQueryValidator.MaxTake + 1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.TakeOutOfRange(AuditQueryValidator.MaxTake));
    }

    #endregion

    #region date range validation

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WhenFromDateNotUtc_ReturnsError()
    {
        var service = CreateService("EfVal_DateNotUtc");
        var fromDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);

        var result = await service.GetBySourceNameAndDateAsync("Product", fromDate);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.DateKindInvalid("fromDate"));
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WhenFromDateUnspecified_ReturnsError()
    {
        var service = CreateService("EfVal_DateUnspecified");
        var fromDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var result = await service.GetBySourceNameAndDateAsync("Product", fromDate);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.DateKindInvalid("fromDate"));
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WhenToDateNotUtc_ReturnsError()
    {
        var service = CreateService("EfVal_ToDateNotUtc");
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);

        var result = await service.GetBySourceNameAndDateAsync("Product", fromDate, toDate);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.DateKindInvalid("toDate"));
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WhenFromDateAfterToDate_ReturnsError()
    {
        var service = CreateService("EfVal_DateRangeInverted");
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-1);

        var result = await service.GetBySourceNameAndDateAsync("Product", fromDate, toDate);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.DateRangeInverted);
    }

    [Fact]
    public async Task GetBySourceNameActionAndDateAsync_WhenFromDateAfterToDate_ReturnsError()
    {
        var service = CreateService("EfVal_ActionDateRangeInverted");
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-1);

        var result = await service.GetBySourceNameActionAndDateAsync("Product", AuditAction.Created, fromDate, toDate);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.DateRangeInverted);
    }

    [Fact]
    public async Task GetPagedBySourceNameAndDateAsync_WhenFromDateNotUtc_ReturnsError()
    {
        var service = CreateService("EfVal_PagedDateNotUtc");
        var fromDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var result = await service.GetPagedBySourceNameAndDateAsync("Product", fromDate);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.DateKindInvalid("fromDate"));
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_WhenFromDateAfterToDate_ReturnsError()
    {
        var service = CreateService("EfVal_PagedSummaryDateInverted");
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-1);

        var result = await service.GetPagedSummaryBySourceNameAsync("Product", null, fromDate, toDate);

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
        var service = CreateService($"EfVal_SourceKeyEmpty_{sourceKey.Length}");

        var result = await service.GetPagedSummaryBySourceNameAsync("Product", sourceKey, null, null);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SourceKeyEmpty);
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_WhenSourceKeyTooLong_ReturnsError()
    {
        var service = CreateService("EfVal_SourceKeyTooLong");
        var sourceKey = new string('1', AuditQueryValidator.MaxSourceKeyLength + 1);

        var result = await service.GetPagedSummaryBySourceNameAsync("Product", sourceKey, null, null);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SourceKeyTooLong(AuditQueryValidator.MaxSourceKeyLength));
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_WhenSourceKeyIsNull_SkipsKeyValidation()
    {
        // Null sourceKey is valid — key validation is skipped and the DB call is attempted.
        // InMemory provider throws at that point (expected in unit tests).
        var service = CreateService("EfVal_SourceKeyNull");

        var action = async () => await service.GetPagedSummaryBySourceNameAsync("Product", null, null, null);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region action enum validation

    [Fact]
    public async Task GetBySourceNameAndActionAsync_WhenInvalidAction_ReturnsError()
    {
        var service = CreateService("EfVal_InvalidAction1");
        var invalidAction = (AuditAction)999;

        var result = await service.GetBySourceNameAndActionAsync("Product", invalidAction);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.InvalidAction(999));
    }

    [Fact]
    public async Task GetBySourceNameActionAndDateAsync_WhenInvalidAction_ReturnsError()
    {
        var service = CreateService("EfVal_InvalidAction2");
        var invalidAction = (AuditAction)999;

        var result = await service.GetBySourceNameActionAndDateAsync("Product", invalidAction, DateTime.UtcNow.AddDays(-1));

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.InvalidAction(999));
    }

    [Fact]
    public async Task GetPagedBySourceNameAndActionAsync_WhenInvalidAction_ReturnsError()
    {
        var service = CreateService("EfVal_InvalidAction3");
        var invalidAction = (AuditAction)999;

        var result = await service.GetPagedBySourceNameAndActionAsync("Product", invalidAction);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.InvalidAction(999));
    }

    [Fact]
    public async Task GetPagedBySourceNameActionAndDateAsync_WhenInvalidAction_ReturnsError()
    {
        var service = CreateService("EfVal_InvalidAction4");
        var invalidAction = (AuditAction)999;

        var result = await service.GetPagedBySourceNameActionAndDateAsync(
            "Product", invalidAction, DateTime.UtcNow.AddDays(-1));

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.InvalidAction(999));
    }

    #endregion

    #region validation order — static before DB

    [Fact]
    public async Task GetBySourceNameAsync_WhenSkipNegativeAndSourceNameEmpty_ReturnsSkipError()
    {
        var service = CreateService("EfVal_Order1");

        var result = await service.GetBySourceNameAsync("", skip: -1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SkipNegative);
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WhenSkipNegativeAndFromDateNotUtc_ReturnsSkipError()
    {
        var service = CreateService("EfVal_Order2");

        var result = await service.GetBySourceNameAndDateAsync(
            "Product",
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local),
            skip: -1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.SkipNegative);
    }

    [Fact]
    public async Task GetPagedBySourceNameAndActionAsync_WhenTakeInvalidAndActionInvalid_ReturnsTakeError()
    {
        var service = CreateService("EfVal_Order3");

        var result = await service.GetPagedBySourceNameAndActionAsync(
            "Product", (AuditAction)999, take: 0);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(AuditQueryMessages.TakeOutOfRange(AuditQueryValidator.MaxTake));
    }

    #endregion
}

using AuditaX.Enums;
using AuditaX.Validation;

namespace AuditaX.Tests.Validation;

public class AuditQueryValidatorTests
{
    #region ValidateSourceName

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateSourceName_WhenNullOrWhitespace_ReturnsError(string? sourceName)
    {
        var result = AuditQueryValidator.ValidateSourceName(sourceName!);

        result.Should().Be(AuditQueryMessages.SourceNameRequired);
    }

    [Fact]
    public void ValidateSourceName_WhenExceedsMaxLength_ReturnsError()
    {
        var sourceName = new string('A', AuditQueryValidator.MaxSourceNameLength + 1);

        var result = AuditQueryValidator.ValidateSourceName(sourceName);

        result.Should().Be(AuditQueryMessages.SourceNameTooLong(AuditQueryValidator.MaxSourceNameLength));
    }

    [Fact]
    public void ValidateSourceName_WhenAtMaxLength_ReturnsNull()
    {
        var sourceName = new string('A', AuditQueryValidator.MaxSourceNameLength);

        var result = AuditQueryValidator.ValidateSourceName(sourceName);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateSourceName_WhenValid_ReturnsNull()
    {
        var result = AuditQueryValidator.ValidateSourceName("Product");

        result.Should().BeNull();
    }

    #endregion

    #region ValidateSourceKey

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateSourceKey_WhenNullOrWhitespace_ReturnsError(string? sourceKey)
    {
        var result = AuditQueryValidator.ValidateSourceKey(sourceKey!);

        result.Should().Be(AuditQueryMessages.SourceKeyRequired);
    }

    [Fact]
    public void ValidateSourceKey_WhenExceedsMaxLength_ReturnsError()
    {
        var sourceKey = new string('1', AuditQueryValidator.MaxSourceKeyLength + 1);

        var result = AuditQueryValidator.ValidateSourceKey(sourceKey);

        result.Should().Be(AuditQueryMessages.SourceKeyTooLong(AuditQueryValidator.MaxSourceKeyLength));
    }

    [Fact]
    public void ValidateSourceKey_WhenValid_ReturnsNull()
    {
        var result = AuditQueryValidator.ValidateSourceKey("42");

        result.Should().BeNull();
    }

    #endregion

    #region ValidateOptionalSourceKey

    [Fact]
    public void ValidateOptionalSourceKey_WhenNull_ReturnsNull()
    {
        var result = AuditQueryValidator.ValidateOptionalSourceKey(null);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateOptionalSourceKey_WhenEmptyOrWhitespace_ReturnsError(string sourceKey)
    {
        var result = AuditQueryValidator.ValidateOptionalSourceKey(sourceKey);

        result.Should().Be(AuditQueryMessages.SourceKeyEmpty);
    }

    [Fact]
    public void ValidateOptionalSourceKey_WhenExceedsMaxLength_ReturnsError()
    {
        var sourceKey = new string('1', AuditQueryValidator.MaxSourceKeyLength + 1);

        var result = AuditQueryValidator.ValidateOptionalSourceKey(sourceKey);

        result.Should().Be(AuditQueryMessages.SourceKeyTooLong(AuditQueryValidator.MaxSourceKeyLength));
    }

    [Fact]
    public void ValidateOptionalSourceKey_WhenValid_ReturnsNull()
    {
        var result = AuditQueryValidator.ValidateOptionalSourceKey("42");

        result.Should().BeNull();
    }

    #endregion

    #region ValidatePagination

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ValidatePagination_WhenSkipNegative_ReturnsError(int skip)
    {
        var result = AuditQueryValidator.ValidatePagination(skip, 10);

        result.Should().Be(AuditQueryMessages.SkipNegative);
    }

    [Fact]
    public void ValidatePagination_WhenSkipZero_ReturnsNull()
    {
        var result = AuditQueryValidator.ValidatePagination(0, 10);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidatePagination_WhenTakeLessThanOne_ReturnsError(int take)
    {
        var result = AuditQueryValidator.ValidatePagination(0, take);

        result.Should().Be(AuditQueryMessages.TakeOutOfRange(AuditQueryValidator.MaxTake));
    }

    [Fact]
    public void ValidatePagination_WhenTakeExceedsMax_ReturnsError()
    {
        var result = AuditQueryValidator.ValidatePagination(0, AuditQueryValidator.MaxTake + 1);

        result.Should().Be(AuditQueryMessages.TakeOutOfRange(AuditQueryValidator.MaxTake));
    }

    [Fact]
    public void ValidatePagination_WhenTakeAtMax_ReturnsNull()
    {
        var result = AuditQueryValidator.ValidatePagination(0, AuditQueryValidator.MaxTake);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 100)]
    [InlineData(100, 50)]
    public void ValidatePagination_WhenValid_ReturnsNull(int skip, int take)
    {
        var result = AuditQueryValidator.ValidatePagination(skip, take);

        result.Should().BeNull();
    }

    #endregion

    #region ValidateDateRange

    [Fact]
    public void ValidateDateRange_WhenFromDateNotUtc_ReturnsError()
    {
        var fromDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);

        var result = AuditQueryValidator.ValidateDateRange(fromDate, null);

        result.Should().Be(AuditQueryMessages.DateKindInvalid("fromDate"));
    }

    [Fact]
    public void ValidateDateRange_WhenFromDateUnspecifiedKind_ReturnsError()
    {
        var fromDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var result = AuditQueryValidator.ValidateDateRange(fromDate, null);

        result.Should().Be(AuditQueryMessages.DateKindInvalid("fromDate"));
    }

    [Fact]
    public void ValidateDateRange_WhenToDateNotUtc_ReturnsError()
    {
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);

        var result = AuditQueryValidator.ValidateDateRange(fromDate, toDate);

        result.Should().Be(AuditQueryMessages.DateKindInvalid("toDate"));
    }

    [Fact]
    public void ValidateDateRange_WhenFromDateAfterToDate_ReturnsError()
    {
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-1);

        var result = AuditQueryValidator.ValidateDateRange(fromDate, toDate);

        result.Should().Be(AuditQueryMessages.DateRangeInverted);
    }

    [Fact]
    public void ValidateDateRange_WhenFromDateEqualsToDate_ReturnsNull()
    {
        var date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var result = AuditQueryValidator.ValidateDateRange(date, date);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateDateRange_WhenToDateIsNull_ReturnsNull()
    {
        var fromDate = DateTime.UtcNow.AddDays(-7);

        var result = AuditQueryValidator.ValidateDateRange(fromDate, null);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateDateRange_WhenValidRange_ReturnsNull()
    {
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;

        var result = AuditQueryValidator.ValidateDateRange(fromDate, toDate);

        result.Should().BeNull();
    }

    #endregion

    #region ValidateAction

    [Fact]
    public void ValidateAction_WhenInvalidEnumValue_ReturnsError()
    {
        var invalidAction = (AuditAction)999;

        var result = AuditQueryValidator.ValidateAction(invalidAction);

        result.Should().Be(AuditQueryMessages.InvalidAction(999));
    }

    [Theory]
    [InlineData(AuditAction.Created)]
    [InlineData(AuditAction.Updated)]
    [InlineData(AuditAction.Deleted)]
    [InlineData(AuditAction.Added)]
    [InlineData(AuditAction.Removed)]
    public void ValidateAction_WhenValidEnum_ReturnsNull(AuditAction action)
    {
        var result = AuditQueryValidator.ValidateAction(action);

        result.Should().BeNull();
    }

    #endregion
}

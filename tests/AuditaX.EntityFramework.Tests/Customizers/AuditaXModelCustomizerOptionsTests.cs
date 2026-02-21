using AuditaX.Configuration;
using AuditaX.EntityFramework.Customizers;
using AuditaX.Interfaces;
using Moq;

namespace AuditaX.EntityFramework.Tests.Customizers;

[Collection("AuditaXModelCustomizerStaticState")]
public class AuditaXModelCustomizerOptionsTests : IDisposable
{
    public AuditaXModelCustomizerOptionsTests()
    {
        // Reset static state before each test
        AuditaXModelCustomizerOptions.Options = null;
        AuditaXModelCustomizerOptions.DatabaseProvider = null;
    }

    public void Dispose()
    {
        // Reset static state after each test
        AuditaXModelCustomizerOptions.Options = null;
        AuditaXModelCustomizerOptions.DatabaseProvider = null;
    }

    [Fact]
    public void DefaultState_ShouldHaveNullOptionsAndProvider()
    {
        AuditaXModelCustomizerOptions.Options.Should().BeNull();
        AuditaXModelCustomizerOptions.DatabaseProvider.Should().BeNull();
    }

    [Fact]
    public void IsConfigured_WhenBothNull_ShouldBeFalse()
    {
        AuditaXModelCustomizerOptions.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WhenOnlyOptionsSet_ShouldBeFalse()
    {
        AuditaXModelCustomizerOptions.Options = new AuditaXOptions();

        AuditaXModelCustomizerOptions.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WhenOnlyProviderSet_ShouldBeFalse()
    {
        AuditaXModelCustomizerOptions.DatabaseProvider = new Mock<IDatabaseProvider>().Object;

        AuditaXModelCustomizerOptions.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WhenBothSet_ShouldBeTrue()
    {
        AuditaXModelCustomizerOptions.Options = new AuditaXOptions();
        AuditaXModelCustomizerOptions.DatabaseProvider = new Mock<IDatabaseProvider>().Object;

        AuditaXModelCustomizerOptions.IsConfigured.Should().BeTrue();
    }
}

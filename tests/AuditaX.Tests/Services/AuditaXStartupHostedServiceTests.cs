using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Exceptions;
using AuditaX.Interfaces;
using AuditaX.Services;

namespace AuditaX.Tests.Services;

public class AuditaXStartupHostedServiceTests
{
    private readonly Mock<IAuditStartupValidator> _validatorMock;
    private readonly Mock<IHostApplicationLifetime> _lifetimeMock;
    private readonly AuditaXOptions _options;
    private readonly AuditaXStartupHostedService _service;

    public AuditaXStartupHostedServiceTests()
    {
        _validatorMock = new Mock<IAuditStartupValidator>();
        _lifetimeMock = new Mock<IHostApplicationLifetime>();
        _options = new AuditaXOptions { EnableLogging = true };

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_validatorMock.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        _service = new AuditaXStartupHostedService(
            serviceProvider,
            _options,
            _lifetimeMock.Object);
    }

    [Fact]
    public async Task StartAsync_SuccessfulValidation_ShouldNotStopApplication()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.StartAsync(CancellationToken.None);

        _lifetimeMock.Verify(l => l.StopApplication(), Times.Never);
    }

    [Fact]
    public async Task StartAsync_AuditTableNotFoundException_ShouldStopApplicationAndThrow()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuditTableNotFoundException("AuditLog", "CREATE TABLE ..."));

        var act = () => _service.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<AuditTableNotFoundException>();
        _lifetimeMock.Verify(l => l.StopApplication(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_AuditColumnFormatMismatchException_ShouldStopApplicationAndThrow()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuditColumnFormatMismatchException(
                "AuditLog", "AuditLog", LogFormat.Json, "nvarchar", "xml"));

        var act = () => _service.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<AuditColumnFormatMismatchException>();
        _lifetimeMock.Verify(l => l.StopApplication(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_AuditTableStructureMismatchException_ShouldStopApplicationAndThrow()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuditTableStructureMismatchException(
                "AuditLog",
                new[] { "LogId" },
                Array.Empty<(string, string, string)>(),
                "CREATE TABLE ..."));

        var act = () => _service.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<AuditTableStructureMismatchException>();
        _lifetimeMock.Verify(l => l.StopApplication(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_GenericException_ShouldStopApplicationAndThrow()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var act = () => _service.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _lifetimeMock.Verify(l => l.StopApplication(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_NoValidatorRegistered_ShouldNotThrow()
    {
        var serviceCollection = new ServiceCollection();
        var emptyProvider = serviceCollection.BuildServiceProvider();

        var service = new AuditaXStartupHostedService(
            emptyProvider,
            _options,
            _lifetimeMock.Object);

        await service.StartAsync(CancellationToken.None);

        _lifetimeMock.Verify(l => l.StopApplication(), Times.Never);
    }

    [Fact]
    public async Task StopAsync_ShouldCompleteImmediately()
    {
        var task = _service.StopAsync(CancellationToken.None);

        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_SuccessfulValidation_ShouldCallValidateAsync()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.StartAsync(CancellationToken.None);

        _validatorMock.Verify(v => v.ValidateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithLogger_ShouldNotThrowOnSuccessfulValidation()
    {
        var loggerMock = new Mock<ILogger<AuditaXStartupHostedService>>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_validatorMock.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var service = new AuditaXStartupHostedService(
            serviceProvider,
            _options,
            _lifetimeMock.Object,
            loggerMock.Object);

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await service.StartAsync(CancellationToken.None);

        _lifetimeMock.Verify(l => l.StopApplication(), Times.Never);
    }
}

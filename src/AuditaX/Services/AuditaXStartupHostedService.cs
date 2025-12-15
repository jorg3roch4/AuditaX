using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AuditaX.Configuration;
using AuditaX.Exceptions;
using AuditaX.Interfaces;

namespace AuditaX.Services;

/// <summary>
/// Hosted service that validates the AuditaX infrastructure at application startup.
/// If validation fails, the application will terminate with a critical error.
/// </summary>
public sealed class AuditaXStartupHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuditaXOptions _options;
    private readonly ILogger<AuditaXStartupHostedService>? _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;

    /// <summary>
    /// Initializes a new instance of the AuditaXStartupHostedService.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="options">The AuditaX options.</param>
    /// <param name="applicationLifetime">The application lifetime.</param>
    /// <param name="logger">Optional logger.</param>
    public AuditaXStartupHostedService(
        IServiceProvider serviceProvider,
        AuditaXOptions options,
        IHostApplicationLifetime applicationLifetime,
        ILogger<AuditaXStartupHostedService>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogInformation("AuditaX startup validation starting...");
        LogInformation($"Configuration: TableName={_options.TableName}, Schema={_options.Schema}, Format={_options.ChangeLogFormat}, AutoCreateTable={_options.AutoCreateTable}");

        try
        {
            // Create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var validator = scope.ServiceProvider.GetService<IAuditStartupValidator>();

            if (validator is null)
            {
                LogWarning("No IAuditStartupValidator registered. Startup validation skipped. " +
                          "Make sure you have called .UseDapper() or .UseEntityFramework<T>() in your configuration.");
                return;
            }

            await validator.ValidateAsync(cancellationToken);

            LogInformation("AuditaX startup validation completed successfully.");
        }
        catch (AuditTableNotFoundException ex)
        {
            LogCritical(ex, $"AuditaX startup validation failed: Audit table '{ex.TableName}' not found.");
            LogCritical(null, "To fix this issue, either:");
            LogCritical(null, "  1. Create the table using the SQL script from the scripts folder, or");
            LogCritical(null, "  2. Set AutoCreateTable = true in your AuditaX configuration.");
            LogCritical(null, $"SQL to create table:\n{ex.CreateTableSql}");

            // Stop the application
            _applicationLifetime.StopApplication();
            throw;
        }
        catch (AuditColumnFormatMismatchException ex)
        {
            LogCritical(ex, $"AuditaX startup validation failed: Column format mismatch in table '{ex.TableName}'.");
            LogCritical(null, $"Configuration specifies ChangeLogFormat.{ex.ExpectedFormat} (expects '{ex.ExpectedColumnType}' column type)");
            LogCritical(null, $"But the actual column '{ex.ColumnName}' has type '{ex.ActualColumnType}'");
            LogCritical(null, "To fix this issue, either:");
            LogCritical(null, "  1. Change ChangeLogFormat in your configuration to match the database column type, or");
            LogCritical(null, "  2. Recreate the audit table with the correct column type using the appropriate script.");

            // Stop the application
            _applicationLifetime.StopApplication();
            throw;
        }
        catch (Exception ex)
        {
            LogCritical(ex, $"AuditaX startup validation failed with unexpected error: {ex.Message}");

            // Stop the application
            _applicationLifetime.StopApplication();
            throw;
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void LogInformation(string message)
    {
        if (_options.EnableLogging && _logger != null)
        {
            _logger.LogInformation("[AuditaX] {Message}", message);
        }
    }

    private void LogWarning(string message)
    {
        if (_options.EnableLogging && _logger != null)
        {
            _logger.LogWarning("[AuditaX] {Message}", message);
        }
    }

    private void LogCritical(Exception? exception, string message)
    {
        // Always log critical errors, regardless of EnableLogging setting
        if (_logger is not null)
        {
            if (exception is not null)
            {
                _logger.LogCritical(exception, "[AuditaX] {Message}", message);
            }
            else
            {
                _logger.LogCritical("[AuditaX] {Message}", message);
            }
        }
        else
        {
            // Fallback to console if no logger available
            Console.Error.WriteLine($"[AuditaX CRITICAL] {message}");
            if (exception is not null)
            {
                Console.Error.WriteLine(exception.ToString());
            }
        }
    }
}

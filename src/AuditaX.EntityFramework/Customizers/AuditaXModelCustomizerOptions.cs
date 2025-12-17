using AuditaX.Configuration;
using AuditaX.Interfaces;

namespace AuditaX.EntityFramework.Customizers;

/// <summary>
/// Holds the AuditaX configuration for the model customizer.
/// This is necessary because EF Core instantiates IModelCustomizer internally
/// and doesn't use the standard DI container for its dependencies.
/// </summary>
internal static class AuditaXModelCustomizerOptions
{
    /// <summary>
    /// The AuditaX options.
    /// </summary>
    public static AuditaXOptions? Options { get; set; }

    /// <summary>
    /// The database provider.
    /// </summary>
    public static IDatabaseProvider? DatabaseProvider { get; set; }

    /// <summary>
    /// Indicates whether the customizer has been configured.
    /// </summary>
    public static bool IsConfigured => Options is not null && DatabaseProvider is not null;
}

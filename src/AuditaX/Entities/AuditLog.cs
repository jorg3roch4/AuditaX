using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuditaX.Entities;

/// <summary>
/// Represents an audit log record in the database.
/// </summary>
[Table("AuditLog")]
public class AuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit log record.
    /// </summary>
    [Key]
    [Column("LogId")]
    public Guid LogId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the entity being audited.
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("SourceName")]
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the entity being audited.
    /// </summary>
    [Required]
    [MaxLength(900)]
    [Column("SourceKey")]
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the XML content containing the audit history.
    /// </summary>
    [Required]
    [Column("AuditLog", TypeName = "xml")]
    public string AuditLogXml { get; set; } = string.Empty;
}

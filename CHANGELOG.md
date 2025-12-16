# Changelog

All notable changes to AuditaX will be documented in this file.

## [1.0.2] - 2025-12-16

### Breaking Changes
- **Fluent API**: Renamed `.AuditProperties()` to `.Properties()` for consistency
- **Fluent API**: Removed `.OnAdded()` and `.OnRemoved()` methods for related entities, replaced with unified `.Properties()`
- **appsettings.json**: Renamed `CaptureProperties` to `Properties` for related entities configuration

### Changed
- **Field serialization**: Added/Removed actions now use `value` field instead of `after`/`before`
- **Field serialization**: Updated action continues to use `before`/`after` fields
- Updated all documentation with correct JSON/XML examples

### Added
- Added support for `Modified` state on related entities (Updated action with `before`/`after` fields)

---

## [1.0.1] - 2025-12-16

### Breaking Changes
- **appsettings.json property names renamed** to match FluentAPI method names:
  - `KeyProperty` → `Key` (matches `.WithKey()`)
  - `AuditableProperties` → `Properties` (matches `.Properties()`)
  - `KeyProperty` in RelatedEntities → `ParentKey` (matches `.WithParentKey()`)

### Fixed
- Fixed documentation: corrected audit table creation scripts (column names and types now match actual entity structure)
- Fixed documentation: `AddAuditaXInterceptors` renamed to `AddAuditaXInterceptor` (singular)
- Fixed startup validation: improved error messages when table exists but has incorrect structure
- Fixed EF Core startup validator `InvalidCastException` when validating table structure on SQL Server
- Fixed JSON format tests: assertions now match actual JSON output (`auditLog` instead of `entries`)

### Added
- Added `AuditTableStructureMismatchException` for detailed table structure validation errors
- Added complete table structure validation at startup (validates all columns in single query)
- Added `TableColumnInfo` and `ExpectedColumnDefinition` models for structure validation
- Added comprehensive unit tests for table structure validation (SQL Server and PostgreSQL)

### Changed
- Updated all documentation and sample files with correct appsettings property names

---

## [1.0.0] - 2025-12-15

### Added
- Initial release
- Core audit logging functionality with `IAuditService`
- `IAuditUnitOfWork` interface for Dapper repositories
- Entity Framework Core support with automatic SaveChanges interceptors
- SQL Server provider (`AuditaX.SqlServer`)
- PostgreSQL provider (`AuditaX.PostgreSql`)
- JSON and XML change log formats
- Configuration via appsettings.json or Fluent API
- Auto table creation (`AutoCreateTable` option)
- Startup validation (`ValidateOnStartup()`)
- Related entity tracking (Added/Removed actions)
- `IAuditQueryService` for querying audit history
- Pagination support for audit queries

### Packages
- `AuditaX` - Core library with interfaces and base services
- `AuditaX.Dapper` - Dapper ORM integration
- `AuditaX.EntityFramework` - Entity Framework Core integration
- `AuditaX.SqlServer` - SQL Server database provider
- `AuditaX.PostgreSql` - PostgreSQL database provider

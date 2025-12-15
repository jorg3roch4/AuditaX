# Changelog

All notable changes to AuditaX will be documented in this file.

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

# AuditaX — Agent Context

## Project

Flexible entity audit logging library for .NET 10+. Tracks entity changes (create/update/delete) across Dapper and EF Core, writing structured JSON or XML audit logs to SQL Server or PostgreSQL. Published to NuGet as 5 packages.

## Solution: AuditaX.slnx

### Source packages (src/)
- **AuditaX** — Core: interfaces, configuration, services, serialization, validation
- **AuditaX.Dapper** — IAuditUnitOfWork for manual audit control with Dapper
- **AuditaX.EntityFramework** — Automatic interceptors for EF Core SaveChanges
- **AuditaX.SqlServer** — SQL Server database provider (NVARCHAR/XML columns)
- **AuditaX.PostgreSql** — PostgreSQL provider (JSONB/XML columns, snake_case)

### Tests (tests/)
- AuditaX.Tests — Core unit tests
- AuditaX.Dapper.Tests — Dapper integration tests
- AuditaX.EntityFramework.Tests — EF Core interceptor tests

### Samples (samples/)
- AuditaX.Sample.Dapper — Console app: Dapper + manual IAuditUnitOfWork
- AuditaX.Sample.EntityFramework — Console app: EF Core automatic tracking

### Tools (tools/)
- AuditaX.Tools.DatabaseSetup — Creates sample SQL Server / PostgreSQL databases

## Stack

- .NET 10 / C# 14, nullable enabled, warnings as errors, strict features
- No implicit usings
- Output: artifacts/ (NuGet packages)
- SQL scripts: scripts/SQLServer/ and scripts/PostgreSQL/

## Key Patterns

- Configuration via appsettings.json OR Fluent API (both supported, 100% parity)
- AuditaX must be registered BEFORE DbContext when using EF Core
- EF Core requires `(sp, options)` overload and `options.UseAuditaX(sp)` call
- v2.0 removed query layer — consumers own querying the AuditLog table
- Related entities tracked via LogRelatedAddedAsync / LogRelatedUpdatedAsync / LogRelatedRemovedAsync

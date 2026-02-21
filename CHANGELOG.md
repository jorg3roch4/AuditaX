# Changelog

All notable changes to AuditaX will be documented in this file.

## [1.1.0] - 2026-02-20

### Added
- **Paged query results with TotalCount**: New `PagedResult<T>` model containing `Items` and `TotalCount` for pagination support
  - Consumers can now build their own `PagedResponse<T>` wrappers using `skip`/`take` and the returned `TotalCount`
- **Paged query methods on `IAuditQueryService`**:
  - `GetPagedBySourceNameAsync` - Paged audit logs by entity type with total count
  - `GetPagedBySourceNameAndDateAsync` - Paged audit logs filtered by date range with total count
  - `GetPagedBySourceNameAndActionAsync` - Paged audit logs filtered by action type with total count (new - previously had no pagination)
  - `GetPagedBySourceNameActionAndDateAsync` - Paged audit logs filtered by action and date range with total count (new - previously had no pagination)
  - `GetPagedSummaryBySourceNameAsync` - Paged summary with total count
  - `GetPagedSummaryBySourceNameAsync` (filtered overload) - Summary with optional `sourceKey` and date range filters
- **Parsed audit detail**: New `GetParsedDetailBySourceNameAndKeyAsync` method returns `AuditDetailResult` with strongly-typed `AuditLogEntry` objects
  - No more raw XML/JSON handling on the consumer side
  - No `@` prefix issues when converting XML externally
  - `Before`/`After`/`Value` fields are already parsed into typed `FieldChange` objects
- **New models**:
  - `PagedResult<T>` - Generic paged result with `Items` and `TotalCount`
  - `AuditDetailResult` - Parsed audit detail with `SourceName`, `SourceKey`, and `List<AuditLogEntry>`
- **New `IDatabaseProvider` members** for COUNT queries and paginated action/summary queries:
  - `CountBySourceNameSql`, `CountBySourceNameAndDateSql`, `CountBySourceNameAndActionSql`, `CountBySourceNameActionAndDateSql`, `CountSummaryBySourceNameSql`
  - `GetSelectBySourceNameAndActionSql(skip, take)`, `GetSelectBySourceNameActionAndDateSql(skip, take)`
  - `GetSelectFilteredSummaryBySourceNameSql(skip, take, sourceKey?, hasDateFilter)`, `GetCountFilteredSummaryBySourceNameSql(sourceKey?, hasDateFilter)`
- SQL Server and PostgreSQL implementations for all new SQL generation (XML and JSON formats)
- Unit tests for new models, Dapper paged methods, and EF Core paged methods

### Changed
- `DapperAuditQueryService` and `EfAuditQueryService` now require `IChangeLogService` in the constructor (used by `GetParsedDetailBySourceNameAndKeyAsync`)
- DI registrations in `DapperServiceExtensions` and `EntityFrameworkServiceExtensions` updated to inject `IChangeLogService`

### Note
- All existing methods remain unchanged - this is a fully additive, non-breaking API extension
- The original unpaginated `GetBySourceNameAndActionAsync` and `GetBySourceNameActionAndDateAsync` methods still exist but now have safer paginated alternatives

---

## [1.0.4] - 2026-02-09

### Fixed
- **EF Core re-creation audit bug**: Fixed UNIQUE constraint violation when re-creating a previously deleted entity with the same ID
  - The `CreateAuditEntry` method in `AuditSaveChangesInterceptor` now checks for an existing audit log before inserting
  - Applies the same lookup pattern already used by `UpdateAuditEntry` and `UpdateAuditEntryWithAction`
  - Enables full lifecycle tracking: Created → Deleted → Re-created

---

## [1.0.3] - 2025-12-16

### Added
- **Lookup Properties (EF Core)**: Resolve foreign key values to human-readable display names
  - Ideal for junction tables like ASP.NET Identity's UserRoles
  - Shows "Administrator" instead of RoleId GUID in audit logs
  - Supports both FluentAPI (`.WithLookup<T>()`) and appsettings.json (`Lookups` configuration)
- **Pre-existing records support (EF Core)**: Entities created before AuditaX was enabled now get audited
  - When an entity without an AuditLog entry is modified, deleted, or has related entities added/removed, a new AuditLog is automatically created
  - The first entry will be the action that triggered the audit (e.g., "Updated", "Deleted", "Added" for related entities)
  - No "Created" entry is added for pre-existing records, making it clear the entity existed before auditing was enabled
- New configuration classes: `LookupOptions`, `LookupOptionsBuilder`
- Runtime type resolution from EF Core Model for appsettings.json configurations
- Complete documentation: [Related Entities and Lookups](./docs/related-entities-and-lookups.md)
- Sample demos (8-14) demonstrating User/Role/UserRole auditing scenarios including pre-existing records
- **Dapper Related Entity Methods**: New `IAuditUnitOfWork` methods for auditing related entities
  - `LogRelatedAddedAsync<TParent, TRelated>(parent, related)` - Log when a related entity is added
  - `LogRelatedUpdatedAsync<TParent, TRelated>(parent, original, modified)` - Log changes to a related entity
  - `LogRelatedRemovedAsync<TParent, TRelated>(parent, related)` - Log when a related entity is removed
  - Automatically uses entity configuration and user provider
  - Updated Dapper sample with comprehensive `IAuditUnitOfWork` demo
- **Dapper Lookup Support**: Resolve foreign key values to display names in Dapper repositories
  - New overloads for passing resolved lookup entities: `LogRelatedAddedAsync(parent, related, lookups)`
  - `LogRelatedUpdatedAsync(parent, original, modified, originalLookups, modifiedLookups)` for tracking FK changes
  - `LogRelatedRemovedAsync(parent, related, lookups)` for capturing display names on removal
  - Developer manually resolves lookup entities and passes them to audit methods
  - Supports multiple lookups per related entity
  - Complete documentation: [Dapper Audit Guide](./docs/dapper-audit-guide.md)

### Fixed
- **EF Core AuditLog entity configuration**: Fixed "Cannot create a DbSet for 'AuditLog'" error
  - `AuditLog` entity is now automatically added to the user's DbContext model via `IModelCustomizer`
  - Users no longer need to manually configure `AuditLog` in their DbContext
  - Added `AuditaXModelCustomizer` and `AuditaXModelCustomizerOptions` for automatic model configuration
- **EF Core Auto-increment ID bug**: Fixed issue where entities with database-generated IDs (IDENTITY/SERIAL) were logged with `SourceKey = "0"` instead of the actual ID
  - Audit logs for new entities are now created **after** `SaveChanges` completes, when the database-generated ID is available
  - Added `SavedChanges`/`SavedChangesAsync` interceptor methods to handle deferred audit log creation

### Changed
- `RelatedEntityOptions` now includes `Lookups` dictionary for lookup configurations
- `AuditSaveChangesInterceptor` resolves lookup values from DbContext at runtime
- `AuditSaveChangesInterceptor` now defers audit log creation for Added entities until after SaveChanges

---

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

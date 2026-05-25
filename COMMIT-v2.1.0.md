# AuditaX v2.1.0 — Commit instructions

Pending commit for the AuditaX library repo. The change was authored in
`references/Libraries/AuditaX/` inside the Payments monorepo (gitignored
there) and must be ported to the real AuditaX library repo + committed +
tagged.

## Files modified

### Source

- `src/AuditaX/Configuration/EntityOptions.cs`
  Added `Identifier`, `IdentifierSelector`, `ResolveIdentifierSelector()`,
  `GetIdentifier()` (falls back to `GetKey()` when not configured).
- `src/AuditaX/Configuration/EntityOptionsBuilder.cs`
  Added `WithIdentifier<TKey>(Expression<Func<TEntity,TKey>>)` Fluent API.
- `src/AuditaX/Configuration/AuditaXOptions.cs`
  Wired `ResolveIdentifierSelector()` after `ResolveKeySelector()` in
  the JSON-binding path.
- `src/AuditaX.EntityFramework/Interceptors/AuditSaveChangesInterceptor.cs`
  Swapped `GetKey → GetIdentifier` at L116 / L173 / L183 (parent path).
  Added private helper `ResolveParentIdentifier(DbContext, object child,
  RelatedEntityOptions)` that looks up the parent entity in
  `ChangeTracker` first, then falls back to `context.Find(...)` and
  finally to the raw FK value (graceful, never throws).
- `src/AuditaX.Dapper/Services/DapperAuditUnitOfWork.cs`
  Swapped `GetKey → GetIdentifier` at L157 (surface parity).

### csproj version + release notes (all 5)

- `src/AuditaX/AuditaX.csproj`
- `src/AuditaX.EntityFramework/AuditaX.EntityFramework.csproj`
- `src/AuditaX.Dapper/AuditaX.Dapper.csproj`
- `src/AuditaX.SqlServer/AuditaX.SqlServer.csproj`
- `src/AuditaX.PostgreSql/AuditaX.PostgreSql.csproj`

`<Version>` bumped `2.0.0 → 2.1.0`. `<PackageReleaseNotes>` updated.

### Samples

- `samples/AuditaX.Sample.EntityFramework/Program.cs`
- `samples/AuditaX.Sample.EntityFramework/appsettings.json`
- `samples/AuditaX.Sample.Dapper/Program.cs`

### Tests (new files)

- `tests/AuditaX.Tests/Configuration/EntityOptionsIdentifierTests.cs`
  (5 tests)
- `tests/AuditaX.Tests/Configuration/EntityOptionsBuilderIdentifierTests.cs`
  (3 tests)
- `tests/AuditaX.Tests/Configuration/AuditaXOptionsJsonBindingTests.cs`
  (2 tests)
- `tests/AuditaX.EntityFramework.Tests/TestEntities/ProductWithTags.cs`
- `tests/AuditaX.EntityFramework.Tests/Interceptors/AuditSaveChangesInterceptorIdentifierTests.cs`
  (8 tests)
- `tests/AuditaX.Dapper.Tests/Services/DapperAuditUnitOfWorkIdentifierTests.cs`
  (4 tests)
- `tests/AuditaX.Tests/AuditaX.Tests.csproj`
  Added `Microsoft.Extensions.Configuration` 10.0.1 +
  `Microsoft.Extensions.Configuration.Binder` 10.0.1 for JSON-binding
  tests.

## Verification before commit

```bash
dotnet build AuditaX.slnx          # 0 warnings
dotnet test AuditaX.slnx           # 272 tests green
dotnet pack AuditaX.slnx -c Release  # 5 nupkgs at 2.1.0 in artifacts/
```

## Commit message

```
feat: add Identifier API to decouple display key from FK match

EntityOptions now exposes an optional Identifier alongside Key:

- Key (existing) — physical primary key, used to match parent ↔ child
  via the real DB FK. Example: User.Id (GUID).
- Identifier (new) — human-readable label written to AuditLog.SourceKey
  and used to find/group audit rows. Example: User.UserName.

When Identifier is not configured, GetIdentifier() falls back to
GetKey(), so all existing configurations keep working unchanged.

Fluent API:

    options.ConfigureEntity<User>("Users")
        .WithKey(u => u.Id)
        .WithIdentifier(u => u.UserName)
        .Properties(...)
        .WithRelatedEntity<UserRole>("Roles")
            .WithParentKey(r => r.UserId)
            .Properties("RoleId");

JSON config:

    "Users": { "Key": "Id", "Identifier": "UserName", ... }

The EF Core interceptor resolves the parent's Identifier from a child
entity by scanning ChangeTracker first, then falling back to
DbContext.Find. If the parent cannot be located, the FK value is used
as SourceKey (graceful, never crashes the host SaveChanges).

Limitations:
- Composite-PK parents fall back to FK value (Find requires the array
  form which is not currently passed).
- Dapper consumers must populate the identifier-bearing property on
  the entity instance themselves — the Dapper UoW has no DbContext
  fallback.

All 5 packages bumped 2.0.0 → 2.1.0. 272 tests green, 0 warnings under
TreatWarningsAsErrors + Features:strict.
```

## Tag

```bash
git tag v2.1.0
git push && git push origin v2.1.0
```

## Stage + commit (from AuditaX repo root)

```bash
git add \
  src/AuditaX/Configuration/EntityOptions.cs \
  src/AuditaX/Configuration/EntityOptionsBuilder.cs \
  src/AuditaX/Configuration/AuditaXOptions.cs \
  src/AuditaX.EntityFramework/Interceptors/AuditSaveChangesInterceptor.cs \
  src/AuditaX.Dapper/Services/DapperAuditUnitOfWork.cs \
  src/AuditaX/AuditaX.csproj \
  src/AuditaX.EntityFramework/AuditaX.EntityFramework.csproj \
  src/AuditaX.Dapper/AuditaX.Dapper.csproj \
  src/AuditaX.SqlServer/AuditaX.SqlServer.csproj \
  src/AuditaX.PostgreSql/AuditaX.PostgreSql.csproj \
  samples/AuditaX.Sample.EntityFramework/Program.cs \
  samples/AuditaX.Sample.EntityFramework/appsettings.json \
  samples/AuditaX.Sample.Dapper/Program.cs \
  tests/AuditaX.Tests/AuditaX.Tests.csproj \
  tests/AuditaX.Tests/Configuration/EntityOptionsIdentifierTests.cs \
  tests/AuditaX.Tests/Configuration/EntityOptionsBuilderIdentifierTests.cs \
  tests/AuditaX.Tests/Configuration/AuditaXOptionsJsonBindingTests.cs \
  tests/AuditaX.EntityFramework.Tests/TestEntities/ProductWithTags.cs \
  tests/AuditaX.EntityFramework.Tests/Interceptors/AuditSaveChangesInterceptorIdentifierTests.cs \
  tests/AuditaX.Dapper.Tests/Services/DapperAuditUnitOfWorkIdentifierTests.cs

git commit
git tag v2.1.0
git push && git push origin v2.1.0
```

## Follow-up after tag

Once `v2.1.0` is published to the internal NuGet feed:

1. In Payments monorepo, remove the local-feed workaround:
   - Delete `nuget.config` at repo root (or remove the `auditax-local`
     entry).
   - Delete `src/PaymentsIdentity/nuget.config`.
   - Revert the `--build-context auditax-feed=...` line in
     `src/PaymentsIdentity/src/UI/API/Dockerfile`.
2. Bump Payments to `0.9.21` and redeploy PaymentsIdentity.

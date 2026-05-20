# Dapper Audit Guide

This guide covers everything you need to know about using AuditaX with Dapper repositories, including related entities and lookup resolution.

---

## Overview

Unlike Entity Framework Core which uses interceptors for automatic auditing, Dapper requires **manual audit calls** in your repositories. AuditaX provides the `IAuditUnitOfWork` interface specifically for this purpose.

**Key Concepts:**
- **Manual Control**: You decide exactly when and what to audit
- **Repository Pattern**: Inject `IAuditUnitOfWork` into your repositories
- **Related Entities**: Track child entity changes under parent audit logs
- **Lookups**: Resolve foreign key values to human-readable display names

---

## Setup

### 1. Install Packages

```bash
dotnet add package AuditaX
dotnet add package AuditaX.Dapper
dotnet add package AuditaX.SqlServer  # or AuditaX.PostgreSql
```

### 2. Configure Services

```csharp
services.AddAuditaX(options =>
{
    options.TableName = "AuditLog";
    options.Schema = "dbo";
    options.AutoCreateTable = true;
    options.LogFormat = LogFormat.Json;

    // Configure entities (see Entity Configuration section)
    options.ConfigureEntity<Product>("Product")
        .WithKey(p => p.Id)
        .Properties("Name", "Price", "Stock");
})
.UseDapper<YourDapperContext>()
.UseSqlServer()  // or .UsePostgreSql()
.ValidateOnStartup();
```

### 3. Dapper Context Requirements

Your Dapper context must have a `CreateConnection()` method:

```csharp
public class YourDapperContext
{
    private readonly string _connectionString;

    public YourDapperContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
```

---

## Basic Usage

### Inject IAuditUnitOfWork

```csharp
public class ProductRepository
{
    private readonly DapperContext _context;
    private readonly IAuditUnitOfWork _audit;

    public ProductRepository(DapperContext context, IAuditUnitOfWork audit)
    {
        _context = context;
        _audit = audit;
    }
}
```

### LogCreateAsync

Log when an entity is created:

```csharp
public async Task<int> CreateAsync(Product product)
{
    using var connection = _context.CreateConnection();

    const string sql = @"
        INSERT INTO Products (Name, Price, Stock)
        OUTPUT INSERTED.Id
        VALUES (@Name, @Price, @Stock)";

    var id = await connection.QuerySingleAsync<int>(sql, product);
    product.Id = id;

    // Log the creation
    await _audit.LogCreateAsync(product);

    return id;
}
```

### LogUpdateAsync

Log when an entity is updated (captures field changes):

```csharp
public async Task<bool> UpdateAsync(Product product)
{
    // IMPORTANT: Get the original state BEFORE updating
    var original = await GetByIdAsync(product.Id);
    if (original == null) return false;

    using var connection = _context.CreateConnection();

    const string sql = @"
        UPDATE Products
        SET Name = @Name, Price = @Price, Stock = @Stock
        WHERE Id = @Id";

    var affected = await connection.ExecuteAsync(sql, product);

    if (affected > 0)
    {
        // Log the update with before/after comparison
        await _audit.LogUpdateAsync(original, product);
    }

    return affected > 0;
}
```

### LogDeleteAsync

Log when an entity is deleted:

```csharp
public async Task<bool> DeleteAsync(int productId)
{
    var product = await GetByIdAsync(productId);
    if (product == null) return false;

    using var connection = _context.CreateConnection();

    const string sql = "DELETE FROM Products WHERE Id = @Id";
    var affected = await connection.ExecuteAsync(sql, new { Id = productId });

    if (affected > 0)
    {
        await _audit.LogDeleteAsync(product);
    }

    return affected > 0;
}
```

---

## Related Entities

Related entities are child records that should be audited as part of a parent entity's audit log. For example, tracking `ProductTag` changes under the `Product` audit history.

### Configuration

```csharp
options.ConfigureEntity<Product>("Product")
    .WithKey(p => p.Id)
    .Properties("Name", "Price", "Stock")
    .WithRelatedEntity<ProductTag>("ProductTag")
        .WithParentKey(t => t.ProductId)
        .Properties("Tag");
```

### LogRelatedAddedAsync

Log when a related entity is added:

```csharp
public async Task AddTagAsync(Product product, ProductTag tag)
{
    using var connection = _context.CreateConnection();

    const string sql = @"
        INSERT INTO ProductTags (ProductId, Tag)
        VALUES (@ProductId, @Tag)";

    await connection.ExecuteAsync(sql, tag);

    // Log the addition under the parent product's audit log
    await _audit.LogRelatedAddedAsync(product, tag);
}
```

### LogRelatedUpdatedAsync

Log when a related entity is updated:

```csharp
public async Task UpdateTagAsync(Product product, ProductTag original, ProductTag modified)
{
    using var connection = _context.CreateConnection();

    const string sql = "UPDATE ProductTags SET Tag = @Tag WHERE Id = @Id";
    await connection.ExecuteAsync(sql, modified);

    // Log the change with before/after comparison
    await _audit.LogRelatedUpdatedAsync(product, original, modified);
}
```

### LogRelatedRemovedAsync

Log when a related entity is removed:

```csharp
public async Task RemoveTagAsync(Product product, ProductTag tag)
{
    using var connection = _context.CreateConnection();

    const string sql = "DELETE FROM ProductTags WHERE Id = @Id";
    await connection.ExecuteAsync(sql, new { tag.Id });

    // Log the removal under the parent product's audit log
    await _audit.LogRelatedRemovedAsync(product, tag);
}
```

### Audit Log Output

```json
{
  "auditLog": [
    {
      "action": "Created",
      "user": "admin@example.com",
      "timestamp": "2025-12-15T10:00:00Z"
    },
    {
      "action": "Added",
      "user": "admin@example.com",
      "timestamp": "2025-12-15T10:05:00Z",
      "related": "ProductTag",
      "fields": [
        { "name": "Tag", "value": "Electronics" }
      ]
    },
    {
      "action": "Removed",
      "user": "admin@example.com",
      "timestamp": "2025-12-15T10:10:00Z",
      "related": "ProductTag",
      "fields": [
        { "name": "Tag", "value": "Electronics" }
      ]
    }
  ]
}
```

---

## Lookups (Resolving FK to Display Values)

When auditing junction tables (like `UserRoles`), you often want to capture human-readable values instead of foreign key IDs. For example, showing "Administrator" instead of a GUID.

### The Problem

Without lookups, your audit log shows:
```json
{ "name": "RoleId", "value": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
```

With lookups, your audit log shows:
```json
{ "name": "RoleName", "value": "Administrator" }
```

### Configuration

Configure lookups in your entity options:

```csharp
options.ConfigureEntity<User>("User")
    .WithKey(u => u.UserId)
    .Properties("UserName", "Email", "PhoneNumber", "IsActive")
    .WithRelatedEntity<UserRole>("UserRoles")
        .WithParentKey(ur => ur.UserId)
        .WithLookup<Role>("Role")
            .ForeignKey(ur => ur.RoleId)  // FK property in UserRole
            .Key(r => r.RoleId)            // PK property in Role
            .Properties("RoleName");       // Properties to capture from Role
```

### Usage with Lookups

The key difference: **you resolve the lookup entity and pass it to the audit method**.

#### Step 1: Perform the database operation
#### Step 2: Resolve the lookup entity (query the referenced table)
#### Step 3: Pass the lookup to the audit method

```csharp
public async Task AssignRoleAsync(User user, Role role)
{
    // Step 1: Insert the UserRole
    var userRole = new UserRole { UserId = user.UserId, RoleId = role.RoleId };

    using var connection = _context.CreateConnection();
    const string sql = "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
    await connection.ExecuteAsync(sql, userRole);

    // Step 2: Role is already resolved (passed as parameter)
    // If you only have RoleId, query it: var role = await GetRoleByIdAsync(userRole.RoleId);

    // Step 3: Pass the Role entity as lookup
    await _audit.LogRelatedAddedAsync(user, userRole, role);
    // Audit log will show: "RoleName": "Administrator"
}
```

### LogRelatedAddedAsync with Lookup

```csharp
public async Task AssignRoleAsync(User user, string roleId)
{
    // Insert UserRole
    var userRole = new UserRole { UserId = user.UserId, RoleId = roleId };
    await InsertUserRoleAsync(userRole);

    // Resolve the lookup
    var role = await GetRoleByIdAsync(roleId);

    // Log with lookup - captures RoleName instead of RoleId
    await _audit.LogRelatedAddedAsync(user, userRole, role);
}
```

### LogRelatedRemovedAsync with Lookup

```csharp
public async Task RemoveRoleAsync(User user, string roleId)
{
    var userRole = new UserRole { UserId = user.UserId, RoleId = roleId };

    // Resolve the lookup BEFORE deleting
    var role = await GetRoleByIdAsync(roleId);

    // Delete from database
    await DeleteUserRoleAsync(user.UserId, roleId);

    // Log with lookup
    await _audit.LogRelatedRemovedAsync(user, userRole, role);
}
```

### LogRelatedUpdatedAsync with Lookups

For updates, you need both the original and modified lookup entities:

```csharp
public async Task ChangeRoleAsync(User user, string oldRoleId, string newRoleId)
{
    // Resolve both lookups
    var oldRole = await GetRoleByIdAsync(oldRoleId);
    var newRole = await GetRoleByIdAsync(newRoleId);

    var originalUserRole = new UserRole { UserId = user.UserId, RoleId = oldRoleId };
    var modifiedUserRole = new UserRole { UserId = user.UserId, RoleId = newRoleId };

    // Update in database
    await UpdateUserRoleAsync(user.UserId, oldRoleId, newRoleId);

    // Log with both original and modified lookups
    await _audit.LogRelatedUpdatedAsync(
        user,
        originalUserRole,
        modifiedUserRole,
        originalLookups: new object[] { oldRole },
        modifiedLookups: new object[] { newRole });
    // Audit log will show: "RoleName": "User" -> "Administrator"
}
```

### Multiple Lookups

You can configure multiple lookups for a single related entity:

```csharp
.WithRelatedEntity<UserRole>("UserRoles")
    .WithParentKey(ur => ur.UserId)
    .WithLookup<Role>("Role")
        .ForeignKey(ur => ur.RoleId)
        .Key(r => r.RoleId)
        .Properties("RoleName")
    .WithLookup<Department>("Department")
        .ForeignKey(ur => ur.DepartmentId)
        .Key(d => d.DepartmentId)
        .Properties("DepartmentName");
```

Pass multiple lookup entities:

```csharp
var role = await GetRoleByIdAsync(userRole.RoleId);
var department = await GetDepartmentByIdAsync(userRole.DepartmentId);

await _audit.LogRelatedAddedAsync(user, userRole, role, department);
// Audit log will show both: "RoleName": "Administrator", "DepartmentName": "IT"
```

---

## Complete Example: User/Role Repository

```csharp
public class UserRepository
{
    private readonly DapperContext _context;
    private readonly IAuditUnitOfWork _audit;

    public UserRepository(DapperContext context, IAuditUnitOfWork audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            INSERT INTO Users (UserId, UserName, Email, IsActive)
            VALUES (@UserId, @UserName, @Email, @IsActive)";

        await connection.ExecuteAsync(sql, user);
        await _audit.LogCreateAsync(user);

        return user;
    }

    public async Task AssignRoleAsync(User user, Role role)
    {
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId
        };

        using var connection = _context.CreateConnection();
        const string sql = "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
        await connection.ExecuteAsync(sql, userRole);

        // Log with lookup - shows "RoleName: Administrator" instead of RoleId GUID
        await _audit.LogRelatedAddedAsync(user, userRole, role);
    }

    public async Task RemoveRoleAsync(User user, Role role)
    {
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId
        };

        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId";
        await connection.ExecuteAsync(sql, userRole);

        await _audit.LogRelatedRemovedAsync(user, userRole, role);
    }

    private async Task<Role?> GetRoleByIdAsync(string roleId)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Role>(
            "SELECT RoleId, RoleName FROM Roles WHERE RoleId = @RoleId",
            new { RoleId = roleId });
    }
}
```

---

## Configuration Reference

### appsettings.json

```json
{
  "AuditaX": {
    "TableName": "AuditLog",
    "Schema": "dbo",
    "AutoCreateTable": true,
    "LogFormat": "Json",
    "Entities": {
      "User": {
        "Key": "UserId",
        "Properties": ["UserName", "Email", "PhoneNumber", "IsActive"],
        "RelatedEntities": {
          "UserRoles": {
            "ParentKey": "UserId",
            "Lookups": {
              "Role": {
                "ForeignKey": "RoleId",
                "Key": "RoleId",
                "Properties": ["RoleName"]
              }
            }
          }
        }
      }
    }
  }
}
```

---

## Method Reference

| Method | Description |
|--------|-------------|
| `LogCreateAsync<T>(entity)` | Log entity creation |
| `LogUpdateAsync<T>(original, modified)` | Log entity update with field changes |
| `LogDeleteAsync<T>(entity)` | Log entity deletion |
| `LogRelatedAddedAsync<TParent, TRelated>(parent, related)` | Log related entity addition |
| `LogRelatedAddedAsync<TParent, TRelated>(parent, related, lookups)` | Log with lookup resolution |
| `LogRelatedUpdatedAsync<TParent, TRelated>(parent, original, modified)` | Log related entity update |
| `LogRelatedUpdatedAsync<TParent, TRelated>(parent, original, modified, originalLookups, modifiedLookups)` | Log with lookup resolution |
| `LogRelatedRemovedAsync<TParent, TRelated>(parent, related)` | Log related entity removal |
| `LogRelatedRemovedAsync<TParent, TRelated>(parent, related, lookups)` | Log with lookup resolution |

---

## Best Practices

1. **Always get original state before updates**: For `LogUpdateAsync`, query the entity before modifying it.

2. **Resolve lookups before delete**: For `LogRelatedRemovedAsync` with lookups, query the lookup entity before deleting.

3. **Use transactions**: Wrap audit logging with database operations in a transaction for consistency.

4. **Handle null lookups gracefully**: If a lookup entity might not exist, handle null cases.

5. **Configure all auditable properties**: Explicitly list properties in `.Properties()` to control what's captured.

---

## Comparison: Dapper vs EF Core

| Feature | Dapper | EF Core |
|---------|--------|---------|
| Audit Trigger | Manual calls | Automatic (interceptor) |
| Related Entities | Manual `LogRelatedAsync` | Automatic detection |
| Lookups | Pass resolved entities | Automatic DB query |
| Control | Full control | Convention-based |
| Performance | You control queries | Interceptor overhead |

Choose Dapper when you need full control over your audit logging. Choose EF Core when you want automatic, convention-based auditing.

# Related Entities and Lookups

AuditaX can track changes to related entities (child records) and log them under the parent entity's audit log. Additionally, Lookups allow you to capture human-readable values from reference tables instead of foreign key IDs.

---

## Related Entities

Related entities are child records that should be audited as part of a parent entity. For example, tracking product tags as part of the product's audit history.

### Fluent API Configuration

```csharp
options.ConfigureEntity<Product>("Product")
    .WithKey(p => p.Id)
    .Properties("Name", "Price", "Stock")
    .HasRelated<ProductTag>("Tags")
        .WithParentKey(t => t.ProductId)
        .Properties("TagName");
```

### appsettings.json Configuration

```json
{
  "AuditaX": {
    "Entities": {
      "Product": {
        "Key": "Id",
        "Properties": [ "Name", "Price", "Stock" ],
        "RelatedEntities": {
          "ProductTag": {
            "ParentKey": "ProductId",
            "Properties": [ "TagName" ]
          }
        }
      }
    }
  }
}
```

### Audit Log Output

When a tag is added, updated, or removed from a product:

**Added:**
```json
{
  "action": "Added",
  "user": "admin@example.com",
  "timestamp": "2025-12-15T10:00:00Z",
  "related": "ProductTag",
  "fields": [
    { "name": "TagName", "value": "Electronics" }
  ]
}
```

**Updated:**
```json
{
  "action": "Updated",
  "user": "admin@example.com",
  "timestamp": "2025-12-15T10:05:00Z",
  "related": "ProductTag",
  "fields": [
    { "name": "TagName", "before": "Electronics", "after": "Gaming" }
  ]
}
```

**Removed:**
```json
{
  "action": "Removed",
  "user": "admin@example.com",
  "timestamp": "2025-12-15T10:10:00Z",
  "related": "ProductTag",
  "fields": [
    { "name": "TagName", "value": "Gaming" }
  ]
}
```

---

## Lookup Properties

When auditing junction tables (like UserRoles), you often want to capture human-readable values instead of foreign key IDs. Lookups resolve values from related reference tables.

> **Note:** Lookups are supported by both EF Core (automatic) and Dapper (manual). See [Dapper Audit Guide](./dapper-audit-guide.md) for Dapper-specific examples.

### Common Scenario: ASP.NET Identity

When a role is assigned to a user, you want the audit log to show "Administrator" instead of a GUID like `3fa85f64-5717-4562-b3fc-2c963f66afa6`.

**Entity Structure:**
```
Users (UserId, UserName, Email)
    └── UserRoles (UserId, RoleId)  <-- Junction table
            └── Roles (RoleId, RoleName)  <-- Lookup table
```

### Fluent API Configuration

```csharp
options.ConfigureEntity<User>("User")
    .WithKey(u => u.UserId)
    .Properties("UserName", "Email", "PhoneNumber", "IsActive")
    .HasRelated<UserRole>("UserRoles")
        .WithParentKey(ur => ur.UserId)
        .WithLookup<Role>("Role")
            .ForeignKey(ur => ur.RoleId)  // FK property in UserRole
            .Key(r => r.RoleId)           // PK property in Role
            .Properties("RoleName");      // Properties to capture from Role
```

### appsettings.json Configuration

```json
{
  "AuditaX": {
    "Entities": {
      "User": {
        "Key": "UserId",
        "Properties": [ "UserName", "Email", "PhoneNumber", "IsActive" ],
        "RelatedEntities": {
          "UserRoles": {
            "ParentKey": "UserId",
            "Lookups": {
              "Role": {
                "ForeignKey": "RoleId",
                "Key": "RoleId",
                "Properties": [ "RoleName" ]
              }
            }
          }
        }
      }
    }
  }
}
```

### Configuration Properties

| Property | Description |
|----------|-------------|
| `ForeignKey` | The property in the junction table that references the lookup table |
| `Key` | The primary key property in the lookup table |
| `Properties` | The properties to capture from the lookup entity |

> **Note:** Always specify both `ForeignKey` and `Key` explicitly, even when they have the same name (e.g., both `RoleId`). This ensures clarity and consistency between FluentAPI and appsettings.json configurations.

### Audit Log Output

When a role is assigned to a user:

```json
{
  "action": "Added",
  "user": "admin@example.com",
  "timestamp": "2025-12-15T10:00:00Z",
  "related": "UserRoles",
  "fields": [
    { "name": "RoleName", "value": "Administrator" }
  ]
}
```

When a role is removed from a user:

```json
{
  "action": "Removed",
  "user": "admin@example.com",
  "timestamp": "2025-12-15T10:05:00Z",
  "related": "UserRoles",
  "fields": [
    { "name": "RoleName", "value": "Administrator" }
  ]
}
```

### Multiple Lookups

You can configure multiple lookups for a single junction table:

```csharp
.HasRelated<UserRole>("UserRoles")
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

### EF Core vs Dapper

| Feature | EF Core | Dapper |
|---------|---------|--------|
| Lookup Resolution | Automatic (via DbContext) | Manual (pass resolved entities) |
| Configuration | Same as above | Same as above |
| Usage | Automatic on `SaveChangesAsync` | Call `LogRelatedAddedAsync(parent, related, lookups)` |

**EF Core Limitations:**
- The lookup entity must be part of the same DbContext as the junction table.
- EF Core automatically queries the lookup entity using the Model metadata.
- **Do NOT use `QueryTrackingBehavior.NoTracking`** - AuditaX requires the ChangeTracker to detect entity changes.

**Dapper:**
- You must resolve lookup entities yourself and pass them to the audit method.
- See [Dapper Audit Guide](./dapper-audit-guide.md) for complete examples.

---

## Pre-existing Records (EF Core)

AuditaX handles entities that were created before auditing was enabled. When such an entity is modified, deleted, or has related entities added/removed, AuditaX automatically creates a new AuditLog entry.

### Behavior

- **No "Created" entry**: Pre-existing records won't have a "Created" action in their audit history, making it clear they existed before auditing was enabled.
- **First action is logged**: The first audit entry will be whatever action triggered it (Updated, Deleted, Added, Removed).
- **Full history from that point**: All subsequent changes are tracked normally.

### Example

A user was created before AuditaX was configured. When a role is assigned to that user:

```json
{
  "auditLog": [
    {
      "action": "Added",
      "user": "admin@example.com",
      "timestamp": "2025-12-15T10:00:00Z",
      "related": "UserRoles",
      "fields": [
        { "name": "RoleName", "value": "Administrator" }
      ]
    }
  ]
}
```

Notice there's no "Created" entry - this indicates the user existed before auditing was enabled.

---

## Complete Example

Here's a complete example showing both Related Entities and Lookups:

### Entities

```csharp
public class User
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Role
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
```

### Configuration (Fluent API)

```csharp
services.AddAuditaX(options =>
{
    options.AutoCreateTable = true;
    options.LogFormat = LogFormat.Json;

    options.ConfigureEntity<User>("User")
        .WithKey(u => u.UserId)
        .Properties("UserName", "Email")
        .HasRelated<UserRole>("UserRoles")
            .WithParentKey(ur => ur.UserId)
            .WithLookup<Role>("Role")
                .ForeignKey(ur => ur.RoleId)
                .Key(r => r.RoleId)
                .Properties("RoleName");
})
.UseEntityFramework<AppDbContext>()
.UseSqlServer()
.ValidateOnStartup();
```

### Configuration (appsettings.json)

```json
{
  "AuditaX": {
    "AutoCreateTable": true,
    "LogFormat": "Json",
    "Entities": {
      "User": {
        "Key": "UserId",
        "Properties": [ "UserName", "Email" ],
        "RelatedEntities": {
          "UserRoles": {
            "ParentKey": "UserId",
            "Lookups": {
              "Role": {
                "ForeignKey": "RoleId",
                "Key": "RoleId",
                "Properties": [ "RoleName" ]
              }
            }
          }
        }
      }
    }
  }
}
```

### Usage

```csharp
// Assign a role to a user
var adminRole = await dbContext.Roles.FirstAsync(r => r.RoleName == "Administrator");
var userRole = new UserRole { UserId = user.UserId, RoleId = adminRole.RoleId };
dbContext.UserRoles.Add(userRole);
await dbContext.SaveChangesAsync();

// Audit log automatically captures: "RoleName": "Administrator"
```

### Resulting Audit Log

```json
{
  "auditLog": [
    {
      "action": "Created",
      "user": "system",
      "timestamp": "2025-12-15T09:00:00Z"
    },
    {
      "action": "Added",
      "user": "admin@example.com",
      "timestamp": "2025-12-15T10:00:00Z",
      "related": "UserRoles",
      "fields": [
        { "name": "RoleName", "value": "Administrator" }
      ]
    }
  ]
}
```

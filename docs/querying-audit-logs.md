# Querying Audit Logs

AuditaX provides `IAuditQueryService` for querying audit logs. This service is automatically registered when you configure AuditaX and can be injected into your services or controllers.

## Getting Started

Inject `IAuditQueryService` into your class:

```csharp
public class AuditController(IAuditQueryService auditQueryService)
{
    // Use auditQueryService to query audit logs
}
```

---

## Available Methods

### GetBySourceNameAsync

Gets audit logs for a specific entity type with pagination.

```csharp
Task<IEnumerable<AuditQueryResult>> GetBySourceNameAsync(
    string sourceName,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
// Get first 50 Product audit logs
var results = await auditQueryService.GetBySourceNameAsync("Product", skip: 0, take: 50);

foreach (var result in results)
{
    Console.WriteLine($"Entity: {result.SourceName}, Key: {result.SourceKey}");
    Console.WriteLine($"Audit Log: {result.AuditLog}");
}
```

**Result:**
```
Entity: Product, Key: 1
Audit Log: {"auditLog":[{"action":"Created","user":"admin@example.com","timestamp":"2025-12-15T10:30:00Z"},{"action":"Updated","user":"admin@example.com","timestamp":"2025-12-15T11:45:00Z","fields":[{"name":"Price","before":"99.99","after":"89.99"}]}]}

Entity: Product, Key: 2
Audit Log: {"auditLog":[{"action":"Created","user":"admin@example.com","timestamp":"2025-12-15T12:00:00Z"}]}
```

---

### GetBySourceNameAndKeyAsync

Gets the complete audit log for a specific entity instance.

```csharp
Task<AuditQueryResult?> GetBySourceNameAndKeyAsync(
    string sourceName,
    string sourceKey,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
// Get audit log for Product with ID 42
var result = await auditQueryService.GetBySourceNameAndKeyAsync("Product", "42");

if (result != null)
{
    Console.WriteLine($"Entity: {result.SourceName}");
    Console.WriteLine($"Key: {result.SourceKey}");
    Console.WriteLine($"Full History: {result.AuditLog}");
}
```

**Result:**
```
Entity: Product
Key: 42
Full History: {
  "auditLog": [
    {
      "action": "Created",
      "user": "admin@example.com",
      "timestamp": "2025-12-10T09:00:00Z"
    },
    {
      "action": "Updated",
      "user": "sales@example.com",
      "timestamp": "2025-12-12T14:30:00Z",
      "fields": [
        { "name": "Price", "before": "149.99", "after": "129.99" },
        { "name": "Stock", "before": "100", "after": "85" }
      ]
    },
    {
      "action": "Added",
      "user": "sales@example.com",
      "timestamp": "2025-12-12T14:35:00Z",
      "related": "ProductTag",
      "fields": [
        { "name": "Tag", "after": "Sale" }
      ]
    }
  ]
}
```

---

### GetBySourceNameAndDateAsync

Gets audit logs for entities that have events within a date range.

```csharp
Task<IEnumerable<AuditQueryResult>> GetBySourceNameAndDateAsync(
    string sourceName,
    DateTime fromDate,
    DateTime? toDate = null,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
// Get all Product changes from the last 7 days
var fromDate = DateTime.UtcNow.AddDays(-7);
var results = await auditQueryService.GetBySourceNameAndDateAsync(
    "Product",
    fromDate,
    toDate: null,  // null = up to now
    skip: 0,
    take: 100);

Console.WriteLine($"Found {results.Count()} products modified in the last 7 days");
```

**Result:**
```
Found 15 products modified in the last 7 days
```

> **Note:** This query searches within JSON/XML content which may be slower on large tables. Consider using indexes or the summary query for better performance.

---

### GetBySourceNameAndActionAsync

Gets audit logs for entities that have a specific action type.

```csharp
Task<IEnumerable<AuditQueryResult>> GetBySourceNameAndActionAsync(
    string sourceName,
    AuditAction action,
    CancellationToken cancellationToken = default);
```

**Available Actions:**
- `AuditAction.Created` - Entity was created
- `AuditAction.Updated` - Entity was updated
- `AuditAction.Deleted` - Entity was deleted
- `AuditAction.Added` - Related entity was added
- `AuditAction.Removed` - Related entity was removed

**Example:**
```csharp
using AuditaX.Enums;

// Get all Products that have been deleted
var deletedProducts = await auditQueryService.GetBySourceNameAndActionAsync(
    "Product",
    AuditAction.Deleted);

foreach (var result in deletedProducts)
{
    Console.WriteLine($"Deleted Product Key: {result.SourceKey}");
}
```

**Result:**
```
Deleted Product Key: 5
Deleted Product Key: 12
Deleted Product Key: 27
```

---

### GetBySourceNameActionAndDateAsync

Gets audit logs for entities that have a specific action within a date range.

```csharp
Task<IEnumerable<AuditQueryResult>> GetBySourceNameActionAndDateAsync(
    string sourceName,
    AuditAction action,
    DateTime fromDate,
    DateTime? toDate = null,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
// Get all Products created in December 2025
var fromDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
var toDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

var newProducts = await auditQueryService.GetBySourceNameActionAndDateAsync(
    "Product",
    AuditAction.Created,
    fromDate,
    toDate);

Console.WriteLine($"Products created in December 2025: {newProducts.Count()}");
```

**Result:**
```
Products created in December 2025: 42
```

---

### GetSummaryBySourceNameAsync

Gets a summary showing only the last event for each entity. This is an optimized query that's much faster than retrieving full audit logs. Returns one record per entity with its last action information.

```csharp
Task<IEnumerable<AuditSummaryResult>> GetSummaryBySourceNameAsync(
    string sourceName,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
// Get summary of last actions for all Products
var summaries = await auditQueryService.GetSummaryBySourceNameAsync("Product", skip: 0, take: 100);

foreach (var summary in summaries)
{
    Console.WriteLine($"Product {summary.SourceKey}: {summary.LastAction} by {summary.LastUser} at {summary.LastTimestamp:g}");
}
```

**Result (as JSON structure):**
```json
[
  {
    "sourceName": "Product",
    "sourceKey": "1",
    "lastAction": "Updated",
    "lastTimestamp": "2025-12-15T14:30:00Z",
    "lastUser": "sales@example.com"
  },
  {
    "sourceName": "Product",
    "sourceKey": "2",
    "lastAction": "Created",
    "lastTimestamp": "2025-12-15T10:00:00Z",
    "lastUser": "admin@example.com"
  },
  {
    "sourceName": "Product",
    "sourceKey": "3",
    "lastAction": "Deleted",
    "lastTimestamp": "2025-12-14T17:45:00Z",
    "lastUser": "admin@example.com"
  },
  {
    "sourceName": "Product",
    "sourceKey": "4",
    "lastAction": "Updated",
    "lastTimestamp": "2025-12-14T15:20:00Z",
    "lastUser": "warehouse@example.com"
  },
  {
    "sourceName": "Product",
    "sourceKey": "5",
    "lastAction": "Added",
    "lastTimestamp": "2025-12-13T11:00:00Z",
    "lastUser": "sales@example.com"
  }
]
```

**Console Output:**
```
Product 1: Updated by sales@example.com at 12/15/2025 2:30 PM
Product 2: Created by admin@example.com at 12/15/2025 10:00 AM
Product 3: Deleted by admin@example.com at 12/14/2025 5:45 PM
Product 4: Updated by warehouse@example.com at 12/14/2025 3:20 PM
Product 5: Added by sales@example.com at 12/13/2025 11:00 AM
```

---

## Result Models

### AuditQueryResult

Returned by most query methods. Contains the full audit log.

| Property | Type | Description |
|----------|------|-------------|
| `SourceName` | string | Name of the audited entity (e.g., "Product") |
| `SourceKey` | string | Unique key of the entity instance (e.g., "42") |
| `AuditLog` | string | Full audit history in JSON or XML format |

### AuditSummaryResult

Returned by `GetSummaryBySourceNameAsync`. Contains only the last event information.

| Property | Type | Description |
|----------|------|-------------|
| `SourceName` | string | Name of the audited entity |
| `SourceKey` | string | Unique key of the entity instance |
| `LastAction` | string | Last action performed (Created, Updated, Deleted, Added, Removed) |
| `LastTimestamp` | DateTime | UTC timestamp of the last action |
| `LastUser` | string | User who performed the last action |

---

## Performance Considerations

- **GetBySourceNameAsync** and **GetBySourceNameAndKeyAsync** are the most efficient queries as they don't search within the audit log content.

- **GetBySourceNameAndDateAsync**, **GetBySourceNameAndActionAsync**, and **GetBySourceNameActionAndDateAsync** search within JSON/XML content and may be slower on large tables.

- **GetSummaryBySourceNameAsync** is optimized for dashboards and reports where you only need the current state of entities.

- All methods that return collections support **pagination** via `skip` and `take` parameters. Use pagination to avoid loading large result sets into memory.

---

## Example: Building an Audit Dashboard

```csharp
public class AuditDashboardService(IAuditQueryService auditQueryService)
{
    public async Task<DashboardData> GetDashboardAsync()
    {
        // Get summary of recent changes
        var productSummary = await auditQueryService.GetSummaryBySourceNameAsync("Product", take: 10);
        var orderSummary = await auditQueryService.GetSummaryBySourceNameAsync("Order", take: 10);

        // Get entities deleted today
        var today = DateTime.UtcNow.Date;
        var deletedToday = await auditQueryService.GetBySourceNameActionAndDateAsync(
            "Product",
            AuditAction.Deleted,
            today);

        return new DashboardData
        {
            RecentProductChanges = productSummary,
            RecentOrderChanges = orderSummary,
            DeletedTodayCount = deletedToday.Count()
        };
    }
}
```

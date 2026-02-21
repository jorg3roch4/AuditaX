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

> **Warning:** This method returns ALL matching records without pagination. For large tables, use `GetPagedBySourceNameAndActionAsync` instead.

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

> **Warning:** This method returns ALL matching records without pagination. For large tables, use `GetPagedBySourceNameActionAndDateAsync` instead.

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

## Paged Query Methods (v1.1.0+)

All paged methods return `PagedResult<T>` which includes both the result items and a `TotalCount` for building paginated UIs.

```csharp
public sealed record PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
```

### GetPagedBySourceNameAsync

Gets audit logs by entity type with pagination and total count.

```csharp
Task<PagedResult<AuditQueryResult>> GetPagedBySourceNameAsync(
    string sourceName,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var result = await auditQueryService.GetPagedBySourceNameAsync("Product", skip: 0, take: 20);

Console.WriteLine($"Showing {result.Items.Count()} of {result.TotalCount} total records");

foreach (var item in result.Items)
{
    Console.WriteLine($"  {item.SourceKey}: {item.AuditLog[..50]}...");
}
```

**Result:**
```
Showing 20 of 1523 total records
  1: {"auditLog":[{"action":"Created","user":"admin...
  2: {"auditLog":[{"action":"Created","user":"admin...
  ...
```

---

### GetPagedBySourceNameAndDateAsync

Gets audit logs filtered by date range with pagination and total count.

```csharp
Task<PagedResult<AuditQueryResult>> GetPagedBySourceNameAndDateAsync(
    string sourceName,
    DateTime fromDate,
    DateTime? toDate = null,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var fromDate = DateTime.UtcNow.AddDays(-30);
var result = await auditQueryService.GetPagedBySourceNameAndDateAsync(
    "Product", fromDate, skip: 0, take: 25);

Console.WriteLine($"Page 1: {result.Items.Count()} items, {result.TotalCount} total");
```

---

### GetPagedBySourceNameAndActionAsync

Gets audit logs filtered by action type with pagination and total count. This is the safe, paginated alternative to `GetBySourceNameAndActionAsync`.

```csharp
Task<PagedResult<AuditQueryResult>> GetPagedBySourceNameAndActionAsync(
    string sourceName,
    AuditAction action,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
// Get deleted products, page by page
var result = await auditQueryService.GetPagedBySourceNameAndActionAsync(
    "Product", AuditAction.Deleted, skip: 0, take: 50);

Console.WriteLine($"Deleted products: {result.TotalCount} total");
foreach (var item in result.Items)
{
    Console.WriteLine($"  Product {item.SourceKey} was deleted");
}
```

---

### GetPagedBySourceNameActionAndDateAsync

Gets audit logs filtered by action and date range with pagination and total count. This is the safe, paginated alternative to `GetBySourceNameActionAndDateAsync`.

```csharp
Task<PagedResult<AuditQueryResult>> GetPagedBySourceNameActionAndDateAsync(
    string sourceName,
    AuditAction action,
    DateTime fromDate,
    DateTime? toDate = null,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
// Get products created this month
var result = await auditQueryService.GetPagedBySourceNameActionAndDateAsync(
    "Product",
    AuditAction.Created,
    DateTime.UtcNow.AddMonths(-1),
    skip: 0,
    take: 50);

Console.WriteLine($"Products created this month: {result.TotalCount}");
```

---

### GetPagedSummaryBySourceNameAsync

Gets a paged summary of the last event for each entity, with total count.

```csharp
Task<PagedResult<AuditSummaryResult>> GetPagedSummaryBySourceNameAsync(
    string sourceName,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var result = await auditQueryService.GetPagedSummaryBySourceNameAsync("Product", skip: 0, take: 10);

Console.WriteLine($"Page 1 of {Math.Ceiling(result.TotalCount / 10.0)} ({result.TotalCount} entities)");

foreach (var summary in result.Items)
{
    Console.WriteLine($"  {summary.SourceKey}: {summary.LastAction} by {summary.LastUser}");
}
```

---

### GetPagedSummaryBySourceNameAsync (Filtered)

Gets a paged summary with optional `sourceKey` and date range filters.

```csharp
Task<PagedResult<AuditSummaryResult>> GetPagedSummaryBySourceNameAsync(
    string sourceName,
    string? sourceKey,
    DateTime? fromDate,
    DateTime? toDate,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
// Filter summary by sourceKey
var result = await auditQueryService.GetPagedSummaryBySourceNameAsync(
    "Product",
    sourceKey: "42",         // filter by specific entity
    fromDate: null,          // no date filter
    toDate: null,
    skip: 0,
    take: 10);

// Filter summary by date range
var recentResult = await auditQueryService.GetPagedSummaryBySourceNameAsync(
    "Product",
    sourceKey: null,                     // all entities
    fromDate: DateTime.UtcNow.AddDays(-7),
    toDate: null,                        // up to now
    skip: 0,
    take: 20);

Console.WriteLine($"Products modified in last 7 days: {recentResult.TotalCount}");
```

---

### GetParsedDetailBySourceNameAndKeyAsync

Gets the parsed audit detail for a specific entity. Returns strongly-typed entries with `Before`/`After`/`Value` fields already parsed -- no raw XML/JSON handling required on the consumer side.

```csharp
Task<AuditDetailResult?> GetParsedDetailBySourceNameAndKeyAsync(
    string sourceName,
    string sourceKey,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var detail = await auditQueryService.GetParsedDetailBySourceNameAndKeyAsync("Product", "42");

if (detail != null)
{
    Console.WriteLine($"History for {detail.SourceName} #{detail.SourceKey}:");

    foreach (var entry in detail.Entries)
    {
        Console.WriteLine($"  [{entry.Timestamp:g}] {entry.Action} by {entry.User}");

        foreach (var field in entry.Fields)
        {
            if (field.Before != null || field.After != null)
                Console.WriteLine($"    {field.Name}: {field.Before} -> {field.After}");
            else
                Console.WriteLine($"    {field.Name}: {field.Value}");
        }
    }
}
```

**Result:**
```
History for Product #42:
  [12/10/2025 9:00 AM] Created by admin@example.com
    Name: Widget
    Price: 149.99
    Stock: 100
  [12/12/2025 2:30 PM] Updated by sales@example.com
    Price: 149.99 -> 129.99
    Stock: 100 -> 85
  [12/12/2025 2:35 PM] Added by sales@example.com
    Tag: Sale
```

**Comparison with `GetBySourceNameAndKeyAsync`:**

| | `GetBySourceNameAndKeyAsync` | `GetParsedDetailBySourceNameAndKeyAsync` |
|---|---|---|
| Returns | `AuditQueryResult` with raw JSON/XML string | `AuditDetailResult` with typed `List<AuditLogEntry>` |
| Parsing | Consumer must parse XML/JSON externally | Already parsed by AuditaX |
| XML `@` prefix | Consumer deals with `@Action`, `@User`, etc. | Clean property names: `Action`, `User`, etc. |
| Field access | Navigate raw string | `entry.Fields[0].Before`, `entry.Fields[0].After` |

---

## Result Models

### AuditQueryResult

Returned by most query methods. Contains the full audit log as a raw string.

| Property | Type | Description |
|----------|------|-------------|
| `SourceName` | string | Name of the audited entity (e.g., "Product") |
| `SourceKey` | string | Unique key of the entity instance (e.g., "42") |
| `AuditLog` | string | Full audit history in JSON or XML format |

### AuditSummaryResult

Returned by `GetSummaryBySourceNameAsync` and `GetPagedSummaryBySourceNameAsync`. Contains only the last event information.

| Property | Type | Description |
|----------|------|-------------|
| `SourceName` | string | Name of the audited entity |
| `SourceKey` | string | Unique key of the entity instance |
| `LastAction` | string | Last action performed (Created, Updated, Deleted, Added, Removed) |
| `LastTimestamp` | DateTime | UTC timestamp of the last action |
| `LastUser` | string | User who performed the last action |

### PagedResult\<T\>

Returned by all `GetPaged*` methods. Wraps the result items with a total count for pagination.

| Property | Type | Description |
|----------|------|-------------|
| `Items` | IEnumerable\<T\> | The items in the current page |
| `TotalCount` | int | Total number of records matching the query (across all pages) |

### AuditDetailResult

Returned by `GetParsedDetailBySourceNameAndKeyAsync`. Contains fully parsed audit entries.

| Property | Type | Description |
|----------|------|-------------|
| `SourceName` | string | Name of the audited entity |
| `SourceKey` | string | Unique key of the entity instance |
| `Entries` | List\<AuditLogEntry\> | Parsed audit entries with typed fields |

### AuditLogEntry

Each entry in `AuditDetailResult.Entries`.

| Property | Type | Description |
|----------|------|-------------|
| `Action` | AuditAction | The action (Created, Updated, Deleted, Added, Removed) |
| `User` | string | User who performed the action |
| `Timestamp` | DateTime | UTC timestamp |
| `Related` | string? | Related entity name (for Added/Removed actions) |
| `Fields` | List\<FieldChange\> | Field changes in this entry |

### FieldChange

Each field change in `AuditLogEntry.Fields`.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | string | Field name |
| `Before` | string? | Value before the change (for Updates) |
| `After` | string? | Value after the change (for Updates) |
| `Value` | string? | Field value (for Added/Removed actions) |

---

## Performance Considerations

- **GetBySourceNameAsync** and **GetBySourceNameAndKeyAsync** are the most efficient queries as they don't search within the audit log content.

- **GetBySourceNameAndDateAsync**, **GetBySourceNameAndActionAsync**, and **GetBySourceNameActionAndDateAsync** search within JSON/XML content and may be slower on large tables.

- **GetSummaryBySourceNameAsync** is optimized for dashboards and reports where you only need the current state of entities.

- **Always prefer `GetPaged*` methods** over the non-paged versions. The paged methods return a `TotalCount` and guarantee bounded result sets via `skip`/`take`. The non-paged `GetBySourceNameAndActionAsync` and `GetBySourceNameActionAndDateAsync` can return unbounded results.

- All `GetPaged*` methods execute two queries internally: one for the data page and one for the COUNT. This is a standard pagination pattern and the overhead is minimal.

---

## Example: Building a Paginated API

```csharp
// Your consumer's wrapper (lives in your app, NOT in AuditaX)
public class PagedResponse<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IEnumerable<T> Data { get; set; } = [];
}

public class AuditController(IAuditQueryService auditQueryService)
{
    [HttpGet("products/audit")]
    public async Task<PagedResponse<AuditSummaryResult>> GetProductAudit(
        int pageNumber = 1, int pageSize = 20)
    {
        var skip = (pageNumber - 1) * pageSize;

        var result = await auditQueryService.GetPagedSummaryBySourceNameAsync(
            "Product", skip: skip, take: pageSize);

        return new PagedResponse<AuditSummaryResult>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = result.TotalCount,
            Data = result.Items
        };
    }

    [HttpGet("products/{id}/history")]
    public async Task<AuditDetailResult?> GetProductHistory(int id)
    {
        // Returns parsed entries, no raw XML/JSON
        return await auditQueryService.GetParsedDetailBySourceNameAndKeyAsync(
            "Product", id.ToString());
    }
}
```

---

## Example: Building an Audit Dashboard

```csharp
public class AuditDashboardService(IAuditQueryService auditQueryService)
{
    public async Task<DashboardData> GetDashboardAsync()
    {
        // Get paged summary of recent changes (with total count)
        var productSummary = await auditQueryService.GetPagedSummaryBySourceNameAsync("Product", take: 10);
        var orderSummary = await auditQueryService.GetPagedSummaryBySourceNameAsync("Order", take: 10);

        // Get entities deleted today (paginated)
        var today = DateTime.UtcNow.Date;
        var deletedToday = await auditQueryService.GetPagedBySourceNameActionAndDateAsync(
            "Product",
            AuditAction.Deleted,
            today,
            take: 50);

        return new DashboardData
        {
            RecentProductChanges = productSummary.Items,
            TotalProducts = productSummary.TotalCount,
            RecentOrderChanges = orderSummary.Items,
            TotalOrders = orderSummary.TotalCount,
            DeletedTodayCount = deletedToday.TotalCount
        };
    }
}
```

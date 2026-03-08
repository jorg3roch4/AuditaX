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

All methods return `Response<T>` or `PagedResponse<T>`. Always check `Succeeded` before accessing `Data`:

```csharp
var result = await auditQueryService.GetBySourceNameAsync("Product");

if (!result.Succeeded)
{
    Console.WriteLine($"Error: {result.Message}");
    return;
}

foreach (var item in result.Data!)
{
    Console.WriteLine(item.SourceKey);
}
```

---

## Input Validation

All methods validate their parameters before touching the database. A failed validation returns `Succeeded = false` with a descriptive message — no exceptions are thrown.

### Validation rules

| Parameter | Rule |
|---|---|
| `sourceName` | Required, max 64 characters |
| `sourceKey` (required) | Required, max 64 characters |
| `sourceKey` (optional filter) | When provided: cannot be empty/whitespace, max 64 characters |
| `skip` | Must be ≥ 0 |
| `take` | Must be between 1 and 1000 |
| `fromDate` | Must be `DateTimeKind.Utc` |
| `toDate` | When provided: must be `DateTimeKind.Utc` and ≥ `fromDate` |
| `action` | Must be a defined `AuditAction` enum value |

### Validation order

Static checks always execute before any database call:

1. Pagination (`skip`/`take`)
2. `action` enum
3. Date range (`fromDate`/`toDate`)
4. Optional `sourceKey`
5. `sourceName` existence in DB
6. `sourceKey` existence in DB (when required)

### Example — handling validation errors

```csharp
var result = await auditQueryService.GetBySourceNameAndDateAsync(
    "Product",
    fromDate: DateTime.Now,  // wrong: must be UTC
    toDate: null);

if (!result.Succeeded)
{
    // result.Message = "'fromDate' must be a UTC date (DateTimeKind.Utc)."
    Console.WriteLine(result.Message);
}
```

---

## Available Methods

### GetBySourceNameAsync

Gets audit logs for a specific entity type.

```csharp
Task<Response<IEnumerable<AuditQueryResult>>> GetBySourceNameAsync(
    string sourceName,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var result = await auditQueryService.GetBySourceNameAsync("Product", skip: 0, take: 50);

if (!result.Succeeded)
{
    Console.WriteLine($"Error: {result.Message}");
    return;
}

foreach (var item in result.Data!)
{
    Console.WriteLine($"Entity: {item.SourceName}, Key: {item.SourceKey}");
    Console.WriteLine($"Audit Log: {item.AuditLog}");
}
```

---

### GetBySourceNameAndKeyAsync

Gets the complete audit log for a specific entity instance.

```csharp
Task<Response<AuditQueryResult?>> GetBySourceNameAndKeyAsync(
    string sourceName,
    string sourceKey,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var result = await auditQueryService.GetBySourceNameAndKeyAsync("Product", "42");

if (!result.Succeeded)
{
    Console.WriteLine($"Error: {result.Message}");
    return;
}

if (result.Data is null)
{
    Console.WriteLine("Not found.");
    return;
}

Console.WriteLine($"Entity: {result.Data.SourceName}");
Console.WriteLine($"Key: {result.Data.SourceKey}");
Console.WriteLine($"Full History: {result.Data.AuditLog}");
```

---

### GetBySourceNameAndDateAsync

Gets audit logs for entities that have events within a date range.

> **Note:** Both `fromDate` and `toDate` (when provided) must be `DateTimeKind.Utc`. `fromDate` must be ≤ `toDate`.

```csharp
Task<Response<IEnumerable<AuditQueryResult>>> GetBySourceNameAndDateAsync(
    string sourceName,
    DateTime fromDate,
    DateTime? toDate = null,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var fromDate = DateTime.UtcNow.AddDays(-7);

var result = await auditQueryService.GetBySourceNameAndDateAsync(
    "Product",
    fromDate,
    toDate: null,   // null = no upper bound
    skip: 0,
    take: 100);

if (result.Succeeded)
    Console.WriteLine($"Found {result.Data!.Count()} products modified in the last 7 days");
```

> **Performance:** This query searches within JSON/XML content and may be slower on large tables. Consider using the summary query or adding appropriate indexes.

---

### GetBySourceNameAndActionAsync

Gets audit logs for entities that have a specific action type.

> **Warning:** Returns all matching records without pagination. For large tables use `GetPagedBySourceNameAndActionAsync`.

```csharp
Task<Response<IEnumerable<AuditQueryResult>>> GetBySourceNameAndActionAsync(
    string sourceName,
    AuditAction action,
    CancellationToken cancellationToken = default);
```

**Available actions:**
- `AuditAction.Created` — entity was created
- `AuditAction.Updated` — entity was updated
- `AuditAction.Deleted` — entity was deleted
- `AuditAction.Added` — related entity was added
- `AuditAction.Removed` — related entity was removed

**Example:**
```csharp
var result = await auditQueryService.GetBySourceNameAndActionAsync(
    "Product",
    AuditAction.Deleted);

if (result.Succeeded)
{
    foreach (var item in result.Data!)
        Console.WriteLine($"Deleted Product Key: {item.SourceKey}");
}
```

---

### GetBySourceNameActionAndDateAsync

Gets audit logs for entities that have a specific action within a date range.

> **Warning:** Returns all matching records without pagination. For large tables use `GetPagedBySourceNameActionAndDateAsync`.

```csharp
Task<Response<IEnumerable<AuditQueryResult>>> GetBySourceNameActionAndDateAsync(
    string sourceName,
    AuditAction action,
    DateTime fromDate,
    DateTime? toDate = null,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var fromDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
var toDate   = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

var result = await auditQueryService.GetBySourceNameActionAndDateAsync(
    "Product",
    AuditAction.Created,
    fromDate,
    toDate);

if (result.Succeeded)
    Console.WriteLine($"Products created in December 2025: {result.Data!.Count()}");
```

---

### GetSummaryBySourceNameAsync

Gets a summary showing only the last event for each entity. Much faster than retrieving full audit logs — only extracts the last entry per row.

```csharp
Task<Response<IEnumerable<AuditSummaryResult>>> GetSummaryBySourceNameAsync(
    string sourceName,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var result = await auditQueryService.GetSummaryBySourceNameAsync("Product", skip: 0, take: 100);

if (result.Succeeded)
{
    foreach (var summary in result.Data!)
    {
        Console.WriteLine($"Product {summary.SourceKey}: {summary.LastAction} " +
                          $"by {summary.LastUser} at {summary.LastTimestamp:g}");
    }
}
```

---

## Paged Query Methods

All paged methods return `PagedResponse<T>` which includes the items, total count, and current page info for building paginated UIs.

```csharp
public class PagedResponse<T> : Response<T>
{
    public int PageNumber  { get; }
    public int PageSize    { get; }
    public int TotalCount  { get; }
    // inherited: bool Succeeded, string? Message, T? Data
}
```

> **Take limit:** `take` is capped at **1000** per request. Requesting more returns a validation error.

---

### GetPagedBySourceNameAsync

```csharp
Task<PagedResponse<IEnumerable<AuditQueryResult>>> GetPagedBySourceNameAsync(
    string sourceName,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var result = await auditQueryService.GetPagedBySourceNameAsync("Product", skip: 0, take: 20);

if (!result.Succeeded)
{
    Console.WriteLine($"Error: {result.Message}");
    return;
}

Console.WriteLine($"Page {result.PageNumber}: {result.Data!.Count()} of {result.TotalCount} total");

foreach (var item in result.Data!)
    Console.WriteLine($"  {item.SourceKey}: {item.AuditLog[..50]}...");
```

---

### GetPagedBySourceNameAndDateAsync

```csharp
Task<PagedResponse<IEnumerable<AuditQueryResult>>> GetPagedBySourceNameAndDateAsync(
    string sourceName,
    DateTime fromDate,
    DateTime? toDate = null,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var result = await auditQueryService.GetPagedBySourceNameAndDateAsync(
    "Product",
    DateTime.UtcNow.AddDays(-30),
    skip: 0,
    take: 25);

if (result.Succeeded)
    Console.WriteLine($"Page 1: {result.Data!.Count()} items, {result.TotalCount} total");
```

---

### GetPagedBySourceNameAndActionAsync

Paginated alternative to `GetBySourceNameAndActionAsync`. Preferred for large tables.

```csharp
Task<PagedResponse<IEnumerable<AuditQueryResult>>> GetPagedBySourceNameAndActionAsync(
    string sourceName,
    AuditAction action,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var result = await auditQueryService.GetPagedBySourceNameAndActionAsync(
    "Product", AuditAction.Deleted, skip: 0, take: 50);

if (result.Succeeded)
{
    Console.WriteLine($"Deleted products: {result.TotalCount} total");
    foreach (var item in result.Data!)
        Console.WriteLine($"  Product {item.SourceKey} was deleted");
}
```

---

### GetPagedBySourceNameActionAndDateAsync

Paginated alternative to `GetBySourceNameActionAndDateAsync`. Preferred for large tables.

```csharp
Task<PagedResponse<IEnumerable<AuditQueryResult>>> GetPagedBySourceNameActionAndDateAsync(
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
var result = await auditQueryService.GetPagedBySourceNameActionAndDateAsync(
    "Product",
    AuditAction.Created,
    DateTime.UtcNow.AddMonths(-1),
    skip: 0,
    take: 50);

if (result.Succeeded)
    Console.WriteLine($"Products created this month: {result.TotalCount}");
```

---

### GetPagedSummaryBySourceNameAsync

```csharp
Task<PagedResponse<IEnumerable<AuditSummaryResult>>> GetPagedSummaryBySourceNameAsync(
    string sourceName,
    int skip = 0,
    int take = 100,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var result = await auditQueryService.GetPagedSummaryBySourceNameAsync("Product", skip: 0, take: 10);

if (result.Succeeded)
{
    Console.WriteLine($"Page {result.PageNumber} of {Math.Ceiling(result.TotalCount / 10.0)} ({result.TotalCount} entities)");

    foreach (var summary in result.Data!)
        Console.WriteLine($"  {summary.SourceKey}: {summary.LastAction} by {summary.LastUser}");
}
```

---

### GetPagedSummaryBySourceNameAsync (Filtered)

Gets a paged summary with optional `sourceKey` and date range filters.

> **Note:** When `sourceKey` is provided it cannot be empty or whitespace. When `fromDate` is provided both dates must be UTC.

```csharp
Task<PagedResponse<IEnumerable<AuditSummaryResult>>> GetPagedSummaryBySourceNameAsync(
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
// Filter by sourceKey
var byKey = await auditQueryService.GetPagedSummaryBySourceNameAsync(
    "Product",
    sourceKey: "42",
    fromDate: null,
    toDate: null,
    skip: 0,
    take: 10);

// Filter by date range
var recent = await auditQueryService.GetPagedSummaryBySourceNameAsync(
    "Product",
    sourceKey: null,                        // all entities
    fromDate: DateTime.UtcNow.AddDays(-7),
    toDate: null,                           // no upper bound
    skip: 0,
    take: 20);

if (recent.Succeeded)
    Console.WriteLine($"Products modified in last 7 days: {recent.TotalCount}");
```

---

### GetParsedDetailBySourceNameAndKeyAsync

Gets the parsed audit detail for a specific entity. Returns strongly-typed entries with `Before`/`After`/`Value` fields already parsed — no raw XML/JSON handling required.

```csharp
Task<Response<AuditDetailResult?>> GetParsedDetailBySourceNameAndKeyAsync(
    string sourceName,
    string sourceKey,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var result = await auditQueryService.GetParsedDetailBySourceNameAndKeyAsync("Product", "42");

if (!result.Succeeded)
{
    Console.WriteLine($"Error: {result.Message}");
    return;
}

if (result.Data is null)
{
    Console.WriteLine("Not found.");
    return;
}

Console.WriteLine($"History for {result.Data.SourceName} #{result.Data.SourceKey}:");

foreach (var entry in result.Data.Entries)
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
```

**Output:**
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
| Returns | `Response<AuditQueryResult?>` with raw JSON/XML | `Response<AuditDetailResult?>` with typed `List<AuditLogEntry>` |
| Parsing | Consumer must parse XML/JSON | Already parsed by AuditaX |
| Field access | Navigate raw string | `entry.Fields[0].Before`, `entry.Fields[0].After` |

---

## Result Models

### Response\<T\>

Returned by all non-paged methods.

| Property | Type | Description |
|---|---|---|
| `Succeeded` | bool | `true` if the operation completed successfully |
| `Message` | string? | Error message when `Succeeded` is `false` |
| `Data` | T? | The query result when `Succeeded` is `true` |
| `Errors` | IEnumerable\<string\> | Additional error details |

### PagedResponse\<T\>

Returned by all `GetPaged*` methods. Extends `Response<T>`.

| Property | Type | Description |
|---|---|---|
| `Succeeded` | bool | `true` if the operation completed successfully |
| `Message` | string? | Error message when `Succeeded` is `false` |
| `Data` | T? | The items in the current page |
| `PageNumber` | int | Current page number (1-based) |
| `PageSize` | int | Number of items requested (`take`) |
| `TotalCount` | int | Total records matching the query across all pages |

### AuditQueryResult

| Property | Type | Description |
|---|---|---|
| `SourceName` | string | Name of the audited entity (e.g., "Product") |
| `SourceKey` | string | Unique key of the entity instance (e.g., "42") |
| `AuditLog` | string | Full audit history in JSON or XML format |

### AuditSummaryResult

| Property | Type | Description |
|---|---|---|
| `SourceName` | string | Name of the audited entity |
| `SourceKey` | string | Unique key of the entity instance |
| `LastAction` | string | Last action performed (Created, Updated, Deleted, Added, Removed) |
| `LastTimestamp` | DateTime | UTC timestamp of the last action |
| `LastUser` | string | User who performed the last action |

### AuditDetailResult

| Property | Type | Description |
|---|---|---|
| `SourceName` | string | Name of the audited entity |
| `SourceKey` | string | Unique key of the entity instance |
| `Entries` | List\<AuditLogEntry\> | Parsed audit entries with typed fields |

### AuditLogEntry

| Property | Type | Description |
|---|---|---|
| `Action` | AuditAction | The action (Created, Updated, Deleted, Added, Removed) |
| `User` | string | User who performed the action |
| `Timestamp` | DateTime | UTC timestamp |
| `Related` | string? | Related entity name (for Added/Removed actions) |
| `Fields` | List\<FieldChange\> | Field changes in this entry |

### FieldChange

| Property | Type | Description |
|---|---|---|
| `Name` | string | Field name |
| `Before` | string? | Value before the change (for Updates) |
| `After` | string? | Value after the change (for Updates) |
| `Value` | string? | Field value (for Added/Removed actions) |

---

## Performance Considerations

- **`GetBySourceNameAsync`** and **`GetBySourceNameAndKeyAsync`** are the most efficient — they filter by indexed columns only.
- **Date and action filters** (`GetBySourceNameAndDateAsync`, `GetBySourceNameAndActionAsync`, etc.) search within JSON/XML content and may be slower on large tables.
- **`GetSummaryBySourceNameAsync`** is optimized for dashboards — only extracts the last entry per entity row.
- **Always prefer `GetPaged*` methods** over non-paged ones. `GetBySourceNameAndActionAsync` and `GetBySourceNameActionAndDateAsync` can return unbounded result sets.
- All `GetPaged*` methods execute two queries internally: one for the data page and one for `COUNT`. This is standard pagination overhead and is minimal.

---

## Example: Building a Paginated API

```csharp
public class AuditController(IAuditQueryService auditQueryService)
{
    [HttpGet("products/audit")]
    public async Task<IActionResult> GetProductAudit(int pageNumber = 1, int pageSize = 20)
    {
        var skip = (pageNumber - 1) * pageSize;

        var result = await auditQueryService.GetPagedSummaryBySourceNameAsync(
            "Product", skip: skip, take: pageSize);

        if (!result.Succeeded)
            return BadRequest(result.Message);

        return Ok(new
        {
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            Data = result.Data
        });
    }

    [HttpGet("products/{id}/history")]
    public async Task<IActionResult> GetProductHistory(int id)
    {
        var result = await auditQueryService.GetParsedDetailBySourceNameAndKeyAsync(
            "Product", id.ToString());

        if (!result.Succeeded)
            return BadRequest(result.Message);

        if (result.Data is null)
            return NotFound();

        return Ok(result.Data);
    }
}
```

---

## Example: Building an Audit Dashboard

```csharp
public class AuditDashboardService(IAuditQueryService auditQueryService)
{
    public async Task<DashboardData?> GetDashboardAsync()
    {
        var productSummary = await auditQueryService.GetPagedSummaryBySourceNameAsync("Product", take: 10);
        var orderSummary   = await auditQueryService.GetPagedSummaryBySourceNameAsync("Order",   take: 10);

        if (!productSummary.Succeeded || !orderSummary.Succeeded)
            return null;

        var deletedToday = await auditQueryService.GetPagedBySourceNameActionAndDateAsync(
            "Product",
            AuditAction.Deleted,
            DateTime.UtcNow.Date,
            take: 50);

        return new DashboardData
        {
            RecentProductChanges = productSummary.Data!,
            TotalProducts        = productSummary.TotalCount,
            RecentOrderChanges   = orderSummary.Data!,
            TotalOrders          = orderSummary.TotalCount,
            DeletedTodayCount    = deletedToday.Succeeded ? deletedToday.TotalCount : 0
        };
    }
}
```

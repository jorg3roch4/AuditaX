# AuditaX Documentation

Complete documentation for configuring and using AuditaX with different ORMs, databases, and serialization formats.

## Guides

| # | Guide | Description |
|---|-------|-------------|
| 01 | [Dapper Audit Guide](./guides/01-dapper-audit-guide.md) | Complete guide for auditing with Dapper (IAuditUnitOfWork, Related Entities, Lookups) |
| 02 | [Related Entities and Lookups (EF Core)](./guides/02-related-entities-and-lookups.md) | Track child entities and resolve FK values to display names |
| 03 | [Dapper + SQL Server + JSON](./guides/03-dapper-sqlserver-json.md) | Configuration guide |
| 04 | [Dapper + SQL Server + XML](./guides/04-dapper-sqlserver-xml.md) | Configuration guide |
| 05 | [Dapper + PostgreSQL + JSON](./guides/05-dapper-postgresql-json.md) | Configuration guide |
| 06 | [Dapper + PostgreSQL + XML](./guides/06-dapper-postgresql-xml.md) | Configuration guide |
| 07 | [EF Core + SQL Server + JSON](./guides/07-efcore-sqlserver-json.md) | Configuration guide |
| 08 | [EF Core + SQL Server + XML](./guides/08-efcore-sqlserver-xml.md) | Configuration guide |
| 09 | [EF Core + PostgreSQL + JSON](./guides/09-efcore-postgresql-json.md) | Configuration guide |
| 10 | [EF Core + PostgreSQL + XML](./guides/10-efcore-postgresql-xml.md) | Configuration guide |

## Configuration Matrix

### Dapper

| Database | Format | Guide |
|----------|--------|-------|
| SQL Server | JSON | [03-dapper-sqlserver-json.md](./guides/03-dapper-sqlserver-json.md) |
| SQL Server | XML | [04-dapper-sqlserver-xml.md](./guides/04-dapper-sqlserver-xml.md) |
| PostgreSQL | JSON | [05-dapper-postgresql-json.md](./guides/05-dapper-postgresql-json.md) |
| PostgreSQL | XML | [06-dapper-postgresql-xml.md](./guides/06-dapper-postgresql-xml.md) |

### Entity Framework Core

| Database | Format | Guide |
|----------|--------|-------|
| SQL Server | JSON | [07-efcore-sqlserver-json.md](./guides/07-efcore-sqlserver-json.md) |
| SQL Server | XML | [08-efcore-sqlserver-xml.md](./guides/08-efcore-sqlserver-xml.md) |
| PostgreSQL | JSON | [09-efcore-postgresql-json.md](./guides/09-efcore-postgresql-json.md) |
| PostgreSQL | XML | [10-efcore-postgresql-xml.md](./guides/10-efcore-postgresql-xml.md) |

## Research

Exploratory notes and investigations live in [`research/`](./research/).

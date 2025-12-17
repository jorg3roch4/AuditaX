# SQL Scripts

Scripts para configurar las bases de datos de ejemplo de AuditaX.

## Estructura

```
scripts/
├── SqlServer/
│   ├── 00_AuditaX_Create.sql           # Crear base de datos
│   ├── 01_Products_Create.sql          # Tabla Products
│   ├── 02_ProductTags_Create.sql       # Tabla ProductTags
│   ├── 03_Users_Create.sql             # Tabla Users
│   ├── 04_Roles_Create.sql             # Tabla Roles
│   ├── 05_UserRoles_Create.sql         # Tabla UserRoles
│   ├── 99_AuditLogDJ_Create.sql        # AuditLog Dapper JSON (alternativo)
│   ├── 99_AuditLogDX_Create.sql        # AuditLog Dapper XML (alternativo)
│   ├── 99_AuditLogEFJ_Create.sql       # AuditLog EF Core JSON (alternativo)
│   └── 99_AuditLogEFX_Create.sql       # AuditLog EF Core XML (alternativo)
│
└── PostgreSQL/
    ├── 00_auditax_create.sql           # Crear base de datos
    ├── 01_products_create.sql          # Tabla products
    ├── 02_product_tags_create.sql      # Tabla product_tags
    ├── 03_users_create.sql             # Tabla users
    ├── 04_roles_create.sql             # Tabla roles
    ├── 05_user_roles_create.sql        # Tabla user_roles
    ├── 99_audit_log_dj_create.sql      # audit_log Dapper JSON (alternativo)
    ├── 99_audit_log_dx_create.sql      # audit_log Dapper XML (alternativo)
    ├── 99_audit_log_efj_create.sql     # audit_log EF Core JSON (alternativo)
    └── 99_audit_log_efx_create.sql     # audit_log EF Core XML (alternativo)
```

## Bases de Datos

| Proveedor | Base de Datos | Descripción |
|-----------|---------------|-------------|
| SQL Server | `AuditaX` | Base de datos única para todos los ejemplos |
| PostgreSQL | `auditax` | Base de datos única para todos los ejemplos |

## Tablas de AuditLog

Cada combinación de ORM + formato tiene su propia tabla de AuditLog:

| Ejemplo | SQL Server | PostgreSQL |
|---------|------------|------------|
| Dapper + JSON | `AuditLogDJ` | `audit_log_dj` |
| Dapper + XML | `AuditLogDX` | `audit_log_dx` |
| EF Core + JSON | `AuditLogEFJ` | `audit_log_efj` |
| EF Core + XML | `AuditLogEFX` | `audit_log_efx` |

## Scripts 00-05 vs 99_*

- **Scripts 00-05**: Ejecutados por DatabaseSetup. Crean la base de datos y las tablas de negocio.
- **Scripts 99_***: **Alternativos** para entornos de producción donde el usuario no tenga permisos de crear tablas. Las tablas AuditLog se crean automáticamente con `AutoCreateTable = true`.

## Uso con DatabaseSetup Tool

```bash
# Crear ambas bases de datos
dotnet run --project tools/AuditaX.Tools.DatabaseSetup -- all

# Solo SQL Server
dotnet run --project tools/AuditaX.Tools.DatabaseSetup -- sqlserver

# Solo PostgreSQL
dotnet run --project tools/AuditaX.Tools.DatabaseSetup -- postgresql
```

## Uso Manual

### SQL Server

```bash
# Ejecutar todos los scripts en orden
sqlcmd -S localhost -U sa -P sa -i scripts/SqlServer/00_AuditaX_Create.sql
sqlcmd -S localhost -U sa -P sa -d AuditaX -i scripts/SqlServer/01_Products_Create.sql
sqlcmd -S localhost -U sa -P sa -d AuditaX -i scripts/SqlServer/02_ProductTags_Create.sql
sqlcmd -S localhost -U sa -P sa -d AuditaX -i scripts/SqlServer/03_Users_Create.sql
sqlcmd -S localhost -U sa -P sa -d AuditaX -i scripts/SqlServer/04_Roles_Create.sql
sqlcmd -S localhost -U sa -P sa -d AuditaX -i scripts/SqlServer/05_UserRoles_Create.sql

# Opcional: Crear tablas AuditLog manualmente (producción)
sqlcmd -S localhost -U sa -P sa -d AuditaX -i scripts/SqlServer/99_AuditLogDJ_Create.sql
```

### PostgreSQL

```bash
# Ejecutar todos los scripts en orden
psql -U postgres -f scripts/PostgreSQL/00_auditax_create.sql
psql -U postgres -d auditax -f scripts/PostgreSQL/01_products_create.sql
psql -U postgres -d auditax -f scripts/PostgreSQL/02_product_tags_create.sql
psql -U postgres -d auditax -f scripts/PostgreSQL/03_users_create.sql
psql -U postgres -d auditax -f scripts/PostgreSQL/04_roles_create.sql
psql -U postgres -d auditax -f scripts/PostgreSQL/05_user_roles_create.sql

# Opcional: Crear tablas audit_log manualmente (producción)
psql -U postgres -d auditax -f scripts/PostgreSQL/99_audit_log_dj_create.sql
```

## Notas

- Las tablas AuditLog se crean automáticamente cuando `AutoCreateTable = true` en la configuración
- Los scripts `99_*` son para entornos donde no hay permisos de CREATE TABLE
- El DatabaseSetup Tool solo ejecuta scripts 00-05, no los 99_*

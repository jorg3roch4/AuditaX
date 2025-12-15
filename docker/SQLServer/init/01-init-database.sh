#!/bin/bash

# AuditaX SQL Server Initialization Script
# This script initializes the database for AuditaX development

set -e

INITIALIZED_FLAG="/var/opt/mssql/.auditax_initialized"
SQL_SERVER="sqlserver"
SA_PASSWORD="${MSSQL_SA_PASSWORD:-SQLServer@2022}"

echo "============================================"
echo "AuditaX SQL Server Initialization"
echo "============================================"

# Check if already initialized
if [ -f "$INITIALIZED_FLAG" ]; then
    echo "Database already initialized. Skipping..."
    exit 0
fi

echo "Waiting for SQL Server to be ready..."

# Wait for SQL Server with retries
MAX_RETRIES=30
RETRY_COUNT=0

while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if /opt/mssql-tools18/bin/sqlcmd -S "$SQL_SERVER" -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" &>/dev/null; then
        echo "SQL Server is ready!"
        break
    fi
    RETRY_COUNT=$((RETRY_COUNT + 1))
    echo "Waiting for SQL Server... (attempt $RETRY_COUNT/$MAX_RETRIES)"
    sleep 2
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo "ERROR: SQL Server did not become ready in time"
    exit 1
fi

echo "Changing SA password for development..."
/opt/mssql-tools18/bin/sqlcmd -S "$SQL_SERVER" -U sa -P "$SA_PASSWORD" -C -i /init/00-change-sa-password.sql

echo "Creating AuditaX database..."
/opt/mssql-tools18/bin/sqlcmd -S "$SQL_SERVER" -U sa -P "sa" -C -i /init/02-create-database.sql

# Mark as initialized
touch "$INITIALIZED_FLAG"

echo "============================================"
echo "AuditaX SQL Server initialization complete!"
echo "============================================"
echo ""
echo "Connection String:"
echo "Server=localhost,1433;Database=AuditaX;User Id=sa;Password=sa;TrustServerCertificate=True"
echo ""

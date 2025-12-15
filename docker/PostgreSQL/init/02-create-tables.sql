-- AuditaX Tables Creation Script for PostgreSQL
-- Creates sample tables for audit testing

\connect auditax;

-- Create Products table (sample entity for audit testing)
CREATE TABLE IF NOT EXISTS products (
    product_id SERIAL PRIMARY KEY,
    name VARCHAR(256) NOT NULL,
    description TEXT,
    price DECIMAL(18,2) NOT NULL,
    stock INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS ix_products_name ON products (name);

-- Create Customers table (sample entity for audit testing)
CREATE TABLE IF NOT EXISTS customers (
    customer_id SERIAL PRIMARY KEY,
    name VARCHAR(256) NOT NULL,
    email VARCHAR(256) NOT NULL,
    phone VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_customers_email ON customers (email);

-- Insert sample data
INSERT INTO products (name, description, price, stock)
SELECT 'Laptop Pro', 'High-performance laptop for professionals', 1299.99, 50
WHERE NOT EXISTS (SELECT 1 FROM products WHERE name = 'Laptop Pro');

INSERT INTO products (name, description, price, stock)
SELECT 'Wireless Mouse', 'Ergonomic wireless mouse', 49.99, 200
WHERE NOT EXISTS (SELECT 1 FROM products WHERE name = 'Wireless Mouse');

INSERT INTO products (name, description, price, stock)
SELECT 'USB-C Hub', '7-in-1 USB-C Hub with HDMI', 79.99, 150
WHERE NOT EXISTS (SELECT 1 FROM products WHERE name = 'USB-C Hub');

INSERT INTO customers (name, email, phone)
SELECT 'John Doe', 'john.doe@example.com', '+1-555-0100'
WHERE NOT EXISTS (SELECT 1 FROM customers WHERE email = 'john.doe@example.com');

INSERT INTO customers (name, email, phone)
SELECT 'Jane Smith', 'jane.smith@example.com', '+1-555-0200'
WHERE NOT EXISTS (SELECT 1 FROM customers WHERE email = 'jane.smith@example.com');

-- Verify setup
SELECT 'AuditaX PostgreSQL setup complete!' AS status;
SELECT 'Products count: ' || COUNT(*)::text AS info FROM products;
SELECT 'Customers count: ' || COUNT(*)::text AS info FROM customers;

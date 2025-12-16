using System.Data;
using Dapper;
using AuditaX.Enums;
using AuditaX.Samples.Common.Demo;
using AuditaX.Samples.Common.Entities;

namespace AuditaX.Sample.Dapper.Data;

/// <summary>
/// Dapper implementation of data operations for the demo.
/// Tables are created by the DatabaseSetup tool before running this sample.
/// </summary>
public class DapperDataOperations : IDemoDataOperations
{
    private readonly IDbConnection _connection;
    private readonly DatabaseType _databaseType;

    public DapperDataOperations(IDbConnection connection, DatabaseType databaseType)
    {
        _connection = connection;
        _databaseType = databaseType;
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        if (_databaseType == DatabaseType.SqlServer)
        {
            var sql = @"
                INSERT INTO Products (Name, Description, Price, Stock, IsActive)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Description, @Price, @Stock, @IsActive)";

            product.Id = await _connection.QuerySingleAsync<int>(sql, product);
        }
        else
        {
            var sql = @"
                INSERT INTO products (name, description, price, stock, is_active)
                VALUES (@Name, @Description, @Price, @Stock, @IsActive)
                RETURNING id";

            product.Id = await _connection.QuerySingleAsync<int>(sql, product);
        }

        return product;
    }

    public async Task UpdateProductAsync(Product product)
    {
        if (_databaseType == DatabaseType.SqlServer)
        {
            var sql = @"
                UPDATE Products
                SET Name = @Name, Description = @Description, Price = @Price, Stock = @Stock, IsActive = @IsActive
                WHERE Id = @Id";

            await _connection.ExecuteAsync(sql, product);
        }
        else
        {
            var sql = @"
                UPDATE products
                SET name = @Name, description = @Description, price = @Price, stock = @Stock, is_active = @IsActive
                WHERE id = @Id";

            await _connection.ExecuteAsync(sql, product);
        }
    }

    public async Task<ProductTag> CreateProductTagAsync(ProductTag tag)
    {
        if (_databaseType == DatabaseType.SqlServer)
        {
            var sql = @"
                INSERT INTO ProductTags (ProductId, Tag)
                OUTPUT INSERTED.Id
                VALUES (@ProductId, @Tag)";

            tag.Id = await _connection.QuerySingleAsync<int>(sql, tag);
        }
        else
        {
            var sql = @"
                INSERT INTO product_tags (product_id, tag)
                VALUES (@ProductId, @Tag)
                RETURNING id";

            tag.Id = await _connection.QuerySingleAsync<int>(sql, tag);
        }

        return tag;
    }

    public async Task DeleteProductTagAsync(int tagId)
    {
        var sql = _databaseType == DatabaseType.SqlServer
            ? "DELETE FROM ProductTags WHERE Id = @Id"
            : "DELETE FROM product_tags WHERE id = @Id";

        await _connection.ExecuteAsync(sql, new { Id = tagId });
    }
}

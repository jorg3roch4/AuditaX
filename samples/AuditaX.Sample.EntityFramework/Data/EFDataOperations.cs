using AuditaX.Samples.Common.Demo;
using AuditaX.Samples.Common.Entities;

namespace AuditaX.Sample.EntityFramework.Data;

/// <summary>
/// Entity Framework implementation of data operations for the demo.
/// Tables are created by the DatabaseSetup tool before running this sample.
/// </summary>
public class EFDataOperations : IDemoDataOperations
{
    private readonly AppDbContext _context;

    public EFDataOperations(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task UpdateProductAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task<ProductTag> CreateProductTagAsync(ProductTag tag)
    {
        _context.ProductTags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task DeleteProductTagAsync(int tagId)
    {
        var tag = await _context.ProductTags.FindAsync(tagId);
        if (tag != null)
        {
            _context.ProductTags.Remove(tag);
            await _context.SaveChangesAsync();
        }
    }
}

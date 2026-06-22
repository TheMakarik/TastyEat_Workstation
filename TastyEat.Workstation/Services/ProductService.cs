using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Models;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services;

public sealed class ProductService : IProductService
{
    private readonly DataContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(DataContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProductType>> SearchAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var types = await _context.ProductTypes
            .AsNoTracking()
            .Include(t => t.Products)
            .ThenInclude(p => p.Prices)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        foreach (var type in types)
            type.Products = [.. type.Products.OrderBy(p => p.Name)];

        if (string.IsNullOrWhiteSpace(pattern) || pattern.Trim() == "*")
            return types;

        var trimmed = pattern.Trim();
        var filtered = new List<ProductType>();

        foreach (var type in types)
        {
            var typeMatches = type.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase);
            var matchingProducts = type.Products
                .Where(p => p.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (typeMatches)
            {
                filtered.Add(type);
                continue;
            }

            if (matchingProducts.Count == 0)
                continue;

            type.Products = matchingProducts;
            filtered.Add(type);
        }

        return filtered;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.ProductType)
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product> CreateAsync(ProductEditDto dto, CancellationToken cancellationToken = default)
    {
        var type = await _context.ProductTypes.FindAsync(new object[] { dto.ProductTypeId }, cancellationToken)
                   ?? throw new InvalidOperationException($"Product type with id {dto.ProductTypeId} not found.");

        var product = new Product
        {
            Name = dto.Name.Trim(),
            ProductType = type,
            IsWeighted = dto.IsWeighted
        };

        product.Prices.Add(new ProductPrice
        {
            Product = product,
            Price = dto.Price,
            EffectiveFrom = DateTime.Now
        });

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created: {ProductName} (Id: {ProductId})", product.Name, product.Id);
        return product;
    }

    public async Task<Product> UpdateAsync(ProductEditDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
                          .Include(p => p.ProductType)
                          .Include(p => p.Prices)
                          .FirstOrDefaultAsync(p => p.Id == dto.Id, cancellationToken)
                      ?? throw new InvalidOperationException($"Product with id {dto.Id} not found.");

        var type = await _context.ProductTypes.FindAsync(new object[] { dto.ProductTypeId }, cancellationToken)
                   ?? throw new InvalidOperationException($"Product type with id {dto.ProductTypeId} not found.");

        product.Name = dto.Name.Trim();
        product.ProductType = type;
        product.IsWeighted = dto.IsWeighted;

        var activePrice = product.Prices.FirstOrDefault(p => p.EffectiveTo == null);
        if (activePrice is null || activePrice.Price != dto.Price)
        {
            if (activePrice is not null)
                activePrice.EffectiveTo = DateTime.Now;

            product.Prices.Add(new ProductPrice
            {
                Product = product,
                Price = dto.Price,
                EffectiveFrom = DateTime.Now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product updated: {ProductName} (Id: {ProductId})", product.Name, product.Id);
        return product;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("Attempted to delete non-existing product with id {ProductId}", id);
            return;
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product deleted: {ProductName} (Id: {ProductId})", product.Name, id);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludingId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .AnyAsync(p => p.Name == name && (!excludingId.HasValue || p.Id != excludingId.Value), cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Models;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services;

public sealed class ProductTypeService : IProductTypeService
{
    private readonly DataContext _context;
    private readonly ILogger<ProductTypeService> _logger;

    public ProductTypeService(DataContext context, ILogger<ProductTypeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProductType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProductTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<ProductType> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var type = new ProductType { Name = name };
        _context.ProductTypes.Add(type);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product type created: {ProductTypeName} (Id: {ProductTypeId})", type.Name, type.Id);
        return type;
    }

    public async Task<ProductType> UpdateAsync(ProductTypeEditDto dto, CancellationToken cancellationToken = default)
    {
        var type = await _context.ProductTypes.FindAsync(new object[] { dto.Id }, cancellationToken)
                   ?? throw new InvalidOperationException($"Product type with id {dto.Id} not found.");

        type.Name = dto.Name;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product type updated: {ProductTypeName} (Id: {ProductTypeId})", type.Name, type.Id);
        return type;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var type = await _context.ProductTypes.FindAsync(new object[] { id }, cancellationToken);
        if (type is null)
        {
            _logger.LogWarning("Attempted to delete non-existing product type with id {ProductTypeId}", id);
            return;
        }

        _context.ProductTypes.Remove(type);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product type deleted: {ProductTypeName} (Id: {ProductTypeId})", type.Name, id);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludingId = null, CancellationToken cancellationToken = default)
    {
        return await _context.ProductTypes
            .AsNoTracking()
            .AnyAsync(t => t.Name == name && (!excludingId.HasValue || t.Id != excludingId.Value), cancellationToken);
    }
}

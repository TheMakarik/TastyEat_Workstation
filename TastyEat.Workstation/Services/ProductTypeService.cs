using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Models;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services;

public sealed class ProductTypeService(DataContext context, ILogger<ProductTypeService> logger) : IProductTypeService
{
    public async Task<IReadOnlyList<ProductType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.ProductTypes
            .AsNoTracking()
            .Include(t => t.Products)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.ProductTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<ProductType> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var type = new ProductType { Name = name };
        context.ProductTypes.Add(type);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Создан тип продукта: {ProductTypeName} (Id: {ProductTypeId})", type.Name, type.Id);
        return type;
    }

    public async Task<ProductType> UpdateAsync(ProductTypeEditDto dto, CancellationToken cancellationToken = default)
    {
        var type = await context.ProductTypes.FindAsync(new object[] { dto.Id }, cancellationToken)
                   ?? throw new InvalidOperationException($"Product type with id {dto.Id} not found.");

        type.Name = dto.Name;
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Тип продукта обновлён: {ProductTypeName} (Id: {ProductTypeId})", type.Name, type.Id);
        return type;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var hasProducts = await context.Products
            .AsNoTracking()
            .AnyAsync(p => p.ProductType.Id == id, cancellationToken);
        if (hasProducts)
            throw new InvalidOperationException("С этим типом связаны товары — сначала удалите или перенесите их");

        var type = await context.ProductTypes.FindAsync(new object[] { id }, cancellationToken);
        if (type is null)
        {
            logger.LogWarning("Попытка удалить несуществующий тип продукта с id {ProductTypeId}", id);
            return;
        }

        context.ProductTypes.Remove(type);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Тип продукта удалён: {ProductTypeName} (Id: {ProductTypeId})", type.Name, id);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludingId = null, CancellationToken cancellationToken = default)
    {
        return await context.ProductTypes
            .AsNoTracking()
            .AnyAsync(t => t.Name == name && (!excludingId.HasValue || t.Id != excludingId.Value), cancellationToken);
    }
}

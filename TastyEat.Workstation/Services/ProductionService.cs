using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Models;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services;

public sealed class ProductionService(DataContext context, ILogger<ProductionService> logger) : IProductionService
{
    public async Task<IReadOnlyList<ProductionBatch>> GetBatchesAsync(string? pattern, CancellationToken cancellationToken = default)
    {
        var batches = await context.ProductionBatches
            .AsNoTracking()
            .Include(b => b.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p!.ProductType)
            .Include(b => b.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p!.Prices)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync(cancellationToken);

        foreach (var batch in batches)
            batch.Items = [.. batch.Items.OrderBy(i => i.Product!.Name)];

        if (string.IsNullOrWhiteSpace(pattern))
            return batches;

        var trimmed = pattern.Trim();
        var filtered = new List<ProductionBatch>();

        foreach (var batch in batches)
        {
            var dateText = batch.StartDate.ToString("yyyy-MM-dd");
            var numberText = $"развод {batch.Number}";

            var batchMatches = numberText.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                               || dateText.Contains(trimmed)
                               || batch.Number.ToString().Contains(trimmed);

            var matchingItems = batch.Items
                .Where(i => i.Product!.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (batchMatches)
            {
                filtered.Add(batch);
                continue;
            }

            if (matchingItems.Count == 0)
                continue;

            batch.Items = matchingItems;
            filtered.Add(batch);
        }

        return filtered;
    }

    public async Task<ProductionBatch?> GetBatchByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.ProductionBatches
            .AsNoTracking()
            .Include(b => b.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p!.ProductType)
            .Include(b => b.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p!.Prices)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<ProductionBatchItem?> GetItemByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.ProductionBatchItems
            .AsNoTracking()
            .Include(i => i.Product)
            .ThenInclude(p => p!.ProductType)
            .Include(i => i.Product)
            .ThenInclude(p => p!.Prices)
            .Include(i => i.ProductionBatch)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<ProductionBatch> CreateAsync(ProductionEditDto dto, CancellationToken cancellationToken = default)
    {
        var date = dto.Date.Date;
        var existingBatch = await context.ProductionBatches
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.StartDate.Date == date, cancellationToken);

        ProductionBatch batch;
        if (existingBatch is not null)
        {
            batch = existingBatch;
        }
        else
        {
            var maxNumber = await context.ProductionBatches
                .MaxAsync(b => (int?)b.Number, cancellationToken) ?? 0;

            batch = new ProductionBatch
            {
                Number = maxNumber + 1,
                StartDate = date
            };
            context.ProductionBatches.Add(batch);
        }

        foreach (var itemDto in dto.Items)
        {
            var product = await context.Products.FindAsync(new object[] { itemDto.ProductId }, cancellationToken)
                          ?? throw new InvalidOperationException($"Product with id {itemDto.ProductId} not found.");

            batch.Items.Add(new ProductionBatchItem
            {
                ProductionBatch = batch,
                Product = product,
                Quantity = itemDto.Quantity
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Партия производства #{BatchNumber} от {BatchDate} создана/обновлена, позиций: {ItemCount}",
            batch.Number,
            batch.StartDate.ToString("yyyy-MM-dd"),
            dto.Items.Count);

        return batch;
    }

    public async Task<ProductionBatch> UpdateBatchAsync(int id, ProductionEditDto dto, CancellationToken cancellationToken = default)
    {
        var batch = await context.ProductionBatches
                          .Include(b => b.Items)
                          .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                      ?? throw new InvalidOperationException($"Production batch with id {id} not found.");

        batch.StartDate = dto.Date.Date;

        context.ProductionBatchItems.RemoveRange(batch.Items);
        batch.Items.Clear();

        foreach (var itemDto in dto.Items)
        {
            var product = await context.Products.FindAsync(new object[] { itemDto.ProductId }, cancellationToken)
                          ?? throw new InvalidOperationException($"Product with id {itemDto.ProductId} not found.");

            batch.Items.Add(new ProductionBatchItem
            {
                ProductionBatch = batch,
                Product = product,
                Quantity = itemDto.Quantity
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Партия производства #{BatchNumber} от {BatchDate} обновлена, позиций: {ItemCount}",
            batch.Number,
            batch.StartDate.ToString("yyyy-MM-dd"),
            dto.Items.Count);

        return batch;
    }

    public async Task<ProductionBatchItem> UpdateItemAsync(ProductionItemEditDto dto, CancellationToken cancellationToken = default)
    {
        var item = await context.ProductionBatchItems
                         .Include(i => i.Product)
                         .FirstOrDefaultAsync(i => i.Id == dto.Id, cancellationToken)
                     ?? throw new InvalidOperationException($"Production item with id {dto.Id} not found.");

        var product = await context.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken)
                      ?? throw new InvalidOperationException($"Product with id {dto.ProductId} not found.");

        item.Product = product;
        item.Quantity = dto.Quantity;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Позиция производства обновлена: {ProductName} x {Quantity}", product.Name, dto.Quantity);
        return item;
    }

    public async Task DeleteItemAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await context.ProductionBatchItems
            .Include(i => i.ProductionBatch)
            .ThenInclude(b => b!.Items)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (item is null)
        {
            logger.LogWarning("Попытка удалить несуществующую позицию производства с id {ItemId}", id);
            return;
        }

        var batch = item.ProductionBatch;
        context.ProductionBatchItems.Remove(item);

        if (batch.Items.Count <= 1)
            context.ProductionBatches.Remove(batch);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Позиция производства удалена: {ItemId}", id);
    }

    public async Task DeleteBatchAsync(int id, CancellationToken cancellationToken = default)
    {
        var batch = await context.ProductionBatches
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (batch is null)
        {
            logger.LogWarning("Попытка удалить несуществующую партию производства с id {BatchId}", id);
            return;
        }

        context.ProductionBatches.Remove(batch);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Партия производства удалена: #{BatchNumber}", batch.Number);
    }
}

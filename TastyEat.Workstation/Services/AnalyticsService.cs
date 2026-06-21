using Microsoft.EntityFrameworkCore;
using TastyEat.Workstation.Models;
using TastyEat.Workstation.Models.Analytics;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services;

public sealed class AnalyticsService(DataContext context) : IAnalyticsService
{
    public async Task<IReadOnlyList<SalesByClientDto>> GetSalesByClientAsync(CancellationToken cancellationToken = default)
    {
        return await context.DistributionItems
            .AsNoTracking()
            .GroupBy(x => new { x.Client.FullName, x.Client.City.Name })
            .Select(g => new SalesByClientDto
            {
                ClientName = g.Key.FullName,
                CityName = g.Key.Name,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Quantity * (x.PriceAtDistribution ?? 0))
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SalesByProductDto>> GetSalesByProductAsync(CancellationToken cancellationToken = default)
    {
        return await context.DistributionItems
            .AsNoTracking()
            .GroupBy(x => new { ProductName = x.Product.Name, TypeName = x.Product.ProductType.Name })
            .Select(g => new SalesByProductDto
            {
                ProductName = g.Key.ProductName,
                ProductTypeName = g.Key.TypeName,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Quantity * (x.PriceAtDistribution ?? 0))
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionStatsDto>> GetProductionStatsAsync(CancellationToken cancellationToken = default)
    {
        var produced = await context.ProductionBatchItems
            .AsNoTracking()
            .GroupBy(x => new { ProductName = x.Product.Name, TypeName = x.Product.ProductType.Name })
            .Select(g => new
            {
                g.Key.ProductName,
                g.Key.TypeName,
                Total = g.Sum(x => x.Quantity)
            })
            .ToListAsync(cancellationToken);

        var sold = await context.DistributionItems
            .AsNoTracking()
            .GroupBy(x => x.Product.Name)
            .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Quantity) })
            .ToListAsync(cancellationToken);

        return produced
            .Select(p => new ProductionStatsDto
            {
                ProductName = p.ProductName,
                ProductTypeName = p.TypeName,
                TotalProduced = p.Total,
                TotalSold = sold.FirstOrDefault(s => s.Name == p.ProductName)?.Total ?? 0,
                Remaining = p.Total - (sold.FirstOrDefault(s => s.Name == p.ProductName)?.Total ?? 0)
            })
            .ToList();
    }
}

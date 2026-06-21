using TastyEat.Workstation.Models.Analytics;

namespace TastyEat.Workstation.Services.Interfaces;

public interface IAnalyticsService
{
    Task<IReadOnlyList<SalesByClientDto>> GetSalesByClientAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesByProductDto>> GetSalesByProductAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductionStatsDto>> GetProductionStatsAsync(CancellationToken cancellationToken = default);
}

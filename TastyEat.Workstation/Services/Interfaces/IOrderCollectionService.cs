using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;

namespace TastyEat.Workstation.Services.Interfaces;

public interface IOrderCollectionService
{
    Task<IReadOnlyList<OrderCollection>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrderCollection?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderCollection?> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<OrderCollection> CreateAsync(CancellationToken cancellationToken = default);
    Task<OrderCollection> CloseAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderCollectionClient> UpsertClientAsync(int collectionId, OrderCollectionClientEditDto dto, CancellationToken cancellationToken = default);
    Task DeleteCollectionAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteClientAsync(int id, CancellationToken cancellationToken = default);
    Task<int> GetAvailableStockAsync(int productId, int? excludingClientId = null, CancellationToken cancellationToken = default);
    Task<int> GetProducedQuantityAsync(int productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderCollectionStatisticDto>> GetCollectionStatisticsAsync(CancellationToken cancellationToken = default);
}

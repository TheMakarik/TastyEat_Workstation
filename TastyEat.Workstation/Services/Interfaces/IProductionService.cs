using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;

namespace TastyEat.Workstation.Services.Interfaces;

public interface IProductionService
{
    Task<IReadOnlyList<ProductionBatch>> GetBatchesAsync(string? pattern, CancellationToken cancellationToken = default);
    Task<ProductionBatch?> GetBatchByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductionBatchItem?> GetItemByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductionBatch> CreateAsync(ProductionEditDto dto, CancellationToken cancellationToken = default);
    Task<ProductionBatch> UpdateBatchAsync(int id, ProductionEditDto dto, CancellationToken cancellationToken = default);
    Task<ProductionBatchItem> UpdateItemAsync(ProductionItemEditDto dto, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteBatchAsync(int id, CancellationToken cancellationToken = default);
}

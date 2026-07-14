using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;

namespace TastyEat.Workstation.Services.Interfaces;

public interface IDistributionService
{
    Task<IReadOnlyList<Distribution>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Distribution?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DistributionClient?> GetClientByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Distribution> CreateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<DistributionClient> AddClientAsync(int distributionId, int clientId, int totalAmount, List<DistributionItemEditDto> items, CancellationToken cancellationToken = default);
    Task<DistributionClient> UpdateClientAsync(int distributionClientId, int clientId, int totalAmount, List<DistributionItemEditDto> items, CancellationToken cancellationToken = default);
    Task DeleteDistributionAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteClientAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientOrderedProductDto>> GetClientOrderedProductsAsync(int clientId, CancellationToken cancellationToken = default);
    Task<int> GetRemainingQuantityAsync(int productId, int? excludingDistributionClientId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientProductShareDto>> GetClientProductSharesAsync(int clientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientPurchaseHistoryDto>> GetClientPurchaseHistoryAsync(int clientId, CancellationToken cancellationToken = default);
}

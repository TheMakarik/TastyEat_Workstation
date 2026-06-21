using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;

namespace TastyEat.Workstation.Services.Interfaces;

public interface IClientService
{
    Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Client>> SearchAsync(string pattern, CancellationToken cancellationToken = default);
    Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Client> CreateAsync(ClientEditDto dto, CancellationToken cancellationToken = default);
    Task<Client> UpdateAsync(ClientEditDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> PhoneExistsAsync(string phoneNumber, int? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByFullNameAsync(string fullName, CancellationToken cancellationToken = default);
    Task<Client?> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPurchasedAmountAsync(int clientId, CancellationToken cancellationToken = default);
}

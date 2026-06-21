using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;

namespace TastyEat.Workstation.Services.Interfaces;

public interface IProductTypeService
{
    Task<IReadOnlyList<ProductType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductType> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<ProductType> UpdateAsync(ProductTypeEditDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludingId = null, CancellationToken cancellationToken = default);
}

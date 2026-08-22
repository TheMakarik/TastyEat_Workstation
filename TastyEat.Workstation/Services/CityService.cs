using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Models;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services;

public sealed class CityService(DataContext context, ILogger<CityService> logger) : ICityService
{
    public async Task<IReadOnlyList<City>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Cities
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<City?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<City> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var city = new City { Name = name };
        context.Cities.Add(city);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Создан город: {CityName} (Id: {CityId})", city.Name, city.Id);
        return city;
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await context.Cities
            .AsNoTracking()
            .AnyAsync(c => c.Name == name, cancellationToken);
    }
}

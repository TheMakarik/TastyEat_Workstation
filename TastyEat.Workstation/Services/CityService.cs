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

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var hasClients = await context.Clients
            .AsNoTracking()
            .AnyAsync(c => c.City.Id == id, cancellationToken);
        if (hasClients)
            throw new InvalidOperationException("В этом городе есть клиенты — сначала перенесите или удалите их");

        var city = await context.Cities.FindAsync(new object[] { id }, cancellationToken);
        if (city is null)
        {
            logger.LogWarning("Попытка удалить несуществующий город с id {CityId}", id);
            return;
        }

        context.Cities.Remove(city);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Город удалён: {CityName} (Id: {CityId})", city.Name, id);
    }
}

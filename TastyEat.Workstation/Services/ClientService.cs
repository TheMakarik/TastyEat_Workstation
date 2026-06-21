using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Models;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services;

public sealed class ClientService : IClientService
{
    private readonly DataContext _context;
    private readonly ILogger<ClientService> _logger;

    public ClientService(DataContext context, ILogger<ClientService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await QueryClients()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Client>> SearchAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var query = QueryClients();

        if (!string.IsNullOrWhiteSpace(pattern) && pattern.Trim() != "*")
        {
            var likePattern = ToLikePattern(pattern);
            query = query.Where(c => EF.Functions.Like(c.FullName, likePattern) ||
                                      EF.Functions.Like(c.PhoneNumber, likePattern));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await QueryClients()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Client> CreateAsync(ClientEditDto dto, CancellationToken cancellationToken = default)
    {
        var city = await _context.Cities.FindAsync(new object[] { dto.CityId }, cancellationToken)
                   ?? throw new InvalidOperationException($"City with id {dto.CityId} not found.");

        Client? referrer = null;
        if (dto.ReferrerId.HasValue)
        {
            referrer = await _context.Clients.FindAsync(new object[] { dto.ReferrerId.Value }, cancellationToken)
                       ?? throw new InvalidOperationException($"Referrer with id {dto.ReferrerId.Value} not found.");
        }

        var client = new Client
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            City = city,
            IsInTelegramChannel = dto.IsInTelegramChannel,
            Referrer = referrer
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Client created: {ClientName} (Id: {ClientId})", client.FullName, client.Id);
        return client;
    }

    public async Task<Client> UpdateAsync(ClientEditDto dto, CancellationToken cancellationToken = default)
    {
        var client = await _context.Clients
                         .Include(c => c.City)
                         .Include(c => c.Referrer)
                         .FirstOrDefaultAsync(c => c.Id == dto.Id, cancellationToken)
                     ?? throw new InvalidOperationException($"Client with id {dto.Id} not found.");

        var city = await _context.Cities.FindAsync(new object[] { dto.CityId }, cancellationToken)
                   ?? throw new InvalidOperationException($"City with id {dto.CityId} not found.");

        Client? referrer = null;
        if (dto.ReferrerId.HasValue)
        {
            referrer = await _context.Clients.FindAsync(new object[] { dto.ReferrerId.Value }, cancellationToken)
                       ?? throw new InvalidOperationException($"Referrer with id {dto.ReferrerId.Value} not found.");
        }

        client.FullName = dto.FullName;
        client.PhoneNumber = dto.PhoneNumber;
        client.City = city;
        client.IsInTelegramChannel = dto.IsInTelegramChannel;
        client.Referrer = referrer;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Client updated: {ClientName} (Id: {ClientId})", client.FullName, client.Id);
        return client;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = await _context.Clients.FindAsync(new object[] { id }, cancellationToken);
        if (client is null)
        {
            _logger.LogWarning("Attempted to delete non-existing client with id {ClientId}", id);
            return;
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Client deleted: {ClientName} (Id: {ClientId})", client.FullName, id);
    }

    public async Task<bool> PhoneExistsAsync(string phoneNumber, int? excludingId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AsNoTracking()
            .AnyAsync(c => c.PhoneNumber == phoneNumber && (!excludingId.HasValue || c.Id != excludingId.Value), cancellationToken);
    }

    public async Task<bool> ExistsByFullNameAsync(string fullName, CancellationToken cancellationToken = default)
    {
        var normalized = fullName.Trim();
        return await _context.Clients
            .AsNoTracking()
            .AnyAsync(c => c.FullName == normalized, cancellationToken);
    }

    public async Task<Client?> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default)
    {
        var normalized = fullName.Trim();
        return await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.FullName == normalized, cancellationToken);
    }

    public async Task<decimal> GetTotalPurchasedAmountAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var items = await _context.DistributionItems
            .AsNoTracking()
            .Where(di => di.Client.Id == clientId)
            .Include(di => di.Distribution)
            .Include(di => di.Product)
            .ThenInclude(p => p.Prices)
            .ToListAsync(cancellationToken);

        decimal total = 0;
        foreach (var item in items)
        {
            var price = item.PriceAtDistribution ?? 0;
            if (price == 0)
            {
                var matchingPrice = item.Product.Prices
                    .FirstOrDefault(p => item.Distribution.Date >= p.EffectiveFrom &&
                                         (p.EffectiveTo == null || item.Distribution.Date <= p.EffectiveTo));
                price = matchingPrice?.Price ?? 0;
            }

            total += (decimal)item.Quantity * price;
        }

        return total;
    }

    private IQueryable<Client> QueryClients()
    {
        return _context.Clients
            .AsNoTracking()
            .Include(c => c.City)
            .Include(c => c.Referrer)
            .OrderBy(c => c.FullName);
    }

    private static string ToLikePattern(string pattern)
    {
        var like = pattern.Replace("*", "%");
        if (!like.Contains('%'))
            like = $"%{like}%";
        return like;
    }
}

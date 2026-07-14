using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Models;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services;

public sealed class DistributionService : IDistributionService
{
    private readonly DataContext _context;
    private readonly ILogger<DistributionService> _logger;

    public DistributionService(DataContext context, ILogger<DistributionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Distribution>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Distributions
            .AsNoTracking()
            .Include(d => d.Clients)
            .ThenInclude(dc => dc.Client)
            .Include(d => d.Clients)
            .ThenInclude(dc => dc.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(d => d.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<Distribution?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Distributions
            .AsNoTracking()
            .Include(d => d.Clients)
            .ThenInclude(dc => dc.Client)
            .Include(d => d.Clients)
            .ThenInclude(dc => dc.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<DistributionClient?> GetClientByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.DistributionClients
            .AsNoTracking()
            .Include(dc => dc.Client)
            .Include(dc => dc.Items)
            .ThenInclude(i => i.Product)
            .Include(dc => dc.Distribution)
            .FirstOrDefaultAsync(dc => dc.Id == id, cancellationToken);
    }

    public async Task<Distribution> CreateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var distribution = new Distribution { Date = date.Date };
        _context.Distributions.Add(distribution);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Distribution created (Id: {DistributionId}, Date: {Date})", distribution.Id, distribution.Date);
        return distribution;
    }

    public async Task<DistributionClient> AddClientAsync(int distributionId, int clientId, int totalAmount, List<DistributionItemEditDto> items, CancellationToken cancellationToken = default)
    {
        var distribution = await _context.Distributions
                               .Include(d => d.Clients)
                               .FirstOrDefaultAsync(d => d.Id == distributionId, cancellationToken)
                           ?? throw new InvalidOperationException($"Distribution with id {distributionId} not found.");

        var client = await _context.Clients.FindAsync(new object[] { clientId }, cancellationToken)
                     ?? throw new InvalidOperationException($"Client with id {clientId} not found.");

        var distributionClient = new DistributionClient
        {
            Distribution = distribution,
            Client = client,
            TotalAmount = totalAmount
        };
        _context.DistributionClients.Add(distributionClient);

        await AddItemsAsync(distributionClient, items, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Distribution client added (DistributionId: {DistributionId}, ClientId: {ClientId}, Total: {Total})",
            distributionId,
            clientId,
            totalAmount);
        return distributionClient;
    }

    public async Task<DistributionClient> UpdateClientAsync(int distributionClientId, int clientId, int totalAmount, List<DistributionItemEditDto> items, CancellationToken cancellationToken = default)
    {
        var distributionClient = await _context.DistributionClients
                                     .Include(dc => dc.Client)
                                     .Include(dc => dc.Items)
                                     .FirstOrDefaultAsync(dc => dc.Id == distributionClientId, cancellationToken)
                                 ?? throw new InvalidOperationException($"Distribution client with id {distributionClientId} not found.");

        var client = await _context.Clients.FindAsync(new object[] { clientId }, cancellationToken)
                     ?? throw new InvalidOperationException($"Client with id {clientId} not found.");

        distributionClient.Client = client;
        distributionClient.TotalAmount = totalAmount;

        _context.DistributionItems.RemoveRange(distributionClient.Items);
        distributionClient.Items.Clear();

        await AddItemsAsync(distributionClient, items, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Distribution client updated (Id: {DistributionClientId}, ClientId: {ClientId}, Total: {Total})",
            distributionClientId,
            clientId,
            totalAmount);
        return distributionClient;
    }

    public async Task DeleteDistributionAsync(int id, CancellationToken cancellationToken = default)
    {
        var distribution = await _context.Distributions
                                .Include(d => d.Clients)
                                .ThenInclude(dc => dc.Items)
                                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (distribution is null)
        {
            _logger.LogWarning("Attempted to delete non-existing distribution with id {DistributionId}", id);
            return;
        }

        _context.Distributions.Remove(distribution);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Distribution deleted (Id: {DistributionId})", id);
    }

    public async Task DeleteClientAsync(int id, CancellationToken cancellationToken = default)
    {
        var distributionClient = await _context.DistributionClients
                                     .Include(dc => dc.Items)
                                     .FirstOrDefaultAsync(dc => dc.Id == id, cancellationToken);

        if (distributionClient is null)
        {
            _logger.LogWarning("Attempted to delete non-existing distribution client with id {DistributionClientId}", id);
            return;
        }

        _context.DistributionClients.Remove(distributionClient);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Distribution client deleted (Id: {DistributionClientId})", id);
    }

    public async Task<IReadOnlyList<ClientOrderedProductDto>> GetClientOrderedProductsAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var ordered = await _context.OrderCollectionItems
            .AsNoTracking()
            .Where(i => i.OrderCollectionClient.Client.Id == clientId)
            .GroupBy(i => i.Product.Id)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => (int?)i.Quantity) ?? 0 })
            .ToListAsync(cancellationToken);

        if (ordered.Count == 0)
        {
            var allProducts = await _context.Products
                .AsNoTracking()
                .Include(p => p.ProductType)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            return allProducts.Select(p => new ClientOrderedProductDto(p, 0)).ToList();
        }

        var productIds = ordered.Select(o => o.ProductId).ToList();
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.ProductType)
            .Where(p => productIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var quantityByProduct = ordered.ToDictionary(o => o.ProductId, o => o.Quantity);
        return products
            .Select(p => new ClientOrderedProductDto(p, quantityByProduct.GetValueOrDefault(p.Id)))
            .ToList();
    }

    public async Task<int> GetRemainingQuantityAsync(int productId, int? excludingDistributionClientId = null, CancellationToken cancellationToken = default)
    {
        var ordered = await _context.OrderCollectionItems
            .AsNoTracking()
            .Where(i => i.Product.Id == productId)
            .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;

        var deliveredQuery = _context.DistributionItems
            .AsNoTracking()
            .Where(i => i.Product.Id == productId);

        if (excludingDistributionClientId.HasValue)
        {
            deliveredQuery = deliveredQuery.Where(i => i.DistributionClient.Id != excludingDistributionClientId.Value);
        }

        var delivered = await deliveredQuery.SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;

        return ordered - delivered;
    }

    public async Task<IReadOnlyList<ClientProductShareDto>> GetClientProductSharesAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await _context.DistributionItems
            .AsNoTracking()
            .Where(i => i.DistributionClient.Client.Id == clientId)
            .GroupBy(i => new { i.Product.Id, ProductName = i.Product.Name, ProductTypeName = i.Product.ProductType.Name })
            .Select(g => new ClientProductShareDto(g.Key.ProductTypeName, g.Key.ProductName, (int)g.Sum(i => (double)i.Quantity)))
            .OrderByDescending(x => x.Quantity)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClientPurchaseHistoryDto>> GetClientPurchaseHistoryAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await _context.DistributionClients
            .AsNoTracking()
            .Where(dc => dc.Client.Id == clientId)
            .Select(dc => new ClientPurchaseHistoryDto(dc.Distribution.Date, dc.TotalAmount))
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    private async Task AddItemsAsync(DistributionClient distributionClient, List<DistributionItemEditDto> items, CancellationToken cancellationToken)
    {
        foreach (var itemDto in items.Where(i => i.Quantity > 0))
        {
            var product = await _context.Products.FindAsync(new object[] { itemDto.ProductId }, cancellationToken)
                          ?? throw new InvalidOperationException($"Product with id {itemDto.ProductId} not found.");

            distributionClient.Items.Add(new DistributionItem
            {
                DistributionClient = distributionClient,
                Product = product,
                Quantity = itemDto.Quantity
            });
        }
    }
}

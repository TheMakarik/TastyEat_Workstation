using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Models;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services;

public sealed class OrderCollectionService : IOrderCollectionService
{
    private readonly DataContext _context;
    private readonly ILogger<OrderCollectionService> _logger;

    public OrderCollectionService(DataContext context, ILogger<OrderCollectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OrderCollection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OrderCollections
            .AsNoTracking()
            .Include(c => c.Clients)
            .ThenInclude(cc => cc.Client)
            .Include(c => c.Clients)
            .ThenInclude(cc => cc.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderCollection?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.OrderCollections
            .AsNoTracking()
            .Include(c => c.Clients)
            .ThenInclude(cc => cc.Client)
            .Include(c => c.Clients)
            .ThenInclude(cc => cc.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<OrderCollection?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OrderCollections
            .AsNoTracking()
            .Include(c => c.Clients)
            .ThenInclude(cc => cc.Client)
            .Include(c => c.Clients)
            .ThenInclude(cc => cc.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.EndDate == null, cancellationToken);
    }

    public async Task<OrderCollection> CreateAsync(CancellationToken cancellationToken = default)
    {
        var collection = new OrderCollection
        {
            StartDate = DateTime.Now
        };

        _context.OrderCollections.Add(collection);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order collection started (Id: {CollectionId})", collection.Id);
        return collection;
    }

    public async Task<OrderCollection> CloseAsync(int id, CancellationToken cancellationToken = default)
    {
        var collection = await _context.OrderCollections.FindAsync(new object[] { id }, cancellationToken)
                         ?? throw new InvalidOperationException($"Order collection with id {id} not found.");

        collection.EndDate = DateTime.Now;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order collection closed (Id: {CollectionId})", collection.Id);
        return collection;
    }

    public async Task<OrderCollectionClient> UpsertClientAsync(int collectionId, OrderCollectionClientEditDto dto, CancellationToken cancellationToken = default)
    {
        var collection = await _context.OrderCollections
                              .Include(c => c.Clients)
                              .ThenInclude(cc => cc.Items)
                              .FirstOrDefaultAsync(c => c.Id == collectionId, cancellationToken)
                          ?? throw new InvalidOperationException($"Order collection with id {collectionId} not found.");

        var client = await _context.Clients.FindAsync(new object[] { dto.ClientId }, cancellationToken)
                     ?? throw new InvalidOperationException($"Client with id {dto.ClientId} not found.");

        OrderCollectionClient clientEntry;
        if (dto.Id == 0)
        {
            clientEntry = new OrderCollectionClient
            {
                OrderCollection = collection,
                Client = client
            };
            _context.OrderCollectionClients.Add(clientEntry);
        }
        else
        {
            clientEntry = collection.Clients.FirstOrDefault(cc => cc.Id == dto.Id)
                          ?? throw new InvalidOperationException($"Order collection client with id {dto.Id} not found.");

            clientEntry.Client = client;
            _context.OrderCollectionItems.RemoveRange(clientEntry.Items);
            clientEntry.Items.Clear();
        }

        foreach (var itemDto in dto.Items.Where(i => i.Quantity > 0))
        {
            var product = await _context.Products.FindAsync(new object[] { itemDto.ProductId }, cancellationToken)
                          ?? throw new InvalidOperationException($"Product with id {itemDto.ProductId} not found.");

            clientEntry.Items.Add(new OrderCollectionItem
            {
                OrderCollectionClient = clientEntry,
                Product = product,
                Quantity = itemDto.Quantity
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order collection client saved (CollectionId: {CollectionId}, ClientId: {ClientId})",
            collectionId,
            client.Id);
        return clientEntry;
    }

    public async Task DeleteCollectionAsync(int id, CancellationToken cancellationToken = default)
    {
        var collection = await _context.OrderCollections
                              .Include(c => c.Clients)
                              .ThenInclude(cc => cc.Items)
                              .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (collection is null)
        {
            _logger.LogWarning("Attempted to delete non-existing order collection with id {CollectionId}", id);
            return;
        }

        _context.OrderCollections.Remove(collection);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order collection deleted (Id: {CollectionId})", id);
    }

    public async Task DeleteClientAsync(int id, CancellationToken cancellationToken = default)
    {
        var clientEntry = await _context.OrderCollectionClients
                                .Include(cc => cc.Items)
                                .FirstOrDefaultAsync(cc => cc.Id == id, cancellationToken);

        if (clientEntry is null)
        {
            _logger.LogWarning("Attempted to delete non-existing order collection client with id {ClientEntryId}", id);
            return;
        }

        _context.OrderCollectionClients.Remove(clientEntry);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order collection client deleted (Id: {ClientEntryId})", id);
    }

    public async Task<int> GetAvailableStockAsync(int productId, int? excludingClientId = null, CancellationToken cancellationToken = default)
    {
        var produced = await _context.ProductionBatchItems
            .AsNoTracking()
            .Where(i => i.Product.Id == productId)
            .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;

        var distributed = await _context.DistributionItems
            .AsNoTracking()
            .Where(i => i.Product.Id == productId)
            .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;

        var reservedQuery = _context.OrderCollectionItems
            .AsNoTracking()
            .Where(i => i.Product.Id == productId);

        if (excludingClientId.HasValue)
        {
            reservedQuery = reservedQuery.Where(i => i.OrderCollectionClient.Id != excludingClientId.Value);
        }

        var reserved = await reservedQuery.SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;

        return produced - distributed - reserved;
    }

    public async Task<int> GetProducedQuantityAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductionBatchItems
            .AsNoTracking()
            .Where(i => i.Product.Id == productId)
            .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;
    }

    public async Task<IReadOnlyList<OrderCollectionStatisticDto>> GetCollectionStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OrderCollections
            .AsNoTracking()
            .Include(c => c.Clients)
            .Select(c => new OrderCollectionStatisticDto(c.StartDate, c.Clients.Count))
            .OrderBy(c => c.Date)
            .ToListAsync(cancellationToken);
    }
}

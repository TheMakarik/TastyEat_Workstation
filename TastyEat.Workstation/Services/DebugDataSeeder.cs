#if DEBUG
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Models;
using TastyEat.Workstation.Models.Tables;

namespace TastyEat.Workstation.Services;

/// <summary>
/// Заполнение базы фиктивными данными. Существует только в Debug-сборках.
/// </summary>
public static class DebugDataSeeder
{
    private static readonly string[] CityNames =
    [
        "Москва", "Санкт-Петербург", "Новосибирск", "Екатеринбург", "Казань", "Нижний Новгород",
        "Челябинск", "Самара", "Омск", "Ростов-на-Дону", "Уфа", "Красноярск", "Воронеж",
        "Пермь", "Волгоград"
    ];

    private static readonly (string Type, string[] Products)[] Catalog =
    [
        ("Молочная продукция", ["Молоко", "Кефир", "Творог", "Сметана", "Сыр «Гауда»", "Сыр «Российский»", "Масло сливочное", "Йогурт", "Ряженка", "Сливки"]),
        ("Мясная продукция", ["Колбаса варёная", "Колбаса сырокопчёная", "Сосиски", "Куриное филе", "Фарш домашний", "Гуляш", "Окорочка", "Шашлык свиной"]),
        ("Выпечка", ["Хлеб белый", "Хлеб бородинский", "Батон нарезной", "Булочка с маком", "Пирожок с картошкой", "Круассан", "Лаваш", "Слойка с яблоком"]),
        ("Напитки", ["Сок яблочный", "Сок апельсиновый", "Морс клюквенный", "Компот", "Квас", "Лимонад", "Вода минеральная"]),
        ("Кондитерские изделия", ["Торт «Наполеон»", "Печенье овсяное", "Пряники", "Вафли", "Зефир", "Мармелад", "Халва"]),
        ("Полуфабрикаты", ["Пельмени домашние", "Вареники с вишней", "Котлеты", "Блины замороженные", "Голубцы"])
    ];

    private static readonly string[] ProductVariants = ["500 г", "1 л", "900 г", "200 г", "5%", "3,2%", "классические", "домашние", "по-деревенски"];

    public static async Task SeedAsync(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var faker = new Bogus.Faker();
        var random = faker.Random;

        var cities = CityNames.Select(name => new City { Name = name }).ToList();
        await context.Cities.AddRangeAsync(cities);
        await context.SaveChangesAsync();
        logger.LogInformation("Тестовые данные: добавлено городов: {Count}", cities.Count);

        var productTypes = new List<ProductType>();
        var products = new List<Product>();
        foreach (var (typeName, baseNames) in Catalog)
        {
            var type = new ProductType { Name = typeName };
            productTypes.Add(type);

            var counter = 0;
            while (products.Count(p => p.ProductType == type) < Math.Min(baseNames.Length, 100 - products.Count))
            {
                var baseName = baseNames[counter % baseNames.Length];
                var variant = ProductVariants[random.Int(0, ProductVariants.Length - 1)];
                var name = counter < baseNames.Length ? baseName : $"{baseName} ({variant})";

                if (products.Any(p => p.Name == name))
                    break;

                var product = new Product
                {
                    Name = name,
                    ProductType = type,
                    Prices =
                    [
                        new ProductPrice { Price = random.Int(50, 1500), EffectiveFrom = DateTime.Now.AddDays(-random.Int(30, 300)) }
                    ]
                };
                products.Add(product);
                counter++;
            }
        }

        await context.ProductTypes.AddRangeAsync(productTypes);
        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
        logger.LogInformation("Тестовые данные: добавлено типов: {Types}, продуктов: {Products}", productTypes.Count, products.Count);

        var usedPhones = new HashSet<string>();
        var clients = new List<Client>();
        for (var i = 0; i < 500; i++)
        {
            string phone;
            do
            {
                phone = $"+7 9{random.Int(100, 999)} {random.Int(100, 999)} {random.Int(10, 99)} {random.Int(10, 99)}";
            } while (!usedPhones.Add(phone));

            clients.Add(new Client
            {
                FullName = faker.Person.FullName,
                PhoneNumber = phone,
                City = cities[random.Int(0, cities.Count - 1)],
                IsInTelegramChannel = random.Bool(0.4f),
                Referrer = clients.Count > 20 && random.Bool(0.3f)
                    ? clients[random.Int(0, clients.Count - 1)]
                    : null
            });
        }

        await context.Clients.AddRangeAsync(clients);
        await context.SaveChangesAsync();
        logger.LogInformation("Тестовые данные: добавлено клиентов: {Count}", clients.Count);

        var batches = new List<ProductionBatch>();
        for (var number = 1; number <= 30; number++)
        {
            var batch = new ProductionBatch
            {
                Number = number,
                StartDate = DateTime.Today.AddDays(-random.Int(0, 90)),
                Items = []
            };

            foreach (var product in random.ListItems(products, random.Int(3, 8)))
                batch.Items.Add(new ProductionBatchItem { ProductionBatch = batch, Product = product, Quantity = random.Int(10, 200) });

            batch.EndDate = batch.StartDate.AddDays(random.Int(0, 2));
            batches.Add(batch);
        }

        await context.ProductionBatches.AddRangeAsync(batches);
        await context.SaveChangesAsync();
        logger.LogInformation("Тестовые данные: добавлено производственных партий: {Count}", batches.Count);

        var orderCollections = new List<OrderCollection>();
        for (var i = 0; i < 9; i++)
        {
            var isClosed = i < 8;
            var collection = new OrderCollection
            {
                StartDate = DateTime.Today.AddDays(-random.Int(7, 90)),
                Clients = []
            };
            if (isClosed)
                collection.EndDate = collection.StartDate.AddDays(random.Int(2, 6));

            foreach (var client in random.ListItems(clients, random.Int(5, 15)))
            {
                var clientEntry = new OrderCollectionClient { OrderCollection = collection, Client = client, Items = [] };
                foreach (var product in random.ListItems(products, random.Int(1, 6)))
                    clientEntry.Items.Add(new OrderCollectionItem { OrderCollectionClient = clientEntry, Product = product, Quantity = random.Int(1, 10) });
                collection.Clients.Add(clientEntry);
            }

            orderCollections.Add(collection);
        }

        await context.OrderCollections.AddRangeAsync(orderCollections);
        await context.SaveChangesAsync();
        logger.LogInformation("Тестовые данные: добавлено сборов заказов: {Count}", orderCollections.Count);

        var distributions = new List<Distribution>();
        for (var i = 0; i < 20; i++)
        {
            var distribution = new Distribution
            {
                Date = DateTime.Today.AddDays(-random.Int(0, 60)),
                Clients = []
            };

            foreach (var client in random.ListItems(clients, random.Int(3, 10)))
            {
                var distributionClient = new DistributionClient { Distribution = distribution, Client = client, TotalAmount = random.Int(500, 15000), Items = [] };
                foreach (var product in random.ListItems(products, random.Int(1, 5)))
                    distributionClient.Items.Add(new DistributionItem { DistributionClient = distributionClient, Product = product, Quantity = random.Int(1, 8) });
                distribution.Clients.Add(distributionClient);
            }

            distributions.Add(distribution);
        }

        await context.Distributions.AddRangeAsync(distributions);
        await context.SaveChangesAsync();
        logger.LogInformation("Тестовые данные: добавлено развозов: {Count}", distributions.Count);
    }
}
#endif

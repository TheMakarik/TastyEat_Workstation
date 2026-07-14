using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Options;

namespace TastyEat.Workstation.Models;

public sealed class DataContext : DbContext
{
    private readonly StringLengthOptions _stringLengthOptions;

    public DataContext(DbContextOptions<DataContext> options, IOptions<StringLengthOptions> stringLengthOptions) : base(options)
    {
        _stringLengthOptions = stringLengthOptions.Value;
        Database.ExecuteSqlRaw("PRAGMA busy_timeout = 5000;");
    }

    public DbSet<Client> Clients { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductType> ProductTypes { get; set; }
    public DbSet<ProductPrice> ProductPrices { get; set; }
    public DbSet<ProductionBatch> ProductionBatches { get; set; }
    public DbSet<ProductionBatchItem> ProductionBatchItems { get; set; }
    public DbSet<Distribution> Distributions { get; set; }
    public DbSet<DistributionClient> DistributionClients { get; set; }
    public DbSet<DistributionItem> DistributionItems { get; set; }
    public DbSet<OrderCollection> OrderCollections { get; set; }
    public DbSet<OrderCollectionClient> OrderCollectionClients { get; set; }
    public DbSet<OrderCollectionItem> OrderCollectionItems { get; set; }
    public DbSet<ApplicationSetting> ApplicationSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>()
            .Property(e => e.Name).HasMaxLength(_stringLengthOptions.CityNameMaxLength);
        modelBuilder.Entity<City>()
            .HasIndex(e => e.Name).IsUnique();

        modelBuilder.Entity<Client>()
            .Property(e => e.FullName).HasMaxLength(_stringLengthOptions.ClientFullNameMaxLength);
        modelBuilder.Entity<Client>()
            .Property(e => e.PhoneNumber).HasMaxLength(_stringLengthOptions.PhoneNumberMaxLength);
        modelBuilder.Entity<Client>()
            .HasIndex(e => e.PhoneNumber).IsUnique();
        modelBuilder.Entity<Client>()
            .Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");

        modelBuilder.Entity<ProductType>()
            .Property(e => e.Name).HasMaxLength(_stringLengthOptions.ProductTypeNameMaxLength);
        modelBuilder.Entity<ProductType>()
            .HasIndex(e => e.Name).IsUnique();

        modelBuilder.Entity<Product>()
            .Property(e => e.Name).HasMaxLength(_stringLengthOptions.ProductNameMaxLength);
     
        modelBuilder.Entity<Product>()
            .Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
    }
}

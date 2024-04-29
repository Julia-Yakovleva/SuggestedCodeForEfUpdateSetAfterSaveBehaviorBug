using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace IssueConsoleTemplate;

public class Store
{
    public int StoreId { get; set; }

    public string Name { get; set; }
    public string CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }

    public List<Item> Items { get; set; }
}

public class Item
{
    public string ItemCode { get; set; }
    public int StoreId { get; set; }

    public string Name { get; set; }
    public string CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}

public class Context : DbContext
{
    public DbSet<Store> Stores { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = "server=127.0.0.1;port=3306;user=root;password=;Database=Issue1908";
            var serverVersion = ServerVersion.AutoDetect(connectionString);

            optionsBuilder
                .UseMySql(connectionString, serverVersion)
                // .UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Integrated Security=True;Initial Catalog=Issue1908")
                // .UseNpgsql(@"server=127.0.0.1;port=5432;user id=postgres;password=postgres;database=Issue1908")
                // .UseSqlite(@"Data Source=Issue1908.db")
                .LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Store>(
            entity =>
            {
                entity.HasKey(e => e.StoreId);

                entity.Property(e => e.StoreId);
                entity.Property(e => e.Name);
                entity.Property(e => e.CreatedAt)
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore); // Never generate `UPDATE` for CreatedAt
                entity.Property(e => e.UpdatedAt)
                    .Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);  // Never generate `INSERT` for UpdatedAt

                entity.HasMany(e => e.Items)
                    .WithOne()
                    .HasForeignKey(e => e.StoreId);

                entity.Navigation(e => e.Items)
                    .AutoInclude();
            });

        modelBuilder.Entity<Item>(
            entity =>
            {
                entity.HasKey(e => new { e.StoreId, e.ItemCode });

                entity.Property(item => item.ItemCode);
                entity.Property(item => item.Name);
                entity.Property(e => e.CreatedAt)
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore); // Never generate `UPDATE` for CreatedAt
                entity.Property(e => e.UpdatedAt)
                    .Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);  // Never generate `INSERT` for UpdatedAt

                entity.HasOne<Store>()
                    .WithMany(store => store.Items)
                    .HasForeignKey(e => e.StoreId);
            });
    }
}

internal static class Program
{
    private static void Main()
    {
        var lastChangeTrackerDebugView = string.Empty;

        using (var context = new Context())
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            context.Stores.Add(
                new Store
                {
                    Name = "Books",
                    CreatedAt = "2023-01-01",
                    UpdatedAt = "2023-01-01",
                    Items =
                    [
                        new Item
                        {
                            ItemCode = "lotr",
                            StoreId = 1,
                            Name = "The Fellowship of the Ring",
                            CreatedAt = "2023-01-01",
                            UpdatedAt = "2023-01-01",
                        }
                    ]
                });

            context.ChangeTracker.DetectChanges();
            lastChangeTrackerDebugView = context.ChangeTracker.DebugView.LongView.Trim().ReplaceLineEndings();

            context.SaveChanges();
        }

        using (var context = new Context())
        {
            var store = context.Stores.Single();

            Trace.Assert(store.StoreId == 1);
            Trace.Assert(store.Name == "Books");
            Trace.Assert(store.CreatedAt == "2023-01-01");
            Trace.Assert(store.UpdatedAt is null);
            Trace.Assert(store.Items is not null);
            Trace.Assert(store.Items.Count == 1);
            Trace.Assert(store.Items[0].StoreId == 1);
            Trace.Assert(store.Items[0].ItemCode == "lotr");
            Trace.Assert(store.CreatedAt == "2023-01-01");
            Trace.Assert(store.UpdatedAt is null);
            Trace.Assert(store.Items[0].Name == "The Fellowship of the Ring");

            Trace.Assert(
                Regex.Replace(lastChangeTrackerDebugView, @"^\s+", string.Empty, RegexOptions.Multiline) ==
                Regex.Replace(
"""
Item {StoreId: -2147482647, ItemCode: lotr} Added
    StoreId: -2147482647 PK FK Temporary
    ItemCode: 'lotr' PK
    CreatedAt: '2023-01-01'
    Name: 'The Fellowship of the Ring'
    UpdatedAt: '2023-01-01'
Store {StoreId: -2147482647} Added
    StoreId: -2147482647 PK Temporary
    CreatedAt: '2023-01-01'
    Name: 'Books'
    UpdatedAt: '2023-01-01'
    Items: [{StoreId: -2147482647, ItemCode: lotr}]
""", @"^\s+", string.Empty, RegexOptions.Multiline));
        }

        using (var context = new Context())
        {
            var store = context.Stores.Single();

            store.Name = "New Books";
            store.CreatedAt = "2024-02-02";
            store.UpdatedAt = "2024-02-02";
            store.Items =
            [
                new Item
                {
                    ItemCode = "lotr",
                    StoreId = 1,
                    Name = "The Two Towers",
                    CreatedAt = "2024-02-02",
                    UpdatedAt = "2024-02-02",
                }
            ];

            context.ChangeTracker.DetectChanges();
            lastChangeTrackerDebugView = context.ChangeTracker.DebugView.LongView.Trim().ReplaceLineEndings();

            context.SaveChanges();
        }

        using (var context = new Context())
        {
            var store = context.Stores.Single();

            Trace.Assert(store.StoreId == 1);
            Trace.Assert(store.Name == "New Books");
            Trace.Assert(store.CreatedAt == "2023-01-01");
            Trace.Assert(store.UpdatedAt == "2024-02-02");
            Trace.Assert(store.Items is not null);
            Trace.Assert(store.Items.Count == 1);
            Trace.Assert(store.Items[0].StoreId == 1);
            Trace.Assert(store.Items[0].ItemCode == "lotr");
            Trace.Assert(store.Items[0].Name == "The Two Towers");

            // I would consider the following unexpected. Either this is handled as a Delete+Insert operation pair,
            // in which case `CreatedAt` should contain `2024-02-02` and `UpdatedAt` should be `null`, or this is handled
            // as an Update operation (which it seems to be, since an `UPDATE` statement is generated), in which case
            // `CreatedAt` should contain the unchanged value of `2023-01-01` and `UpdatedAt` should be `2024-02-02`.
            Trace.Assert(store.Items[0].CreatedAt == "2024-02-02");
            Trace.Assert(store.Items[0].UpdatedAt == "2024-02-02");

            // Trace.Assert(store.Items[0].CreatedAt == "2024-02-02" && store.Items[0].UpdatedAt is null ||
            //              store.Items[0].CreatedAt == "2023-01-01" && store.Items[0].UpdatedAt == "2024-02-02");

            Trace.Assert(
                Regex.Replace(lastChangeTrackerDebugView, @"^\s+", string.Empty, RegexOptions.Multiline) == 
                Regex.Replace(
"""
Item (Shared) {StoreId: 1, ItemCode: lotr} Added
    StoreId: 1 PK FK
    ItemCode: 'lotr' PK
    CreatedAt: '2024-02-02'
    Name: 'The Two Towers'
    UpdatedAt: '2024-02-02'
Item (Shared) {StoreId: 1, ItemCode: lotr} Deleted
    StoreId: 1 PK FK
    ItemCode: 'lotr' PK
    CreatedAt: '2023-01-01'
    Name: 'The Fellowship of the Ring'
    UpdatedAt: <null>
Store {StoreId: 1} Modified
    StoreId: 1 PK
    CreatedAt: '2024-02-02' Modified Originally '2023-01-01'
    Name: 'New Books' Modified Originally 'Books'
    UpdatedAt: '2024-02-02' Modified Originally <null>
    Items: [{StoreId: 1, ItemCode: lotr}]
""", @"^\s+", string.Empty, RegexOptions.Multiline));
        }
    }
}

using CrudElite.Models;
using Microsoft.EntityFrameworkCore;

namespace CrudElite.Data;

public class DefaultDBContext : DbContext
{
    public DefaultDBContext(DbContextOptions<DefaultDBContext> options)
        : base(options) { }

    public DbSet<Client> Clients { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.RemovePluralizingTableNameConvention();
        base.OnModelCreating(builder);
    }
}

public static class ModelBuilderExtensions
{
    public static void RemovePluralizingTableNameConvention(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            entity.SetTableName(entity.DisplayName());
    }
}
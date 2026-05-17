using InventoryApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        //  DbSet ------> Table
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<InventoryField> InventoryFields { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemFieldValue> ItemFieldValues { get; set; }
        public DbSet<InventoryAccess> InventoryAccesses { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<InventoryTag> InventoryTags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Many-to-Many Keys
            builder.Entity<InventoryTag>()
                .HasKey(it => new { it.InventoryId, it.TagId });

            builder.Entity<Like>()
                .HasKey(l => new { l.ItemId, l.UserId });

            // Unique CustomId per Inventory
            builder.Entity<Item>()
                .HasIndex(i => new { i.InventoryId, i.CustomId })
                .IsUnique();

            // Optimistic Locking
            builder.Entity<Inventory>()
                .Property(i => i.Version)
                .IsConcurrencyToken();

            builder.Entity<Item>()
                .Property(i => i.Version)
                .IsConcurrencyToken();

            //  NoAction Cascade fix
            builder.Entity<Comment>()
                .HasOne(c => c.Inventory)
                .WithMany(i => i.Comments)
                .HasForeignKey(c => c.InventoryId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Like>()
                .HasOne(l => l.Item)
                .WithMany(i => i.Likes)
                .HasForeignKey(l => l.ItemId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<InventoryAccess>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Item>()
                .HasOne(i => i.CreatedBy)
                .WithMany()
                .HasForeignKey(i => i.CreatedById)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ItemFieldValue>()
                .HasOne(v => v.Item)
                .WithMany(i => i.FieldValues)
                .HasForeignKey(v => v.ItemId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ItemFieldValue>()
                .HasOne(v => v.Field)
                .WithMany(f => f.Values)
                .HasForeignKey(v => v.FieldId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
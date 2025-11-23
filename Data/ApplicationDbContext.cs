using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DebtSnowballApp.Models;

namespace DebtSnowballApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<DebtItem> DebtItems { get; set; } = default!;
        public DbSet<Partner> Partners { get; set; } = default!;
        public DbSet<QaDebtItem> QaDebtItems { get; set; } = default!;
        public DbSet<DebtType> DebtTypes { get; set; } = default!;
        public DbSet<QuickAnalysisPersonal> QuickAnalysisPersonals { get; set; } = default!;

        // 🧭 Relationship and model configuration
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // <-- keep Identity setup intact

            modelBuilder.Entity<Partner>()
                .HasIndex(p => p.Code)
                .IsUnique();

            // === ApplicationUser → Partner (optional) ===
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Partner)
                .WithMany(p => p.Users)
                .HasForeignKey(u => u.PartnerId)
                .OnDelete(DeleteBehavior.Restrict);
                //.OnDelete(DeleteBehavior.SetNull); // if partner deleted, keep user

            // === DebtItem → User (required) ===
            modelBuilder.Entity<DebtItem>()
                .HasOne(d => d.User)
                .WithMany(u => u.DebtItems)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade); // delete debts when user deleted

            // === Index for fast lookups ===
            modelBuilder.Entity<DebtItem>()
                .HasIndex(d => new { d.UserId, d.Name });

            // === DebtType relationships (optional, but good practice) ===
            modelBuilder.Entity<DebtItem>()
                .HasOne(d => d.DebtType)
                .WithMany()
                .HasForeignKey(d => d.DebtTypeId)
                .OnDelete(DeleteBehavior.Restrict); // keep DebtTypes safe from deletion
        }

        // === Audit timestamps ===
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            foreach (var e in ChangeTracker.Entries<Partner>())
            {
                if (e.State == EntityState.Modified)
                    e.Entity.UpdatedAt = utcNow;
                if (e.State == EntityState.Added && e.Entity.CreatedAt == default)
                    e.Entity.CreatedAt = utcNow;
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}

using McAttributes.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace McAttributes.Data {
    public class IdDbContext : DbContext {
        public IdDbContext(DbContextOptions<IdDbContext> options)
            : base(options) { }

        public DbSet<User>? Users { get; set; }

        public DbSet<Stargate>? Stargate { get; set; }

        public DbSet<AlertLogEntry> AlertLogEntry { get; set; } = default!;

        public DbSet<AlertApproval> AlertApprovals { get; set; } = default!;

        public DbSet<CohortDescription> CohortDescriptions { get; set; } = default!;

        public DbSet<CohortDescriptionMember> CohortMember { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder) {
            builder.Entity<AlertLogEntry>(entity => {
                entity.Property(p => p.Created).HasDefaultValue(DateTime.UtcNow)
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
            });

            // AadId should be unique
            builder.Entity<AlertLogEntry>()
                .HasIndex(u => u.AlertHash)
                .IsUnique();

            // AadId should be unique
            builder.Entity<User>()
                .HasIndex(u => u.AadId)
                .IsUnique();

            // Index Mail and EmployeeId
            var userBuilder = builder.Entity<User>();
            userBuilder.HasIndex(u => u.Mail);
            userBuilder.HasIndex(u => u.EmployeeId);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
            foreach (var entry in ChangeTracker.Entries<RowVersionedModel>()) {
                var prop = entry.Property(nameof(RowVersionedModel.Version));
                prop.OriginalValue = prop.CurrentValue;
            }
            return base.SaveChangesAsync(cancellationToken);
        }


    }
}

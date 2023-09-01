using McAttributes.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace McAttributes.Data {
    public class IdDbContext : DbContext {
        public IdDbContext(DbContextOptions<IdDbContext> options)
            : base(options) { }

        public DbSet<User>? Users { get; set; }

        public DbSet<EmployeeIdRecord>? EmployeeIds { get; set; }

        public DbSet<Stargate>? Stargate { get; set; }


        protected override void OnModelCreating(ModelBuilder builder) {
            builder.Entity<IssueLogEntry>(entity => {
                entity.Property(p => p.Created).HasDefaultValue(DateTime.UtcNow)
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
            });

            builder.Entity<EmployeeIdRecord>();

            // AadId should be unique
            builder.Entity<IssueLogEntry>()
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


        public DbSet<McAttributes.Models.IssueLogEntry> IssueLogEntry { get; set; } = default!;
    }
}

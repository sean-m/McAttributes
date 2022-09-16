using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using McAttributes.Models;

namespace McAttributes.Data
{
    public class IssueLogContext : DbContext
    {
        public IssueLogContext (DbContextOptions<IssueLogContext> options)
            : base(options)
        {
        }

        public DbSet<IssueLogEntry> IssueLogEntry { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<IssueLogEntry>(entity =>
            {
                entity.Property(p => p.Created).HasDefaultValue(DateTime.UtcNow)
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
            });
        }
    }
}

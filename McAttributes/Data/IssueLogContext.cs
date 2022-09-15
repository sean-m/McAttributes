using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
    }
}

using McAttributes.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace McAttributes.Data
{
    public class IdDbContext : DbContext
    {
        public IdDbContext(DbContextOptions<IdDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<EmployeeIdRecord> EmployeeIds { get; set; }
    }
}

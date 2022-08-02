using McAttributes.Models;
using Microsoft.EntityFrameworkCore;

namespace McAttributes {
    public class IdDbContext : DbContext {
        public IdDbContext(DbContextOptions<IdDbContext> options)
            : base(options) {  }

        public DbSet<User> Users { get; set; }
    }
}

using Microsoft.EntityFrameworkCore;
using AYU.Models;

namespace AYU.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<BankTransaction> Transactions { get; set; }
    }
}
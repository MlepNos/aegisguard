using AegisGuard.Domain;
using Microsoft.EntityFrameworkCore;

namespace AegisGuard.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SecurityLog> SecurityLogs => Set<SecurityLog>();
}

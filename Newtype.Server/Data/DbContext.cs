using Microsoft.EntityFrameworkCore;

using Newtype.Server.Models;
namespace Newtype.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}
    public DbSet<SubmissionLog> SubmissionLogs { get; set; }
}

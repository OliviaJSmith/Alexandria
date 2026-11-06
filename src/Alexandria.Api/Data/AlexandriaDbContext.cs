using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.Api.Data;

public class AlexandriaDbContext(DbContextOptions<AlexandriaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Library> Libraries => Set<Library>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<LibraryBook> LibraryBooks => Set<LibraryBook>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<Friendship> Friendships => Set<Friendship>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlexandriaDbContext).Assembly);
    }
}

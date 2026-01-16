using Alexandria.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.API.Data;

public class AlexandriaDbContext : DbContext
{
    public AlexandriaDbContext(DbContextOptions<AlexandriaDbContext> options) 
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Library> Libraries { get; set; }
    public DbSet<LibraryBook> LibraryBooks { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<Friendship> Friendships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GoogleId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Book configuration
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Isbn);
        });

        // Library configuration
        modelBuilder.Entity<Library>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Libraries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LibraryBook configuration
        modelBuilder.Entity<LibraryBook>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Library)
                .WithMany(l => l.LibraryBooks)
                .HasForeignKey(e => e.LibraryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Book)
                .WithMany(b => b.LibraryBooks)
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Loan configuration
        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.LibraryBook)
                .WithMany(lb => lb.Loans)
                .HasForeignKey(e => e.LibraryBookId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Lender)
                .WithMany(u => u.LoansGiven)
                .HasForeignKey(e => e.LenderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Borrower)
                .WithMany(u => u.LoansReceived)
                .HasForeignKey(e => e.BorrowerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Friendship configuration
        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Requester)
                .WithMany(u => u.FriendshipsInitiated)
                .HasForeignKey(e => e.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Addressee)
                .WithMany(u => u.FriendshipsReceived)
                .HasForeignKey(e => e.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

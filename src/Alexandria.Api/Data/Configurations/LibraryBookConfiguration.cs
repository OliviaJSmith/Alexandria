using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alexandria.Api.Data.Configurations;

public class LibraryBookConfiguration : IEntityTypeConfiguration<LibraryBook>
{
    public void Configure(EntityTypeBuilder<LibraryBook> builder)
    {
        builder.Property(lb => lb.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(lb => lb.LoanNote).HasMaxLength(500);

        builder.HasOne(lb => lb.Library)
            .WithMany(l => l.LibraryBooks)
            .HasForeignKey(lb => lb.LibraryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lb => lb.Book)
            .WithMany(b => b.LibraryBooks)
            .HasForeignKey(lb => lb.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

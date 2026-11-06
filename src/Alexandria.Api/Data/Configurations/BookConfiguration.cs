using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alexandria.Api.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.Property(b => b.Title).HasMaxLength(500);
        builder.Property(b => b.Author).HasMaxLength(500);
        builder.Property(b => b.Isbn).HasMaxLength(13);
        builder.Property(b => b.Publisher).HasMaxLength(300);
        builder.Property(b => b.Description).HasMaxLength(10000);
        builder.Property(b => b.CoverImageUrl).HasMaxLength(2048);
        builder.Property(b => b.Genre).HasMaxLength(100);

        builder.HasIndex(b => b.Isbn);
    }
}

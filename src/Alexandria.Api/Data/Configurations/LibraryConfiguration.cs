using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alexandria.Api.Data.Configurations;

public class LibraryConfiguration : IEntityTypeConfiguration<Library>
{
    public void Configure(EntityTypeBuilder<Library> builder)
    {
        builder.Property(l => l.Name).HasMaxLength(200);

        builder.HasOne(l => l.Owner)
            .WithMany(u => u.Libraries)
            .HasForeignKey(l => l.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

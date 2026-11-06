using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alexandria.Api.Data.Configurations;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(f => f.Requester)
            .WithMany()
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Addressee)
            .WithMany()
            .HasForeignKey(f => f.AddresseeId)
            .OnDelete(DeleteBehavior.Cascade);

        // One friendship row per ordered pair; services check the reverse pair before creating.
        builder.HasIndex(f => new { f.RequesterId, f.AddresseeId }).IsUnique();
        builder.HasIndex(f => f.AddresseeId);
    }
}

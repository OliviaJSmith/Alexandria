using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alexandria.Api.Data.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public const int EndpointMaxLength = 1000;
    public const int KeyMaxLength = 200;

    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.Property(s => s.Endpoint).HasMaxLength(EndpointMaxLength);
        builder.Property(s => s.P256dh).HasMaxLength(KeyMaxLength);
        builder.Property(s => s.Auth).HasMaxLength(KeyMaxLength);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.Endpoint).IsUnique();
        builder.HasIndex(s => s.UserId);
    }
}

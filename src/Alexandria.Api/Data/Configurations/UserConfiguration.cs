using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alexandria.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.GoogleSubjectId).HasMaxLength(255);
        builder.Property(u => u.Email).HasMaxLength(320);
        builder.Property(u => u.DisplayName).HasMaxLength(200);
        builder.Property(u => u.UserName).HasMaxLength(50);
        builder.Property(u => u.NormalizedUserName).HasMaxLength(50);
        builder.Property(u => u.ProfilePictureUrl).HasMaxLength(2048);

        builder.HasIndex(u => u.GoogleSubjectId).IsUnique();
        builder.HasIndex(u => u.NormalizedUserName).IsUnique();
    }
}

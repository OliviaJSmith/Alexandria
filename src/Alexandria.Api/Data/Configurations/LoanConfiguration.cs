using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alexandria.Api.Data.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(l => l.LibraryBook)
            .WithMany(lb => lb.Loans)
            .HasForeignKey(l => l.LibraryBookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Lender)
            .WithMany()
            .HasForeignKey(l => l.LenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Borrower)
            .WithMany()
            .HasForeignKey(l => l.BorrowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.LenderId, l.Status });
        builder.HasIndex(l => new { l.BorrowerId, l.Status });
        builder.HasIndex(l => new { l.Status, l.DueDate });
    }
}

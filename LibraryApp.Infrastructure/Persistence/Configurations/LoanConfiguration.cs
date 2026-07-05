using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibraryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryApp.Infrastructure.Persistence.Configurations
{
    public class LoanConfiguration : IEntityTypeConfiguration<Loan>
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            builder.HasKey(l => l.Id);

            builder
                .OwnsOne(l => l.Period, period =>
                {
                    period.Property(p => p.BorrowedAt)
                    .HasColumnName("BorrowedAt")
                    .IsRequired();

                    period.Property(p => p.ExpectedReturnDate)
                    .HasColumnName("ExpectedReturnDate")
                    .IsRequired();
                }
               );

            builder.OwnsOne(l => l.Fee, fee =>
            {
                fee.Property(m => m.Amount)
                    .HasColumnName("Price")
                    .HasPrecision(18, 2)
                    .IsRequired();

                fee.Property(m => m.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(10)
                    .IsRequired();
            }
            );

            builder
                .HasOne<Book>()
                .WithMany()
                .HasForeignKey(l => l.BookId);

            builder.
                HasOne<Member>()
                .WithMany()
                .HasForeignKey(l => l.MemberId);

            builder.Ignore(b => b.DomainEvents);
            builder.HasQueryFilter(e => !e.IsDeleted);
        }
    }
}

using LibraryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryApp.Infrastructure.Persistence.Configurations
{
    public class BookConfiguration : IEntityTypeConfiguration<Book>
    {
        public void Configure(EntityTypeBuilder<Book> builder)
        {
            builder.
                HasKey(b => b.Id);

            builder.OwnsOne(b => b.Isbn, isbn =>
            {
                isbn.Property(i => i.Value)
                    .HasColumnName("Isbn")
                    .IsRequired();
            }
            );

            builder.OwnsOne(b => b.Money, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("Price")
                    .HasPrecision(18, 2)
                    .IsRequired();

                money.Property(m => m.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(10)
                    .IsRequired();
            }
            );

            builder
                .HasOne<Author>()
                .WithMany()
                .HasForeignKey(b => b.AuthorId);

            builder.Ignore(b => b.DomainEvents);
            builder.HasQueryFilter(e => !e.IsDeleted);
        }
    }
}

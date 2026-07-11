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
    public class MemberConfiguration : IEntityTypeConfiguration<Member>
    {
        public void Configure(EntityTypeBuilder<Member> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(m => m.Surname)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(m => m.Email)
                .HasMaxLength(256)
                .IsRequired();

            builder.HasIndex(m => m.Email)
                .IsUnique();

            builder.HasQueryFilter(e => !e.IsDeleted);
        }
    }
}

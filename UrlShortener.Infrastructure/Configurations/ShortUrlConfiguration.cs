using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShortener.Domain.Constants;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Configurations
{
    public class ShortUrlConfiguration : IEntityTypeConfiguration<ShortUrl>
    {
        public void Configure(EntityTypeBuilder<ShortUrl> builder)
        {
            builder.ToTable("ShortUrls");

            builder.HasKey(x => x.Id);

            // Columns constraints
            builder.Property(x => x.ShortCode)
               .IsRequired(false)
               .HasMaxLength(ShortUrlConstants.ShortCodeMaxLength);

            builder.Property(x => x.OriginalUrl)
                .IsRequired()
                .HasMaxLength(ShortUrlConstants.OriginalUrlMaxLength);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.ExpiresAt)
                .IsRequired(false);

            // Indexing
            builder.HasIndex(x => x.ShortCode)
                .IsUnique()
                .HasDatabaseName("uq_shorturls_shortcode");

            builder.HasIndex(x => x.ExpiresAt)
                .HasDatabaseName("idx_shorturls_expiresat");
        }
    }
}

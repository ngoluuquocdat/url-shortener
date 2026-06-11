using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl.DTOs
{
    public class ShortUrlDTO
    {
        public long Id { get; set; }
        public required string ShortCode { get; set; }
        public required string OriginalUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}

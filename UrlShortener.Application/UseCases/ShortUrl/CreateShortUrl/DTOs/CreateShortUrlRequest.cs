using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl.DTOs
{
    public class CreateShortUrlRequest
    {
        [Required]
        public required string OriginalUrl { get; set; }
        public string? CustomShortCode { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}



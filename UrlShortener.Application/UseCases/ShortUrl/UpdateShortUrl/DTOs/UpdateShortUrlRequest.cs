namespace UrlShortener.Application.UseCases.ShortUrl.UpdateShortUrl.DTOs
{
    public class UpdateShortUrlRequest
    {
        public string? OriginalUrl { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}

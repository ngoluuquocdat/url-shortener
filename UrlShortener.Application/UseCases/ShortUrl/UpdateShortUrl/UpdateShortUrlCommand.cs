using MediatR;
using UrlShortener.Application.Common;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl.DTOs;

namespace UrlShortener.Application.UseCases.ShortUrl.UpdateShortUrl
{
    public record UpdateShortUrlCommand(
        long Id,
        string? OriginalUrl,
        DateTimeOffset? ExpiresAt) : IRequest<Result<ShortUrlDTO>>;
}

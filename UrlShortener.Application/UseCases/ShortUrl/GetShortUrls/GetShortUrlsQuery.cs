using MediatR;
using UrlShortener.Application.Common;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl.DTOs;

namespace UrlShortener.Application.UseCases.ShortUrl.GetShortUrls
{
    public record GetShortUrlsQuery(int Page, int PageSize) : IRequest<Result<PagedResult<ShortUrlDTO>>>;
}

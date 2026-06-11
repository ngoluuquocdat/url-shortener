using MediatR;
using UrlShortener.Application.Common;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl.DTOs;

namespace UrlShortener.Application.UseCases.ShortUrl.GetShortUrls
{
    public class GetShortUrlsQueryHandler(
        IShortUrlRepository shortUrlRepo) : IRequestHandler<GetShortUrlsQuery, Result<PagedResult<ShortUrlDTO>>>
    {
        public async Task<Result<PagedResult<ShortUrlDTO>>> Handle(GetShortUrlsQuery query, CancellationToken ct)
        {
            var (items, totalCount) = await shortUrlRepo.GetPagedAsync(query.Page, query.PageSize, ct);

            var dtos = items.Select(x => new ShortUrlDTO
            {
                Id = x.Id,
                ShortCode = x.ShortCode,
                OriginalUrl = x.OriginalUrl,
                CreatedAt = x.CreatedAt,
                ExpiresAt = x.ExpiresAt
            }).ToList();

            var pagedResult = new PagedResult<ShortUrlDTO>
            {
                Items = dtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };

            return Result<PagedResult<ShortUrlDTO>>.Success(pagedResult);
        }
    }
}

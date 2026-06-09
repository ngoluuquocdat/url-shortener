using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShortener.Application.Common;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl.DTOs;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Application.UseCases.ShortUrl.Redirect
{
    public class RedirectShortUrlHandler(
        IShortUrlRepository shortUrlRepo) : IRequestHandler<RedirectShortUrlQuery, Result<string>>
    {
        public async Task<Result<string>> Handle(RedirectShortUrlQuery query, CancellationToken ct)
        {
            try
            {
                Domain.Entities.ShortUrl.ValidateShortCode(query.ShortCode);

                var shortUrl = await shortUrlRepo.GetByShortCodeAsync(query.ShortCode, ct);

                if (shortUrl == null)
                {
                    return Result<string>.NotFound("ShortUrl not found");
                }

                if (shortUrl.IsExpired(DateTimeOffset.UtcNow))
                {
                    return Result<string>.Gone("ShortUrl expired");
                }

                return Result<string>.Success(shortUrl.OriginalUrl);
            }
            catch (DomainValidationException domainValidationEx)
            {
                return Result<string>.ValidationFailed(domainValidationEx.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UrlShortener.Application.Common;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl.DTOs;

namespace UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl
{
    public record CreateShortUrlCommand(
        string OriginalUrl,
        string? CustomCode,
        DateTimeOffset? ExpiresAt) : IRequest<Result<ShortUrlDTO>>;
}

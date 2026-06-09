using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShortener.Application.Common;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl.DTOs;

namespace UrlShortener.Application.UseCases.ShortUrl.Redirect
{
    public record RedirectShortUrlQuery(string ShortCode) : IRequest<Result<string>>;
}

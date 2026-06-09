using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl.DTOs;
using UrlShortener.Application.UseCases.ShortUrl.Redirect;

namespace UrlShortener.Api.Controllers
{
    [Route("api/short-urls")]
    public class ShortUrlsController : WebApiControllerBase
    {
        private readonly IMediator _mediator;

        public ShortUrlsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateShortUrlRequest request, CancellationToken ct)
        {
            var command = new CreateShortUrlCommand(
                request.OriginalUrl, request.CustomShortCode, request.ExpiresAt);

            var result = await _mediator.Send(command, ct);

            if (result.IsSuccess)
            {
                return Created(String.Empty, result.Value);
            }
            else
            {
                return CreateFailureResponse(result);
            }
        }
    }
}

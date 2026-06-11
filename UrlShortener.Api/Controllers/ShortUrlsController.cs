using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl.DTOs;
using UrlShortener.Application.UseCases.ShortUrl.GetShortUrls;
using UrlShortener.Application.UseCases.ShortUrl.Redirect;
using UrlShortener.Application.UseCases.ShortUrl.UpdateShortUrl;
using UrlShortener.Application.UseCases.ShortUrl.UpdateShortUrl.DTOs;

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

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var query = new GetShortUrlsQuery(page, pageSize);
            var result = await _mediator.Send(query, ct);

            return Ok(result.Value);
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult> Update(long id, [FromBody] UpdateShortUrlRequest request, CancellationToken ct)
        {
            var command = new UpdateShortUrlCommand(id, request.OriginalUrl, request.ExpiresAt);
            var result = await _mediator.Send(command, ct);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return CreateFailureResponse(result);
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

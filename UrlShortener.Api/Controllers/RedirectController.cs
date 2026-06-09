using MediatR;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.UseCases.ShortUrl.Redirect;

namespace UrlShortener.Api.Controllers
{
    [Route("/")]
    public class RedirectController : WebApiControllerBase
    {
        private readonly IMediator _mediator;

        public RedirectController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{shortCode:shortCodeRedirectConstraint}")]
        public async Task<ActionResult> Redirect([FromRoute] string shortCode, CancellationToken ct)
        {
            var query = new RedirectShortUrlQuery(shortCode);

            var result = await _mediator.Send(query, ct);

            if (result.IsSuccess)
            {
                return Redirect(result.Value);
            }
            else
            {
                return CreateFailureResponse(result);
            }
        }
    }
}

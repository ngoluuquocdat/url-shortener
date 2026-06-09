using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Common;

namespace UrlShortener.Api.Controllers
{
    [ApiController]
    public class WebApiControllerBase : ControllerBase
    {
        public ActionResult CreateFailureResponse<T>(Result<T> result)
        {
            switch (result.Status)
            {
                case ResultStatus.ValidationFailed:
                    return BadRequest(result.Error);
                case ResultStatus.Conflict:
                    return Conflict(result.Error);
                case ResultStatus.NotFound:
                    return NotFound(result.Error);
                case ResultStatus.Gone:
                    return StatusCode(410);
                case ResultStatus.UnexpectedFailure:
                default:
                    return StatusCode(500);
            }
        }
    }
}

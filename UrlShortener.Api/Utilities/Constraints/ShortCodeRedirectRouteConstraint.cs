
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Api.Utilities.Constraints
{
    public class ShortCodeRedirectRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (!values.TryGetValue(routeKey, out var value))
            {
                return false;
            }

            if (value is not string str)
            {
                return false;
            }

            try
            {
                Domain.Entities.ShortUrl.ValidateShortCode(str);
                return true;
            }
            catch (DomainValidationException)
            {
                return false;
            }
        }
    }
}

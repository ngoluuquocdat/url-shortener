using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UrlShortener.Domain.Constants
{
    public static class ShortUrlConstants
    {
        public const int ShortCodeMaxLength = 64;

        public const int OriginalUrlMaxLength = 2048;

        public static readonly Regex ValidShortCodeRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        public static readonly HashSet<string> ReservedRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "swagger",
            "health",
            "api",
            "metrics",
            "favicon.ico"
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UrlShortener.Application.Exceptions
{
    public class ShortCodeConflictException : Exception
    {
        // Simple message only (no field context)
        public ShortCodeConflictException(string message)
            : base(message) { }
    }
}

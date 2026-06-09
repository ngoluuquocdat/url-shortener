using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShortener.Domain.Exceptions
{
    public class DomainValidationException : Exception
    {
        public string ErrorCode { get; private set; }
        public string Field { get; private set; }

        public DomainValidationException(string errCode, string field, string message)
            : base(message) 
        {
            ErrorCode = errCode;
            Field = field;
        }
    }
}

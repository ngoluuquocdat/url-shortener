using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShortener.Application.Utilities
{
    public static class Base62
    {
        private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private const int Base = 62;

        public static string Encode(long number)
        {
            if (number == 0) return "0";
            if (number < 0) throw new ArgumentOutOfRangeException(nameof(number), "Must be non-negative.");

            var result = new StringBuilder();

            while (number > 0)
            {
                result.Insert(0, Alphabet[(int)(number % Base)]);
                number /= Base;
            }

            return result.ToString();
        }
    }
}

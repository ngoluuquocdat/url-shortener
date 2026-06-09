using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShortener.Domain.Constants;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Domain.Entities
{
    public class ShortUrl
    {
        public long Id { get; set; }
        public string? ShortCode  { get; private set; }
        public string OriginalUrl { get; private set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; private set; }

        private ShortUrl() { }

        public bool IsExpired(DateTimeOffset now)
        {
            return ExpiresAt is not null
                && ExpiresAt <= now;
        }

        public void SetExpiration(DateTimeOffset? expiresAt)
        {
            string propName = nameof(ExpiresAt);
            if (expiresAt.HasValue && expiresAt < DateTimeOffset.UtcNow)
            {
                throw new DomainValidationException(
                    "InvalidShortUrl",
                    propName,
                    $"{propName} must be greater than now");
            }
            ExpiresAt = expiresAt;
        }

        public void SetOriginalUrl(string originalUrl)
        {
            ValidateOriginalUrl(originalUrl);
            OriginalUrl = originalUrl;
        }

        public void UpdateDestination(string newUrl)
        {
            if (IsExpired(DateTimeOffset.UtcNow))
            {
                throw new ShortUrlExpiredException();
            }

            OriginalUrl = newUrl;
        }

        public void AssignShortCode(string shortCode)
        {
            if (!string.IsNullOrWhiteSpace(ShortCode))
            {
                throw new DomainValidationException(
                    "InvalidShortUrl",
                    nameof(ShortCode),
                    $"{nameof(ShortCode)} already assigned");
            }

            ValidateShortCode(shortCode);

            ShortCode = shortCode;
        }

        public static void ValidateShortCode(string shortCode)
        {
            string propName = nameof(ShortCode);
            if (string.IsNullOrWhiteSpace(shortCode))
            {
                throw new DomainValidationException(
                    "InvalidShortUrl",
                    propName,
                    $"{propName} must not be empty");
            }
            if (shortCode.Length > ShortUrlConstants.ShortCodeMaxLength)
            {
                throw new DomainValidationException(
                    "InvalidShortUrl",
                    propName,
                    $"{propName} is too long");
            }
            if (!ShortUrlConstants.ValidShortCodeRegex.IsMatch(shortCode))
            {
                throw new DomainValidationException(
                    "InvalidShortUrl",
                    propName,
                    $"{propName} contains invalid characters");
            }
            if (ShortUrlConstants.ReservedRoutes.Contains(shortCode))
            {
                throw new DomainValidationException(
                    "InvalidShortUrl",
                    propName,
                    $"{propName} conflicts with reserved routes");
            }
        }

        private void ValidateOriginalUrl(string originalUrl)
        {
            string propName = nameof(OriginalUrl);
            if (String.IsNullOrEmpty(originalUrl))
            {
                throw new DomainValidationException(
                    "InvalidShortUrl",
                    propName,
                    $"{propName} must not be empty");
            }

            bool isValid = Uri.TryCreate(
                            originalUrl,
                            UriKind.Absolute,
                            out _);
            if (!isValid)
            {
                throw new DomainValidationException(
                    "InvalidShortUrl",
                    propName,
                    $"{propName} must be a valid URL");
            }
        }

        public static ShortUrl Create(string originalUrl, DateTimeOffset? expiresAt)
        {
            var shortUrl = new ShortUrl();
            shortUrl.SetOriginalUrl(originalUrl);
            shortUrl.SetExpiration(expiresAt);
            shortUrl.CreatedAt = DateTimeOffset.UtcNow;

            return shortUrl;
        }
    }
}

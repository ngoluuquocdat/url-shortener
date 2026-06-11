using MediatR;
using UrlShortener.Application.Common;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl.DTOs;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Application.UseCases.ShortUrl.UpdateShortUrl
{
    public class UpdateShortUrlCommandHandler(
        IShortUrlRepository shortUrlRepo,
        IUnitOfWork unitOfWork) : IRequestHandler<UpdateShortUrlCommand, Result<ShortUrlDTO>>
    {
        public async Task<Result<ShortUrlDTO>> Handle(UpdateShortUrlCommand command, CancellationToken cancellationToken)
        {
            var shortUrl = await shortUrlRepo.GetByIdAsync(command.Id, cancellationToken);

            if (shortUrl is null)
            {
                return Result<ShortUrlDTO>.NotFound($"ShortUrl with id {command.Id} not found.");
            }

            try
            {
                if (command.OriginalUrl is not null)
                {
                    shortUrl.UpdateDestination(command.OriginalUrl);
                }

                if (command.ExpiresAt.HasValue)
                {
                    shortUrl.UpdateExpiration(command.ExpiresAt);
                }
            }
            catch (ShortUrlExpiredException)
            {
                return Result<ShortUrlDTO>.Gone("ShortUrl is expired and cannot be updated.");
            }
            catch (DomainValidationException ex)
            {
                return Result<ShortUrlDTO>.ValidationFailed(ex.Message);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ShortUrlDTO>.Success(new ShortUrlDTO
            {
                Id = shortUrl.Id,
                ShortCode = shortUrl.ShortCode!,
                OriginalUrl = shortUrl.OriginalUrl,
                CreatedAt = shortUrl.CreatedAt,
                ExpiresAt = shortUrl.ExpiresAt
            });
        }
    }
}

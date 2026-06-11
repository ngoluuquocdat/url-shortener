using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShortener.Application.Common;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl.DTOs;
using UrlShortener.Application.Utilities;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl
{
    public class CreateShortUrlCommandHandler(
        IUnitOfWork unitOfWork,
        IShortUrlRepository shortUrlRepo) : IRequestHandler<CreateShortUrlCommand, Result<ShortUrlDTO>>
    {
        public async Task<Result<ShortUrlDTO>> Handle(CreateShortUrlCommand command, CancellationToken cancellationToken)
        {
            Domain.Entities.ShortUrl newShortUrl;

            try
            {
                // Domain is validated by domain behaviors
                newShortUrl = Domain.Entities.ShortUrl.Create(
                    command.OriginalUrl, command.ExpiresAt);

                if (!String.IsNullOrEmpty(command.CustomCode))
                {
                    newShortUrl.AssignShortCode(command.CustomCode);
                }
            }
            catch (DomainValidationException domainValidationEx)
            {
                return Result<ShortUrlDTO>.ValidationFailed(domainValidationEx.Message);
            }

            // Start handling in a single transaction
            try
            {                
                await unitOfWork.BeginTransactionAsync();

                await shortUrlRepo.AddAsync(newShortUrl);
                await unitOfWork.SaveChangesAsync();

                if (String.IsNullOrEmpty(command.CustomCode))
                {
                    // Generate and assign default ShortCode
                    string shortCode = Base62.Encode(newShortUrl.Id);
                    newShortUrl.AssignShortCode(shortCode);
                    await unitOfWork.SaveChangesAsync();
                }

                await unitOfWork.CommitTransactionAsync();

                var shortUrlDTO = new ShortUrlDTO
                {
                    Id = newShortUrl.Id,
                    ShortCode = newShortUrl.ShortCode!,
                    OriginalUrl = newShortUrl.OriginalUrl,
                    CreatedAt = newShortUrl.CreatedAt,
                    ExpiresAt = newShortUrl.ExpiresAt
                };

                return Result<ShortUrlDTO>.Success(shortUrlDTO);
            }
            catch (ShortCodeConflictException shortCodeConflictEx)
            {
                await unitOfWork.RollbackTransactionAsync();
                return Result<ShortUrlDTO>.Conflict(shortCodeConflictEx.Message);
            }
            catch (Exception)
            {
                await unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}

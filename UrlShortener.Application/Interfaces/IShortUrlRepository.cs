using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Interfaces
{
    public interface IShortUrlRepository
    {
        Task AddAsync(ShortUrl shortUrl, CancellationToken ct = default);
        Task<ShortUrl?> GetByIdAsync(long id, CancellationToken ct = default);
        Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken ct = default);
        Task<(IReadOnlyList<ShortUrl> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    }
}

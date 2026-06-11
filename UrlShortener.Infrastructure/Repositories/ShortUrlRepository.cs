using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Repositories
{
    public class ShortUrlRepository : IShortUrlRepository
    {
        private readonly AppDbContext _context;

        public ShortUrlRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ShortUrl shortUrl, CancellationToken ct)
        {
            await _context.ShortUrls.AddAsync(shortUrl);
        }

        public async Task<ShortUrl?> GetByIdAsync(long id, CancellationToken ct)
        {
            return await _context.ShortUrls
                .SingleOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken ct)
        {
            return await _context.ShortUrls
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.ShortCode == shortCode, ct);
        }

        public async Task<(IReadOnlyList<ShortUrl> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct)
        {
            var query = _context.ShortUrls.AsNoTracking().OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }
    }
}

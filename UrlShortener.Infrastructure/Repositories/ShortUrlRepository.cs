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

        public async Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken ct)
        {
            return await _context.ShortUrls
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.ShortCode == shortCode, ct);
        }
    }
}

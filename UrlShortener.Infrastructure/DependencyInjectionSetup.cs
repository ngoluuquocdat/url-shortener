using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShortener.Application.Interfaces;
using UrlShortener.Infrastructure.EventPublisher;
using UrlShortener.Infrastructure.Repositories;

namespace UrlShortener.Infrastructure
{
    public static class DependencyInjectionSetup
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IShortUrlRepository, ShortUrlRepository>();

            // Healthcheck
            services.AddHealthChecks()
                .AddDbContextCheck<AppDbContext>();

            return services;
        }
    }
}

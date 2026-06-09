using UrlShortener.Api.Utilities.Constraints;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.UseCases.ShortUrl.CreateShortUrl;
using UrlShortener.Infrastructure;
using UrlShortener.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// AppDbContext, IUnitOfWork, and Repositories
builder.Services.AddInfrastructure(builder.Configuration);

// Mediator
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateShortUrlCommand).Assembly));

builder.Services.AddRouting(options =>
{
    options.ConstraintMap.Add("shortCodeRedirectConstraint", typeof(ShortCodeRedirectRouteConstraint));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

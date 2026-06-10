# --- build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /App

# Copy .csproj and restore as distinct layers for better caching
COPY UrlShortener.sln ./
COPY UrlShortener.Api/UrlShortener.Api.csproj ./UrlShortener.Api/
COPY UrlShortener.Domain/UrlShortener.Domain.csproj ./UrlShortener.Domain/
COPY UrlShortener.Application/UrlShortener.Application.csproj ./UrlShortener.Application/
COPY UrlShortener.Infrastructure/UrlShortener.Infrastructure.csproj ./UrlShortener.Infrastructure/

# Restore dependencies
RUN dotnet restore ./UrlShortener.sln

# Copy the rest of the project and publish the application
COPY . ./
# Build the application
RUN dotnet publish -c Release -o published ./UrlShortener.Api/UrlShortener.Api.csproj

# --- migrator stage
FROM build AS migrator
WORKDIR /App
RUN dotnet tool install --global dotnet-ef --version 8.*
ENV PATH="${PATH}:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "./UrlShortener.Infrastructure/UrlShortener.Infrastructure.csproj"]

# --- runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /App
# Copy the published application from the build stage to the runtime stage
COPY --from=build /App/published .
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "UrlShortener.Api.dll"]
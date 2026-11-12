FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy project files first to preserve the NuGet restore cache.
COPY AssetManagement.Domain/AssetManagement.Domain.csproj \
    AssetManagement.Domain/

COPY AssetManagement.Application/AssetManagement.Application.csproj \
    AssetManagement.Application/

COPY AssetManagement.Infrastructure/AssetManagement.Infrastructure.csproj \
    AssetManagement.Infrastructure/

COPY AssetManagement.WebApi/AssetManagement.WebApi.csproj \
    AssetManagement.WebApi/

RUN dotnet restore \
    "AssetManagement.WebApi/AssetManagement.WebApi.csproj"

# Copy the remaining source files only after restore.
COPY . .

RUN dotnet publish \
    "AssetManagement.WebApi/AssetManagement.WebApi.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ARG APP_PORT=8080

ENV ASPNETCORE_HTTP_PORTS=${APP_PORT}

EXPOSE ${APP_PORT}

ENTRYPOINT ["dotnet", "AssetManagement.WebApi.dll"]
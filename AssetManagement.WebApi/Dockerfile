FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore "AssetManagement.WebApi/AssetManagement.WebApi.csproj"

RUN dotnet publish "AssetManagement.WebApi/AssetManagement.WebApi.csproj" \
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
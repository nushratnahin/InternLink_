# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY InternLink/InternLink.csproj InternLink/
RUN dotnet restore InternLink/InternLink.csproj

COPY InternLink/. InternLink/
WORKDIR /src/InternLink
RUN dotnet publish -c Release -o /app --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

# Render (and most PaaS hosts) inject a $PORT env var and expect the
# app to listen on it. Default to 8080 for local `docker run`.
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet InternLink.dll"]

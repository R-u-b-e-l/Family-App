# Build context must be the repository root (the API references Domain, Application, Infrastructure and Shared).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/FamilyManagement.API/FamilyManagement.API.csproj
RUN dotnet publish src/FamilyManagement.API/FamilyManagement.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
# Render injects PORT; fall back to 8080 for local runs.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet FamilyManagement.API.dll"]

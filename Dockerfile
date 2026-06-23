# Build stage
FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "DBPBusinessCardEditable/DBPBusinessCardEditable.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS final
WORKDIR /app

# Install SQLite native library
RUN apt-get update && apt-get install -y libsqlite3-dev && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DBPBusinessCardEditable.dll"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY src/*.sln ./
COPY src/CSharpApp.Api/*.csproj ./CSharpApp.Api/
COPY src/CSharpApp.Application/*.csproj ./CSharpApp.Application/
COPY src/CSharpApp.Core/*.csproj ./CSharpApp.Core/
COPY src/CSharpApp.Infrastructure/*.csproj ./CSharpApp.Infrastructure/
COPY src/CSharpApp.Models/*.csproj ./CSharpApp.Models/
COPY src/test/CSharpApp.Tests/*.csproj ./test/CSharpApp.Tests/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY src/ ./

# Build and publish
RUN dotnet publish CSharpApp.Api/CSharpApp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CSharpApp.Api.dll"]

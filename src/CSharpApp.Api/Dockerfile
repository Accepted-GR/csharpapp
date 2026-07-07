FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["CSharpApp.Api/CSharpApp.Api.csproj", "CSharpApp.Api/"]
COPY ["CSharpApp.Application/CSharpApp.Application.csproj", "CSharpApp.Application/"]
COPY ["CSharpApp.Infrastructure/CSharpApp.Infrastructure.csproj", "CSharpApp.Infrastructure/"]
COPY ["CSharpApp.Models/CSharpApp.Models.csproj", "CSharpApp.Models/"]
COPY ["CSharpApp.Core/CSharpApp.Core.csproj", "CSharpApp.Core/"]
COPY ["test/CSharpApp.Tests/CSharpApp.Tests.csproj", "test/CSharpApp.Tests/"]

RUN dotnet restore "CSharpApp.Api/CSharpApp.Api.csproj"

# copy everything else and build
COPY . .
RUN dotnet publish "CSharpApp.Api/CSharpApp.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "CSharpApp.Api.dll"]

# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src    

# Copy project files first to improve Docker layer caching
COPY WIMS.Api/WIMS.Api.csproj WIMS.Api/
COPY WIMS.Application/WIMS.Application.csproj WIMS.Application/
COPY WIMS.Domain/WIMS.Domain.csproj WIMS.Domain/
COPY WIMS.Infrastructure/WIMS.Infrastructure.csproj WIMS.Infrastructure/

# Restore dependencies required by the API and referenced projects
RUN dotnet restore WIMS.Api/WIMS.Api.csproj

# Copy the remaining source code
COPY . .

# Publish the API in Release mode
RUN dotnet publish WIMS.Api/WIMS.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Stage 2: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WIMS.Api.dll"]

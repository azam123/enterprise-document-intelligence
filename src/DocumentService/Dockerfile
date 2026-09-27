FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/BuildingBlocks/BuildingBlocks.csproj src/BuildingBlocks/
COPY src/DocumentService/DocumentService.Domain/DocumentService.Domain.csproj src/DocumentService/DocumentService.Domain/
COPY src/DocumentService/DocumentService.Application/DocumentService.Application.csproj src/DocumentService/DocumentService.Application/
COPY src/DocumentService/DocumentService.Infrastructure/DocumentService.Infrastructure.csproj src/DocumentService/DocumentService.Infrastructure/
COPY src/DocumentService/DocumentService.Api/DocumentService.Api.csproj src/DocumentService/DocumentService.Api/

RUN dotnet restore src/DocumentService/DocumentService.Api/DocumentService.Api.csproj

COPY src/ src/
RUN dotnet publish src/DocumentService/DocumentService.Api/DocumentService.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DocumentService.Api.dll"]

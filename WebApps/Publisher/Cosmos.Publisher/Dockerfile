#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebApps/Publisher/Cosmos.Publisher/Cosmos.Publisher.csproj", "WebApps/Publisher/Cosmos.Publisher/"]
COPY ["Libraries/Cosmos.BlobService/Cosmos.BlobService/Cosmos.BlobService.csproj", "Libraries/Cosmos.BlobService/Cosmos.BlobService/"]
COPY ["Libraries/Cosmos.Common/Cosmos.Common/Cosmos.Common.csproj", "Libraries/Cosmos.Common/Cosmos.Common/"]
COPY ["Libraries/Cosmos.EmailServices/Cosmos.EmailServices.csproj", "Libraries/Cosmos.EmailServices/"]
RUN dotnet restore "./WebApps/Publisher/Cosmos.Publisher/Cosmos.Publisher.csproj"
COPY . .
WORKDIR "/src/WebApps/Publisher/Cosmos.Publisher"
RUN dotnet build "./Cosmos.Publisher.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cosmos.Publisher.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cosmos.Publisher.dll"]
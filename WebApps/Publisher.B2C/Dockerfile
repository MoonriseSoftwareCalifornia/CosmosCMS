# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebApps/Publisher.B2C/Cosmos.Publisher.B2C.csproj", "WebApps/Publisher.B2C/"]
COPY ["Libraries/Cosmos.BlobService/Cosmos.BlobService/Cosmos.BlobService.csproj", "Libraries/Cosmos.BlobService/Cosmos.BlobService/"]
COPY ["Libraries/Cosmos.Common/Cosmos.Common/Cosmos.Common.csproj", "Libraries/Cosmos.Common/Cosmos.Common/"]
COPY ["Libraries/Cosmos.EmailServices/Cosmos.EmailServices.csproj", "Libraries/Cosmos.EmailServices/"]
COPY ["Libraries/Cosmos.MicrosoftGraph/Cosmos.MicrosoftGraph.csproj", "Libraries/Cosmos.MicrosoftGraph/"]
RUN dotnet restore "./WebApps/Publisher.B2C/Cosmos.Publisher.B2C.csproj"
COPY . .
WORKDIR "/src/WebApps/Publisher.B2C"
RUN dotnet build "./Cosmos.Publisher.B2C.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cosmos.Publisher.B2C.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cosmos.Publisher.B2C.dll"]
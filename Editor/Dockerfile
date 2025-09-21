# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Add mount point for SQLite data
VOLUME ["/data/sqlite"]

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AspNetCore.Identity.FlexDb/AspNetCore.Identity.FlexDb.csproj", "AspNetCore.Identity.FlexDb/"]
COPY ["Cosmos.BlobService/Cosmos.BlobService.csproj", "Cosmos.BlobService/"]
COPY ["Cosmos.ConnectionStrings/Cosmos.DynamicConfig.csproj", "Cosmos.ConnectionStrings/"]
COPY ["Common/Cosmos.Common.csproj", "Common/"]
COPY ["Editor/Sky.Editor.csproj", "Editor/"]
RUN dotnet restore "./Editor/Sky.Editor.csproj"
COPY . .
WORKDIR "/src/Editor"
RUN dotnet build "./Sky.Editor.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Sky.Editor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sky.Editor.dll"]

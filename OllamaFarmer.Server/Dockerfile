# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
ARG FILE_REPOSITORY=/file_repository
#USER $APP_UID
USER 99
WORKDIR ${FILE_REPOSITORY}
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS with-node
RUN apt-get update
RUN apt-get install curl
RUN curl -sL https://deb.nodesource.com/setup_22.x | bash
RUN apt-get -y install nodejs


#ENV SPA_BUILD_OUTPUT_DIR=/app/build/wwwroot
FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
ARG BUILD_VERSION=0.0.1
WORKDIR /src
COPY ["OllamaFarmer.Server/OllamaFarmer.Server.csproj", "OllamaFarmer.Server/"]
COPY ["ollamafarmer.client/ollamafarmer.client.esproj", "ollamafarmer.client/"]
RUN dotnet restore "./OllamaFarmer.Server/OllamaFarmer.Server.csproj"
COPY . .
WORKDIR /src/OllamaFarmer.Server
RUN dotnet build "./OllamaFarmer.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:AssemblyVersion=$BUILD_VERSION


# This stage is used to publish the service project to be copied to the final stage
ENV SPA_BUILD_OUTPUT_DIR=/app/publish/wwwroot
FROM build AS publish
WORKDIR /src/OllamaFarmer.Server
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OllamaFarmer.Server.csproj" --no-restore -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "OllamaFarmer.Server.dll"]

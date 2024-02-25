# syntax=docker/dockerfile:1

ARG PreRestoreStage=setup

# A debian based image is required here in order to get the console cursor position
# manipulation to work. I think it is related to debian using dash as the default shell
# and alpine using ash. Some testing will be required to confirm this.
FROM mcr.microsoft.com/dotnet/runtime:8.0.0-bookworm-slim-amd64 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0.100-1-alpine3.18-amd64 AS setup
WORKDIR /src
COPY Directory.Packages.props .
COPY Client/Client.csproj Client/
COPY GrainInterfaces/GrainInterfaces.csproj GrainInterfaces/
COPY Grains/Grains.csproj Grains/
COPY Shared/Shared.csproj Shared/

FROM setup AS postgres
ARG AdoNetProviderPackageVersion
RUN dotnet add Client/Client.csproj package Npgsql --version ${AdoNetProviderPackageVersion}

FROM ${PreRestoreStage} AS restore
RUN dotnet restore Client/Client.csproj

FROM restore AS build
COPY . .
COPY --from=restore /src/Directory.Packages.props /src
COPY --from=restore /src/Client/Client.csproj /src/Client
WORKDIR /src/Client
ARG Configuration=Release
RUN dotnet build Client.csproj -c $Configuration -o /app/build

FROM build AS publish
ARG Configuration
RUN dotnet publish Client.csproj -c $Configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Client.dll"]
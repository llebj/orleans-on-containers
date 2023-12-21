# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/runtime:8.0.0-alpine3.18-amd64 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0.100-1-alpine3.18-amd64 AS build
WORKDIR /src
COPY ["Silo/Silo.csproj", "Silo/"]
COPY ["GrainInterfaces/GrainInterfaces.csproj", "GrainInterfaces/"]
COPY ["Grains/Grains.csproj", "Grains/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Silo/Silo.csproj"
COPY . .
WORKDIR "/src/Silo"
ARG Configuration=Release
RUN dotnet build "Silo.csproj" -c $Configuration -o /app/build

FROM build AS publish
ARG Configuration
RUN dotnet publish "Silo.csproj" -c $Configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Silo.dll"]
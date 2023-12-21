# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/runtime:8.0.0-alpine3.18-amd64 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0.100-1-alpine3.18-amd64 AS build
WORKDIR /src
COPY ["Client/Client.csproj", "Client/"]
COPY ["GrainInterfaces/GrainInterfaces.csproj", "GrainInterfaces/"]
COPY ["Grains/Grains.csproj", "Grains/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Client/Client.csproj"
COPY . .
WORKDIR "/src/Client"
ARG Configuration=Release
RUN dotnet build "Client.csproj" -c $Configuration -o /app/build

FROM build AS publish
ARG Configuration
RUN dotnet publish "Client.csproj" -c $Configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Client.dll"]
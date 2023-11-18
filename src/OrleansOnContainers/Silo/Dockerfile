# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/runtime:7.0.14-alpine3.18-amd64 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0.404-1-alpine3.18-amd64 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Silo.csproj"
RUN dotnet build "Silo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Silo.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Silo.dll"]
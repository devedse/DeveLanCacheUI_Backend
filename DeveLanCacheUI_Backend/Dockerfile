#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim-amd64 AS build
WORKDIR /src
COPY ["DeveLanCacheUI_Backend/DeveLanCacheUI_Backend.csproj", "DeveLanCacheUI_Backend/"]
RUN dotnet restore "DeveLanCacheUI_Backend/DeveLanCacheUI_Backend.csproj"
COPY . .
WORKDIR "/src/DeveLanCacheUI_Backend"
RUN dotnet build "DeveLanCacheUI_Backend.csproj" -c Release -o /app/build

FROM build AS publish
ARG BUILD_VERSION
ARG VER=${BUILD_VERSION:-1.0.0}
RUN dotnet publish "DeveLanCacheUI_Backend.csproj" -c Release -o /app/publish /p:Version=$VER /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_HTTP_PORTS=80
ENTRYPOINT ["dotnet", "DeveLanCacheUI_Backend.dll"]
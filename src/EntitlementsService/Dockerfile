# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/EntitlementsService/EntitlementsService.csproj", "src/EntitlementsService/"]
RUN dotnet restore "src/EntitlementsService/EntitlementsService.csproj"

# Copy source and publish
COPY ["src/EntitlementsService/", "src/EntitlementsService/"]
WORKDIR "/src/src/EntitlementsService"
RUN dotnet publish "EntitlementsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

# Alpine native healthcheck using wget
HEALTHCHECK --interval=5s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "EntitlementsService.dll"]

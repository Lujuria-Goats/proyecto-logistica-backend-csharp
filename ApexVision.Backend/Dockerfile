FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ApexVision.Backend/ApexVision.Backend.csproj", "ApexVision.Backend/"]
RUN dotnet restore "ApexVision.Backend/ApexVision.Backend.csproj"
COPY . .
WORKDIR "/src/ApexVision.Backend"
RUN dotnet build "./ApexVision.Backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ApexVision.Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApexVision.Backend.dll"]

# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["ApexVision.Backend/ApexVision.Backend.csproj", "ApexVision.Backend/"]
RUN dotnet restore "ApexVision.Backend/ApexVision.Backend.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/ApexVision.Backend"
RUN dotnet build "ApexVision.Backend.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "ApexVision.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Create the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApexVision.Backend.dll"]


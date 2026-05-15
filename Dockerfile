# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (better layer caching)
COPY ["McAttributes/McAttributes.csproj", "McAttributes/"]
RUN dotnet restore "McAttributes/McAttributes.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/McAttributes"
RUN dotnet build "McAttributes.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "McAttributes.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install timezone data for PostgreSQL
RUN apt-get update && apt-get install -y tzdata && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Set environment variables (non-privileged port)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose non-privileged port
EXPOSE 8080

# Add health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1

# Create non-root user for security
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "McAttributes.dll"]
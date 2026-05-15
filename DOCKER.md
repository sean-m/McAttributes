# Docker Setup for McAttributes

## Overview
This guide explains how to build and run the McAttributes application using Docker.

## Prerequisites
- Docker Desktop installed
- Azure AD application configured (for authentication)

## Quick Start

### 1. Configure Environment Variables

Copy the example environment file:
```sh
copy .env.example .env
```

Edit `.env` and set your Azure AD values:
- `AZURE_AD_TENANT_ID`: Your Azure AD tenant ID
- `AZURE_AD_CLIENT_ID`: Your application client ID
- `AZURE_AD_CLIENT_SECRET`: Your application client secret

### 2. Run with Docker Compose (Recommended)

Start the application and PostgreSQL database:
```sh
docker-compose up -d
```

The application will be available at: http://localhost:8080

### 3. Stop the Application
```sh
docker-compose down
```

To stop and remove all data:
```sh
docker-compose down -v
```

## Manual Docker Build

### Build the Docker Image
```sh
docker build -t mcattributes:latest .
```

### Run the Container
```sh
docker run -d -p 8080:8080 \
  -e ConnectionStrings__Identity="Host=host.docker.internal;Database=mcattributes;Username=postgres;Password=yourpassword" \
  -e AzureAd__Instance="https://login.microsoftonline.com/" \
  -e AzureAd__TenantId="your-tenant-id" \
  -e AzureAd__ClientId="your-client-id" \
  -e AzureAd__ClientSecret="your-client-secret" \
  --name mcattributes \
  mcattributes:latest
```

## Docker Image Features

### Security
- ? Runs as non-root user (appuser)
- ? Uses non-privileged port 8080
- ? Multi-stage build for minimal image size
- ? Based on official .NET 8 images

### Optimizations
- ? Layer caching for faster rebuilds
- ? Separate restore and build stages
- ? Includes timezone data for PostgreSQL compatibility
- ? Built-in health check endpoint

## Health Check

The application includes a health check endpoint at `/health` that Docker uses to monitor container health:

```sh
curl http://localhost:8080/health
```

## Environment Variables

### Required
- `ConnectionStrings__Identity` - PostgreSQL connection string
- `AzureAd__Instance` - Azure AD instance URL
- `AzureAd__TenantId` - Azure AD tenant ID
- `AzureAd__ClientId` - Azure AD application client ID

### Optional
- `ASPNETCORE_ENVIRONMENT` - Environment (Production/Development)
- `InitializeDatabaseWhenMissing` - Auto-create database tables (default: false)
- `ForceHttpsScheme` - Force HTTPS scheme in requests (default: false)
- `maxTopValue` - OData max top value (default: 1000)
- `AppConfigConnectionString` - Azure App Configuration connection string

## Troubleshooting

### View Logs
```sh
docker-compose logs -f mcattributes
```

### View PostgreSQL Logs
```sh
docker-compose logs -f postgres
```

### Connect to PostgreSQL
```sh
docker exec -it mcattributes-db psql -U postgres -d mcattributes
```

### Check Container Health
```sh
docker inspect mcattributes --format='{{.State.Health.Status}}'
```

### Rebuild After Code Changes
```sh
docker-compose up -d --build
```

## Production Deployment

For production:

1. **Update passwords** in `docker-compose.yml` or use Docker secrets
2. **Enable HTTPS** by configuring a reverse proxy (nginx/traefik)
3. **Set proper environment** variables for production
4. **Configure Azure AD** redirect URIs for your production domain
5. **Use volume mounts** for persistent data
6. **Set up monitoring** and log aggregation

## Development with Docker

To run in development mode with hot reload, mount the source code:

```yaml
volumes:
  - ./McAttributes:/src/McAttributes:ro
```

Note: The current Dockerfile is optimized for production builds.

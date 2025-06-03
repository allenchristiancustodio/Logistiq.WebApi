# Complete optimized Dockerfile for Logistiq API on Render
# Multi-stage build for better performance and smaller final image

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for better Docker layer caching
# This allows Docker to cache the restore step when only source code changes
COPY *.sln .
COPY Logistiq.API/*.csproj ./Logistiq.API/
COPY Logistiq.Application/*.csproj ./Logistiq.Application/
COPY Logistiq.Domain/*.csproj ./Logistiq.Domain/
COPY Logistiq.Infrastructure/*.csproj ./Logistiq.Infrastructure/
COPY Logistiq.Persistence/*.csproj ./Logistiq.Persistence/

# Restore NuGet packages
# This layer gets cached when only source code changes (not project files)
RUN dotnet restore "Logistiq.API/Logistiq.API.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish the application
RUN dotnet publish "Logistiq.API/Logistiq.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    --verbosity minimal \
    --self-contained false \
    --no-cache

# Verify the build output
RUN ls -la /app/publish

# Runtime stage - smaller base image for production
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a non-root user for security best practices
RUN groupadd -r dotnetgroup && useradd -r -g dotnetgroup dotnetuser

# Install curl for health checks (useful for monitoring)
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Copy the published application from build stage
# Set proper ownership for the non-root user
COPY --from=build --chown=dotnetuser:dotnetgroup /app/publish .

# Switch to non-root user for security
USER dotnetuser

# Set environment variables for production
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV DOTNET_EnableDiagnostics=0

# Add health check for container orchestration
# This allows Render and other platforms to monitor container health
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose the port that your application listens on
EXPOSE 8080

# Use exec form of ENTRYPOINT for better signal handling
# This ensures graceful shutdown when the container is stopped
ENTRYPOINT ["dotnet", "Logistiq.API.dll"]

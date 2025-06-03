#!/usr/bin/env bash
# render-build.sh

set -o errexit  # exit on error

# Install dependencies and build
dotnet restore
dotnet publish Logistiq.API/Logistiq.API.csproj -c Release -o out

# Run migrations (will be set up with database URL from Render)
cd out
./Logistiq.API --seed-data || echo "Seeding skipped"

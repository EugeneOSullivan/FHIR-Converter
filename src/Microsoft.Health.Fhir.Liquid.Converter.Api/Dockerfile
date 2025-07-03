# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.sln ./
COPY src/Microsoft.Health.Fhir.Liquid.Converter.Api/*.csproj ./src/Microsoft.Health.Fhir.Liquid.Converter.Api/
COPY src/Microsoft.Health.Fhir.Liquid.Converter/*.csproj ./src/Microsoft.Health.Fhir.Liquid.Converter/
COPY src/Microsoft.Health.Fhir.TemplateManagement/*.csproj ./src/Microsoft.Health.Fhir.TemplateManagement/
RUN dotnet restore src/Microsoft.Health.Fhir.Liquid.Converter.Api/Microsoft.Health.Fhir.Liquid.Converter.Api.csproj

# Copy everything else and build
COPY . .
RUN dotnet publish src/Microsoft.Health.Fhir.Liquid.Converter.Api/Microsoft.Health.Fhir.Liquid.Converter.Api.csproj -c Release -o /out --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

COPY --from=build /out .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Expose port
EXPOSE 8080

# Healthcheck
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/api/v1/health/check || exit 1

# Run as non-root user
USER appuser

ENTRYPOINT ["dotnet", "Microsoft.Health.Fhir.Liquid.Converter.Api.dll"] 
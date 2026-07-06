# Docker Support for CSharpApp

This document describes how to build and run the CSharpApp solution using Docker.

## Prerequisites

- Docker Desktop or Docker Engine (20.10+)
- Docker Compose (v2.0+)

## Quick Start

### Build and Run

```bash
# Build and start the application
docker-compose up --build

# Run in detached mode
docker-compose up -d --build

# View logs
docker-compose logs -f csharpapp-api

# Stop the application
docker-compose down
```

### Access the Application

- API Base URL: http://localhost:8080
- OpenAPI/Swagger (Development): http://localhost:8080/openapi/v1.json
- Health Check Endpoint: http://localhost:8080/api/v1/products?limit=1

### Example API Calls

```bash
# Get all products
curl http://localhost:8080/api/v1/products

# Get product by ID
curl http://localhost:8080/api/v1/products/1

# Create a product
curl -X POST http://localhost:8080/api/v1/products \
  -H "Content-Type: application/json" \
  -d '{
	"title": "Test Product",
	"price": 99.99,
	"description": "A test product",
	"categoryId": 1,
	"images": ["https://example.com/image.jpg"]
  }'

# Get all categories
curl http://localhost:8080/api/v1/categories

# Get category by ID
curl http://localhost:8080/api/v1/categories/1

# Create a category
curl -X POST http://localhost:8080/api/v1/categories \
  -H "Content-Type: application/json" \
  -d '{
	"name": "Test Category",
	"image": "https://example.com/category.jpg"
  }'
```

## Configuration

### Environment Variables

You can override configuration via environment variables in `docker-compose.yml`:

- `ASPNETCORE_ENVIRONMENT`: Set to `Development` or `Production`
- `RestApiSettings__BaseUrl`: Upstream API base URL
- `RestApiSettings__Products`: Products endpoint path
- `RestApiSettings__Categories`: Categories endpoint path
- `RestApiSettings__Auth`: Authentication endpoint path
- `RestApiSettings__Username`: Upstream API username
- `RestApiSettings__Password`: Upstream API password
- `HttpClientSettings__LifeTime`: HTTP client lifetime in minutes
- `HttpClientSettings__RetryCount`: Number of HTTP retry attempts
- `HttpClientSettings__SleepDuration`: Sleep duration between retries (ms)
- `Serilog__MinimumLevel__Default`: Logging level (Debug, Information, Warning, Error)

### Custom Configuration File

To use a custom appsettings file, mount it as a volume:

```yaml
volumes:
  - ./custom-appsettings.json:/app/appsettings.Production.json:ro
```

## Development Mode

For development with enhanced logging:

```bash
docker-compose -f docker-compose.yml -f docker-compose.override.yml up --build
```

This enables:
- Development environment settings
- Debug logging
- Source code volume mounting (read-only)

## Building for Production

### Build the Docker Image

```bash
docker build -t csharpapp-api:1.0.0 -f Dockerfile .
```

### Run the Container

```bash
docker run -d \
  --name csharpapp-api \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  csharpapp-api:1.0.0
```

### Tag and Push to Registry

```bash
# Tag for your registry
docker tag csharpapp-api:1.0.0 your-registry.com/csharpapp-api:1.0.0

# Push to registry
docker push your-registry.com/csharpapp-api:1.0.0
```

## Health Checks

The Docker Compose configuration includes a health check that polls the `/api/v1/products` endpoint. You can check container health:

```bash
# Check container status
docker ps

# Inspect health status
docker inspect csharpapp-api --format='{{json .State.Health}}'

# View health check logs
docker inspect csharpapp-api | grep -A 10 Health
```

## Troubleshooting

### Port Already in Use

If port 8080 is already in use, change the port mapping in `docker-compose.yml`:

```yaml
ports:
  - "8081:8080"  # Use 8081 on host, 8080 in container
```

Then access the API at http://localhost:8081

### View Container Logs

```bash
# Follow logs in real-time
docker-compose logs -f

# Last 100 lines
docker-compose logs --tail=100

# Specific service logs
docker-compose logs -f csharpapp-api
```

### Enter the Container

```bash
# Access container shell
docker exec -it csharpapp-api /bin/bash

# Run as root if needed
docker exec -it -u root csharpapp-api /bin/bash
```

### Rebuild Without Cache

```bash
docker-compose build --no-cache
docker-compose up
```

### Check Container Resource Usage

```bash
docker stats csharpapp-api
```

### Network Issues

If the container can't reach the upstream API:

```bash
# Test network connectivity from inside container
docker exec -it csharpapp-api curl -v https://api.escuelajs.co/api/v1/products?limit=1
```

## Multi-Stage Build Benefits

The Dockerfile uses multi-stage builds:

1. **Build Stage**: Full .NET 9 SDK image for building and publishing
2. **Runtime Stage**: Lightweight ASP.NET Core 9.0 runtime image

This approach:
- Reduces final image size (~100MB runtime vs ~800MB SDK)
- Improves security (no build tools in production)
- Speeds up container startup
- Separates build dependencies from runtime dependencies

## Image Size Optimization

Current image size breakdown:
- Base ASP.NET Core 9.0 runtime: ~80MB
- Application binaries: ~20-30MB
- **Total runtime image**: ~100-110MB

Tips for further optimization:
- Use `dotnet publish` trimming features for smaller assemblies
- Consider Alpine-based images for even smaller footprint
- Remove unnecessary dependencies

## Security Considerations

- ✅ Application runs as non-root user (`appuser`, UID 1000)
- ✅ Only necessary files are copied to runtime image
- ✅ .dockerignore excludes sensitive and unnecessary files
- ✅ Multi-stage build prevents build tools in production
- ✅ No hardcoded secrets (use environment variables)

### Production Security Best Practices

1. **Use Docker Secrets** for sensitive data:
   ```bash
   echo "mysecretpassword" | docker secret create api_password -
   ```

2. **Scan images for vulnerabilities**:
   ```bash
   docker scan csharpapp-api:latest
   ```

3. **Run security audits**:
   ```bash
   docker run --rm -v /var/run/docker.sock:/var/run/docker.sock aquasec/trivy image csharpapp-api:latest
   ```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Docker Build and Push

on:
  push:
	branches: [ main ]
  pull_request:
	branches: [ main ]

jobs:
  build:
	runs-on: ubuntu-latest

	steps:
	- uses: actions/checkout@v3

	- name: Set up Docker Buildx
	  uses: docker/setup-buildx-action@v2

	- name: Log in to Docker Hub
	  uses: docker/login-action@v2
	  with:
		username: ${{ secrets.DOCKER_USERNAME }}
		password: ${{ secrets.DOCKER_PASSWORD }}

	- name: Build and push
	  uses: docker/build-push-action@v4
	  with:
		context: .
		push: true
		tags: |
		  your-dockerhub-username/csharpapp-api:latest
		  your-dockerhub-username/csharpapp-api:${{ github.sha }}
		cache-from: type=registry,ref=your-dockerhub-username/csharpapp-api:buildcache
		cache-to: type=registry,ref=your-dockerhub-username/csharpapp-api:buildcache,mode=max
```

### Azure DevOps Example

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: Docker@2
  displayName: 'Build Docker Image'
  inputs:
	command: build
	dockerfile: '**/Dockerfile'
	tags: |
	  $(Build.BuildId)
	  latest

- task: Docker@2
  displayName: 'Push Docker Image'
  inputs:
	command: push
	containerRegistry: 'DockerHubConnection'
	repository: 'your-dockerhub-username/csharpapp-api'
	tags: |
	  $(Build.BuildId)
	  latest
```

## Docker Compose Profiles

For running different configurations:

```yaml
# In docker-compose.yml, add profiles
services:
  csharpapp-api:
	profiles: ["app"]

  csharpapp-api-debug:
	profiles: ["debug"]
	# Debug configuration
```

Run specific profile:
```bash
docker-compose --profile app up
docker-compose --profile debug up
```

## Performance Tuning

### Optimize Layer Caching

The Dockerfile is structured to maximize layer caching:
1. Copy only .csproj files first
2. Run restore (cached unless dependencies change)
3. Copy source code
4. Build and publish

### Resource Limits

Add resource constraints in docker-compose.yml:

```yaml
services:
  csharpapp-api:
	deploy:
	  resources:
		limits:
		  cpus: '1'
		  memory: 512M
		reservations:
		  cpus: '0.5'
		  memory: 256M
```

## Running Tests in Docker

Create a separate docker-compose.test.yml:

```yaml
version: '3.8'

services:
  tests:
	build:
	  context: .
	  dockerfile: Dockerfile
	  target: build
	command: dotnet test test/CSharpApp.Tests/CSharpApp.Tests.csproj --logger "trx;LogFileName=test-results.trx"
	volumes:
	  - ./test-results:/src/test/CSharpApp.Tests/TestResults
```

Run tests:
```bash
docker-compose -f docker-compose.test.yml up --abort-on-container-exit
```

## Environment-Specific Configurations

### Development
```bash
docker-compose -f docker-compose.yml -f docker-compose.override.yml up
```

### Staging
```bash
docker-compose -f docker-compose.yml -f docker-compose.staging.yml up
```

### Production
```bash
docker-compose -f docker-compose.yml up
```

## Monitoring and Logging

### Centralized Logging

Integrate with logging systems:

```yaml
services:
  csharpapp-api:
	logging:
	  driver: "json-file"
	  options:
		max-size: "10m"
		max-file: "3"
```

### Log Aggregation (ELK Stack)

```yaml
services:
  csharpapp-api:
	logging:
	  driver: "syslog"
	  options:
		syslog-address: "tcp://logstash:5000"
```

## Backup and Persistence

If you add persistent volumes in the future:

```yaml
volumes:
  app-data:
	driver: local

services:
  csharpapp-api:
	volumes:
	  - app-data:/app/data
```

## Additional Resources

- [.NET Docker Official Images](https://hub.docker.com/_/microsoft-dotnet)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [ASP.NET Core in Docker](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)

## Support

For issues or questions:
- Check the troubleshooting section above
- Review container logs: `docker-compose logs -f`
- Open an issue in the GitHub repository

## License

See the main repository README for license information.

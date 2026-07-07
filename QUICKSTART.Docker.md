# Docker Quick Start Guide

## 🚀 Getting Started with Docker

All Docker files have been successfully added to your repository!

### 📁 Files Created:

1. **Dockerfile** - Multi-stage build configuration
2. **docker-compose.yml** - Production configuration
3. **docker-compose.override.yml** - Development overrides
4. **.dockerignore** - Build optimization
5. **README.Docker.md** - Complete documentation

---

## ⚡ Quick Commands

### Build and Run
```powershell
# Navigate to repository root
cd C:\Users\Boss\source\repos\novibet

# Build and start the application
docker-compose up --build

# Run in detached mode (background)
docker-compose up -d --build
```

### Access the API
- **API Base URL**: http://localhost:8080
- **Products Endpoint**: http://localhost:8080/api/v1/products
- **Categories Endpoint**: http://localhost:8080/api/v1/categories

### Test the API
```powershell
# Get all products
curl http://localhost:8080/api/v1/products

# Get product by ID
curl http://localhost:8080/api/v1/products/1

# Get all categories
curl http://localhost:8080/api/v1/categories
```

### View Logs
```powershell
# Follow logs in real-time
docker-compose logs -f

# View last 50 lines
docker-compose logs --tail=50
```

### Stop the Application
```powershell
# Stop containers
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

---

## 🔧 Development Mode

For enhanced debugging and logging:

```powershell
docker-compose -f docker-compose.yml -f docker-compose.override.yml up --build
```

This enables:
- ✅ Development environment
- ✅ Debug-level logging
- ✅ Detailed HTTP client logs

---

## 📦 What's Included

### Dockerfile Features:
- ✅ .NET 9 SDK for build stage
- ✅ .NET 9 ASP.NET Core runtime for production
- ✅ Multi-stage build (optimized image size ~100MB)
- ✅ Non-root user for security
- ✅ Port 8080 exposed

### Docker Compose Features:
- ✅ Automatic health checks
- ✅ Environment variable configuration
- ✅ Network isolation
- ✅ Restart policy (unless-stopped)
- ✅ Development overrides

### Security Features:
- ✅ Non-root user (appuser)
- ✅ No build tools in production image
- ✅ Minimal attack surface
- ✅ Environment-based secrets

---

## 🐛 Troubleshooting

### Port 8080 Already in Use
Edit `docker-compose.yml` and change:
```yaml
ports:
  - "8081:8080"  # Use different host port
```

### View Container Details
```powershell
# List running containers
docker ps

# Inspect container
docker inspect csharpapp-api

# Enter container shell
docker exec -it csharpapp-api /bin/bash
```

### Rebuild from Scratch
```powershell
docker-compose down
docker-compose build --no-cache
docker-compose up
```

---

## 📚 Next Steps

1. **Test the build**: `docker-compose up --build`
2. **Verify endpoints**: Access http://localhost:8080/api/v1/products
3. **Review logs**: `docker-compose logs -f`
4. **Read full docs**: See `README.Docker.md` for complete documentation
5. **Commit changes**: Add Docker files to Git

---

## 🎯 Git Commands

```powershell
# Stage Docker files
git add Dockerfile docker-compose.yml docker-compose.override.yml .dockerignore README.Docker.md QUICKSTART.Docker.md

# Commit changes
git commit -m "feat: add Docker support to solution"

# Push to remote
git push origin feat/-add-docker-support-to-solution
```

---

## 📖 Full Documentation

For detailed information, see **README.Docker.md** which includes:
- Complete configuration options
- Security best practices
- CI/CD integration examples
- Production deployment guide
- Performance tuning tips
- Monitoring and logging setup

---

## ✅ Validation Checklist

Before pushing to production:

- [ ] Docker build completes successfully
- [ ] Application starts and responds to requests
- [ ] Health checks pass
- [ ] Logs show no errors
- [ ] API endpoints return expected responses
- [ ] Environment variables are configured correctly
- [ ] Security scan passes (optional: `docker scan csharpapp-api:latest`)

---

## 🆘 Support

For issues or questions:
1. Check logs: `docker-compose logs -f`
2. Review README.Docker.md troubleshooting section
3. Verify Docker Desktop is running
4. Ensure port 8080 is available
5. Check network connectivity

---

**Happy containerizing! 🐳**

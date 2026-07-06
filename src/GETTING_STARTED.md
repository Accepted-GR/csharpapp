# Getting Started

Quick guide to run the CSharpApp solution locally after cloning.

---

## 🐳 Running with Docker Compose

### Start the Application

```bash
docker-compose up --build
```

The API will be available at: **http://localhost:8080**

### Stop the Application

```bash
docker-compose down
```

### Quick Test

```bash
curl http://localhost:8080/api/v1/categories
```

---

## 🧪 Running Unit Tests

### Run Locally

```bash
cd src
dotnet test test/CSharpApp.Tests/CSharpApp.Tests.csproj
```

### Run in Docker

```bash
docker-compose -f docker-compose.test.yml up unit-tests --build --abort-on-container-exit
```

---

## 🔗 Running Integration Tests

### Run All Integration Tests in Docker

```bash
docker-compose -f docker-compose.test.yml up --build --abort-on-container-exit
```

This will:
1. Start the API container
2. Wait for it to be healthy
3. Run integration tests (GET categories, GET products)

### Manual Integration Tests

Start the API:
```bash
docker-compose up -d
```

Test endpoints:
```bash
# Get all categories
curl http://localhost:8080/api/v1/categories

# Get category by ID
curl http://localhost:8080/api/v1/categories/1

# Get products with pagination
curl http://localhost:8080/api/v1/products?limit=5

# Create a new category
curl -X POST http://localhost:8080/api/v1/categories \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Category","image":"https://placeimg.com/640/480/any"}'
```

---

## 📝 Summary

| Task | Command |
|------|---------|
| **Start API** | `docker-compose up --build` |
| **Stop API** | `docker-compose down` |
| **Unit Tests (local)** | `cd src && dotnet test test/CSharpApp.Tests/CSharpApp.Tests.csproj` |
| **Unit Tests (Docker)** | `docker-compose -f docker-compose.test.yml up unit-tests --build --abort-on-container-exit` |
| **Integration Tests** | `docker-compose -f docker-compose.test.yml up --build --abort-on-container-exit` |

---

## 🔧 Prerequisites

- Docker Desktop
- .NET 9 SDK (for local test runs)

---

For more detailed documentation, see:
- [README.Docker.md](README.Docker.md) - Comprehensive Docker guide
- [QUICKSTART.Docker.md](QUICKSTART.Docker.md) - Docker quick reference
- [TESTING.Docker.md](TESTING.Docker.md) - Detailed testing guide

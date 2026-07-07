# Docker Testing Guide

This guide explains how to run tests in Docker for the CSharpApp solution.

## 🧪 Test Types

### 1. Unit Tests
Tests that run inside Docker using the build stage (includes .NET SDK).

### 2. Integration Tests
Tests that make real HTTP calls to the running API container.

---

## 🚀 Running Tests

### Run Unit Tests in Docker

```powershell
# Run unit tests inside Docker container
docker-compose -f docker-compose.test.yml run --rm unit-tests

# Results will be saved to ./test-results/test-results.trx
```

### Run Integration Tests

```powershell
# Start API and run integration tests
docker-compose -f docker-compose.test.yml up --abort-on-container-exit integration-tests

# Or run everything together
docker-compose -f docker-compose.test.yml up --abort-on-container-exit
```

### Quick Integration Test (Manual)

```powershell
# 1. Start the API
docker-compose up -d

# 2. Wait for it to be ready (check logs)
docker-compose logs -f

# 3. Run manual tests
curl http://localhost:8080/api/v1/categories
curl http://localhost:8080/api/v1/categories/1
curl http://localhost:8080/api/v1/products?limit=5

# 4. Stop
docker-compose down
```

---

## 📝 Example Integration Test Results

### Test 1: Get Categories
```json
[
  {
	"id": 1,
	"name": "Clothes",
	"image": "https://i.imgur.com/QkIa5tT.jpeg"
  },
  {
	"id": 2,
	"name": "Electronics",
	"image": "https://i.imgur.com/ZANVnHE.jpeg"
  }
]
```

### Test 2: Get Category by ID
```json
{
  "id": 1,
  "name": "Clothes",
  "image": "https://i.imgur.com/QkIa5tT.jpeg"
}
```

### Test 3: Get Products
```json
[
  {
	"id": 1,
	"title": "Product 1",
	"price": 100,
	"description": "Description",
	"images": ["https://i.imgur.com/image.jpeg"],
	"category": {
	  "id": 1,
	  "name": "Clothes",
	  "image": "https://i.imgur.com/QkIa5tT.jpeg"
	}
  }
]
```

---

## 🔍 What's Included

### docker-compose.test.yml

Contains three services:

1. **unit-tests**: Runs xUnit tests inside Docker
2. **api-for-tests**: Runs the API container with health checks
3. **integration-tests**: Makes HTTP calls to verify API endpoints

---

## 🎯 Commands Cheat Sheet

```powershell
# Run only unit tests
docker-compose -f docker-compose.test.yml run --rm unit-tests

# Run integration tests (includes starting API)
docker-compose -f docker-compose.test.yml up --abort-on-container-exit integration-tests

# Run all tests
docker-compose -f docker-compose.test.yml up --abort-on-container-exit

# Clean up
docker-compose -f docker-compose.test.yml down

# View test results
Get-Content ./test-results/test-results.trx
```

---

## 📊 Test Results Location

- **Unit test results**: `./test-results/test-results.trx`
- **Integration test output**: Console output from `integration-tests` service

---

## 🐛 Troubleshooting

### Unit Tests Fail
```powershell
# Check the logs
docker-compose -f docker-compose.test.yml run --rm unit-tests

# Run with more verbosity
docker-compose -f docker-compose.test.yml run --rm unit-tests dotnet test --logger "console;verbosity=detailed"
```

### Integration Tests Fail

```powershell
# Check if API is healthy
docker-compose -f docker-compose.test.yml ps

# Check API logs
docker-compose -f docker-compose.test.yml logs api-for-tests

# Manually test the API
docker-compose -f docker-compose.test.yml up -d api-for-tests
curl http://localhost:8080/api/v1/categories
```

### Health Check Timeout

If the API takes too long to start, edit `docker-compose.test.yml`:

```yaml
healthcheck:
  start_period: 40s  # Increase from 20s
  interval: 15s      # Increase from 10s
```

---

## 🔧 Extending Tests

### Add More Integration Tests

Edit the `integration-tests` service in `docker-compose.test.yml`:

```yaml
integration-tests:
  command: >
	sh -c "
	  echo '=== Test: Create Category ===' &&
	  curl -X POST http://api-for-tests:8080/api/v1/categories \
		-H 'Content-Type: application/json' \
		-d '{\"name\":\"TestCategory\",\"image\":\"http://test.png\"}' &&
	  echo 'Test passed!'
	"
```

### Add Real xUnit Integration Tests

Create `test/CSharpApp.IntegrationTests/`:

```csharp
public class ApiIntegrationTests
{
	private readonly HttpClient _client;

	public ApiIntegrationTests()
	{
		_client = new HttpClient
		{
			BaseAddress = new Uri("http://api-for-tests:8080")
		};
	}

	[Fact]
	public async Task GetCategories_ReturnsOk()
	{
		var response = await _client.GetAsync("/api/v1/categories");
		response.EnsureSuccessStatusCode();

		var content = await response.Content.ReadAsStringAsync();
		Assert.NotEmpty(content);
	}
}
```

---

## 🎓 Best Practices

1. ✅ **Run unit tests first** (fast, no dependencies)
2. ✅ **Run integration tests in CI/CD** (slower, requires containers)
3. ✅ **Use health checks** to wait for API readiness
4. ✅ **Save test results** to `./test-results/` for CI/CD
5. ✅ **Clean up** containers after tests: `docker-compose -f docker-compose.test.yml down`

---

## 🔗 Integration with CI/CD

### GitHub Actions Example

```yaml
- name: Run Unit Tests in Docker
  run: docker-compose -f docker-compose.test.yml run --rm unit-tests

- name: Run Integration Tests
  run: docker-compose -f docker-compose.test.yml up --abort-on-container-exit integration-tests

- name: Upload Test Results
  uses: actions/upload-artifact@v3
  with:
	name: test-results
	path: test-results/
```

---

**Happy Testing! 🧪**

# 🤝 Contributing Guide

Cảm ơn bạn vì quan tâm đến dự án Attendance Service! 🎉

## 📋 Yêu cầu trước khi bắt đầu

- Git được cài đặt
- .NET 10 SDK
- Visual Studio 2026 (hoặc VS Code)
- Docker Desktop (tuỳ chọn)
- SQL Server (local hoặc Docker)

## 🚀 Setup phát triển

### 1. Clone repository

```bash
git clone https://github.com/Linhbonsochin/FULL_Stack.git
cd FULL_Stack
```

### 2. Khôi phục dependencies

```bash
cd API
dotnet restore
```

### 3. Cập nhật database

```bash
# Tạo database migration
dotnet ef database update

# Hoặc seed data mẫu
sqlcmd -S localhost -d AttendanceDB -E -i Migrations/SeedAttendanceData.sql
```

### 4. Chạy application

```bash
dotnet run
```

API sẽ chạy tại `https://localhost:7149` (hoặc cổng được cấu hình)

Swagger documentation: `https://localhost:7149/swagger`

---

## 🔄 Git Workflow

### Branch Naming Convention

```
feature/feature-name          # Tính năng mới
bugfix/bug-description        # Sửa lỗi
hotfix/critical-issue         # Hotfix production
refactor/component-name       # Refactoring
docs/documentation-topic      # Tài liệu
ci/pipeline-update           # CI/CD updates
```

### Commit Message Convention

Sử dụng **Conventional Commits**:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat:` Tính năng mới
- `fix:` Sửa lỗi
- `docs:` Thay đổi tài liệu
- `style:` Định dạng code (không logic)
- `refactor:` Cấu trúc lại code
- `perf:` Cải thiện performance
- `test:` Thêm/cập nhật tests
- `ci:` CI/CD changes
- `chore:` Dependencies, configs

**Ví dụ:**

```
feat(attendance): add late detection algorithm

- Calculate late minutes based on grace period
- Update StandardWorkday for partial days
- Add Status enum for attendance state

Closes #42
```

### Pull Request Process

1. **Tạo branch mới từ `main`**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Implement tính năng/sửa lỗi**
   ```bash
   # Commit messages theo convention
   git commit -m "feat: add new feature"
   ```

3. **Push lên GitHub**
   ```bash
   git push origin feature/your-feature-name
   ```

4. **Tạo Pull Request**
   - Mô tả chi tiết thay đổi
   - Liên kết issue (nếu có): `Closes #123`
   - Chỉ định reviewers
   - Đợi CI/CD pass

5. **Code Review**
   - Nhận feedback từ maintainers
   - Thực hiện requested changes
   - Push cập nhật

6. **Merge**
   - Après approval, PR sẽ được merge vào `main`
   - Branch được xoá tự động

---

## 🧪 Testing

### Chạy unit tests

```bash
dotnet test API/
```

### Chạy tests với coverage

```bash
dotnet test API/ /p:CollectCoverage=true /p:CoverageFileName=coverage.json
```

### Viết tests mới

Đặt test files trong `API.Tests/` (hoặc project test tương ứng)

```csharp
[Fact]
public void CheckIn_WithValidEmployeeId_ReturnsSuccess()
{
	// Arrange
	var employeeId = Guid.NewGuid();

	// Act
	var result = _attendanceService.CheckInAsync(employeeId);

	// Assert
	Assert.True(result.IsSuccess);
}
```

---

## 🎯 Code Standards

### C# Coding Guidelines

- **File Organization**
  - 1 public class per file
  - Namespace phù hợp với directory structure
  - Imports sorted alphabetically

- **Naming**
  - PascalCase: classes, methods, properties
  - camelCase: local variables, parameters
  - UPPER_CASE: constants
  - Prefix interfaces với `I`: `IAttendanceService`

- **Formatting**
  ```bash
  # Format code tự động
  dotnet format API/
  ```

- **Comments**
  ```csharp
  /// <summary>
  /// Calculate late minutes based on check-in time
  /// </summary>
  public int CalculateLateMinutes(TimeSpan checkInTime)
  {
	  // Implementation
  }
  ```

### Architecture

- **Separation of Concerns**
  - Controllers: handle HTTP, validation
  - Services: business logic
  - Data: EF Core, repositories
  - Models: DTOs, entities

- **Dependency Injection**
  ```csharp
  // Register in Program.cs
  services.AddScoped<IAttendanceService, AttendanceService>();
  ```

- **Error Handling**
  ```csharp
  try
  {
	  // Business logic
  }
  catch (InvalidOperationException ex)
  {
	  _logger.LogError(ex, "Invalid operation");
	  throw new ApiException("User-friendly message");
  }
  ```

---

## 📝 Documentation

### Update cuando agregando new features

- Update README.md con nueva funcionalidad
- Add XML comments para methods públicos
- Agregar ejemplos en API documentation
- Update DEPLOYMENT.md si hay cambios infrastructure

### Documentation Format

```markdown
### Feature: Name

**Description:** What does it do?

**Endpoint:** `POST /api/...`

**Parameters:**
- `employeeId` (Guid): The employee identifier

**Response:**
```json
{
  "success": true,
  "data": { ... }
}
```

**Example:**
```bash
curl -X POST ...
```
```

---

## 🐛 Reporting Issues

### Bug Report Template

```markdown
## Description
[Clear and concise description]

## Steps to Reproduce
1. Step 1
2. Step 2
3. Step 3

## Expected Behavior
[What should happen]

## Actual Behavior
[What actually happens]

## Environment
- .NET Version: 10.0
- OS: Windows/Linux/macOS
- Docker: Yes/No

## Logs/Screenshots
[Attach relevant logs or screenshots]

## Possible Solution
[Optional: suggest a fix]
```

---

## 🔐 Security Guidelines

### Do's ✅
- ✅ Use HTTPS always
- ✅ Validate user input
- ✅ Use parameterized queries
- ✅ Hash sensitive data
- ✅ Rotate secrets regularly
- ✅ Follow OWASP Top 10

### Don'ts ❌
- ❌ Commit secrets/passwords
- ❌ Log sensitive information
- ❌ Use deprecated libraries
- ❌ Hardcode configuration
- ❌ Disable security features

### Secrets Management

```bash
# Use User Secrets for development
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."

# Use Azure Key Vault for production
```

---

## 📊 Performance Considerations

- Use async/await for I/O operations
- Implement caching where appropriate
- Optimize database queries (use indexes)
- Monitor memory usage
- Profile before optimizing

```csharp
// Good: Async operation
public async Task<AttendanceLog> GetHistoryAsync(Guid employeeId)
{
	return await _context.AttendanceLogs
		.Where(x => x.EmployeeId == employeeId)
		.ToListAsync();
}
```

---

## 🚀 Deployment

### Development
```bash
dotnet run
```

### Staging
```bash
docker build -t attendance-service:staging .
docker run -e ASPNETCORE_ENVIRONMENT=Staging attendance-service:staging
```

### Production
```bash
docker pull ghcr.io/your-org/attendance-service:latest
docker-compose -f docker-compose.prod.yml up -d
```

---

## 📞 Questions & Support

- 💬 GitHub Discussions: [Link]
- 📧 Email: nlinh3040@gmail.com
- 🐛 Issues: https://github.com/Linhbonsochin/FULL_Stack/issues

---

## 📜 Code of Conduct

- Respect all contributors
- Give constructive feedback
- Help others learn
- No harassment or discrimination
- Report issues to maintainers

---

## 📄 License

This project is licensed under the [License Type]. See LICENSE file for details.

---

Thank you for contributing! 🙏

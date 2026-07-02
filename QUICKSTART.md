# 🚀 Quick Start Guide

Hướng dẫn nhanh để bắt đầu với Attendance Service.

## ⏱️ 5 Phút Setup

### 1️⃣ Clone & Setup

```bash
# Clone repository
git clone https://github.com/Linhbonsochin/FULL_Stack.git
cd FULL_Stack

# Restore dependencies
cd API
dotnet restore
```

### 2️⃣ Database Configuration

**Option A: Using Docker (Khuyến nghị)**

```bash
# Quay lại thư mục gốc
cd ..

# Khởi động SQL Server container
docker-compose up -d mssql

# Chờ SQL Server start (~30s)
# Sau đó chạy migrations
cd API
dotnet ef database update
```

**Option B: Local SQL Server**

```bash
# Update connection string in appsettings.json
# Sau đó chạy migrations
cd API
dotnet ef database update
```

### 3️⃣ Run Application

```bash
cd API
dotnet run
```

✅ **API is running!**
- 🌐 Swagger: https://localhost:7149/swagger
- 📊 Health: https://localhost:7149/health

---

## 🐳 Docker Approach (Recommended)

### One Command Setup

```bash
# Khởi động toàn bộ stack
docker-compose up -d

# Xem logs
docker-compose logs -f attendance-api

# Xem trạng thái
docker-compose ps
```

**Access:**
- API: http://localhost:8080/swagger
- RabbitMQ Management: http://localhost:15672 (guest/guest)
- SQL Server: localhost,1433

### Stop Services

```bash
docker-compose stop
docker-compose down  # Remove containers
docker-compose down -v  # Remove data (⚠️ DATA LOSS)
```

---

## 🧪 Test API

### 1. Check-In Request

```bash
curl -X POST http://localhost:8080/api/attendance/check-in \
  -H "Content-Type: application/json" \
  -d '{
	"employeeId": "f4b93dbb-d8ae-4b8b-86f8-d86d35dadde9"
  }'
```

**Response:**
```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "employeeId": "f4b93dbb-d8ae-4b8b-86f8-d86d35dadde9",
  "checkIn": "2026-01-15T08:00:00Z",
  "status": 0
}
```

### 2. Check-Out Request

```bash
curl -X POST http://localhost:8080/api/attendance/check-out \
  -H "Content-Type: application/json" \
  -d '{
	"employeeId": "f4b93dbb-d8ae-4b8b-86f8-d86d35dadde9"
  }'
```

### 3. Get Attendance History

```bash
curl -X GET "http://localhost:8080/api/attendance/history/f4b93dbb-d8ae-4b8b-86f8-d86d35dadde9" \
  -H "Content-Type: application/json"
```

### 4. Get Monthly Summary

```bash
curl -X GET "http://localhost:8080/api/attendance/monthly-summary/f4b93dbb-d8ae-4b8b-86f8-d86d35dadde9?year=2026&month=1" \
  -H "Content-Type: application/json"
```

---

## 📁 Project Structure

```
FULL_Stack/
├── API/                           # Main API Project
│   ├── Controllers/              # API Endpoints
│   │   └── AttendanceController.cs
│   ├── Services/                 # Business Logic
│   │   └── AttendanceService.cs
│   ├── Models/
│   │   ├── Entities/            # Database Models
│   │   │   └── AttendanceLog.cs
│   │   └── DTOs/                # Data Transfer Objects
│   │       └── AttendanceDto.cs
│   ├── Data/                    # EF Core DbContext
│   │   └── ApplicationDbContext.cs
│   ├── Migrations/              # Database Migrations
│   │   └── SeedAttendanceData.sql
│   ├── Program.cs               # Application Entry Point
│   ├── appsettings.json         # Configuration
│   └── Dockerfile               # Docker Configuration
│
├── docker-compose.yml           # Multi-container Setup
├── README.md                    # Project Documentation
├── DEPLOYMENT.md                # Deployment Guide
├── CONTRIBUTING.md              # Contributing Guidelines
├── CHANGELOG.md                 # Version History
└── API.slnx                     # Solution File
```

---

## 🔧 Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Data Source=localhost;Database=AttendanceDB;Integrated Security=True;..."
  },
  "ExternalServices": {
	"HrServiceUrl": "https://[HR-Service-URL]:7084",
	"HrEmployeesEndpoint": "api/v1/hr/Employees"
  },
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft.EntityFrameworkCore": "Information"
	}
  }
}
```

### Docker Environment Variables

```bash
# .env file (create in root)
MSSQL_SA_PASSWORD=YourPassword123!
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=mssql,1433;Database=AttendanceDB;...
```

---

## 🐛 Troubleshooting

### Port Already in Use

```bash
# Find process using port 8080
lsof -i :8080  # macOS/Linux
netstat -ano | findstr :8080  # Windows

# Kill process
kill -9 <PID>  # macOS/Linux
taskkill /PID <PID> /F  # Windows
```

### Database Connection Failed

```bash
# Check SQL Server is running
docker-compose ps

# View SQL Server logs
docker-compose logs mssql

# Verify connection string
# Make sure password in docker-compose.yml matches appsettings

# Manual database check
sqlcmd -S localhost -U sa -P YourPassword123! -Q "SELECT 1"
```

### Docker Build Failed

```bash
# Clean Docker
docker system prune -a

# Rebuild
docker-compose build --no-cache

# Try again
docker-compose up -d
```

---

## 📚 Common Commands

```bash
# Development
cd API
dotnet run                          # Run locally
dotnet watch run                    # Auto-reload on changes
dotnet build                        # Build project
dotnet test                         # Run tests

# Database
dotnet ef migrations add MigrationName   # Create migration
dotnet ef database update               # Apply migrations
dotnet ef database drop                 # Delete database

# Docker
docker-compose up -d                # Start all services
docker-compose logs -f              # View logs
docker-compose ps                   # Show status
docker-compose down                 # Stop services
docker build -t attendance-service . # Build image

# Git
git clone <repo-url>               # Clone repository
git pull                           # Pull latest changes
git add .                          # Stage changes
git commit -m "message"            # Commit changes
git push                           # Push to GitHub
```

---

## 📖 Next Steps

1. **Read the Documentation**
   - Xem [README.md](README.md) để biêu overview
   - Xem [DEPLOYMENT.md](DEPLOYMENT.md) để deployment options
   - Xem [API Documentation](https://localhost:7149/swagger) cho endpoint details

2. **Explore the Code**
   - Xem `API/Controllers/AttendanceController.cs` để hiểu endpoints
   - Xem `API/Services/AttendanceService.cs` để hiểu business logic
   - Xem `API/Models/Entities/` để hiểu data models

3. **Make Changes**
   - Create feature branch: `git checkout -b feature/your-feature`
   - Make changes
   - Commit: `git commit -m "feat: add your feature"`
   - Push: `git push origin feature/your-feature`
   - Create Pull Request on GitHub

4. **Deploy**
   - Xem [DEPLOYMENT.md](DEPLOYMENT.md) cho production setup
   - Configure environment variables
   - Run migrations: `dotnet ef database update`
   - Start services: `docker-compose up -d`

---

## 🆘 Need Help?

- 📖 [[README.md](README.md) - Project documentation
- 📋 [CONTRIBUTING.md](CONTRIBUTING.md) - How to contribute
- 🚀 [DEPLOYMENT.md](DEPLOYMENT.md) - Deployment guide
- 📊 [Swagger API Docs](http://localhost:7149/swagger) - API reference
- 🐛 [GitHub Issues](https://github.com/Linhbonsochin/FULL_Stack/issues) - Report problems
- 📧 [Email Support](mailto:nlinh3040@gmail.com) - Contact maintainer

---

## ⚡ Performance Tips

- Use `dotnet watch run` để development nhanh hơn
- Enable EF Core query logging để debug queries:
  ```csharp
  optionsBuilder.LogTo(Console.WriteLine);
  ```
- Use proper indexing trên database columns
- Implement caching cho frequently accessed data

---

## 🎓 Learning Resources

- [Microsoft .NET Documentation](https://docs.microsoft.com/dotnet/)
- [[Entity Framework Core Docs](https://docs.microsoft.com/ef/core/)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [RESTful API Design](https://restfulapi.net/)

---

**Happy coding! 🎉**

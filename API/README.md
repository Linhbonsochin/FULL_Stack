# 🎯 Attendance Service - Microservice

Microservice quản lý chấm công nhân viên, tính toán đi muộn/về sớm, overtime, và tổng hợp công tháng cho tích hợp với Payroll.

## 🚀 Tính năng

- ✅ **Check-in/Check-out**: Ghi nhận thời gian chấm công
- ✅ **Late Detection**: Tính toán phút đi muộ so với shift
- ✅ **Early Checkout**: Phát hiện checkout sớm
- ✅ **Standard Workday**: Quy đổi thành ngày công chuẩn (0.0-1.0)
- ✅ **Overtime Calculation**: Tính giờ làm thêm
- ✅ **Monthly Summary**: Tổng hợp dữ liệu tháng cho Payroll
- ✅ **Leave Integration**: Đồng bộ dữ liệu leave từ HR Service
- ✅ **Shift Management**: Quản lý ca làm việc với grace period

## 📋 Technology Stack

- **.NET 10** - Latest ASP.NET Core framework
- **Entity Framework Core 10** - ORM for SQL Server
- **SQL Server** - Database engine
- **MassTransit** - Message bus with RabbitMQ
- **Swagger/OpenAPI** - API documentation
- **Docker** - Container deployment

## 🐳 Docker

### Build Image

```bash
docker build -t attendance-service:latest -f API/Dockerfile .
```

### Run Container

```bash
docker run -d \
  -p 8080:8080 \
  -p 8081:8081 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=AttendanceDB;Integrated Security=False;User Id=sa;Password=YourPassword" \
  attendance-service:latest
```

## 🔌 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/attendance/check-in` | Employee check-in |
| POST | `/api/attendance/check-out` | Employee check-out |
| GET | `/api/attendance/history/{employeeId}` | Attendance history |
| GET | `/api/attendance/monthly-summary/{employeeId}` | Monthly summary |
| GET | `/api/attendance/leave-summary/{employeeId}` | Leave summary |
| POST | `/api/attendance/close-month` | Close month for Payroll |

## 📊 Sample Data

23 bản ghi chấm công mẫu được seed cho nhân viên `f4b93dbb-d8ae-4b8b-86f8-d86d35dadde9` (tháng 7/2026).

Chạy script để seed:

```bash
sqlcmd -S localhost -d AttendanceDB -E -i API/Migrations/SeedAttendanceData.sql
```

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
  }
}
```

## 🚦 Status Codes

| Status | Meaning |
|--------|---------|
| 0 | Valid (on-time) |
| 1 | Late (over grace period) |
| 2 | EarlyLeave (checkout before shift end) |
| 3 | Absent |
| 4 | OnLeave |

## 🔄 Integration with HR Service

- Mỗi 30 phút, service sync dữ liệu nhân viên từ HR Service
- Cập nhật: taxCode, bankInfo, identityNumber, dependentsCount
- Kiểm tra status "Active" trước khi cho phép check-in/out

## 📦 Database

### Tables

- `Employees` - Employee information
- `Shifts` - Shift configuration
- `AttendanceLogs` - Daily attendance records
- `LeaveRequests` - Leave requests
- `AttendanceSummaries` - Monthly summaries

### Migrations

```bash
cd API
dotnet ef migrations add MigrationName
dotnet ef database update
```

## 🧪 Testing

```bash
dotnet test API/
```

## 🔐 Security

- JWT token validation for protected endpoints
- Employee status check (Active/Inactive)
- API key validation for HR Service integration

## 📝 Logs

Logs được lưu trong:
- Console output
- Application Insights (nếu cấu hình)

## 👨‍💻 Author

**Nguyễn Đình Bảo Linh** (nlinh3040@gmail.com)

## 📄 License

Proprietary - BTL Full Stack Project

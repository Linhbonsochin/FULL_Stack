# 🚀 Getting Started - Employee Sync & Database Integration

## 📋 Prerequisites

✅ .NET 10 SDK installed  
✅ SQL Server running on localhost  
✅ HR Service available at: `https://172.16.6.17:7084`  
✅ appsettings.json configured with:
- ConnectionStrings:DefaultConnection
- JwtSettings (SecretKey, Issuer, Audience)
- ExternalServices:HrServiceUrl

---

## 🔧 Setup Steps

### **Step 1: Apply Database Migration (Optional)**

If using EF Core Migrations:

```bash
# PowerShell in API project folder
cd API

# Create migration for UpdatedAt column
dotnet ef migrations add AddUpdatedAtToEmployees

# Apply migration to database
dotnet ef database update
```

**Manual SQL (if not using EF migrations):**
```sql
-- Execute: API/Migrations/AddUpdatedAtToEmployees.sql
ALTER TABLE Employees
ADD UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE()
```

---

### **Step 2: Verify appsettings.json**

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Data Source=localhost;Database=AttendanceDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
	"SecretKey": "HrCore_BTL_7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f",
	"Issuer": "UserAuthService",
	"Audience": "AllMicroservices"
  },
  "ExternalServices": {
	"HrServiceUrl": "https://172.16.6.17:7084",
	"HrTokenEndpoint": "api/v1/hr/TestAuth/generate-token",
	"HrEmployeesEndpoint": "api/v1/hr/Employees"
  }
}
```

---

## 🏃 Run Application

### **Option A: Run from Visual Studio**

```
1. Open API.slnx in Visual Studio
2. Press F5 (Start Debugging)
3. Watch output for "[STARTUP]" and "[SYNC]" logs
4. Application ready when all employees synced from HR Service
```

### **Option B: Run from Terminal**

```powershell
# Navigate to API folder
cd D:\LapTrinh\BTL_Full-stack\back_end\API\

# Build
dotnet build "API.slnx"

# Run
dotnet run --project API/Attendance\ Service.csproj

# Expected output:
# ✅ https://localhost:5001
# 🚀 [STARTUP] Starting employee sync from HR Service...
# 📥 [SYNC] Fetched 15 employees from HR Service
# ✅ [STARTUP] Employee sync completed: 15 employees synced
```

---

## 📊 Verify Sync Completed

### **Method 1: Check Logs**

Look for these messages:
```
✅ [STARTUP] Starting employee sync from HR Service...
✅ [STARTUP] Employee sync completed: X employees synced
```

### **Method 2: Check Database**

```sql
-- SQL Server
SELECT COUNT(*) as TotalEmployees FROM Employees
-- Should show: 15 (or number of employees in HR Service)

SELECT TOP 5 * FROM Employees
-- Should show: EmployeeCode, FullName, Status, CreatedAt, UpdatedAt
```

### **Method 3: Test API**

```powershell
# PowerShell
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/employee-sync/status" -Method Get
$response.data | Format-Table

# Output:
# localEmployeeCount hrEmployeeCount syncedCount lastSyncTime        isHealthy
# 15                 15              15          2024-01-20T10:30:00 True
```

**or:**

```bash
# cURL
curl -X GET "http://localhost:5000/api/employee-sync/status" | jq .
```

---

## 🧪 Full Testing Workflow

### **1. Verify Connection to HR Service**

```powershell
# Test HTTP connectivity (ignore SSL error in Dev)
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/test/hr-employees" -SkipCertificateCheck
$response.count  # Should show number of employees
```

### **2. Trigger Manual Sync**

```powershell
# Post to sync endpoint
$result = Invoke-RestMethod -Uri "http://localhost:5000/api/employee-sync/sync" -Method Post
$result.syncedCount  # Number of employees synced
```

### **3. Get Local Employees**

```powershell
# Get all employees from local database
$employees = Invoke-RestMethod -Uri "http://localhost:5000/api/employee-sync/local-employees"
$employees.count     # Should match HR Service count
$employees.data[0]   # Show first employee
```

### **4. Test Check-in**

```powershell
# Get first employee ID from local DB
$employees = Invoke-RestMethod -Uri "http://localhost:5000/api/employee-sync/local-employees"
$empId = $employees.data[0].id

# Test check-in
$checkIn = Invoke-RestMethod -Uri "http://localhost:5000/api/attendance/check-in?employeeId=$empId&shiftId=1" -Method Post
$checkIn.success     # Should be True
$checkIn.status      # Should be "Valid" or "Late"
```

---

## 📈 Performance Expectations

| Operation | Expected Time | Notes |
|-----------|---------------|-------|
| **Initial Sync (Startup)** | 2-10 seconds | Depends on employee count |
| **Manual Sync** | 2-10 seconds | Must complete before using data |
| **Get Status** | <1 second | Fast query |
| **Check-in** | <500ms | Uses local DB only |
| **List Employees** | <500ms | Local DB query |

---

## ⚠️ Troubleshooting

### **Issue: "HR Service unavailable"**

```
⚠️ [STARTUP] Employee sync failed: Connection refused
```

**Solution:**
1. Verify HR Service URL: `https://172.16.6.17:7084`
2. Check network connectivity: `ping 172.16.6.17`
3. Verify SSL certificate handling in development
4. Application continues with existing employees (if any)

### **Issue: "JWT configuration missing"**

```
❌ [SYNC] JWT generation error: SecretKey not configured
```

**Solution:**
1. Check appsettings.json has JwtSettings
2. Verify SecretKey is not empty
3. Restart application

### **Issue: Database errors**

```
❌ [SYNC] SQL Server error: Connection timeout
```

**Solution:**
1. Verify SQL Server running: `SELECT 1`
2. Check connection string in appsettings.json
3. Verify database exists: AttendanceDB
4. Check credentials have permission

### **Issue: Employees not synced**

```
📥 [SYNC] Fetched 0 employees from HR Service
```

**Solution:**
1. Verify HR Service has employees
2. Test endpoint directly: `GET https://172.16.6.17:7084/api/v1/hr/Employees`
3. Check JWT token generation (logs)
4. Verify Bearer token format

---

## 📚 Available Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| **GET** | `/api/employee-sync/status` | Get sync status |
| **POST** | `/api/employee-sync/sync` | Trigger manual sync |
| **GET** | `/api/employee-sync/local-employees` | List all employees in DB |
| **GET** | `/api/test/hr-employees` | Test HR connection |
| **POST** | `/api/attendance/check-in` | Check-in (uses synced data) |

---

## 🔄 Sync Schedule

### **Current Implementation:**
- ✅ **Automatic:** On application startup
- ✅ **Manual:** Call `POST /api/employee-sync/sync` anytime

### **Future Enhancements (Optional):**
- Scheduled sync every hour (using Hangfire or hosted service)
- Webhook from HR Service when employees change
- Two-way sync (send attendance data back)

---

## 🎯 Success Criteria

✅ Application starts without errors  
✅ "[STARTUP] Employee sync completed" message in logs  
✅ Database contains 15 (or your employee count) records  
✅ `GET /api/employee-sync/status` shows `isHealthy: true`  
✅ Check-in works using local employee data  
✅ All columns present: EmployeeCode, FullName, Status, CreatedAt, UpdatedAt  

---

## 📞 Support

**For questions:**
1. Check logs in Visual Studio output window
2. Review DATABASE_SYNC_GUIDE.md for architecture
3. See INTER_SERVICE_COMMUNICATION_GUIDE.md for authentication
4. Check API_TEST_COLLECTION.md for request examples

**Common issues:**
- HR Service connectivity → Check network
- JWT errors → Verify appsettings.json
- Database errors → Check SQL Server
- Sync not starting → Restart application (need to stop debugger)

---

## 🚀 You're Ready!

Once you see these logs:
```
✅ [STARTUP] Employee sync completed: 15 employees synced
```

**You can:**
✅ Register employees (from HR Service)  
✅ Check-in/Check-out (using local data)  
✅ View attendance history  
✅ Generate reports  

**Enjoy!** 🎉

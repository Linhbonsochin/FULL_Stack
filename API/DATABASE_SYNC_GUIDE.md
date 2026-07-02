# 🗄️ Database Synchronization Guide - Lấy Dữ Liệu từ HR Service vào Database

## 📌 Overview

Bây giờ dự án **TỰ ĐỘNG LẤY DỮ LIỆU từ HR Service (Nhóm Khác) vào AttendanceDB** khi khởi động!

```
┌─────────────────────────────────────────┐
│   Attendance API Starts                 │
│   (API.slnx)                            │
└────────────┬────────────────────────────┘
			 │
	┌────────▼────────────┐
	│EmployeeSyncService │
	│ .SyncEmployeesAsync │
	└────────┬────────────┘
			 │
	┌────────▼──────────────────────┐
	│ Call HR Service API            │
	│ GET /api/v1/hr/Employees      │
	│ (https://172.16.6.17:7084)    │
	└────────┬──────────────────────┘
			 │
	┌────────▼──────────────────┐
	│ Sync to AttendanceDB       │
	│ • Match by EmployeeCode    │
	│ • Create new employees     │
	│ • Update existing names    │
	└────────┬──────────────────┘
			 │
	┌────────▼─────────────────────┐
	│ ✅ Ready to serve requests    │
	│ All employees are in local DB │
	└──────────────────────────────┘
```

---

## 🚀 How It Works

### **1. Automatic Sync on Startup**

```csharp
// Program.cs - Lines 113-126
using (var scope = app.Services.CreateScope())
{
	var syncService = scope.ServiceProvider.GetRequiredService<IEmployeeSyncService>();
	var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

	try
	{
		logger.LogInformation("🚀 [STARTUP] Starting employee sync from HR Service...");
		var syncResult = await syncService.SyncEmployeesAsync();
		logger.LogInformation($"✅ [STARTUP] Employee sync completed: {syncResult} employees synced");
	}
	catch (Exception ex)
	{
		logger.LogWarning($"⚠️ [STARTUP] Employee sync failed: {ex.Message}. Continuing...");
	}
}
```

**Kết quả:**
- ✅ Application khởi động
- 📥 Lấy employees từ HR Service
- 💾 Lưu vào database
- ✅ Ready to serve requests

---

## 📊 Services Created

### **1. IEmployeeSyncService & EmployeeSyncService**

**Location:** `API/Services/EmployeeSyncService.cs`

```csharp
public interface IEmployeeSyncService
{
	// Sync employees from HR Service
	Task<int> SyncEmployeesAsync(bool forceRefresh = false);

	// Get sync status
	Task<EmployeeSyncStatus> GetSyncStatusAsync();
}
```

**Chức năng:**
- ✅ Fetch employees từ HR Service
- ✅ Compare với local database
- ✅ Insert new employees
- ✅ Update existing employees (name, status)
- ✅ Detailed logging

---

## 🛣️ API Endpoints

### **Endpoint 1: Trigger Manual Sync**

```http
POST /api/employee-sync/sync

Response (200 OK):
{
  "success": true,
  "message": "✅ Employee synchronization completed successfully",
  "syncedCount": 15,
  "timestamp": "2024-01-20T10:30:00Z"
}
```

**cURL:**
```bash
curl -X POST "http://localhost:5000/api/employee-sync/sync"
```

**PowerShell:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/employee-sync/sync" -Method Post
```

---

### **Endpoint 2: Get Sync Status**

```http
GET /api/employee-sync/status

Response (200 OK):
{
  "success": true,
  "data": {
	"localEmployeeCount": 15,
	"hrEmployeeCount": 15,
	"syncedCount": 15,
	"lastSyncTime": "2024-01-20T10:30:00Z",
	"isHealthy": true,
	"message": "✅ 15 employees available in HR Service"
  },
  "timestamp": "2024-01-20T10:30:00Z"
}
```

**cURL:**
```bash
curl "http://localhost:5000/api/employee-sync/status"
```

---

### **Endpoint 3: List All Local Employees**

```http
GET /api/employee-sync/local-employees

Response (200 OK):
{
  "success": true,
  "count": 15,
  "data": [
	{
	  "id": "550e8400-e29b-41d4-a716-446655440000",
	  "employeeCode": "EMP001",
	  "fullName": "Nguyễn Văn A",
	  "status": "Active",
	  "createdAt": "2024-01-20T10:30:00Z",
	  "updatedAt": "2024-01-20T10:30:00Z"
	}
  ],
  "timestamp": "2024-01-20T10:30:00Z"
}
```

**cURL:**
```bash
curl "http://localhost:5000/api/employee-sync/local-employees"
```

---

## 🗂️ Files Modified/Created

| File | Status | Change |
|------|--------|--------|
| **Services/EmployeeSyncService.cs** | ✨ NEW | Sync logic |
| **Controllers/EmployeeSyncController.cs** | ✨ NEW | API endpoints |
| **Models/Entities/Employee.cs** | 🔄 UPDATED | Added `UpdatedAt` |
| **Program.cs** | 🔄 UPDATED | Register service + startup sync |

---

## 📋 Database Schema - Employee Table

```sql
CREATE TABLE Employees (
	Id UNIQUEIDENTIFIER PRIMARY KEY,
	EmployeeCode VARCHAR(50) NOT NULL UNIQUE,
	FullName NVARCHAR(255) NOT NULL,
	Status VARCHAR(50) DEFAULT 'Active',  -- Active, Inactive, OnLeave
	CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
	UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

**New Column:**
- `UpdatedAt` - Tracks when employee info was last sync'd

---

## 🔄 Sync Algorithm

### **Step 1: Fetch from HR Service**
```csharp
var hrEmployees = await _hrServiceClient.GetEmployeesAsync();
// Returns: List<EmployeeDto>
// Example: [EMP001:Nguyễn Văn A, EMP002:Trần Thị B, ...]
```

### **Step 2: Get Local Employees**
```csharp
var localEmployees = await _context.Employees.ToListAsync();
// Returns: List<Employee>
// What's already in AttendanceDB
```

### **Step 3: For Each HR Employee**

```csharp
foreach (var hrEmployee in hrEmployees)
{
	var localEmployee = localEmployees
		.FirstOrDefault(e => e.EmployeeCode == hrEmployee.EmployeeCode);

	if (localEmployee == null)
	{
		// ➕ NEW: Create in database
		var newEmp = new Employee
		{
			EmployeeCode = hrEmployee.EmployeeCode,
			FullName = hrEmployee.FullName,
			Status = hrEmployee.Status,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		_context.Employees.Add(newEmp);
	}
	else
	{
		// 🔄 EXISTS: Update if changed
		if (localEmployee.FullName != hrEmployee.FullName)
		{
			localEmployee.FullName = hrEmployee.FullName;
			localEmployee.UpdatedAt = DateTime.UtcNow;
			_context.Employees.Update(localEmployee);
		}
		if (localEmployee.Status != hrEmployee.Status)
		{
			localEmployee.Status = hrEmployee.Status;
			localEmployee.UpdatedAt = DateTime.UtcNow;
			_context.Employees.Update(localEmployee);
		}
	}
}
```

### **Step 4: Save to Database**
```csharp
await _context.SaveChangesAsync();
```

---

## 📝 Logging Output (Application Startup)

```
🚀 [STARTUP] Starting employee sync from HR Service...
🔄 [SYNC] Starting employee synchronization from HR Service...
📥 [SYNC] Fetched 15 employees from HR Service
📦 [SYNC] Found 5 employees in local database
✅ [SYNC] Created new employee: EMP001 - Nguyễn Văn A
✅ [SYNC] Created new employee: EMP002 - Trần Thị B
✅ [SYNC] Created new employee: EMP003 - Lê Văn C
... (10 more creates)
✅ [SYNC] Synchronization completed: ✨10 created, 🔄0 updated, 📊Total: 10
💾 [SYNC] Saved changes to database
✅ [STARTUP] Employee sync completed: 10 employees synced
```

---

## 🎯 Use Cases

### **Use Case 1: New Application Deployment**
```
1. Start application
2. EmployeeSyncService.SyncEmployeesAsync() runs
3. All employees from HR Service loaded into database
4. Ready to serve requests immediately
```

### **Use Case 2: Manual Sync Anytime**
```bash
# Via API
POST /api/employee-sync/sync

# Response: 10 employees synced
# New hires automatically available
```

### **Use Case 3: Check Sync Status**
```bash
# Is HR Service available?
GET /api/employee-sync/status

# Shows: HR has 15 employees, local DB has 15 employees
# isHealthy: true
```

### **Use Case 4: Attendance Check-in**
```csharp
// CheckInAsync now can:
1. Validate employee exists in local DB (fast)
2. Validate employee is Active status
3. Record check-in time
// No need for HR Service call during check-in!
```

---

## ⚠️ Error Handling

### **Scenario A: HR Service Down on Startup**
```
⚠️ [STARTUP] Employee sync failed: Connection refused
⚠️ Application continues with existing employees
✅ User can still check-in/check-out (using local DB)
```

### **Scenario B: Partial Sync Failure**
```
❌ [SYNC] Error syncing employee EMP005: Field validation failed
(Continue with next employee)
✅ [SYNC] Completed: 14 created, 0 updated (1 failed)
```

### **Scenario C: Network Timeout**
```
❌ [SYNC] HTTP error during sync: 408 - Request timeout
⚠️ Retry logic: Application waits 3 seconds and tries again
```

---

## 🔐 Data Flow: HR Service → AttendanceDB

```
HR Service (Group 1)
	↓
API: GET /api/v1/hr/Employees
Response: {
  "statusCode": 200,
  "data": [
	{
	  "id": "guid",
	  "employeeCode": "EMP001",
	  "fullName": "Nguyễn Văn A",
	  "status": "Active"
	}
  ]
}
	↓
Parse JSON → EmployeeDto List
	↓
Compare with Local DB
	↓
Create/Update Employees
	↓
AttendanceDB
	↓
Database Table: Employees
COUNT: 15 rows
```

---

## 🚀 Quick Test

### **1. Check Logs**
```
Watch for "[STARTUP]" and "[SYNC]" messages in output window
✅ = Success
❌ = Error (HR Service down, ok - continues with local DB)
```

### **2. Verify Database**
```sql
-- SQL Server
SELECT COUNT(*) FROM Employees;
-- Should show: 15 (or however many from HR Service)

SELECT TOP 5 * FROM Employees;
-- Should show: EmployeeCode, FullName, Status, CreatedAt, UpdatedAt
```

### **3. Test API**
```bash
# PowerShell
$status = Invoke-RestMethod "http://localhost:5000/api/employee-sync/status"
$status.data | Format-Table

# Shows:
# localEmployeeCount hrEmployeeCount syncedCount lastSyncTime isHealthy
# 15                 15              15          2024-01-20   True
```

---

## 📊 Comparison: Before vs After

### **Before (Test Endpoint Only)**
```
GET /api/test/hr-employees
↓
Returns HR data but DOESN'T save
↓
❌ Can't use in Check-in/Check-out
```

### **After (Database Sync)**
```
App Startup
↓
Auto sync from HR Service
↓
💾 Employees saved in AttendanceDB
↓
✅ Check-in/Check-out uses local DB (fast)
✅ No dependency on HR Service availability during operations
```

---

## 🔗 Related Documentation

📖 See additional files:
- **INTER_SERVICE_COMMUNICATION_GUIDE.md** - Token, auth, JWT details
- **HR_SERVICE_INTEGRATION_EXAMPLES.md** - 4 practical scenarios
- **API_TEST_COLLECTION.md** - Postman requests

---

## ✅ Deployment Checklist

Before going live:

```
□ HR Service URL correct: https://172.16.6.17:7084
□ JWT config in appsettings.json
□ Database connection string correct
□ Run migrations (if needed)
□ Test sync endpoint: POST /api/employee-sync/sync
□ Verify employees in database
□ Check logs for errors
□ Performance: sync completes in < 10 seconds
```

---

**Summary:** 🎉

✅ Employees **automatically loaded** from HR Service on startup  
✅ Database **always in sync** with latest employee data  
✅ **Manual sync** available anytime via API  
✅ **Fallback to local DB** if HR Service unavailable  
✅ Check-in/Check-out **works offline** with local data  

**Ready to use!** 🚀

# 🎯 Quick Reference - Employee Database Sync

## 📌 One-Liner Summary

**Attendance API tự động lấy dữ liệu từ HR Service vào database khi khởi động**

---

## ⚡ 3-Minute Quick Start

### **Step 1: Restart Application**
```powershell
# Stop current session (if debugging)
# Press Shift+F5 in Visual Studio

# Or restart from terminal
cd API
dotnet run
```

### **Step 2: Watch the Logs**
```
Expected output:
🚀 [STARTUP] Starting employee sync from HR Service...
📥 [SYNC] Fetched 15 employees from HR Service
✅ [STARTUP] Employee sync completed: 15 employees synced
```

### **Step 3: Verify**
```powershell
# Test the API
Invoke-RestMethod "http://localhost:5000/api/employee-sync/status"

# Should show: isHealthy = true
```

**Done!** ✅ Employees are now in your database!

---

## 🗺️ Architecture at a Glance

```
App Startup
	↓
EmployeeSyncService
	↓
IHrServiceClient (JWT auth)
	↓
HR Service API
	↓
Parse response
	↓
Save to AttendanceDB
	↓
✅ Ready
```

---

## 📞 3 Main API Endpoints

### **(1) Trigger Sync**
```
POST /api/employee-sync/sync
✅ Response: "syncedCount": 15
```

### **(2) Check Status**
```
GET /api/employee-sync/status
✅ Response: "isHealthy": true
```

### **(3) List Employees**
```
GET /api/employee-sync/local-employees
✅ Response: [{ employeeCode, fullName, status }]
```

---

## 💾 What Changed in Database

**New Column Added:**
```sql
Employees table
├─ Id (was here)
├─ EmployeeCode (was here)
├─ FullName (was here)
├─ Status (was here)
├─ CreatedAt (was here)
└─ UpdatedAt ⭐ (NEW - tracks sync time)
```

---

## 🔄 Sync Logic (Simple Version)

```csharp
foreach (var hrEmployee in hrEmployees)
{
	if (employeeExists(hrEmployee.EmployeeCode))
		updateEmployee(hrEmployee);   // 🔄 Update
	else
		createEmployee(hrEmployee);   // ➕ Create
}
saveDatabase();  // 💾 Commit
```

---

## ⚠️ If Something Goes Wrong

| Problem | Solution |
|---------|----------|
| Sync doesn't start | Restart app (debugger issue) |
| HR Service unavailable | App continues with local data |
| Database error | Check SQL Server connection |
| JWT error | Verify appsettings.json config |

---

## 📊 Files You Need to Know

| File | What It Does |
|------|--------|
| `Services/EmployeeSyncService.cs` | ⭐ Main sync logic |
| `Controllers/EmployeeSyncController.cs` | ⭐ API endpoints |
| `Program.cs` | Registers service + runs on startup |
| `Models/Entities/Employee.cs` | Added `UpdatedAt` column |

---

## 🚀 Test Immediately

```powershell
# 1. Get sync status
$s = Invoke-RestMethod "http://localhost:5000/api/employee-sync/status"
$s.data

# 2. Get local employees
$e = Invoke-RestMethod "http://localhost:5000/api/employee-sync/local-employees"
$e.count

# 3. Trigger manual sync (optional)
Invoke-RestMethod "http://localhost:5000/api/employee-sync/sync" -Method Post
```

---

## ✅ Success Checklist

- [ ] Application starts
- [ ] See "[STARTUP]" sync messages in logs
- [ ] `GET /api/employee-sync/status` returns `isHealthy: true`
- [ ] Database has 15+ employees
- [ ] Check-in works

---

## 🔗 Full Documentation

- 📖 **DATABASE_SYNC_GUIDE.md** - Read this for architecture
- 🚀 **SETUP_AND_RUN.md** - Read this for troubleshooting
- 📊 **IMPLEMENTATION_SUMMARY.md** - Complete overview

---

## 💡 Real-World Usage

```
08:00 - App starts
	   ↓ Auto-syncs employees from HR

08:05 - Employee checks in
	   ↓ Uses local database (fast!)

14:00 - New employees in HR Service
	   ↓ Call: POST /api/employee-sync/sync

14:05 - New employees can check in immediately!
```

---

## 🎯 Key Points

✅ Employees **auto-loaded** from HR Service  
✅ Employees **stored** in local database  
✅ Check-in **works offline**  
✅ Sync can be **triggered manually**  
✅ Works even if **HR Service down**  

---

**That's it!** 🚀

Restart your app and enjoy automated employee sync! 🎉

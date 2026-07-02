using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    /// <summary>
    /// Interface for employee synchronization service
    /// Syncs employees from HR Service (Group 1) to local database
    /// </summary>
    public interface IEmployeeSyncService
    {
        /// <summary>
        /// Sync employees from HR Service to local database
        /// </summary>
        /// <param name="forceRefresh">If true, refresh all employees; if false, only add new ones</param>
        /// <returns>Number of employees synced (created + updated)</returns>
        Task<int> SyncEmployeesAsync(bool forceRefresh = false);

        /// <summary>
        /// Get sync status
        /// </summary>
        Task<EmployeeSyncStatus> GetSyncStatusAsync();
    }

    /// <summary>
    /// Sync status DTO
    /// </summary>
    public class EmployeeSyncStatus
    {
        public int LocalEmployeeCount { get; set; }
        public int HrEmployeeCount { get; set; }
        public int SyncedCount { get; set; }
        public DateTime LastSyncTime { get; set; }
        public bool IsHealthy { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Implementation of employee synchronization service
    /// Fetches employees from HR Service and syncs to local database
    /// </summary>
    public class EmployeeSyncService : IEmployeeSyncService
    {
        private readonly IHrServiceClient _hrServiceClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeeSyncService> _logger;

        public EmployeeSyncService(
            IHrServiceClient hrServiceClient,
            ApplicationDbContext context,
            ILogger<EmployeeSyncService> logger)
        {
            _hrServiceClient = hrServiceClient;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Sync employees from HR Service to local database
        /// Process:
        /// 1. Fetch employees from HR Service
        /// 2. Compare with local database
        /// 3. Insert new employees
        /// 4. Update existing employees (name, status)
        /// 5. Log results
        /// </summary>
        public async Task<int> SyncEmployeesAsync(bool forceRefresh = false)
        {
            try
            {
                _logger.LogInformation("🔄 [SYNC] Starting employee synchronization from HR Service...");

                // Step 1: Fetch employees from HR Service
                var hrEmployees = await _hrServiceClient.GetEmployeesAsync();
                _logger.LogInformation($"📥 [SYNC] Fetched {hrEmployees.Count} employees from HR Service");

                if (hrEmployees.Count == 0)
                {
                    _logger.LogWarning("⚠️ [SYNC] No employees returned from HR Service");
                    return 0;
                }

                // Step 2: Get existing employees from local database
                var localEmployees = await _context.Employees.ToListAsync();
                _logger.LogInformation($"📦 [SYNC] Found {localEmployees.Count} employees in local database");

                int createdCount = 0;
                int updatedCount = 0;

                // Step 3: Sync each HR employee
                foreach (var hrEmployee in hrEmployees)
                {
                    try
                    {
                        // Check if employee already exists (by EmployeeCode)
                        var localEmployee = localEmployees.FirstOrDefault(
                            e => e.EmployeeCode == hrEmployee.EmployeeCode);

                        if (localEmployee == null)
                        {
                            // ➕ CREATE new employee
                            var newEmployee = new Employee
                            {
                                Id = hrEmployee.Id,
                                EmployeeCode = hrEmployee.EmployeeCode ?? string.Empty,
                                FullName = hrEmployee.FullName ?? string.Empty,
                                Email = hrEmployee.Email ?? string.Empty,
                                Position = hrEmployee.Position ?? string.Empty,
                                Phone = hrEmployee.Phone ?? string.Empty,
                                Address = hrEmployee.Address ?? string.Empty,
                                HireDate = hrEmployee.HireDate,
                                TaxCode = hrEmployee.TaxCode ?? string.Empty,
                                DependentsCount = hrEmployee.DependentsCount,
                                IdentityNumber = hrEmployee.IdentityNumber ?? string.Empty,
                                BankName = hrEmployee.BankName ?? string.Empty,
                                BankAccountNumber = hrEmployee.BankAccountNumber ?? string.Empty,
                                BankBranch = hrEmployee.BankBranch ?? string.Empty,
                                Status = hrEmployee.Status ?? "Active",
                                CreatedAt = hrEmployee.CreatedAt,
                                UpdatedAt = DateTime.UtcNow
                            };

                            _context.Employees.Add(newEmployee);
                            createdCount++;
                            _logger.LogInformation(
                                $"✅ [SYNC] Created new employee: {hrEmployee.EmployeeCode} - {hrEmployee.FullName}");
                        }
                        else
                        {
                            // 🔄 UPDATE existing employee
                            bool hasChanges = false;

                            // Update full name if different
                            if (localEmployee.FullName != hrEmployee.FullName)
                            {
                                _logger.LogDebug(
                                    $"📝 [SYNC] Updating name: {hrEmployee.EmployeeCode} " +
                                    $"from '{localEmployee.FullName}' to '{hrEmployee.FullName}'");
                                localEmployee.FullName = hrEmployee.FullName ?? string.Empty;
                                hasChanges = true;
                            }

                            // Update status if different
                            if (localEmployee.Status != hrEmployee.Status)
                            {
                                _logger.LogDebug(
                                    $"📝 [SYNC] Updating status: {hrEmployee.EmployeeCode} " +
                                    $"from '{localEmployee.Status}' to '{hrEmployee.Status}'");
                                localEmployee.Status = hrEmployee.Status ?? "Active";
                                hasChanges = true;
                            }

                            // 🆕 Update new fields from HR Service
                            if (localEmployee.Email != hrEmployee.Email)
                            {
                                localEmployee.Email = hrEmployee.Email ?? string.Empty;
                                hasChanges = true;
                            }
                            if (localEmployee.Position != hrEmployee.Position)
                            {
                                localEmployee.Position = hrEmployee.Position ?? string.Empty;
                                hasChanges = true;
                            }
                            if (localEmployee.Phone != hrEmployee.Phone)
                            {
                                localEmployee.Phone = hrEmployee.Phone ?? string.Empty;
                                hasChanges = true;
                            }
                            if (localEmployee.Address != hrEmployee.Address)
                            {
                                localEmployee.Address = hrEmployee.Address ?? string.Empty;
                                hasChanges = true;
                            }
                            if (localEmployee.HireDate != hrEmployee.HireDate)
                            {
                                localEmployee.HireDate = hrEmployee.HireDate;
                                hasChanges = true;
                            }
                            if (localEmployee.TaxCode != hrEmployee.TaxCode)
                            {
                                localEmployee.TaxCode = hrEmployee.TaxCode ?? string.Empty;
                                hasChanges = true;
                            }
                            if (localEmployee.DependentsCount != hrEmployee.DependentsCount)
                            {
                                localEmployee.DependentsCount = hrEmployee.DependentsCount;
                                hasChanges = true;
                            }
                            if (localEmployee.IdentityNumber != hrEmployee.IdentityNumber)
                            {
                                localEmployee.IdentityNumber = hrEmployee.IdentityNumber ?? string.Empty;
                                hasChanges = true;
                            }
                            if (localEmployee.BankName != hrEmployee.BankName)
                            {
                                localEmployee.BankName = hrEmployee.BankName ?? string.Empty;
                                hasChanges = true;
                            }
                            if (localEmployee.BankAccountNumber != hrEmployee.BankAccountNumber)
                            {
                                localEmployee.BankAccountNumber = hrEmployee.BankAccountNumber ?? string.Empty;
                                hasChanges = true;
                            }
                            if (localEmployee.BankBranch != hrEmployee.BankBranch)
                            {
                                localEmployee.BankBranch = hrEmployee.BankBranch ?? string.Empty;
                                hasChanges = true;
                            }

                            if (hasChanges)
                            {
                                localEmployee.UpdatedAt = DateTime.UtcNow;
                                _context.Employees.Update(localEmployee);
                                updatedCount++;
                                _logger.LogInformation($"🔄 [SYNC] Updated employee: {hrEmployee.EmployeeCode}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            $"❌ [SYNC] Error syncing employee {hrEmployee?.EmployeeCode}: {ex.Message}");
                        // Continue with next employee instead of failing entire sync
                    }
                }

                // Step 4: Save all changes to database
                if (createdCount > 0 || updatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"💾 [SYNC] Saved changes to database");
                }

                // Step 5: Log summary
                int totalSynced = createdCount + updatedCount;
                _logger.LogInformation(
                    $"✅ [SYNC] Synchronization completed: " +
                    $"✨{createdCount} created, 🔄{updatedCount} updated, 📊Total: {totalSynced}");

                return totalSynced;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    $"❌ [SYNC] HTTP error during sync: {ex.StatusCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ [SYNC] Error during employee sync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get current sync status
        /// Returns comparison between HR Service and local database
        /// </summary>
        public async Task<EmployeeSyncStatus> GetSyncStatusAsync()
        {
            try
            {
                var localCount = await _context.Employees.CountAsync();

                List<EmployeeDto> hrEmployees;
                try
                {
                    hrEmployees = await _hrServiceClient.GetEmployeesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"⚠️ [STATUS] HR Service unavailable: {ex.Message}");
                    return new EmployeeSyncStatus
                    {
                        LocalEmployeeCount = localCount,
                        HrEmployeeCount = 0,
                        SyncedCount = 0,
                        LastSyncTime = DateTime.UtcNow,
                        IsHealthy = false,
                        Message = $"HR Service unavailable: {ex.Message}"
                    };
                }

                return new EmployeeSyncStatus
                {
                    LocalEmployeeCount = localCount,
                    HrEmployeeCount = hrEmployees.Count,
                    SyncedCount = hrEmployees.Count,
                    LastSyncTime = DateTime.UtcNow,
                    IsHealthy = true,
                    Message = $"✅ {hrEmployees.Count} employees available in HR Service"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error getting sync status: {ex.Message}");
                throw;
            }
        }
    }
}

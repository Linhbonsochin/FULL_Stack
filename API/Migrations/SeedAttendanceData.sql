-- Seed sample attendance data for employee f4b93dbb-d8ae-4b8b-86f8-d86d35dadde9
-- This script inserts 20 attendance records for July 2026 (weekdays only)
-- Shift ID: 00000000-0000-0000-0000-000000000001 (Ca Hành chính)

DECLARE @EmployeeId UNIQUEIDENTIFIER = 'f4b93dbb-d8ae-4b8b-86f8-d86d35dadde9'
DECLARE @ShiftId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001'
DECLARE @BaseDate DATE = '2026-07-01'
DECLARE @Counter INT = 0
DECLARE @CurrentDate DATE
DECLARE @DayOfWeek INT
DECLARE @CheckInTime TIME
DECLARE @CheckOutTime TIME
DECLARE @LateMinutes INT = 0
DECLARE @EarlyMinutes INT = 0
DECLARE @StandardWorkday DECIMAL(5,1)
DECLARE @Status INT = 0  -- Valid

-- Insert 20 working days (skip weekends)
WHILE @Counter < 31
BEGIN
	SET @CurrentDate = DATEADD(DAY, @Counter, @BaseDate)
	SET @DayOfWeek = DATEPART(DW, @CurrentDate)

	-- Skip weekends (Saturday=7, Sunday=1)
	IF @DayOfWeek NOT IN (1, 7)
	BEGIN
		-- Simulate different check-in/out times
		IF @Counter % 5 = 0
		BEGIN
			-- On-time check-in at 08:00, normal check-out at 17:30
			SET @CheckInTime = '08:00:00'
			SET @CheckOutTime = '17:30:00'
			SET @LateMinutes = 0
			SET @EarlyMinutes = 0
			SET @StandardWorkday = 1.0
			SET @Status = 0  -- Valid
		END
		ELSE IF @Counter % 5 = 1
		BEGIN
			-- Late by 10 minutes (but within grace period of 15)
			SET @CheckInTime = '08:10:00'
			SET @CheckOutTime = '17:30:00'
			SET @LateMinutes = 0  -- Within grace period
			SET @EarlyMinutes = 0
			SET @StandardWorkday = 1.0
			SET @Status = 0  -- Valid
		END
		ELSE IF @Counter % 5 = 2
		BEGIN
			-- Late by 25 minutes (exceeds grace period)
			SET @CheckInTime = '08:25:00'
			SET @CheckOutTime = '17:30:00'
			SET @LateMinutes = 10  -- 25 - 15 grace period = 10 late
			SET @EarlyMinutes = 0
			SET @StandardWorkday = 0.95
			SET @Status = 1  -- Late
		END
		ELSE IF @Counter % 5 = 3
		BEGIN
			-- Early checkout by 30 minutes
			SET @CheckInTime = '08:00:00'
			SET @CheckOutTime = '17:00:00'
			SET @LateMinutes = 0
			SET @EarlyMinutes = 30
			SET @StandardWorkday = 0.94
			SET @Status = 2  -- EarlyLeave
		END
		ELSE
		BEGIN
			-- Normal working day
			SET @CheckInTime = '07:55:00'
			SET @CheckOutTime = '17:35:00'
			SET @LateMinutes = 0
			SET @EarlyMinutes = 0
			SET @StandardWorkday = 1.0
			SET @Status = 0  -- Valid
		END

		INSERT INTO AttendanceLogs (
			Id,
			EmployeeId,
			Date,
			CheckIn,
			CheckOut,
			ShiftId,
			LateCheckInMinutes,
			EarlyCheckOutMinutes,
			OvertimeHours,
			StandardWorkday,
			Status,
			CreatedAt
		) VALUES (
			NEWID(),
			@EmployeeId,
			@CurrentDate,
			@CheckInTime,
			@CheckOutTime,
			@ShiftId,
			@LateMinutes,
			@EarlyMinutes,
			0.25,  -- Sample overtime
			@StandardWorkday,
			@Status,
			GETUTCDATE()
		)
	END

	SET @Counter = @Counter + 1
END

-- Verify inserted data
SELECT COUNT(*) AS TotalRecords, 
	   @EmployeeId AS EmployeeId,
	   MIN(Date) AS FirstDate,
	   MAX(Date) AS LastDate
FROM AttendanceLogs
WHERE EmployeeId = @EmployeeId
  AND YEAR(Date) = 2026
  AND MONTH(Date) = 7

using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using UniSpace.Bo.DTOs.ScheduleDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Model.Interfaces;
using UniSpace.Service.Interfaces;
using UniSpace.Service.Utils;


namespace UniSpace.Service.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;
        private readonly ICurrentTime _currentTime;
        private readonly ILogger<ScheduleService> _logger;

        public ScheduleService(
            IUnitOfWork unitOfWork,
            IClaimsService claimsService,
            ICurrentTime currentTime,
            ILogger<ScheduleService> logger)
        {
            _unitOfWork = unitOfWork;
            _claimsService = claimsService;
            _currentTime = currentTime;
            _logger = logger;
        }

        #region Create

        public async Task<ScheduleDto?> CreateScheduleAsync(CreateScheduleDto createDto)
        {
            try
            {
                _logger.LogInformation($"Creating new schedule: {createDto.Title} for room: {createDto.RoomId}");

                // Validate input
                if (string.IsNullOrWhiteSpace(createDto.Title))
                {
                    throw ErrorHelper.BadRequest("Schedule title is required");
                }

                if (createDto.StartTime >= createDto.EndTime)
                {
                    throw ErrorHelper.BadRequest("Start time must be before end time");
                }

                if (createDto.StartDate >= createDto.EndDate)
                {
                    throw ErrorHelper.BadRequest("Start date must be before end date");
                }

                // Check if room exists
                var room = await _unitOfWork.Room.GetByIdAsync(createDto.RoomId, r => r.Campus);
                if (room == null || room.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Room with ID '{createDto.RoomId}' not found");
                }

                // Check for conflicts with break time
                if (!await IsTimeSlotAvailableAsync(
                    createDto.RoomId,
                    createDto.DayOfWeek,
                    createDto.StartTime,
                    createDto.EndTime,
                    createDto.StartDate,
                    createDto.EndDate,
                    createDto.BreakTimeMinutes))
                {
                    var conflicts = await GetConflictingSchedulesAsync(
                        createDto.RoomId,
                        createDto.DayOfWeek,
                        createDto.StartTime,
                        createDto.EndTime,
                        createDto.StartDate,
                        createDto.EndDate);

                    var conflictDetails = string.Join(", ", conflicts.Select(s =>
                        $"{s.Title} ({s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm})"));

                    throw ErrorHelper.Conflict(
                        $"Schedule conflicts with existing schedule(s): {conflictDetails}. " +
                        $"Please ensure there is at least {createDto.BreakTimeMinutes} minutes break time between schedules.");
                }

                var currentUserId = _claimsService.GetCurrentUserId;
                var currentTime = _currentTime.GetCurrentTime();

                var schedule = new Schedule
                {
                    Id = Guid.NewGuid(),
                    RoomId = createDto.RoomId,
                    ScheduleType = createDto.ScheduleType,
                    Title = createDto.Title.Trim(),
                    StartTime = createDto.StartTime,
                    EndTime = createDto.EndTime,
                    DayOfWeek = createDto.DayOfWeek,
                    StartDate = createDto.StartDate.ToUniversalTime(),
                    EndDate = createDto.EndDate.ToUniversalTime(),
                    IsDeleted = false,
                    CreatedAt = currentTime,
                    CreatedBy = currentUserId
                };

                await _unitOfWork.Schedule.AddAsync(schedule);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Schedule created successfully: {schedule.Id}");

                return await GetScheduleByIdAsync(schedule.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating schedule: {createDto.Title}");
                throw;
            }
        }

        #endregion

        #region Read

        public async Task<List<ScheduleDto>> GetAllSchedulesAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all schedules");

                var schedules = await _unitOfWork.Schedule
                    .GetAllAsync(
                        predicate: s => !s.IsDeleted,
                        includes: new Expression<Func<Schedule, object>>[] { s => s.Room, s => s.Room.Campus }
                    );

                var scheduleDtos = schedules.Select(MapToDto).ToList();

                _logger.LogInformation($"Retrieved {scheduleDtos.Count} schedules");
                return scheduleDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all schedules");
                throw;
            }
        }

        public async Task<ScheduleDto?> GetScheduleByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Retrieving schedule with ID: {id}");

                var schedule = await _unitOfWork.Schedule
                    .GetByIdAsync(id, s => s.Room, s => s.Room.Campus);

                if (schedule == null || schedule.IsDeleted)
                {
                    _logger.LogWarning($"Schedule not found: {id}");
                    throw ErrorHelper.NotFound($"Schedule with ID '{id}' not found");
                }

                return MapToDto(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving schedule: {id}");
                throw;
            }
        }

        public async Task<List<ScheduleDto>> GetSchedulesByRoomAsync(Guid roomId)
        {
            try
            {
                _logger.LogInformation($"Retrieving schedules for room: {roomId}");

                var schedules = await _unitOfWork.Schedule
                    .GetAllAsync(
                        predicate: s => !s.IsDeleted && s.RoomId == roomId,
                        includes: new Expression<Func<Schedule, object>>[] { s => s.Room, s => s.Room.Campus }
                    );

                return schedules.Select(MapToDto)
                    .OrderBy(s => s.DayOfWeek)
                    .ThenBy(s => s.StartTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving schedules for room: {roomId}");
                throw;
            }
        }

        public async Task<List<ScheduleDto>> GetSchedulesByTypeAsync(ScheduleType type)
        {
            try
            {
                _logger.LogInformation($"Retrieving schedules by type: {type}");

                var schedules = await _unitOfWork.Schedule
                    .GetAllAsync(
                        predicate: s => !s.IsDeleted && s.ScheduleType == type,
                        includes: new Expression<Func<Schedule, object>>[] { s => s.Room, s => s.Room.Campus }
                    );

                return schedules.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving schedules by type: {type}");
                throw;
            }
        }

        public async Task<List<ScheduleDto>> GetSchedulesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation($"Retrieving schedules from {startDate} to {endDate}");

                var startDateUtc = startDate.ToUniversalTime();
                var endDateUtc = endDate.ToUniversalTime();

                var schedules = await _unitOfWork.Schedule
                    .GetAllAsync(
                        predicate: s => !s.IsDeleted &&
                            ((s.StartDate <= endDateUtc && s.EndDate >= startDateUtc)),
                        includes: new Expression<Func<Schedule, object>>[] { s => s.Room, s => s.Room.Campus }
                    );

                return schedules.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schedules by date range");
                throw;
            }
        }

        public async Task<List<ScheduleDto>> GetSchedulesByDayOfWeekAsync(int dayOfWeek)
        {
            try
            {
                _logger.LogInformation($"Retrieving schedules for day of week: {dayOfWeek}");

                if (dayOfWeek < 0 || dayOfWeek > 6)
                {
                    throw ErrorHelper.BadRequest("Day of week must be between 0 (Sunday) and 6 (Saturday)");
                }

                var schedules = await _unitOfWork.Schedule
                    .GetAllAsync(
                        predicate: s => !s.IsDeleted && s.DayOfWeek == dayOfWeek,
                        includes: new Expression<Func<Schedule, object>>[] { s => s.Room, s => s.Room.Campus }
                    );

                return schedules.Select(MapToDto)
                    .OrderBy(s => s.StartTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving schedules for day of week: {dayOfWeek}");
                throw;
            }
        }

        public async Task<List<ScheduleDto>> GetSchedulesForRoomOnDateAsync(Guid roomId, DateTime date)
        {
            try
            {
                _logger.LogInformation($"Retrieving schedules for room {roomId} on {date:yyyy-MM-dd}");

                var dayOfWeek = (int)date.DayOfWeek;
                var dateUtc = date.ToUniversalTime().Date;

                var schedules = await _unitOfWork.Schedule
                    .GetAllAsync(
                        predicate: s => !s.IsDeleted &&
                            s.RoomId == roomId &&
                            s.DayOfWeek == dayOfWeek &&
                            s.StartDate.Date <= dateUtc &&
                            s.EndDate.Date >= dateUtc,
                        includes: new Expression<Func<Schedule, object>>[] { s => s.Room, s => s.Room.Campus }
                    );

                return schedules.Select(MapToDto)
                    .OrderBy(s => s.StartTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving schedules for room {roomId} on date");
                throw;
            }
        }

        public async Task<Pagination<ScheduleDto>> GetSchedulesAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? searchTerm = null,
            Guid? roomId = null,
            ScheduleType? scheduleType = null,
            int? dayOfWeek = null)
        {
            try
            {
                _logger.LogInformation("Retrieving schedules with pagination. Page: {Page}, Size: {Size}", pageNumber, pageSize);

                var query = _unitOfWork.Schedule.GetQueryable()
                    .Where(s => !s.IsDeleted);

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var lowerSearch = searchTerm.ToLower();
                    query = query.Where(s =>
                        s.Title.ToLower().Contains(lowerSearch) ||
                        s.Room.Name.ToLower().Contains(lowerSearch) ||
                        s.Room.Campus.Name.ToLower().Contains(lowerSearch));
                }

                if (roomId.HasValue)
                {
                    query = query.Where(s => s.RoomId == roomId.Value);
                }

                if (scheduleType.HasValue)
                {
                    query = query.Where(s => s.ScheduleType == scheduleType.Value);
                }

                if (dayOfWeek.HasValue)
                {
                    query = query.Where(s => s.DayOfWeek == dayOfWeek.Value);
                }

                // Get total count before pagination
                var totalCount = query.Count();

                // Apply sorting
                query = query.OrderBy(s => s.DayOfWeek)
                    .ThenBy(s => s.StartTime);

                // Apply pagination
                var schedules = query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        Schedule = s,
                        RoomName = s.Room.Name,
                        CampusName = s.Room.Campus.Name
                    })
                    .ToList();

                var scheduleDtos = schedules.Select(s => MapToDto(s.Schedule)).ToList();

                _logger.LogInformation("Retrieved {Count} schedules for page {Page}", scheduleDtos.Count, pageNumber);

                return new Pagination<ScheduleDto>(scheduleDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schedules with pagination");
                throw;
            }
        }

        #endregion

        #region Update

        public async Task<ScheduleDto?> UpdateScheduleAsync(UpdateScheduleDto updateDto)
        {
            try
            {
                _logger.LogInformation($"Updating schedule: {updateDto.Id}");

                var schedule = await _unitOfWork.Schedule.GetByIdAsync(updateDto.Id);

                if (schedule == null || schedule.IsDeleted)
                {
                    _logger.LogWarning($"Schedule not found for update: {updateDto.Id}");
                    throw ErrorHelper.NotFound($"Schedule with ID '{updateDto.Id}' not found");
                }

                // Validate input
                if (updateDto.StartTime >= updateDto.EndTime)
                {
                    throw ErrorHelper.BadRequest("Start time must be before end time");
                }

                if (updateDto.StartDate >= updateDto.EndDate)
                {
                    throw ErrorHelper.BadRequest("Start date must be before end date");
                }

                // Check if room exists
                var room = await _unitOfWork.Room.GetByIdAsync(updateDto.RoomId);
                if (room == null || room.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Room with ID '{updateDto.RoomId}' not found");
                }

                // Check for conflicts (excluding current schedule)
                if (await HasScheduleConflictAsync(
                    updateDto.RoomId,
                    updateDto.DayOfWeek,
                    updateDto.StartTime,
                    updateDto.EndTime,
                    updateDto.StartDate,
                    updateDto.EndDate,
                    updateDto.Id))
                {
                    var conflicts = await GetConflictingSchedulesAsync(
                        updateDto.RoomId,
                        updateDto.DayOfWeek,
                        updateDto.StartTime,
                        updateDto.EndTime,
                        updateDto.StartDate,
                        updateDto.EndDate);

                    var conflictDetails = string.Join(", ", conflicts
                        .Where(s => s.Id != updateDto.Id)
                        .Select(s => $"{s.Title} ({s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm})"));

                    throw ErrorHelper.Conflict($"Schedule conflicts with existing schedule(s): {conflictDetails}");
                }

                var currentUserId = _claimsService.GetCurrentUserId;
                var currentTime = _currentTime.GetCurrentTime();

                schedule.RoomId = updateDto.RoomId;
                schedule.ScheduleType = updateDto.ScheduleType;
                schedule.Title = updateDto.Title.Trim();
                schedule.StartTime = updateDto.StartTime;
                schedule.EndTime = updateDto.EndTime;
                schedule.DayOfWeek = updateDto.DayOfWeek;
                schedule.StartDate = updateDto.StartDate.ToUniversalTime();
                schedule.EndDate = updateDto.EndDate.ToUniversalTime();
                schedule.UpdatedAt = currentTime;
                schedule.UpdatedBy = currentUserId;

                await _unitOfWork.Schedule.Update(schedule);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Schedule updated successfully: {schedule.Id}");

                return await GetScheduleByIdAsync(schedule.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating schedule: {updateDto.Id}");
                throw;
            }
        }

        #endregion

        #region Delete

        public async Task<bool> SoftDeleteScheduleAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Soft deleting schedule: {id}");

                var schedule = await _unitOfWork.Schedule.GetByIdAsync(id);

                if (schedule == null)
                {
                    _logger.LogWarning($"Schedule not found for deletion: {id}");
                    throw ErrorHelper.NotFound($"Schedule with ID '{id}' not found");
                }

                if (schedule.IsDeleted)
                {
                    _logger.LogWarning($"Schedule already deleted: {id}");
                    return false;
                }

                var currentUserId = _claimsService.GetCurrentUserId;
                var currentTime = _currentTime.GetCurrentTime();

                await _unitOfWork.Schedule.SoftRemove(schedule);
                schedule.DeletedAt = currentTime;
                schedule.DeletedBy = currentUserId;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Schedule soft deleted successfully: {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error soft deleting schedule: {id}");
                throw;
            }
        }

        public async Task<bool> DeleteScheduleAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Hard deleting schedule: {id}");

                var schedule = await _unitOfWork.Schedule.GetByIdAsync(id);

                if (schedule == null)
                {
                    _logger.LogWarning($"Schedule not found for deletion: {id}");
                    throw ErrorHelper.NotFound($"Schedule with ID '{id}' not found");
                }

                await _unitOfWork.Schedule.HardRemove(s => s.Id == id);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Schedule hard deleted successfully: {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hard deleting schedule: {id}");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        public async Task<bool> ScheduleExistsAsync(Guid id)
        {
            try
            {
                var schedule = await _unitOfWork.Schedule.GetByIdAsync(id);
                return schedule != null && !schedule.IsDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking schedule existence: {id}");
                return false;
            }
        }

        public async Task<bool> HasScheduleConflictAsync(
            Guid roomId,
            int dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeScheduleId = null)
        {
            try
            {
                var startDateUtc = startDate.ToUniversalTime();
                var endDateUtc = endDate.ToUniversalTime();

                var schedules = await _unitOfWork.Schedule
                    .GetAllAsync(s => !s.IsDeleted &&
                        s.RoomId == roomId &&
                        s.DayOfWeek == dayOfWeek &&
                        s.StartDate <= endDateUtc &&
                        s.EndDate >= startDateUtc);

                if (excludeScheduleId.HasValue)
                {
                    schedules = schedules.Where(s => s.Id != excludeScheduleId.Value).ToList();
                }

                // Check for time overlap
                var hasConflict = schedules.Any(s =>
                    (startTime >= s.StartTime && startTime < s.EndTime) ||
                    (endTime > s.StartTime && endTime <= s.EndTime) ||
                    (startTime <= s.StartTime && endTime >= s.EndTime));

                return hasConflict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking schedule conflict");
                return true; // Return true to be safe
            }
        }

        public async Task<bool> IsTimeSlotAvailableAsync(
            Guid roomId,
            int dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            DateTime startDate,
            DateTime endDate,
            int breakTimeMinutes = 15)
        {
            try
            {
                var startDateUtc = startDate.ToUniversalTime();
                var endDateUtc = endDate.ToUniversalTime();

                var schedules = await _unitOfWork.Schedule
                    .GetAllAsync(s => !s.IsDeleted &&
                        s.RoomId == roomId &&
                        s.DayOfWeek == dayOfWeek &&
                        s.StartDate <= endDateUtc &&
                        s.EndDate >= startDateUtc);

                var breakTime = TimeSpan.FromMinutes(breakTimeMinutes);

                // Check for time overlap with break time buffer
                foreach (var schedule in schedules)
                {
                    // Extend existing schedule times with break time
                    var existingStart = schedule.StartTime - breakTime;
                    var existingEnd = schedule.EndTime + breakTime;

                    // Check if new schedule overlaps with extended times
                    if ((startTime >= existingStart && startTime < existingEnd) ||
                        (endTime > existingStart && endTime <= existingEnd) ||
                        (startTime <= existingStart && endTime >= existingEnd))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking time slot availability");
                return false; // Return false to be safe
            }
        }

        public async Task<List<ScheduleDto>> GetConflictingSchedulesAsync(
            Guid roomId,
            int dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                var startDateUtc = startDate.ToUniversalTime();
                var endDateUtc = endDate.ToUniversalTime();

                var schedules = await _unitOfWork.Schedule
                    .GetAllAsync(
                        predicate: s => !s.IsDeleted &&
                            s.RoomId == roomId &&
                            s.DayOfWeek == dayOfWeek &&
                            s.StartDate <= endDateUtc &&
                            s.EndDate >= startDateUtc,
                        includes: new Expression<Func<Schedule, object>>[] { s => s.Room, s => s.Room.Campus }
                    );

                var conflictingSchedules = schedules.Where(s =>
                    (startTime >= s.StartTime && startTime < s.EndTime) ||
                    (endTime > s.StartTime && endTime <= s.EndTime) ||
                    (startTime <= s.StartTime && endTime >= s.EndTime))
                    .Select(MapToDto)
                    .ToList();

                return conflictingSchedules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conflicting schedules");
                return new List<ScheduleDto>();
            }
        }

        private ScheduleDto MapToDto(Schedule schedule)
        {
            return new ScheduleDto
            {
                Id = schedule.Id,
                RoomId = schedule.RoomId,
                RoomName = schedule.Room?.Name ?? "Unknown",
                CampusName = schedule.Room?.Campus?.Name ?? "Unknown",
                ScheduleType = schedule.ScheduleType,
                ScheduleTypeDisplay = GetScheduleTypeDisplay(schedule.ScheduleType),
                Title = schedule.Title,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                DayOfWeek = schedule.DayOfWeek,
                DayOfWeekDisplay = GetDayOfWeekDisplay(schedule.DayOfWeek),
                StartDate = schedule.StartDate,
                EndDate = schedule.EndDate,
                CreatedAt = schedule.CreatedAt
            };
        }

        private string GetScheduleTypeDisplay(ScheduleType type)
        {
            return type switch
            {
                ScheduleType.Academic_Course => "Academic Course",
                ScheduleType.Recurring_Maintenance => "Recurring Maintenance",
                _ => type.ToString()
            };
        }

        private string GetDayOfWeekDisplay(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                0 => "Sunday",
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                6 => "Saturday",
                _ => "Unknown"
            };
        }

        #endregion
    }
}

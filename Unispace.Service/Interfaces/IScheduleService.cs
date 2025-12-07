using UniSpace.Bo.DTOs.ScheduleDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Service.Utils;

namespace UniSpace.Service.Interfaces
{
    public interface IScheduleService
    {
        // Create
        Task<ScheduleDto?> CreateScheduleAsync(CreateScheduleDto createDto);

        // Read
        Task<List<ScheduleDto>> GetAllSchedulesAsync();
        Task<ScheduleDto?> GetScheduleByIdAsync(Guid id);
        Task<List<ScheduleDto>> GetSchedulesByRoomAsync(Guid roomId);
        Task<List<ScheduleDto>> GetSchedulesByTypeAsync(ScheduleType type);
        Task<List<ScheduleDto>> GetSchedulesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<ScheduleDto>> GetSchedulesByDayOfWeekAsync(int dayOfWeek);
        Task<List<ScheduleDto>> GetSchedulesForRoomOnDateAsync(Guid roomId, DateTime date);

        // Pagination
        Task<Pagination<ScheduleDto>> GetSchedulesAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? searchTerm = null,
            Guid? roomId = null,
            ScheduleType? scheduleType = null,
            int? dayOfWeek = null);

        // Update
        Task<ScheduleDto?> UpdateScheduleAsync(UpdateScheduleDto updateDto);

        // Delete
        Task<bool> SoftDeleteScheduleAsync(Guid id);
        Task<bool> DeleteScheduleAsync(Guid id);

        // Helper Methods
        Task<bool> ScheduleExistsAsync(Guid id);
        Task<bool> HasScheduleConflictAsync(Guid roomId, int dayOfWeek, TimeSpan startTime, TimeSpan endTime, DateTime startDate, DateTime endDate, Guid? excludeScheduleId = null);
        Task<bool> IsTimeSlotAvailableAsync(Guid roomId, int dayOfWeek, TimeSpan startTime, TimeSpan endTime, DateTime startDate, DateTime endDate, int breakTimeMinutes = 15);
        Task<List<ScheduleDto>> GetConflictingSchedulesAsync(Guid roomId, int dayOfWeek, TimeSpan startTime, TimeSpan endTime, DateTime startDate, DateTime endDate);
    }
}

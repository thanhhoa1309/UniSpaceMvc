using UniSpace.Bo.DTOs.RoomReportDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Service.Utils;

namespace UniSpace.Service.Interfaces
{
    public interface IRoomReportService
    {
        // Create
        Task<RoomReportDto?> CreateRoomReportAsync(CreateRoomReportDto createDto);

        // Read
        Task<Pagination<RoomReportDto>> GetRoomReportsAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? searchTerm = null,
            Guid? userId = null,
            Guid? roomId = null,
            Guid? bookingId = null,
            ReportStatus? status = null);

        Task<RoomReportDto?> GetRoomReportByIdAsync(Guid id);
        Task<List<RoomReportDto>> GetUserReportsAsync(Guid userId);
        Task<List<RoomReportDto>> GetRoomReportsAsync(Guid roomId);
        Task<RoomReportDto?> GetReportByBookingIdAsync(Guid bookingId);

        // Update
        Task<RoomReportDto?> UpdateRoomReportAsync(UpdateRoomReportDto updateDto);
        Task<bool> UpdateReportStatusAsync(Guid reportId, ReportStatus status, string? adminResponse = null);

        // Delete
        Task<bool> SoftDeleteRoomReportAsync(Guid id);
        Task<bool> DeleteRoomReportAsync(Guid id);

        // Helper Methods
        Task<bool> ReportExistsAsync(Guid id);
        Task<bool> HasUserReportedBookingAsync(Guid bookingId);
        Task<bool> CanUserReportBookingAsync(Guid userId, Guid bookingId);
        Task<int> GetPendingReportsCountAsync();
        Task<int> GetUserReportsCountAsync(Guid userId);
    }
}

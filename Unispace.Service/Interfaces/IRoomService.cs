using UniSpace.Bo.DTOs.RoomDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Service.Utils;

namespace UniSpace.Service.Interfaces
{
    public interface IRoomService
    {
        // Create
        Task<RoomDto?> CreateRoomAsync(CreateRoomDto createDto);

        // Read - Single unified method with filters
        Task<Pagination<RoomDto>> GetRoomsAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? searchTerm = null,
            Guid? campusId = null,
            RoomType? type = null,
            BookingStatus? status = null,
            DateTime? availableFrom = null,
            DateTime? availableTo = null);

        Task<RoomDto?> GetRoomByIdAsync(Guid id);

        // Update
        Task<RoomDto?> UpdateRoomAsync(UpdateRoomDto updateDto);
        Task<bool> UpdateRoomStatusAsync(Guid roomId, BookingStatus status);

        // Delete
        Task<bool> SoftDeleteRoomAsync(Guid id);
        Task<bool> DeleteRoomAsync(Guid id);

        // Helper Methods
        Task<bool> RoomExistsAsync(Guid id);
        Task<bool> RoomNameExistsInCampusAsync(Guid campusId, string name, Guid? excludeId = null);
        Task<int> GetRoomCapacityAsync(Guid roomId);
        Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime startTime, DateTime endTime);
    }
}

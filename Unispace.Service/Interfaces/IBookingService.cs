using UniSpace.Bo.DTOs.BookingDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Service.Utils;

namespace UniSpace.Service.Interfaces
{
    public interface IBookingService
    {
        // Create
        Task<BookingDto?> CreateBookingAsync(CreateBookingDto createDto);

        // Read
        Task<Pagination<BookingDto>> GetBookingsAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? searchTerm = null,
            Guid? userId = null,
            Guid? roomId = null,
            BookingStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        Task<BookingDto?> GetBookingByIdAsync(Guid id);
        Task<List<BookingDto>> GetUserBookingsAsync(Guid userId);
        Task<List<BookingDto>> GetRoomBookingsAsync(Guid roomId, DateTime? fromDate = null, DateTime? toDate = null);

        // Update
        Task<BookingDto?> UpdateBookingAsync(UpdateBookingDto updateDto);
        Task<bool> CancelBookingAsync(Guid bookingId);
        Task<bool> ApproveBookingAsync(Guid bookingId, string? adminNote = null);
        Task<bool> RejectBookingAsync(Guid bookingId, string adminNote);
        Task<bool> CompleteBookingAsync(Guid bookingId);

        // Delete
        Task<bool> SoftDeleteBookingAsync(Guid id);
        Task<bool> DeleteBookingAsync(Guid id);

        // Helper Methods
        Task<bool> BookingExistsAsync(Guid id);
        Task<bool> CanUserModifyBookingAsync(Guid userId, Guid bookingId);
        Task<bool> IsRoomAvailableForBookingAsync(Guid roomId, DateTime startTime, DateTime endTime, Guid? excludeBookingId = null);
        Task<int> GetUserBookingsCountAsync(Guid userId);
        Task<int> GetPendingBookingsCountAsync();
    }
}

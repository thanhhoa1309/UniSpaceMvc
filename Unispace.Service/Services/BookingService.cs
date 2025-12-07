using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using UniSpace.Bo.DTOs.BookingDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Model.Entities;
using UniSpace.Model.Interfaces;
using UniSpace.Service.Interfaces;
using UniSpace.Service.Utils;


namespace UniSpace.Service.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;
        private readonly ICurrentTime _currentTime;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            IUnitOfWork unitOfWork,
            IClaimsService claimsService,
            ICurrentTime currentTime,
            ILogger<BookingService> logger)
        {
            _unitOfWork = unitOfWork;
            _claimsService = claimsService;
            _currentTime = currentTime;
            _logger = logger;
        }

        #region Create

        public async Task<BookingDto?> CreateBookingAsync(CreateBookingDto createDto)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                _logger.LogInformation($"User {currentUserId} is creating a booking for room {createDto.RoomId}");

                // Validate input
                if (string.IsNullOrWhiteSpace(createDto.Purpose))
                {
                    throw ErrorHelper.BadRequest("Purpose is required");
                }

                if (createDto.StartTime >= createDto.EndTime)
                {
                    throw ErrorHelper.BadRequest("End time must be after start time");
                }

                if (createDto.StartTime < DateTime.UtcNow)
                {
                    throw ErrorHelper.BadRequest("Cannot book rooms in the past");
                }

                // Check if room exists and is active
                var room = await _unitOfWork.Room.GetByIdAsync(createDto.RoomId, r => r.Campus);
                if (room == null || room.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Room with ID '{createDto.RoomId}' not found");
                }

                if (room.RoomStatus != RoomStatus.Active)
                {
                    throw ErrorHelper.BadRequest($"Room is not available for booking. Current status: {room.RoomStatus}");
                }

                // Check if room is available for the requested time
                if (!await IsRoomAvailableForBookingAsync(createDto.RoomId, createDto.StartTime, createDto.EndTime))
                {
                    throw ErrorHelper.Conflict("Room is not available for the selected time period");
                }

                // Check for schedule conflicts
                if (await HasScheduleConflictAsync(createDto.RoomId, createDto.StartTime, createDto.EndTime))
                {
                    throw ErrorHelper.Conflict("The selected time conflicts with the room's schedule (classes or maintenance)");
                }

                var currentTime = _currentTime.GetCurrentTime();

                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUserId,
                    RoomId = createDto.RoomId,
                    StartTime = createDto.StartTime.ToUniversalTime(),
                    EndTime = createDto.EndTime.ToUniversalTime(),
                    Purpose = createDto.Purpose.Trim(),
                    Status = BookingStatus.Pending,
                    AdminNote = string.Empty,
                    IsDeleted = false,
                    CreatedAt = currentTime,
                    CreatedBy = currentUserId
                };

                await _unitOfWork.Booking.AddAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Booking created successfully: {booking.Id}");

                return await GetBookingByIdAsync(booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating booking for room: {createDto.RoomId}");
                throw;
            }
        }

        #endregion

        #region Read

        public async Task<Pagination<BookingDto>> GetBookingsAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? searchTerm = null,
            Guid? userId = null,
            Guid? roomId = null,
            BookingStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                _logger.LogInformation($"Retrieving bookings - Page {pageNumber}, Size {pageSize}");

                IQueryable<Booking> query = _unitOfWork.Booking.GetQueryable()
                    .Where(b => !b.IsDeleted)
                    .Include(b => b.User)
                    .Include(b => b.Room)
                        .ThenInclude(r => r.Campus);

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var lowerSearch = searchTerm.ToLower();
                    query = query.Where(b =>
                        b.Purpose.ToLower().Contains(lowerSearch) ||
                        b.Room.Name.ToLower().Contains(lowerSearch) ||
                        b.User.FullName.ToLower().Contains(lowerSearch));
                }

                // Apply filters
                if (userId.HasValue)
                {
                    query = query.Where(b => b.UserId == userId.Value);
                }

                if (roomId.HasValue)
                {
                    query = query.Where(b => b.RoomId == roomId.Value);
                }

                if (status.HasValue)
                {
                    query = query.Where(b => b.Status == status.Value);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(b => b.StartTime >= fromDate.Value.ToUniversalTime());
                }

                if (toDate.HasValue)
                {
                    query = query.Where(b => b.EndTime <= toDate.Value.ToUniversalTime());
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply sorting and pagination
                var bookings = await query
                    .OrderByDescending(b => b.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var bookingDtos = bookings.Select(MapToDto).ToList();

                _logger.LogInformation($"Retrieved {bookingDtos.Count} of {totalCount} bookings");
                return new Pagination<BookingDto>(bookingDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookings with pagination");
                throw;
            }
        }

        public async Task<BookingDto?> GetBookingByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Retrieving booking with ID: {id}");

                var booking = await _unitOfWork.Booking.GetByIdAsync(
                    id,
                    b => b.User,
                    b => b.Room,
                    b => b.Room.Campus);

                if (booking == null || booking.IsDeleted)
                {
                    _logger.LogWarning($"Booking not found: {id}");
                    throw ErrorHelper.NotFound($"Booking with ID '{id}' not found");
                }

                return MapToDto(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving booking: {id}");
                throw;
            }
        }

        public async Task<List<BookingDto>> GetUserBookingsAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation($"Retrieving bookings for user: {userId}");

                var bookings = await _unitOfWork.Booking.GetAllAsync(
                    predicate: b => !b.IsDeleted && b.UserId == userId,
                    includes: new Expression<Func<Booking, object>>[]
                    {
                        b => b.User,
                        b => b.Room,
                        b => b.Room.Campus
                    });

                return bookings.OrderByDescending(b => b.StartTime)
                    .Select(MapToDto)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bookings for user: {userId}");
                throw;
            }
        }

        public async Task<List<BookingDto>> GetRoomBookingsAsync(Guid roomId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                _logger.LogInformation($"Retrieving bookings for room: {roomId}");

                Expression<Func<Booking, bool>> predicate = b => !b.IsDeleted && b.RoomId == roomId;

                if (fromDate.HasValue && toDate.HasValue)
                {
                    var fromUtc = fromDate.Value.ToUniversalTime();
                    var toUtc = toDate.Value.ToUniversalTime();
                    predicate = b => !b.IsDeleted && b.RoomId == roomId &&
                                   b.StartTime >= fromUtc && b.EndTime <= toUtc;
                }

                var bookings = await _unitOfWork.Booking.GetAllAsync(
                    predicate: predicate,
                    includes: new Expression<Func<Booking, object>>[]
                    {
                        b => b.User,
                        b => b.Room,
                        b => b.Room.Campus
                    });

                return bookings.OrderBy(b => b.StartTime)
                    .Select(MapToDto)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bookings for room: {roomId}");
                throw;
            }
        }

        #endregion

        #region Update

        public async Task<BookingDto?> UpdateBookingAsync(UpdateBookingDto updateDto)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                _logger.LogInformation($"User {currentUserId} is updating booking: {updateDto.Id}");

                var booking = await _unitOfWork.Booking.GetByIdAsync(updateDto.Id);

                if (booking == null || booking.IsDeleted)
                {
                    _logger.LogWarning($"Booking not found for update: {updateDto.Id}");
                    throw ErrorHelper.NotFound($"Booking with ID '{updateDto.Id}' not found");
                }

                // Only the user who created the booking can update it
                if (booking.UserId != currentUserId)
                {
                    throw ErrorHelper.Forbidden("You can only update your own bookings");
                }

                // Only pending bookings can be updated
                if (booking.Status != BookingStatus.Pending)
                {
                    throw ErrorHelper.BadRequest($"Cannot update booking with status: {booking.Status}");
                }

                // Validate time
                if (updateDto.StartTime >= updateDto.EndTime)
                {
                    throw ErrorHelper.BadRequest("End time must be after start time");
                }

                if (updateDto.StartTime < DateTime.UtcNow)
                {
                    throw ErrorHelper.BadRequest("Cannot book rooms in the past");
                }

                // Check if room is available for the new time (excluding this booking)
                if (!await IsRoomAvailableForBookingAsync(booking.RoomId, updateDto.StartTime, updateDto.EndTime, updateDto.Id))
                {
                    throw ErrorHelper.Conflict("Room is not available for the selected time period");
                }

                // Check for schedule conflicts
                if (await HasScheduleConflictAsync(booking.RoomId, updateDto.StartTime, updateDto.EndTime))
                {
                    throw ErrorHelper.Conflict("The selected time conflicts with the room's schedule");
                }

                var currentTime = _currentTime.GetCurrentTime();

                booking.StartTime = updateDto.StartTime.ToUniversalTime();
                booking.EndTime = updateDto.EndTime.ToUniversalTime();
                booking.Purpose = updateDto.Purpose.Trim();
                booking.UpdatedAt = currentTime;
                booking.UpdatedBy = currentUserId;

                await _unitOfWork.Booking.Update(booking);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Booking updated successfully: {booking.Id}");

                return await GetBookingByIdAsync(booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating booking: {updateDto.Id}");
                throw;
            }
        }

        public async Task<bool> CancelBookingAsync(Guid bookingId)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                _logger.LogInformation($"User {currentUserId} is cancelling booking: {bookingId}");

                var booking = await _unitOfWork.Booking.GetByIdAsync(bookingId);

                if (booking == null || booking.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Booking with ID '{bookingId}' not found");
                }

                // Only the user who created the booking can cancel it
                if (booking.UserId != currentUserId)
                {
                    throw ErrorHelper.Forbidden("You can only cancel your own bookings");
                }

                // Can only cancel pending or approved bookings
                if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Approved)
                {
                    throw ErrorHelper.BadRequest($"Cannot cancel booking with status: {booking.Status}");
                }

                var currentTime = _currentTime.GetCurrentTime();

                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = currentTime;
                booking.UpdatedBy = currentUserId;

                await _unitOfWork.Booking.Update(booking);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Booking cancelled successfully: {bookingId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling booking: {bookingId}");
                throw;
            }
        }

        public async Task<bool> ApproveBookingAsync(Guid bookingId, string? adminNote = null)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                _logger.LogInformation($"Admin {currentUserId} is approving booking: {bookingId}");

                var booking = await _unitOfWork.Booking.GetByIdAsync(bookingId);

                if (booking == null || booking.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Booking with ID '{bookingId}' not found");
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    throw ErrorHelper.BadRequest($"Can only approve pending bookings");
                }

                var currentTime = _currentTime.GetCurrentTime();

                booking.Status = BookingStatus.Approved;
                booking.AdminNote = adminNote?.Trim() ?? string.Empty;
                booking.UpdatedAt = currentTime;
                booking.UpdatedBy = currentUserId;

                await _unitOfWork.Booking.Update(booking);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Booking approved successfully: {bookingId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving booking: {bookingId}");
                throw;
            }
        }

        public async Task<bool> RejectBookingAsync(Guid bookingId, string adminNote)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                _logger.LogInformation($"Admin {currentUserId} is rejecting booking: {bookingId}");

                var booking = await _unitOfWork.Booking.GetByIdAsync(bookingId);

                if (booking == null || booking.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Booking with ID '{bookingId}' not found");
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    throw ErrorHelper.BadRequest($"Can only reject pending bookings");
                }

                if (string.IsNullOrWhiteSpace(adminNote))
                {
                    throw ErrorHelper.BadRequest("Admin note is required when rejecting a booking");
                }

                var currentTime = _currentTime.GetCurrentTime();

                booking.Status = BookingStatus.Rejected;
                booking.AdminNote = adminNote.Trim();
                booking.UpdatedAt = currentTime;
                booking.UpdatedBy = currentUserId;

                await _unitOfWork.Booking.Update(booking);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Booking rejected successfully: {bookingId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting booking: {bookingId}");
                throw;
            }
        }

        public async Task<bool> CompleteBookingAsync(Guid bookingId)
        {
            try
            {
                _logger.LogInformation($"Completing booking: {bookingId}");

                var booking = await _unitOfWork.Booking.GetByIdAsync(bookingId);

                if (booking == null || booking.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Booking with ID '{bookingId}' not found");
                }

                if (booking.Status != BookingStatus.Approved)
                {
                    throw ErrorHelper.BadRequest($"Can only complete approved bookings");
                }

                var currentTime = _currentTime.GetCurrentTime();

                booking.Status = BookingStatus.Completed;
                booking.UpdatedAt = currentTime;

                await _unitOfWork.Booking.Update(booking);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Booking completed successfully: {bookingId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error completing booking: {bookingId}");
                throw;
            }
        }

        #endregion

        #region Delete

        public async Task<bool> SoftDeleteBookingAsync(Guid id)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                _logger.LogInformation($"Soft deleting booking: {id}");

                var booking = await _unitOfWork.Booking.GetByIdAsync(id);

                if (booking == null)
                {
                    _logger.LogWarning($"Booking not found for deletion: {id}");
                    throw ErrorHelper.NotFound($"Booking with ID '{id}' not found");
                }

                if (booking.IsDeleted)
                {
                    _logger.LogWarning($"Booking already deleted: {id}");
                    return false;
                }

                var currentTime = _currentTime.GetCurrentTime();

                await _unitOfWork.Booking.SoftRemove(booking);
                booking.DeletedAt = currentTime;
                booking.DeletedBy = currentUserId;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Booking soft deleted successfully: {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error soft deleting booking: {id}");
                throw;
            }
        }

        public async Task<bool> DeleteBookingAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Hard deleting booking: {id}");

                var booking = await _unitOfWork.Booking.GetByIdAsync(id, b => b.RoomReports);

                if (booking == null)
                {
                    _logger.LogWarning($"Booking not found for deletion: {id}");
                    throw ErrorHelper.NotFound($"Booking with ID '{id}' not found");
                }

                // Check if booking has reports
                if (booking.RoomReports != null && booking.RoomReports.Any())
                {
                    _logger.LogWarning($"Cannot delete booking with reports: {id}");
                    throw ErrorHelper.BadRequest("Cannot delete booking that has reports. Use soft delete instead.");
                }

                await _unitOfWork.Booking.HardRemove(b => b.Id == id);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Booking hard deleted successfully: {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hard deleting booking: {id}");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        public async Task<bool> BookingExistsAsync(Guid id)
        {
            try
            {
                var booking = await _unitOfWork.Booking.GetByIdAsync(id);
                return booking != null && !booking.IsDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking booking existence: {id}");
                return false;
            }
        }

        public async Task<bool> CanUserModifyBookingAsync(Guid userId, Guid bookingId)
        {
            try
            {
                var booking = await _unitOfWork.Booking.GetByIdAsync(bookingId);

                if (booking == null || booking.IsDeleted)
                {
                    return false;
                }

                // User can only modify their own pending bookings
                return booking.UserId == userId && booking.Status == BookingStatus.Pending;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if user can modify booking: {bookingId}");
                return false;
            }
        }

        public async Task<bool> IsRoomAvailableForBookingAsync(Guid roomId, DateTime startTime, DateTime endTime, Guid? excludeBookingId = null)
        {
            try
            {
                var startUtc = startTime.ToUniversalTime();
                var endUtc = endTime.ToUniversalTime();

                var bookings = await _unitOfWork.Booking.GetAllAsync(
                    predicate: b => !b.IsDeleted &&
                                   b.RoomId == roomId &&
                                   (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved));

                if (excludeBookingId.HasValue)
                {
                    bookings = bookings.Where(b => b.Id != excludeBookingId.Value).ToList();
                }

                // Check for overlapping bookings
                var hasConflict = bookings.Any(b =>
                    (startUtc >= b.StartTime && startUtc < b.EndTime) ||
                    (endUtc > b.StartTime && endUtc <= b.EndTime) ||
                    (startUtc <= b.StartTime && endUtc >= b.EndTime));

                return !hasConflict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking room availability: {roomId}");
                return false;
            }
        }

        private async Task<bool> HasScheduleConflictAsync(Guid roomId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var schedules = await _unitOfWork.Schedule.GetAllAsync(
                    predicate: s => !s.IsDeleted && s.RoomId == roomId);

                var startUtc = startTime.ToUniversalTime();
                var endUtc = endTime.ToUniversalTime();

                foreach (var schedule in schedules)
                {
                    // Check if booking date falls within schedule date range
                    if (startUtc.Date < schedule.StartDate.Date || endUtc.Date > schedule.EndDate.Date)
                        continue;

                    // Check if booking day matches schedule day
                    if ((int)startUtc.DayOfWeek != schedule.DayOfWeek)
                        continue;

                    // Check if booking time overlaps with schedule time
                    var bookingStartTime = startUtc.TimeOfDay;
                    var bookingEndTime = endUtc.TimeOfDay;

                    if ((bookingStartTime >= schedule.StartTime && bookingStartTime < schedule.EndTime) ||
                        (bookingEndTime > schedule.StartTime && bookingEndTime <= schedule.EndTime) ||
                        (bookingStartTime <= schedule.StartTime && bookingEndTime >= schedule.EndTime))
                    {
                        return true; // Has conflict
                    }
                }

                return false; // No conflict
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking schedule conflict for room: {roomId}");
                return true; // Return true (has conflict) to be safe
            }
        }

        public async Task<int> GetUserBookingsCountAsync(Guid userId)
        {
            try
            {
                var bookings = await _unitOfWork.Booking.GetAllAsync(
                    predicate: b => !b.IsDeleted && b.UserId == userId);

                return bookings.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user bookings count: {userId}");
                return 0;
            }
        }

        public async Task<int> GetPendingBookingsCountAsync()
        {
            try
            {
                var bookings = await _unitOfWork.Booking.GetAllAsync(
                    predicate: b => !b.IsDeleted && b.Status == BookingStatus.Pending);

                return bookings.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending bookings count");
                return 0;
            }
        }

        private BookingDto MapToDto(Booking booking)
        {
            return new BookingDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                UserName = booking.User?.FullName ?? "Unknown",
                UserEmail = booking.User?.Email ?? "Unknown",
                RoomId = booking.RoomId,
                RoomName = booking.Room?.Name ?? "Unknown",
                CampusName = booking.Room?.Campus?.Name ?? "Unknown",
                RoomType = booking.Room?.Type ?? RoomType.Classroom,
                RoomTypeDisplay = GetRoomTypeDisplay(booking.Room?.Type ?? RoomType.Classroom),
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status,
                StatusDisplay = GetStatusDisplay(booking.Status),
                Purpose = booking.Purpose,
                AdminNote = booking.AdminNote,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt
            };
        }

        private string GetRoomTypeDisplay(RoomType type)
        {
            return type switch
            {
                RoomType.Classroom => "Classroom",
                RoomType.Lab => "Laboratory",
                RoomType.Stadium => "Stadium",
                _ => type.ToString()
            };
        }

        private string GetStatusDisplay(BookingStatus status)
        {
            return status switch
            {
                BookingStatus.Pending => "Pending",
                BookingStatus.Approved => "Approved",
                BookingStatus.Rejected => "Rejected",
                BookingStatus.Completed => "Completed",
                BookingStatus.Cancelled => "Cancelled",
                _ => status.ToString()
            };
        }

        #endregion
    }
}

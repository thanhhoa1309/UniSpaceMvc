using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniSpace.Bo.DTOs.RoomDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Model.Entities;
using UniSpace.Model.Interfaces;
using UniSpace.Service.Interfaces;
using UniSpace.Service.Utils;


namespace UniSpace.Service.Services
{
    public class RoomService : IRoomService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;
        private readonly ICurrentTime _currentTime;
        private readonly ILogger<RoomService> _logger;

        public RoomService(
            IUnitOfWork unitOfWork,
            IClaimsService claimsService,
            ICurrentTime currentTime,
            ILogger<RoomService> logger)
        {
            _unitOfWork = unitOfWork;
            _claimsService = claimsService;
            _currentTime = currentTime;
            _logger = logger;
        }

        #region Create

        public async Task<RoomDto?> CreateRoomAsync(CreateRoomDto createDto)
        {
            try
            {
                _logger.LogInformation($"Creating new room: {createDto.Name} in campus: {createDto.CampusId}");

                // Validate input
                if (string.IsNullOrWhiteSpace(createDto.Name))
                {
                    throw ErrorHelper.BadRequest("Room name is required");
                }

                // Check if campus exists
                var campus = await _unitOfWork.Campus.GetByIdAsync(createDto.CampusId);
                if (campus == null || campus.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Campus with ID '{createDto.CampusId}' not found");
                }

                // Check if room name already exists in campus
                if (await RoomNameExistsInCampusAsync(createDto.CampusId, createDto.Name))
                {
                    _logger.LogWarning($"Room name already exists in campus: {createDto.Name}");
                    throw ErrorHelper.Conflict($"Room with name '{createDto.Name}' already exists in this campus");
                }

                var currentUserId = _claimsService.GetCurrentUserId;
                var currentTime = _currentTime.GetCurrentTime();

                var room = new Room
                {
                    Id = Guid.NewGuid(),
                    CampusId = createDto.CampusId,
                    Name = createDto.Name.Trim(),
                    Type = createDto.Type,
                    Capacity = createDto.Capacity,
                    CurrentStatus = BookingStatus.Approved, // Available by default
                    RoomStatus = createDto.RoomStatus,
                    Description = createDto.Description?.Trim() ?? string.Empty,
                    IsDeleted = false,
                    CreatedAt = currentTime,
                    CreatedBy = currentUserId
                };

                await _unitOfWork.Room.AddAsync(room);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Room created successfully: {room.Id}");

                return await GetRoomByIdAsync(room.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating room: {createDto.Name}");
                throw;
            }
        }

        #endregion

        #region Read

        public async Task<Pagination<RoomDto>> GetRoomsAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? searchTerm = null,
            Guid? campusId = null,
            RoomType? type = null,
            BookingStatus? status = null,
            DateTime? availableFrom = null,
            DateTime? availableTo = null)
        {
            try
            {
                _logger.LogInformation($"Retrieving rooms - Page {pageNumber}, Size {pageSize}, Filters: Search='{searchTerm}', Campus={campusId}, Type={type}, Status={status}");

                // Start with base query
                IQueryable<Room> query = _unitOfWork.Room
                    .GetQueryable()
                    .Where(r => !r.IsDeleted)
                    .Include(r => r.Campus)
                    .Include(r => r.Bookings)
                    .Include(r => r.Reports);

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(r =>
                        r.Name.Contains(searchTerm) ||
                        r.Description.Contains(searchTerm) ||
                        r.Campus.Name.Contains(searchTerm));
                }

                // Apply campus filter
                if (campusId.HasValue && campusId != Guid.Empty)
                {
                    query = query.Where(r => r.CampusId == campusId.Value);
                }

                // Apply type filter
                if (type.HasValue)
                {
                    query = query.Where(r => r.Type == type.Value);
                }

                // Apply status filter
                if (status.HasValue)
                {
                    query = query.Where(r => r.CurrentStatus == status.Value);
                }

                // Order by name for consistency
                query = query.OrderBy(r => r.Name);

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var rooms = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to DTOs
                var roomDtos = rooms.Select(MapToDto).ToList();

                // Apply availability filter if specified (post-query filter)
                if (availableFrom.HasValue && availableTo.HasValue)
                {
                    var availableRoomIds = new List<Guid>();

                    foreach (var roomDto in roomDtos)
                    {
                        if (await IsRoomAvailableAsync(roomDto.Id, availableFrom.Value, availableTo.Value))
                        {
                            availableRoomIds.Add(roomDto.Id);
                        }
                    }

                    roomDtos = roomDtos.Where(r => availableRoomIds.Contains(r.Id)).ToList();

                    // Note: When using availability filter, total count might not be accurate
                    // as we're filtering after pagination. For accurate count, we'd need to check
                    // all rooms which would be expensive. This is a trade-off for performance.
                    totalCount = roomDtos.Count;
                }

                _logger.LogInformation($"Retrieved {roomDtos.Count} of {totalCount} rooms with applied filters");
                return new Pagination<RoomDto>(roomDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated rooms with filters");
                throw;
            }
        }

        public async Task<RoomDto?> GetRoomByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Retrieving room with ID: {id}");

                var room = await _unitOfWork.Room
                    .GetByIdAsync(id, r => r.Campus, r => r.Bookings, r => r.Reports);

                if (room == null || room.IsDeleted)
                {
                    _logger.LogWarning($"Room not found: {id}");
                    throw ErrorHelper.NotFound($"Room with ID '{id}' not found");
                }

                return MapToDto(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving room: {id}");
                throw;
            }
        }

        #endregion

        #region Update

        public async Task<RoomDto?> UpdateRoomAsync(UpdateRoomDto updateDto)
        {
            try
            {
                _logger.LogInformation($"Updating room: {updateDto.Id}");

                var room = await _unitOfWork.Room.GetByIdAsync(updateDto.Id);

                if (room == null || room.IsDeleted)
                {
                    _logger.LogWarning($"Room not found for update: {updateDto.Id}");
                    throw ErrorHelper.NotFound($"Room with ID '{updateDto.Id}' not found");
                }

                // Check if campus exists
                var campus = await _unitOfWork.Campus.GetByIdAsync(updateDto.CampusId);
                if (campus == null || campus.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Campus with ID '{updateDto.CampusId}' not found");
                }

                // Check if new name conflicts with existing room in campus
                if (await RoomNameExistsInCampusAsync(updateDto.CampusId, updateDto.Name, updateDto.Id))
                {
                    _logger.LogWarning($"Room name already exists in campus: {updateDto.Name}");
                    throw ErrorHelper.Conflict($"Room with name '{updateDto.Name}' already exists in this campus");
                }

                var currentUserId = _claimsService.GetCurrentUserId;
                var currentTime = _currentTime.GetCurrentTime();

                room.CampusId = updateDto.CampusId;
                room.Name = updateDto.Name.Trim();
                room.Type = updateDto.Type;
                room.Capacity = updateDto.Capacity;
                room.CurrentStatus = updateDto.CurrentStatus;
                room.RoomStatus = updateDto.RoomStatus;
                room.Description = updateDto.Description?.Trim() ?? string.Empty;
                room.UpdatedAt = currentTime;
                room.UpdatedBy = currentUserId;

                await _unitOfWork.Room.Update(room);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Room updated successfully: {room.Id}");

                return await GetRoomByIdAsync(room.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating room: {updateDto.Id}");
                throw;
            }
        }

        public async Task<bool> UpdateRoomStatusAsync(Guid roomId, BookingStatus status)
        {
            try
            {
                _logger.LogInformation($"Updating room status: {roomId} to {status}");

                var room = await _unitOfWork.Room.GetByIdAsync(roomId);

                if (room == null || room.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Room with ID '{roomId}' not found");
                }

                var currentUserId = _claimsService.GetCurrentUserId;
                var currentTime = _currentTime.GetCurrentTime();

                room.CurrentStatus = status;
                room.UpdatedAt = currentTime;
                room.UpdatedBy = currentUserId;

                await _unitOfWork.Room.Update(room);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Room status updated successfully: {roomId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating room status: {roomId}");
                throw;
            }
        }

        #endregion

        #region Delete

        public async Task<bool> SoftDeleteRoomAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Soft deleting room: {id}");

                var room = await _unitOfWork.Room.GetByIdAsync(id, r => r.Bookings);

                if (room == null)
                {
                    _logger.LogWarning($"Room not found for deletion: {id}");
                    throw ErrorHelper.NotFound($"Room with ID '{id}' not found");
                }

                if (room.IsDeleted)
                {
                    _logger.LogWarning($"Room already deleted: {id}");
                    return false;
                }

                // Check if room has active bookings
                var hasActiveBookings = room.Bookings?.Any(b =>
                    !b.IsDeleted &&
                    (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved) &&
                    b.EndTime > DateTime.UtcNow) ?? false;

                if (hasActiveBookings)
                {
                    throw ErrorHelper.BadRequest("Cannot delete room with active bookings. Cancel bookings first.");
                }

                var currentUserId = _claimsService.GetCurrentUserId;
                var currentTime = _currentTime.GetCurrentTime();

                await _unitOfWork.Room.SoftRemove(room);
                room.DeletedAt = currentTime;
                room.DeletedBy = currentUserId;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Room soft deleted successfully: {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error soft deleting room: {id}");
                throw;
            }
        }

        public async Task<bool> DeleteRoomAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Hard deleting room: {id}");

                var room = await _unitOfWork.Room.GetByIdAsync(id, r => r.Bookings, r => r.Reports);

                if (room == null)
                {
                    _logger.LogWarning($"Room not found for deletion: {id}");
                    throw ErrorHelper.NotFound($"Room with ID '{id}' not found");
                }

                // Check if room has any bookings or reports
                if ((room.Bookings != null && room.Bookings.Any()) ||
                    (room.Reports != null && room.Reports.Any()))
                {
                    _logger.LogWarning($"Cannot delete room with existing bookings or reports: {id}");
                    throw ErrorHelper.BadRequest("Cannot delete room that has bookings or reports. Use soft delete instead.");
                }

                await _unitOfWork.Room.HardRemove(r => r.Id == id);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Room hard deleted successfully: {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hard deleting room: {id}");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        public async Task<bool> RoomExistsAsync(Guid id)
        {
            try
            {
                var room = await _unitOfWork.Room.GetByIdAsync(id);
                return room != null && !room.IsDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking room existence: {id}");
                return false;
            }
        }

        public async Task<bool> RoomNameExistsInCampusAsync(Guid campusId, string name, Guid? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                var rooms = await _unitOfWork.Room
                    .GetAllAsync(r => !r.IsDeleted &&
                                     r.CampusId == campusId &&
                                     r.Name.ToLower() == name.ToLower().Trim());

                if (excludeId.HasValue)
                {
                    rooms = rooms.Where(r => r.Id != excludeId.Value).ToList();
                }

                return rooms.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking room name existence: {name}");
                return false;
            }
        }

        public async Task<int> GetRoomCapacityAsync(Guid roomId)
        {
            try
            {
                var room = await _unitOfWork.Room.GetByIdAsync(roomId);
                return room?.Capacity ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting room capacity: {roomId}");
                return 0;
            }
        }

        public async Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var room = await _unitOfWork.Room.GetByIdAsync(roomId, r => r.Bookings);

                if (room == null || room.IsDeleted || room.CurrentStatus != BookingStatus.Approved)
                {
                    return false;
                }

                // Check for overlapping bookings
                var hasConflict = room.Bookings?.Any(b =>
                    !b.IsDeleted &&
                    (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved) &&
                    ((startTime >= b.StartTime && startTime < b.EndTime) ||
                     (endTime > b.StartTime && endTime <= b.EndTime) ||
                     (startTime <= b.StartTime && endTime >= b.EndTime))) ?? false;

                return !hasConflict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking room availability: {roomId}");
                return false;
            }
        }

        private RoomDto MapToDto(Room room)
        {
            return new RoomDto
            {
                Id = room.Id,
                CampusId = room.CampusId,
                CampusName = room.Campus?.Name ?? "Unknown",
                Name = room.Name,
                Type = room.Type,
                TypeDisplay = GetRoomTypeDisplay(room.Type),
                Capacity = room.Capacity,
                CurrentStatus = room.CurrentStatus,
                CurrentStatusDisplay = GetStatusDisplay(room.CurrentStatus),
                RoomStatus = room.RoomStatus,
                RoomStatusDisplay = GetRoomStatusDisplay(room.RoomStatus),
                Description = room.Description,
                TotalBookings = room.Bookings?.Count(b => !b.IsDeleted) ?? 0,
                PendingReports = room.Reports?.Count(r => !r.IsDeleted && r.Status == ReportStatus.Open) ?? 0,
                CreatedAt = room.CreatedAt
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
                BookingStatus.Approved => "Available",
                BookingStatus.Rejected => "Unavailable",
                BookingStatus.Completed => "Completed",
                BookingStatus.Cancelled => "Cancelled",
                _ => status.ToString()
            };
        }

        private string GetRoomStatusDisplay(RoomStatus status)
        {
            return status switch
            {
                RoomStatus.Active => "Active",
                RoomStatus.UnderMaintenance => "Under Maintenance",
                RoomStatus.OutOfService => "Out of Service",
                RoomStatus.Renovating => "Renovating",
                _ => status.ToString()
            };
        }

        #endregion
    }
}

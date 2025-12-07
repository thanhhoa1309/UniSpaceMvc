using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using UniSpace.Bo.DTOs.RoomReportDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Model.Entities;
using UniSpace.Model.Interfaces;
using UniSpace.Service.Interfaces;
using UniSpace.Service.Utils;


namespace UniSpace.Service.Services
{
    public class RoomReportService : IRoomReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;
        private readonly ICurrentTime _currentTime;
        private readonly ILogger<RoomReportService> _logger;

        public RoomReportService(
            IUnitOfWork unitOfWork,
            IClaimsService claimsService,
            ICurrentTime currentTime,
            ILogger<RoomReportService> logger)
        {
            _unitOfWork = unitOfWork;
            _claimsService = claimsService;
            _currentTime = currentTime;
            _logger = logger;
        }

        #region Create

        public async Task<RoomReportDto?> CreateRoomReportAsync(CreateRoomReportDto createDto)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                _logger.LogInformation($"User {currentUserId} is creating a room report for booking {createDto.BookingId}");

                // Validate input
                if (string.IsNullOrWhiteSpace(createDto.IssueType))
                {
                    throw ErrorHelper.BadRequest("Issue type is required");
                }

                if (string.IsNullOrWhiteSpace(createDto.Description))
                {
                    throw ErrorHelper.BadRequest("Description is required");
                }

                // Check if booking exists
                var booking = await _unitOfWork.Booking.GetByIdAsync(
                    createDto.BookingId,
                    b => b.User,
                    b => b.Room,
                    b => b.Room.Campus);

                if (booking == null || booking.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Booking with ID '{createDto.BookingId}' not found");
                }

                // Check if user owns the booking
                if (booking.UserId != currentUserId)
                {
                    throw ErrorHelper.Forbidden("You can only report issues for your own bookings");
                }

                // Check if booking is completed or approved
                if (booking.Status != BookingStatus.Completed && booking.Status != BookingStatus.Approved)
                {
                    throw ErrorHelper.BadRequest("You can only report issues for completed or approved bookings");
                }

                // Check if booking already has a report
                if (await HasUserReportedBookingAsync(createDto.BookingId))
                {
                    throw ErrorHelper.Conflict("This booking has already been reported. Each booking can only be reported once.");
                }

                var currentTime = _currentTime.GetCurrentTime();

                var report = new RoomReport
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUserId,
                    RoomId = booking.RoomId,
                    BookingId = createDto.BookingId,
                    IssueType = createDto.IssueType.Trim(),
                    Description = createDto.Description.Trim(),
                    Status = ReportStatus.Open,
                    IsDeleted = false,
                    CreatedAt = currentTime,
                    CreatedBy = currentUserId
                };

                await _unitOfWork.RoomReport.AddAsync(report);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Room report created successfully: {report.Id}");

                return await GetRoomReportByIdAsync(report.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating room report for booking: {createDto.BookingId}");
                throw;
            }
        }

        #endregion

        #region Read

        public async Task<Pagination<RoomReportDto>> GetRoomReportsAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? searchTerm = null,
            Guid? userId = null,
            Guid? roomId = null,
            Guid? bookingId = null,
            ReportStatus? status = null)
        {
            try
            {
                _logger.LogInformation($"Retrieving room reports - Page {pageNumber}, Size {pageSize}");

                IQueryable<RoomReport> query = _unitOfWork.RoomReport.GetQueryable()
                    .Where(r => !r.IsDeleted)
                    .Include(r => r.User)
                    .Include(r => r.Room)
                        .ThenInclude(room => room.Campus)
                    .Include(r => r.Booking);

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var lowerSearch = searchTerm.ToLower();
                    query = query.Where(r =>
                        r.IssueType.ToLower().Contains(lowerSearch) ||
                        r.Description.ToLower().Contains(lowerSearch) ||
                        r.Room.Name.ToLower().Contains(lowerSearch) ||
                        r.User.FullName.ToLower().Contains(lowerSearch));
                }

                // Apply filters
                if (userId.HasValue)
                {
                    query = query.Where(r => r.UserId == userId.Value);
                }

                if (roomId.HasValue)
                {
                    query = query.Where(r => r.RoomId == roomId.Value);
                }

                if (bookingId.HasValue)
                {
                    query = query.Where(r => r.BookingId == bookingId.Value);
                }

                if (status.HasValue)
                {
                    query = query.Where(r => r.Status == status.Value);
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply sorting and pagination
                var reports = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var reportDtos = reports.Select(MapToDto).ToList();

                _logger.LogInformation($"Retrieved {reportDtos.Count} of {totalCount} room reports");
                return new Pagination<RoomReportDto>(reportDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving room reports with pagination");
                throw;
            }
        }

        public async Task<RoomReportDto?> GetRoomReportByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Retrieving room report with ID: {id}");

                var report = await _unitOfWork.RoomReport.GetByIdAsync(
                    id,
                    r => r.User,
                    r => r.Room,
                    r => r.Room.Campus,
                    r => r.Booking);

                if (report == null || report.IsDeleted)
                {
                    _logger.LogWarning($"Room report not found: {id}");
                    throw ErrorHelper.NotFound($"Room report with ID '{id}' not found");
                }

                return MapToDto(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving room report: {id}");
                throw;
            }
        }

        public async Task<List<RoomReportDto>> GetUserReportsAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation($"Retrieving reports for user: {userId}");

                var reports = await _unitOfWork.RoomReport.GetAllAsync(
                    predicate: r => !r.IsDeleted && r.UserId == userId,
                    includes: new Expression<Func<RoomReport, object>>[]
                    {
                        r => r.User,
                        r => r.Room,
                        r => r.Room.Campus,
                        r => r.Booking
                    });

                return reports.OrderByDescending(r => r.CreatedAt)
                    .Select(MapToDto)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving reports for user: {userId}");
                throw;
            }
        }

        public async Task<List<RoomReportDto>> GetRoomReportsAsync(Guid roomId)
        {
            try
            {
                _logger.LogInformation($"Retrieving reports for room: {roomId}");

                var reports = await _unitOfWork.RoomReport.GetAllAsync(
                    predicate: r => !r.IsDeleted && r.RoomId == roomId,
                    includes: new Expression<Func<RoomReport, object>>[]
                    {
                        r => r.User,
                        r => r.Room,
                        r => r.Room.Campus,
                        r => r.Booking
                    });

                return reports.OrderByDescending(r => r.CreatedAt)
                    .Select(MapToDto)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving reports for room: {roomId}");
                throw;
            }
        }

        public async Task<RoomReportDto?> GetReportByBookingIdAsync(Guid bookingId)
        {
            try
            {
                _logger.LogInformation($"Retrieving report for booking: {bookingId}");

                var report = await _unitOfWork.RoomReport.FirstOrDefaultAsync(
                    predicate: r => !r.IsDeleted && r.BookingId == bookingId,
                    includes: new Expression<Func<RoomReport, object>>[]
                    {
                        r => r.User,
                        r => r.Room,
                        r => r.Room.Campus,
                        r => r.Booking
                    });

                return report != null ? MapToDto(report) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving report for booking: {bookingId}");
                throw;
            }
        }

        #endregion

        #region Update

        public async Task<RoomReportDto?> UpdateRoomReportAsync(UpdateRoomReportDto updateDto)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                _logger.LogInformation($"Updating room report: {updateDto.Id}");

                var report = await _unitOfWork.RoomReport.GetByIdAsync(updateDto.Id);

                if (report == null || report.IsDeleted)
                {
                    _logger.LogWarning($"Room report not found for update: {updateDto.Id}");
                    throw ErrorHelper.NotFound($"Room report with ID '{updateDto.Id}' not found");
                }

                var currentTime = _currentTime.GetCurrentTime();

                report.IssueType = updateDto.IssueType.Trim();
                report.Description = updateDto.Description.Trim();
                report.Status = updateDto.Status;
                report.UpdatedAt = currentTime;
                report.UpdatedBy = currentUserId;

                await _unitOfWork.RoomReport.Update(report);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Room report updated successfully: {report.Id}");

                return await GetRoomReportByIdAsync(report.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating room report: {updateDto.Id}");
                throw;
            }
        }

        public async Task<bool> UpdateReportStatusAsync(Guid reportId, ReportStatus status, string? adminResponse = null)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                _logger.LogInformation($"Updating report status: {reportId} to {status}");

                var report = await _unitOfWork.RoomReport.GetByIdAsync(reportId);

                if (report == null || report.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Room report with ID '{reportId}' not found");
                }

                var currentTime = _currentTime.GetCurrentTime();

                report.Status = status;
                report.UpdatedAt = currentTime;
                report.UpdatedBy = currentUserId;

                await _unitOfWork.RoomReport.Update(report);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Report status updated successfully: {reportId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating report status: {reportId}");
                throw;
            }
        }

        #endregion

        #region Delete

        public async Task<bool> SoftDeleteRoomReportAsync(Guid id)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                _logger.LogInformation($"Soft deleting room report: {id}");

                var report = await _unitOfWork.RoomReport.GetByIdAsync(id);

                if (report == null)
                {
                    _logger.LogWarning($"Room report not found for deletion: {id}");
                    throw ErrorHelper.NotFound($"Room report with ID '{id}' not found");
                }

                if (report.IsDeleted)
                {
                    _logger.LogWarning($"Room report already deleted: {id}");
                    return false;
                }

                var currentTime = _currentTime.GetCurrentTime();

                await _unitOfWork.RoomReport.SoftRemove(report);
                report.DeletedAt = currentTime;
                report.DeletedBy = currentUserId;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Room report soft deleted successfully: {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error soft deleting room report: {id}");
                throw;
            }
        }

        public async Task<bool> DeleteRoomReportAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Hard deleting room report: {id}");

                var report = await _unitOfWork.RoomReport.GetByIdAsync(id);

                if (report == null)
                {
                    _logger.LogWarning($"Room report not found for deletion: {id}");
                    throw ErrorHelper.NotFound($"Room report with ID '{id}' not found");
                }

                await _unitOfWork.RoomReport.HardRemove(r => r.Id == id);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Room report hard deleted successfully: {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hard deleting room report: {id}");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        public async Task<bool> ReportExistsAsync(Guid id)
        {
            try
            {
                var report = await _unitOfWork.RoomReport.GetByIdAsync(id);
                return report != null && !report.IsDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking report existence: {id}");
                return false;
            }
        }

        public async Task<bool> HasUserReportedBookingAsync(Guid bookingId)
        {
            try
            {
                var report = await _unitOfWork.RoomReport.FirstOrDefaultAsync(
                    predicate: r => !r.IsDeleted && r.BookingId == bookingId);

                return report != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if booking has been reported: {bookingId}");
                return false;
            }
        }

        public async Task<bool> CanUserReportBookingAsync(Guid userId, Guid bookingId)
        {
            try
            {
                // Check if booking exists
                var booking = await _unitOfWork.Booking.GetByIdAsync(bookingId);

                if (booking == null || booking.IsDeleted)
                {
                    return false;
                }

                // Check if user owns the booking
                if (booking.UserId != userId)
                {
                    return false;
                }

                // Check if booking is completed or approved
                if (booking.Status != BookingStatus.Completed && booking.Status != BookingStatus.Approved)
                {
                    return false;
                }

                // Check if booking hasn't been reported yet
                if (await HasUserReportedBookingAsync(bookingId))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if user can report booking: {bookingId}");
                return false;
            }
        }

        public async Task<int> GetPendingReportsCountAsync()
        {
            try
            {
                var reports = await _unitOfWork.RoomReport.GetAllAsync(
                    predicate: r => !r.IsDeleted && r.Status == ReportStatus.Open);

                return reports.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending reports count");
                return 0;
            }
        }

        public async Task<int> GetUserReportsCountAsync(Guid userId)
        {
            try
            {
                var reports = await _unitOfWork.RoomReport.GetAllAsync(
                    predicate: r => !r.IsDeleted && r.UserId == userId);

                return reports.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user reports count: {userId}");
                return 0;
            }
        }

        private RoomReportDto MapToDto(RoomReport report)
        {
            return new RoomReportDto
            {
                Id = report.Id,
                UserId = report.UserId,
                UserName = report.User?.FullName ?? "Unknown",
                UserEmail = report.User?.Email ?? "Unknown",
                RoomId = report.RoomId,
                RoomName = report.Room?.Name ?? "Unknown",
                CampusName = report.Room?.Campus?.Name ?? "Unknown",
                BookingId = report.BookingId,
                IssueType = report.IssueType,
                Description = report.Description,
                Status = report.Status,
                StatusDisplay = GetStatusDisplay(report.Status),
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };
        }

        private string GetStatusDisplay(ReportStatus status)
        {
            return status switch
            {
                ReportStatus.Open => "Open",
                ReportStatus.Resolved => "Resolved",
                _ => status.ToString()
            };
        }

        #endregion
    }
}

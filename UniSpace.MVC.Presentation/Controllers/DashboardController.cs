using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSpace.Bo.Enums;
using UniSpace.Service.Interfaces;

namespace UniSpace.MVC.Presentation.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class DashboardController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IRoomReportService _roomReportService;
        private readonly ICampusService _campusService;
        private readonly IRoomService _roomService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IBookingService bookingService,
            IRoomReportService roomReportService,
            ICampusService campusService,
            IRoomService roomService,
            ILogger<DashboardController> logger)
        {
            _bookingService = bookingService;
            _roomReportService = roomReportService;
            _campusService = campusService;
            _roomService = roomService;
            _logger = logger;
        }

        // GET: Dashboard
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get statistics
                var pendingBookingsCount = await _bookingService.GetPendingBookingsCountAsync();
                var pendingReportsCount = await _roomReportService.GetPendingReportsCountAsync();

                // Get pending bookings for confirmation
                var pendingBookings = await _bookingService.GetBookingsAsync(
                    pageNumber: 1,
                    pageSize: 10,
                    status: BookingStatus.Pending);

                // Get recent reports
                var recentReports = await _roomReportService.GetRoomReportsAsync(
                    pageNumber: 1,
                    pageSize: 5,
                    status: ReportStatus.Open);

                // Get all campuses, rooms, and bookings for stats
                var campuses = await _campusService.GetCampusesAsync(pageSize: 1000);
                var rooms = await _roomService.GetRoomsAsync(pageSize: 1000);
                var allBookings = await _bookingService.GetBookingsAsync(pageSize: 1000);

                ViewBag.PendingBookingsCount = pendingBookingsCount;
                ViewBag.PendingReportsCount = pendingReportsCount;
                ViewBag.TotalCampuses = campuses.TotalCount;
                ViewBag.TotalRooms = rooms.TotalCount;
                ViewBag.TotalBookings = allBookings.TotalCount;
                ViewBag.TotalSchedules = 0; // Will calculate if needed
                ViewBag.PendingBookings = pendingBookings;
                ViewBag.RecentReports = recentReports;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["ErrorMessage"] = "Error loading dashboard: " + ex.Message;
                return View();
            }
        }

        // GET: Dashboard/BookingStats
        public async Task<IActionResult> BookingStats(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var from = fromDate ?? DateTime.Now.AddMonths(-1);
                var to = toDate ?? DateTime.Now;

                var bookings = await _bookingService.GetBookingsAsync(
                    pageSize: 1000,
                    fromDate: from,
                    toDate: to);

                var stats = new
                {
                    Total = bookings.TotalCount,
                    Pending = bookings.Count(b => b.Status == BookingStatus.Pending),
                    Approved = bookings.Count(b => b.Status == BookingStatus.Approved),
                    Rejected = bookings.Count(b => b.Status == BookingStatus.Rejected),
                    Completed = bookings.Count(b => b.Status == BookingStatus.Completed),
                    Cancelled = bookings.Count(b => b.Status == BookingStatus.Cancelled)
                };

                ViewBag.Stats = stats;
                ViewBag.FromDate = from;
                ViewBag.ToDate = to;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking stats");
                TempData["ErrorMessage"] = "Error loading statistics: " + ex.Message;
                return View();
            }
        }

        // GET: Dashboard/RoomStats
        public async Task<IActionResult> RoomStats()
        {
            try
            {
                var rooms = await _roomService.GetRoomsAsync(pageSize: 1000);

                var stats = new
                {
                    Total = rooms.TotalCount,
                    Classroom = rooms.Count(r => r.Type == RoomType.Classroom),
                    Lab = rooms.Count(r => r.Type == RoomType.Lab),
                    Stadium = rooms.Count(r => r.Type == RoomType.Stadium),
                    Available = rooms.Count(r => r.CurrentStatus == BookingStatus.Approved),
                    Active = rooms.Count(r => r.RoomStatus == RoomStatus.Active),
                    UnderMaintenance = rooms.Count(r => r.RoomStatus == RoomStatus.UnderMaintenance)
                };

                ViewBag.Stats = stats;
                ViewBag.Rooms = rooms;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading room stats");
                TempData["ErrorMessage"] = "Error loading statistics: " + ex.Message;
                return View();
            }
        }

        // GET: Dashboard/ReportStats
        public async Task<IActionResult> ReportStats()
        {
            try
            {
                var reports = await _roomReportService.GetRoomReportsAsync(pageSize: 1000);

                var stats = new
                {
                    Total = reports.TotalCount,
                    Open = reports.Count(r => r.Status == ReportStatus.Open),
                    Resolved = reports.Count(r => r.Status == ReportStatus.Resolved)
                };

                ViewBag.Stats = stats;
                ViewBag.Reports = reports;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report stats");
                TempData["ErrorMessage"] = "Error loading statistics: " + ex.Message;
                return View();
            }
        }

        // GET: Dashboard/Export
        public async Task<IActionResult> Export(string type, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var from = fromDate ?? DateTime.Now.AddMonths(-1);
                var to = toDate ?? DateTime.Now;

                switch (type.ToLower())
                {
                    case "bookings":
                        var bookings = await _bookingService.GetBookingsAsync(
                            pageSize: 10000,
                            fromDate: from,
                            toDate: to);

                        // TODO: Implement CSV/Excel export
                        TempData["InfoMessage"] = "Export functionality coming soon";
                        break;

                    case "reports":
                        var reports = await _roomReportService.GetRoomReportsAsync(pageSize: 10000);

                        // TODO: Implement CSV/Excel export
                        TempData["InfoMessage"] = "Export functionality coming soon";
                        break;

                    default:
                        TempData["ErrorMessage"] = "Invalid export type";
                        break;
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                TempData["ErrorMessage"] = "Error exporting data: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Dashboard/ApproveBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBooking(Guid id, string? adminNote)
        {
            try
            {
                await _bookingService.ApproveBookingAsync(id, adminNote);
                TempData["SuccessMessage"] = "Booking approved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving booking: {id}");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Dashboard/RejectBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(Guid id, string adminNote)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(adminNote))
                {
                    TempData["ErrorMessage"] = "Admin note is required when rejecting a booking";
                    return RedirectToAction(nameof(Index));
                }

                await _bookingService.RejectBookingAsync(id, adminNote);
                TempData["SuccessMessage"] = "Booking rejected successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting booking: {id}");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSpace.Bo.DTOs.RoomReportDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Service.Interfaces;

namespace UniSpace.MVC.Presentation.Controllers
{
    [Authorize]
    public class RoomReportController : Controller
    {
        private readonly IRoomReportService _roomReportService;
        private readonly IBookingService _bookingService;
        private readonly ILogger<RoomReportController> _logger;

        public RoomReportController(
            IRoomReportService roomReportService,
            IBookingService bookingService,
            ILogger<RoomReportController> logger)
        {
            _roomReportService = roomReportService;
            _bookingService = bookingService;
            _logger = logger;
        }

        // GET: RoomReport
        public async Task<IActionResult> Index(
            int page = 1,
            string? search = null,
            Guid? roomId = null,
            ReportStatus? status = null)
        {
            try
            {
                var reports = await _roomReportService.GetRoomReportsAsync(
                    pageNumber: page,
                    pageSize: 20,
                    searchTerm: search,
                    roomId: roomId,
                    status: status);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = reports.TotalPages;
                ViewBag.Search = search;
                ViewBag.RoomId = roomId;
                ViewBag.Status = status;

                return View(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading room reports");
                TempData["ErrorMessage"] = "Error loading reports: " + ex.Message;
                return View(new List<RoomReportDto>());
            }
        }

        // GET: RoomReport/MyReports
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> MyReports()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var reports = await _roomReportService.GetUserReportsAsync(Guid.Parse(userId));
                return View(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user reports");
                TempData["ErrorMessage"] = "Error loading your reports: " + ex.Message;
                return View(new List<RoomReportDto>());
            }
        }

        // GET: RoomReport/PendingReports
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> PendingReports(int page = 1)
        {
            try
            {
                var reports = await _roomReportService.GetRoomReportsAsync(
                    pageNumber: page,
                    pageSize: 20,
                    status: ReportStatus.Open);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = reports.TotalPages;

                return View(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending reports");
                TempData["ErrorMessage"] = "Error loading pending reports: " + ex.Message;
                return View(new List<RoomReportDto>());
            }
        }

        // GET: RoomReport/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var report = await _roomReportService.GetRoomReportByIdAsync(id);
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading report details: {id}");
                TempData["ErrorMessage"] = "Report not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: RoomReport/Create
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> Create(Guid? bookingId)
        {
            try
            {
                if (!bookingId.HasValue)
                {
                    TempData["ErrorMessage"] = "Booking ID is required";
                    return RedirectToAction("MyBookings", "Booking");
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Check if user can report this booking
                if (!await _roomReportService.CanUserReportBookingAsync(Guid.Parse(userId), bookingId.Value))
                {
                    TempData["ErrorMessage"] = "You cannot report this booking";
                    return RedirectToAction("MyBookings", "Booking");
                }

                // Get booking details
                var booking = await _bookingService.GetBookingByIdAsync(bookingId.Value);
                ViewBag.Booking = booking;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create report page");
                TempData["ErrorMessage"] = "Error loading form";
                return RedirectToAction("MyBookings", "Booking");
            }
        }

        // POST: RoomReport/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> Create(CreateRoomReportDto createDto)
        {
            if (!ModelState.IsValid)
            {
                var booking = await _bookingService.GetBookingByIdAsync(createDto.BookingId);
                ViewBag.Booking = booking;
                return View(createDto);
            }

            try
            {
                var report = await _roomReportService.CreateRoomReportAsync(createDto);
                TempData["SuccessMessage"] = "Report submitted successfully";
                return RedirectToAction(nameof(Details), new { id = report.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                ModelState.AddModelError(string.Empty, ex.Message);
                var booking = await _bookingService.GetBookingByIdAsync(createDto.BookingId);
                ViewBag.Booking = booking;
                return View(createDto);
            }
        }

        // GET: RoomReport/Edit/5
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var report = await _roomReportService.GetRoomReportByIdAsync(id);
                
                // Check if user owns this report
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (report.UserId.ToString() != userId && !User.IsInRole("Admin"))
                {
                    TempData["ErrorMessage"] = "You can only edit your own reports";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var updateDto = new UpdateRoomReportDto
                {
                    Id = report.Id,
                    IssueType = report.IssueType,
                    Description = report.Description,
                    Status = report.Status
                };

                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading report for edit: {id}");
                TempData["ErrorMessage"] = "Report not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: RoomReport/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> Edit(Guid id, UpdateRoomReportDto updateDto)
        {
            if (id != updateDto.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(updateDto);
            }

            try
            {
                await _roomReportService.UpdateRoomReportAsync(updateDto);
                TempData["SuccessMessage"] = "Report updated successfully";
                return RedirectToAction(nameof(Details), new { id = updateDto.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating report: {id}");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(updateDto);
            }
        }

        // POST: RoomReport/Resolve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Resolve(Guid id, string? adminResponse)
        {
            try
            {
                await _roomReportService.UpdateReportStatusAsync(id, ReportStatus.Resolved, adminResponse);
                TempData["SuccessMessage"] = "Report marked as resolved";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resolving report: {id}");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: RoomReport/Reopen/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Reopen(Guid id)
        {
            try
            {
                await _roomReportService.UpdateReportStatusAsync(id, ReportStatus.Open);
                TempData["SuccessMessage"] = "Report reopened";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reopening report: {id}");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: RoomReport/Delete/5
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var report = await _roomReportService.GetRoomReportByIdAsync(id);
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading report for delete: {id}");
                TempData["ErrorMessage"] = "Report not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: RoomReport/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await _roomReportService.SoftDeleteRoomReportAsync(id);
                TempData["SuccessMessage"] = "Report deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting report: {id}");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: RoomReport/Stats
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Stats()
        {
            try
            {
                var pendingCount = await _roomReportService.GetPendingReportsCountAsync();
                ViewBag.PendingCount = pendingCount;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report stats");
                TempData["ErrorMessage"] = "Error loading statistics";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

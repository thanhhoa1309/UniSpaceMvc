using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSpace.Bo.DTOs.BookingDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Service.Interfaces;

namespace UniSpace.MVC.Presentation.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IRoomService _roomService;
        private readonly ICampusService _campusService;
        private readonly ILogger<BookingController> _logger;

        public BookingController(
            IBookingService bookingService,
            IRoomService roomService,
            ICampusService campusService,
            ILogger<BookingController> logger)
        {
            _bookingService = bookingService;
            _roomService = roomService;
            _campusService = campusService;
            _logger = logger;
        }

        // GET: Booking
        public async Task<IActionResult> Index(
            int page = 1,
            string? search = null,
            BookingStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsAsync(
                    pageNumber: page,
                    pageSize: 20,
                    searchTerm: search,
                    status: status,
                    fromDate: fromDate,
                    toDate: toDate);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = bookings.TotalPages;
                ViewBag.Search = search;
                ViewBag.Status = status;
                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;

                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bookings");
                TempData["ErrorMessage"] = "Error loading bookings: " + ex.Message;
                return View(new List<BookingDto>());
            }
        }

        // GET: Booking/MyBookings
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> MyBookings()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var bookings = await _bookingService.GetUserBookingsAsync(Guid.Parse(userId));
                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user bookings");
                TempData["ErrorMessage"] = "Error loading your bookings: " + ex.Message;
                return View(new List<BookingDto>());
            }
        }

        // GET: Booking/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);
                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading booking details: {id}");
                TempData["ErrorMessage"] = "Booking not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Booking/Create
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> Create(Guid? roomId)
        {
            try
            {
                // Load campuses for dropdown
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;

                // If roomId is provided, load room details
                if (roomId.HasValue)
                {
                    var room = await _roomService.GetRoomByIdAsync(roomId.Value);
                    ViewBag.SelectedRoom = room;
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create booking page");
                TempData["ErrorMessage"] = "Error loading form";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> Create(CreateBookingDto createDto)
        {
            if (!ModelState.IsValid)
            {
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;
                return View(createDto);
            }

            try
            {
                var booking = await _bookingService.CreateBookingAsync(createDto);
                TempData["SuccessMessage"] = "Booking created successfully and is pending approval";
                return RedirectToAction(nameof(Details), new { id = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");
                ModelState.AddModelError(string.Empty, ex.Message);
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;
                return View(createDto);
            }
        }

        // GET: Booking/Edit/5
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);
                
                var updateDto = new UpdateBookingDto
                {
                    Id = booking.Id,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    Purpose = booking.Purpose
                };

                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading booking for edit: {id}");
                TempData["ErrorMessage"] = "Booking not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Booking/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> Edit(Guid id, UpdateBookingDto updateDto)
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
                await _bookingService.UpdateBookingAsync(updateDto);
                TempData["SuccessMessage"] = "Booking updated successfully";
                return RedirectToAction(nameof(Details), new { id = updateDto.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating booking: {id}");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(updateDto);
            }
        }

        // POST: Booking/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            try
            {
                await _bookingService.CancelBookingAsync(id);
                TempData["SuccessMessage"] = "Booking cancelled successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling booking: {id}");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(MyBookings));
        }

        // POST: Booking/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Approve(Guid id, string? adminNote)
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

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Booking/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Reject(Guid id, string adminNote)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(adminNote))
                {
                    TempData["ErrorMessage"] = "Admin note is required when rejecting a booking";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _bookingService.RejectBookingAsync(id, adminNote);
                TempData["SuccessMessage"] = "Booking rejected successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting booking: {id}");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Booking/PendingApprovals
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> PendingApprovals(int page = 1)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsAsync(
                    pageNumber: page,
                    pageSize: 20,
                    status: BookingStatus.Pending);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = bookings.TotalPages;

                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending bookings");
                TempData["ErrorMessage"] = "Error loading pending bookings: " + ex.Message;
                return View(new List<BookingDto>());
            }
        }

        // GET: Booking/CheckAvailability
        [HttpGet]
        public async Task<JsonResult> CheckAvailability(Guid roomId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var isAvailable = await _bookingService.IsRoomAvailableForBookingAsync(roomId, startTime, endTime);
                return Json(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking room availability");
                return Json(new { available = false, error = ex.Message });
            }
        }

        // POST: Booking/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _bookingService.SoftDeleteBookingAsync(id);
                TempData["SuccessMessage"] = "Booking deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting booking: {id}");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

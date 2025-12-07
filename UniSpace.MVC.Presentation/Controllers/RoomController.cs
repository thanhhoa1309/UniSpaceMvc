using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSpace.Bo.DTOs.RoomDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Service.Interfaces;

namespace UniSpace.MVC.Presentation.Controllers
{
    public class RoomController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly ICampusService _campusService;
        private readonly ILogger<RoomController> _logger;

        public RoomController(
            IRoomService roomService,
            ICampusService campusService,
            ILogger<RoomController> logger)
        {
            _roomService = roomService;
            _campusService = campusService;
            _logger = logger;
        }

        // GET: Room
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            int page = 1,
            string? search = null,
            Guid? campusId = null,
            RoomType? type = null,
            BookingStatus? status = null)
        {
            try
            {
                var rooms = await _roomService.GetRoomsAsync(
                    pageNumber: page,
                    pageSize: 20,
                    searchTerm: search,
                    campusId: campusId,
                    type: type,
                    status: status);

                // Load campuses for filter dropdown
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = rooms.TotalPages;
                ViewBag.Search = search;
                ViewBag.CampusId = campusId;
                ViewBag.Type = type;
                ViewBag.Status = status;

                return View(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading rooms");
                TempData["ErrorMessage"] = "Error loading rooms: " + ex.Message;
                return View(new List<RoomDto>());
            }
        }

        // GET: Room/Available
        [AllowAnonymous]
        public async Task<IActionResult> Available(
            DateTime? startTime,
            DateTime? endTime,
            int page = 1,
            Guid? campusId = null,
            RoomType? type = null)
        {
            try
            {
                var rooms = await _roomService.GetRoomsAsync(
                    pageNumber: page,
                    pageSize: 20,
                    campusId: campusId,
                    type: type,
                    status: BookingStatus.Approved,
                    availableFrom: startTime,
                    availableTo: endTime);

                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = rooms.TotalPages;
                ViewBag.StartTime = startTime;
                ViewBag.EndTime = endTime;
                ViewBag.CampusId = campusId;
                ViewBag.Type = type;

                return View(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available rooms");
                TempData["ErrorMessage"] = "Error loading available rooms: " + ex.Message;
                return View(new List<RoomDto>());
            }
        }

        // GET: Room/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var room = await _roomService.GetRoomByIdAsync(id);
                return View(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading room details: {id}");
                TempData["ErrorMessage"] = "Room not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Room/Create
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create room page");
                TempData["ErrorMessage"] = "Error loading form";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Room/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Create(CreateRoomDto createDto)
        {
            if (!ModelState.IsValid)
            {
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;
                return View(createDto);
            }

            try
            {
                var room = await _roomService.CreateRoomAsync(createDto);
                TempData["SuccessMessage"] = "Room created successfully";
                return RedirectToAction(nameof(Details), new { id = room.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating room");
                ModelState.AddModelError(string.Empty, ex.Message);
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;
                return View(createDto);
            }
        }

        // GET: Room/Edit/5
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var room = await _roomService.GetRoomByIdAsync(id);
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;

                var updateDto = new UpdateRoomDto
                {
                    Id = room.Id,
                    CampusId = room.CampusId,
                    Name = room.Name,
                    Type = room.Type,
                    Capacity = room.Capacity,
                    CurrentStatus = room.CurrentStatus,
                    RoomStatus = room.RoomStatus,
                    Description = room.Description
                };

                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading room for edit: {id}");
                TempData["ErrorMessage"] = "Room not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Room/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Edit(Guid id, UpdateRoomDto updateDto)
        {
            if (id != updateDto.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;
                return View(updateDto);
            }

            try
            {
                await _roomService.UpdateRoomAsync(updateDto);
                TempData["SuccessMessage"] = "Room updated successfully";
                return RedirectToAction(nameof(Details), new { id = updateDto.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating room: {id}");
                ModelState.AddModelError(string.Empty, ex.Message);
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;
                return View(updateDto);
            }
        }

        // POST: Room/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> UpdateStatus(Guid id, BookingStatus status)
        {
            try
            {
                await _roomService.UpdateRoomStatusAsync(id, status);
                TempData["SuccessMessage"] = "Room status updated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating room status: {id}");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Room/Delete/5
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var room = await _roomService.GetRoomByIdAsync(id);
                return View(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading room for delete: {id}");
                TempData["ErrorMessage"] = "Room not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Room/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await _roomService.SoftDeleteRoomAsync(id);
                TempData["SuccessMessage"] = "Room deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting room: {id}");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Room/CheckAvailability
        [HttpGet]
        public async Task<JsonResult> CheckAvailability(Guid roomId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var isAvailable = await _roomService.IsRoomAvailableAsync(roomId, startTime, endTime);
                return Json(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking room availability");
                return Json(new { available = false, error = ex.Message });
            }
        }
    }
}

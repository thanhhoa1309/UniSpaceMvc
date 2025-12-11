using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSpace.Bo.DTOs.ScheduleDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Service.Interfaces;

namespace UniSpace.MVC.Presentation.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly IScheduleService _scheduleService;
        private readonly IRoomService _roomService;
        private readonly ICampusService _campusService;
        private readonly ILogger<ScheduleController> _logger;

        public ScheduleController(
            IScheduleService scheduleService,
            IRoomService roomService,
            ICampusService campusService,
            ILogger<ScheduleController> logger)
        {
            _scheduleService = scheduleService;
            _roomService = roomService;
            _campusService = campusService;
            _logger = logger;
        }

        // GET: Schedule
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            int page = 1,
            string? search = null,
            Guid? roomId = null,
            ScheduleType? scheduleType = null,
            int? dayOfWeek = null)
        {
            try
            {
                var schedules = await _scheduleService.GetSchedulesAsync(
                    pageNumber: page,
                    pageSize: 20,
                    searchTerm: search,
                    roomId: roomId,
                    scheduleType: scheduleType,
                    dayOfWeek: dayOfWeek);

                // Load campuses for filter
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = schedules.TotalPages;
                ViewBag.Search = search;
                ViewBag.RoomId = roomId;
                ViewBag.ScheduleType = scheduleType;
                ViewBag.DayOfWeek = dayOfWeek;

                return View(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading schedules");
                TempData["ErrorMessage"] = "Error loading schedules: " + ex.Message;
                return View(new List<ScheduleDto>());
            }
        }

        // GET: Schedule/Calendar
        [AllowAnonymous]
        public async Task<IActionResult> Calendar(Guid? roomId = null, DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.Now;
                List<ScheduleDto> schedules;

                if (roomId.HasValue)
                {
                    schedules = await _scheduleService.GetSchedulesForRoomOnDateAsync(roomId.Value, targetDate);
                }
                else
                {
                    schedules = await _scheduleService.GetSchedulesByDayOfWeekAsync((int)targetDate.DayOfWeek);
                }

                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;
                ViewBag.TargetDate = targetDate;
                ViewBag.RoomId = roomId;

                return View(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading calendar");
                TempData["ErrorMessage"] = "Error loading calendar: " + ex.Message;
                return View(new List<ScheduleDto>());
            }
        }

        // GET: Schedule/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var schedule = await _scheduleService.GetScheduleByIdAsync(id);
                return View(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading schedule details: {id}");
                TempData["ErrorMessage"] = "Schedule not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Schedule/Create
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Create(Guid? roomId, Guid? campusId)
        {
            try
            {
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;

                if (roomId.HasValue)
                {
                    var room = await _roomService.GetRoomByIdAsync(roomId.Value);
                    ViewBag.SelectedRoom = room;
                }
                else
                {
                    // Load ALL rooms to allow direct selection
                    // We fetch a large page size to ensure we get all/most rooms for the dropdown
                    var roomsPagination = await _roomService.GetRoomsAsync(pageSize: 1000);
                    ViewBag.Rooms = roomsPagination;

                    // If a campus filter was passed in URL, we'll use it to pre-set the filter dropdown,
                    // but we still provide all rooms so the user can change their mind or select directly.
                    ViewBag.SelectedCampusId = campusId;
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create schedule page");
                TempData["ErrorMessage"] = "Error loading form";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Schedule/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Create(CreateScheduleDto createDto)
        {
            if (!ModelState.IsValid)
            {
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;

                // Reload rooms if RoomId is selected to keep dropdown active
                if (createDto.RoomId != Guid.Empty)
                {
                    var room = await _roomService.GetRoomByIdAsync(createDto.RoomId);
                    if (room != null)
                    {
                        var roomsPagination = await _roomService.GetRoomsAsync(pageSize: 1000, campusId: room.CampusId);
                        ViewBag.Rooms = roomsPagination;
                        ViewBag.SelectedCampusId = room.CampusId;
                    }
                }

                return View(createDto);
            }

            try
            {
                var schedule = await _scheduleService.CreateScheduleAsync(createDto);
                TempData["SuccessMessage"] = "Schedule created successfully";
                return RedirectToAction(nameof(Details), new { id = schedule.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule");
                ModelState.AddModelError(string.Empty, ex.Message);
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;

                // Reload rooms if RoomId is selected to keep dropdown active
                if (createDto.RoomId != Guid.Empty)
                {
                    try
                    {
                        var room = await _roomService.GetRoomByIdAsync(createDto.RoomId);
                        if (room != null)
                        {
                            var roomsPagination = await _roomService.GetRoomsAsync(pageSize: 1000, campusId: room.CampusId);
                            ViewBag.Rooms = roomsPagination;
                            ViewBag.SelectedCampusId = room.CampusId;
                        }
                    }
                    catch { /* If room lookup fails, ignore */ }
                }

                return View(createDto);
            }
        }

        // GET: Schedule/Edit/5
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var schedule = await _scheduleService.GetScheduleByIdAsync(id);
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;

                var updateDto = new UpdateScheduleDto
                {
                    Id = schedule.Id,
                    RoomId = schedule.RoomId,
                    ScheduleType = schedule.ScheduleType,
                    Title = schedule.Title,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    DayOfWeek = schedule.DayOfWeek,
                    StartDate = schedule.StartDate,
                    EndDate = schedule.EndDate
                };

                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading schedule for edit: {id}");
                TempData["ErrorMessage"] = "Schedule not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Schedule/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Edit(Guid id, UpdateScheduleDto updateDto)
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
                await _scheduleService.UpdateScheduleAsync(updateDto);
                TempData["SuccessMessage"] = "Schedule updated successfully";
                return RedirectToAction(nameof(Details), new { id = updateDto.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating schedule: {id}");
                ModelState.AddModelError(string.Empty, ex.Message);
                var campuses = await _campusService.GetCampusesAsync(pageSize: 100);
                ViewBag.Campuses = campuses;
                return View(updateDto);
            }
        }

        // GET: Schedule/GetRoomsByCampus
        [HttpGet]
        public async Task<JsonResult> GetRoomsByCampus(Guid campusId)
        {
            try
            {
                // Use the room service which handles filtering
                var roomsPagination = await _roomService.GetRoomsAsync(pageSize: 1000, campusId: campusId);
                var rooms = roomsPagination.Select(r => new { id = r.Id, name = r.Name }).OrderBy(r => r.name).ToList();
                return Json(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching rooms for campus {campusId}");
                return Json(new List<object>());
            }
        }

        // GET: Schedule/CheckConflict
        [HttpGet]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<JsonResult> CheckConflict(
            Guid roomId,
            int dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeId = null)
        {
            try
            {
                var hasConflict = await _scheduleService.HasScheduleConflictAsync(
                    roomId, dayOfWeek, startTime, endTime, startDate, endDate, excludeId);

                if (hasConflict)
                {
                    var conflicts = await _scheduleService.GetConflictingSchedulesAsync(
                        roomId, dayOfWeek, startTime, endTime, startDate, endDate);

                    return Json(new
                    {
                        hasConflict = true,
                        conflicts = conflicts.Select(s => new
                        {
                            s.Title,
                            s.StartTime,
                            s.EndTime,
                            s.DayOfWeekDisplay
                        })
                    });
                }

                return Json(new { hasConflict = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking schedule conflict");
                return Json(new { hasConflict = true, error = ex.Message });
            }
        }

        // GET: Schedule/Delete/5
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var schedule = await _scheduleService.GetScheduleByIdAsync(id);
                return View(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading schedule for delete: {id}");
                TempData["ErrorMessage"] = "Schedule not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Schedule/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await _scheduleService.SoftDeleteScheduleAsync(id);
                TempData["SuccessMessage"] = "Schedule deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting schedule: {id}");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

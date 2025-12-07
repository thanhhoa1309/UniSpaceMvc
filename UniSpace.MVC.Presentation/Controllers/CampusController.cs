using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSpace.Bo.DTOs.CampusDTOs;
using UniSpace.Service.Interfaces;

namespace UniSpace.MVC.Presentation.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class CampusController : Controller
    {
        private readonly ICampusService _campusService;
        private readonly ILogger<CampusController> _logger;

        public CampusController(ICampusService campusService, ILogger<CampusController> logger)
        {
            _campusService = campusService;
            _logger = logger;
        }

        // GET: Campus
        [AllowAnonymous]
        public async Task<IActionResult> Index(int page = 1, string? search = null)
        {
            try
            {
                var campuses = await _campusService.GetCampusesAsync(
                    pageNumber: page,
                    pageSize: 20,
                    searchTerm: search);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = campuses.TotalPages;
                ViewBag.Search = search;

                return View(campuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading campuses");
                TempData["ErrorMessage"] = "Error loading campuses: " + ex.Message;
                return View(new List<CampusDto>());
            }
        }

        // GET: Campus/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var campus = await _campusService.GetCampusByIdAsync(id);
                return View(campus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading campus details: {id}");
                TempData["ErrorMessage"] = "Campus not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Campus/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Campus/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCampusDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return View(createDto);
            }

            try
            {
                var campus = await _campusService.CreateCampusAsync(createDto);
                TempData["SuccessMessage"] = "Campus created successfully";
                return RedirectToAction(nameof(Details), new { id = campus.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating campus");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(createDto);
            }
        }

        // GET: Campus/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var campus = await _campusService.GetCampusByIdAsync(id);
                
                var updateDto = new UpdateCampusDto
                {
                    Id = campus.Id,
                    Name = campus.Name,
                    Address = campus.Address
                };

                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading campus for edit: {id}");
                TempData["ErrorMessage"] = "Campus not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Campus/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdateCampusDto updateDto)
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
                await _campusService.UpdateCampusAsync(updateDto);
                TempData["SuccessMessage"] = "Campus updated successfully";
                return RedirectToAction(nameof(Details), new { id = updateDto.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating campus: {id}");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(updateDto);
            }
        }

        // GET: Campus/Delete/5
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var campus = await _campusService.GetCampusByIdAsync(id);
                return View(campus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading campus for delete: {id}");
                TempData["ErrorMessage"] = "Campus not found";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Campus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await _campusService.SoftDeleteCampusAsync(id);
                TempData["SuccessMessage"] = "Campus deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting campus: {id}");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

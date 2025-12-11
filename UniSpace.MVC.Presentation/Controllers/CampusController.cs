using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSpace.Bo.DTOs.CampusDTOs;
using UniSpace.Service.Interfaces;

namespace UniSpace.MVC.Presentation.Controllers
{
    public class CampusController : Controller
    {
        private readonly ICampusService _campusService;
        private readonly ILogger<CampusController> _logger;

        public CampusController(ICampusService campusService, ILogger<CampusController> logger)
        {
            _campusService = campusService;
            _logger = logger;
        }

        // ============================================================
        // INDEX (Public)
        // ============================================================
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
                TempData["ErrorMessage"] = "Error loading campuses.";
                return View(new List<CampusDto>());
            }
        }

        // ============================================================
        // DETAILS (Public)
        // ============================================================
        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var campus = await _campusService.GetCampusByIdAsync(id);
                return View(campus);
            }
            catch
            {
                TempData["ErrorMessage"] = "Campus không tồn tại.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // CREATE (Admin only)
        // ============================================================
        public IActionResult Create()
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "❌ Bạn không phải Admin.";
                return RedirectToAction(nameof(Index));
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCampusDto createDto)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "❌ Bạn không phải Admin.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(createDto);
            }

            try
            {
                var campus = await _campusService.CreateCampusAsync(createDto);
                TempData["SuccessMessage"] = "Tạo campus thành công!";
                return RedirectToAction(nameof(Details), new { id = campus.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating campus");
                ModelState.AddModelError("", ex.Message);
                return View(createDto);
            }
        }

        // ============================================================
        // EDIT (Admin only)
        // ============================================================
        public async Task<IActionResult> Edit(Guid id)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "❌ Bạn không có quyền chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var campus = await _campusService.GetCampusByIdAsync(id);

                var dto = new UpdateCampusDto
                {
                    Id = campus.Id,
                    Name = campus.Name,
                    Address = campus.Address
                };

                return View(dto);
            }
            catch
            {
                TempData["ErrorMessage"] = "Không tìm thấy campus.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdateCampusDto updateDto)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "❌ Bạn không phải Admin.";
                return RedirectToAction(nameof(Index));
            }

            if (id != updateDto.Id)
            {
                TempData["ErrorMessage"] = "ID không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(updateDto);
            }

            try
            {
                await _campusService.UpdateCampusAsync(updateDto);
                TempData["SuccessMessage"] = "Cập nhật campus thành công!";
                return RedirectToAction(nameof(Details), new { id = updateDto.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(updateDto);
            }
        }

        // ============================================================
        // DELETE (Admin only)
        // ============================================================
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "❌ Bạn không có quyền xóa.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var campus = await _campusService.GetCampusByIdAsync(id);
                return View(campus);
            }
            catch
            {
                TempData["ErrorMessage"] = "Không tìm thấy campus.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "❌ Bạn không có quyền xóa.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _campusService.SoftDeleteCampusAsync(id);
                TempData["SuccessMessage"] = "Xóa campus thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using UniSpace.Bo.DTOs.AuthDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Services.Interfaces;

namespace UniSpace.MVC.Presentation.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _authService = authService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // N?u ?ã ??ng nh?p, redirect v? Home
            if (HttpContext.Session.GetString("AuthToken") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequestDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return View(loginDto);
            }

            try
            {
                var response = await _authService.LoginAsync(loginDto, _configuration);
                if (response == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(loginDto);
                }

                _logger.LogInformation($"User {loginDto.Email} logged in successfully.");

                // Store token in session for later use
                HttpContext.Session.SetString("AuthToken", response.Token);

                // Redirect to Home page
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error for {loginDto.Email}: {ex.Message}");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(loginDto);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            // N?u ?ã ??ng nh?p, redirect v? Home
            if (HttpContext.Session.GetString("AuthToken") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserRegistrationDto registrationDto, string role = "Student")
        {
            if (!ModelState.IsValid)
            {
                return View(registrationDto);
            }

            try
            {
                // Parse role
                RoleType userRole = role.ToLower() switch
                {
                    "lecturer" => RoleType.Lecturer,
                    "student" => RoleType.Student,
                    _ => RoleType.Student
                };

                var user = await _authService.RegisterUserAsync(registrationDto, userRole);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Registration failed.");
                    return View(registrationDto);
                }

                _logger.LogInformation($"User {user.Email} registered successfully as {userRole}.");
                TempData["SuccessMessage"] = "Registration successful! Please login.";

                // ??ng ký thành công, chuy?n h??ng ??n trang ??ng nh?p
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Registration error: {ex.Message}");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(registrationDto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");
                if (!string.IsNullOrEmpty(token))
                {
                    // Parse userId from token if needed
                    // For now, just clear session
                    HttpContext.Session.Remove("AuthToken");
                    HttpContext.Session.Clear();
                    
                    _logger.LogInformation("User logged out successfully.");
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Logout error: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

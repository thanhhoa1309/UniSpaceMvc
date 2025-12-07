using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UniSpace.Bo.DTOs.AuthDTOs;
using UniSpace.Bo.Enums;
using UniSpace.Model.Entities;
using UniSpace.Model.Interfaces;
using UniSpace.Service.Utils;
using UniSpace.Services.Interfaces;


namespace UniSpace.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public AuthService(IUnitOfWork unitOfWork, ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration)
        {
            try
            {
                _logger.LogInformation($"Attempting login for {loginDto?.Email}");

                if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    _logger.LogWarning("Login failed: missing email or password.");
                    throw ErrorHelper.BadRequest("Email and password are required.");
                }

                var user = await _unitOfWork.User.FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning($"Login failed: user {loginDto.Email} not found or inactive.");
                    throw ErrorHelper.NotFound("User not found or inactive.");
                }

                var passwordHasher = new PasswordHasher();
                if (!passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Login failed: invalid password for {loginDto.Email}.");
                    throw ErrorHelper.Unauthorized("Invalid password.");
                }

                var jwtToken = JwtUtils.GenerateJwtToken(
                    user.Id,
                    user.Email,
                    user.Role.ToString(),
                    configuration,
                    TimeSpan.FromHours(8)
                );

                var response = new LoginResponseDto
                {
                    Token = jwtToken
                };

                _logger.LogInformation($"Login successful for {user.Email}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error for {loginDto?.Email}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> LogoutAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation($"User with ID {userId} logged out");
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Logout error for user {userId}: {ex.Message}");
                return false;
            }
        }

        public async Task<UserDto?> RegisterUserAsync(UserRegistrationDto registrationDto, RoleType role = RoleType.Student)
        {
            try
            {
                _logger.LogInformation($"Registering new user with role: {role}");

                // Validate registration data
                if (string.IsNullOrWhiteSpace(registrationDto.Email) ||
                    string.IsNullOrWhiteSpace(registrationDto.Password) ||
                    string.IsNullOrWhiteSpace(registrationDto.FullName))
                {
                    _logger.LogWarning("Registration failed: missing required fields");
                    throw ErrorHelper.BadRequest("All fields are required.");
                }

                // Check if user already exists
                if (await UserExistsAsync(registrationDto.Email))
                {
                    _logger.LogWarning($"Registration failed: email {registrationDto.Email} already in use.");
                    throw ErrorHelper.Conflict("Email already in use.");
                }

                // Validate role (only Student and Lecturer can register)
                if (role != RoleType.Student && role != RoleType.Lecturer)
                {
                    _logger.LogWarning($"Registration failed: invalid role {role}");
                    throw ErrorHelper.BadRequest("Invalid role. Only Student and Lecturer can register.");
                }

                var hashedPassword = new PasswordHasher().HashPassword(registrationDto.Password);

                var user = new User
                {
                    FullName = registrationDto.FullName,
                    Email = registrationDto.Email,
                    Role = role, // Use the provided role instead of hardcoded Student
                    PasswordHash = hashedPassword ?? throw ErrorHelper.Internal("Password hashing failed."),
                    IsActive = true
                };

                await _unitOfWork.User.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation($"User {user.Email} registered successfully as {role}");

                var userDto = new UserDto
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    IsActive = user.IsActive
                };

                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating account: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> UserExistsAsync(string email)
        {
            var accounts = await _unitOfWork.User.GetAllAsync();
            return accounts.Any(a => a.Email == email);
        }
    }
}

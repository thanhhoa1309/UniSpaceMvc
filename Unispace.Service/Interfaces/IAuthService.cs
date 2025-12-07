using Microsoft.Extensions.Configuration;
using UniSpace.Bo.DTOs.AuthDTOs;
using UniSpace.Bo.Enums;

namespace UniSpace.Services.Interfaces
{
    public interface IAuthService
    {
        Task<UserDto?> RegisterUserAsync(UserRegistrationDto registrationDto, RoleType role = RoleType.Student);

        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration);

        Task<bool> LogoutAsync(Guid userId);
    }
}

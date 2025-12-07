using UniSpace.Bo.Enums;

namespace UniSpace.Bo.DTOs.AuthDTOs
{
    public class UserDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public RoleType Role { get; set; }
        public bool IsActive { get; set; }
    }
}

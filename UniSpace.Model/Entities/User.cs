using UniSpace.Bo.Enums;

namespace UniSpace.Model.Entities
{
    public class User : BaseEntity
    {

        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public RoleType Role { get; set; }
        public bool IsActive { get; set; } = true;


        public ICollection<Booking> Bookings { get; set; }
        public ICollection<RoomReport> Reports { get; set; }
    }
}

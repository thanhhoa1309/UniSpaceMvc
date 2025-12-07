using UniSpace.Bo.Enums;
namespace UniSpace.Model.Entities
{
    public class Booking : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid RoomId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BookingStatus Status { get; set; }
        public string Purpose { get; set; }
        public string AdminNote { get; set; }

        public User User { get; set; }
        public Room Room { get; set; }
        public ICollection<RoomReport> RoomReports { get; set; }
    }
}

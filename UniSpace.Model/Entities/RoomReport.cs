using UniSpace.Bo.Enums;

namespace UniSpace.Model.Entities
{
    public class RoomReport : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid RoomId { get; set; }
        public Guid BookingId { get; set; }
        public string IssueType { get; set; }
        public string Description { get; set; }
        public ReportStatus Status { get; set; }

        public User User { get; set; }
        public Room Room { get; set; }
        public Booking Booking { get; set; }
    }
}

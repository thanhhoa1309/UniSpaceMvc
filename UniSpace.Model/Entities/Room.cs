using UniSpace.Bo.Enums;

namespace UniSpace.Model.Entities
{
    public class Room : BaseEntity
    {
        public Guid CampusId { get; set; }
        public string Name { get; set; }
        public RoomType Type { get; set; }
        public int Capacity { get; set; }
        public BookingStatus CurrentStatus { get; set; }
        public RoomStatus RoomStatus { get; set; }
        public string Description { get; set; }

        public Campus Campus { get; set; }
        public ICollection<Booking> Bookings { get; set; }
        public ICollection<RoomReport> Reports { get; set; }
        public ICollection<Schedule> Schedule { get; set; }
    }
}

using UniSpace.Bo.Enums;

namespace UniSpace.Bo.DTOs.BookingDTOs
{
    public class BookingDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string CampusName { get; set; } = string.Empty;
        public RoomType RoomType { get; set; }
        public string RoomTypeDisplay { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BookingStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string AdminNote { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

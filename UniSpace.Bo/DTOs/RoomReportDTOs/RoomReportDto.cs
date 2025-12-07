using UniSpace.Bo.Enums;

namespace UniSpace.Bo.DTOs.RoomReportDTOs
{
    public class RoomReportDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string CampusName { get; set; } = string.Empty;
        public Guid BookingId { get; set; }
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReportStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

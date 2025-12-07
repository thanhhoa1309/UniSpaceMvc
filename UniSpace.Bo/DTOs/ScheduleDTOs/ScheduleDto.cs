using UniSpace.Bo.Enums;

namespace UniSpace.Bo.DTOs.ScheduleDTOs
{
    public class ScheduleDto
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string CampusName { get; set; } = string.Empty;
        public ScheduleType ScheduleType { get; set; }
        public string ScheduleTypeDisplay { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int DayOfWeek { get; set; }
        public string DayOfWeekDisplay { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

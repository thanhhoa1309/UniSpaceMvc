using System.ComponentModel.DataAnnotations;
using UniSpace.Bo.Enums;

namespace UniSpace.Bo.DTOs.ScheduleDTOs
{
    public class UpdateScheduleDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Room is required")]
        public Guid RoomId { get; set; }

        [Required(ErrorMessage = "Schedule type is required")]
        public ScheduleType ScheduleType { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start time is required")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Day of week is required")]
        [Range(0, 6, ErrorMessage = "Day of week must be between 0 (Sunday) and 6 (Saturday)")]
        public int DayOfWeek { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace UniSpace.Bo.DTOs.RoomReportDTOs
{
    public class CreateRoomReportDto
    {
        [Required(ErrorMessage = "Booking ID is required")]
        public Guid BookingId { get; set; }

        [Required(ErrorMessage = "Issue type is required")]
        [StringLength(100, ErrorMessage = "Issue type cannot exceed 100 characters")]
        public string IssueType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 1000 characters")]
        public string Description { get; set; } = string.Empty;
    }
}

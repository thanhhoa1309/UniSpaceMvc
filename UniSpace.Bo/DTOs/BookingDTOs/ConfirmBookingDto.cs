using System.ComponentModel.DataAnnotations;
using UniSpace.Bo.Enums;

namespace UniSpace.BusinessObject.DTOs.BookingDTOs
{
    public class ConfirmBookingDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Action is required")]
        public BookingStatus Status { get; set; }

        [StringLength(500, ErrorMessage = "Admin note cannot exceed 500 characters")]
        public string? AdminNote { get; set; }
    }
}

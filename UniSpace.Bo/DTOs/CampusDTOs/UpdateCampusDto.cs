using System.ComponentModel.DataAnnotations;

namespace UniSpace.Bo.DTOs.CampusDTOs
{
    public class UpdateCampusDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Campus name is required")]
        [StringLength(100, ErrorMessage = "Campus name must not exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, ErrorMessage = "Address must not exceed 200 characters")]
        public string Address { get; set; } = string.Empty;
    }
}

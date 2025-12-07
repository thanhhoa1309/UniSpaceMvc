using System.ComponentModel.DataAnnotations;
using UniSpace.Bo.Enums;

namespace UniSpace.Bo.DTOs.RoomDTOs
{
    public class CreateRoomDto
    {
        [Required(ErrorMessage = "Campus is required")]
        public Guid CampusId { get; set; }

        [Required(ErrorMessage = "Room name is required")]
        [StringLength(100, ErrorMessage = "Room name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Room type is required")]
        public RoomType Type { get; set; }

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, 1000, ErrorMessage = "Capacity must be between 1 and 1000")]
        public int Capacity { get; set; }

        public RoomStatus RoomStatus { get; set; } = RoomStatus.Active;  // Default to Active

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;
    }
}

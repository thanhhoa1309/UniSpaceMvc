namespace UniSpace.Bo.DTOs.CampusDTOs
{
    public class CampusDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int TotalRooms { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

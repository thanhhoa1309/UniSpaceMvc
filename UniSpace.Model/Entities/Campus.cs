namespace UniSpace.Model.Entities
{
    public class Campus : BaseEntity
    {

        public string Name { get; set; }
        public string Address { get; set; }


        public ICollection<Room> Rooms { get; set; }
    }
}

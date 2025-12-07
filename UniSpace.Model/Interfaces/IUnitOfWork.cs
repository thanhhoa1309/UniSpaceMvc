using UniSpace.Model.Entities;

namespace UniSpace.Model.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<User> User { get; }
        IGenericRepository<Campus> Campus { get; }
        IGenericRepository<Room> Room { get; }
        IGenericRepository<Schedule> Schedule { get; }
        IGenericRepository<Booking> Booking { get; }
        IGenericRepository<RoomReport> RoomReport { get; }
        Task<int> SaveChangesAsync();
    }
}

using UniSpace.Model.Entities;
using UniSpace.Model.Interfaces;

namespace UniSpace.Model
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly UniSpaceMvcDbContext _dbContext;

        public UnitOfWork(
            UniSpaceMvcDbContext dbContext,
            IGenericRepository<User> userRepository,
            IGenericRepository<Campus> campusRepository,
            IGenericRepository<Room> roomRepository,
            IGenericRepository<Schedule> scheduleRepository,
            IGenericRepository<Booking> bookingRepository,
            IGenericRepository<RoomReport> roomReportRepository)
        {
            _dbContext = dbContext;
            User = userRepository;
            Campus = campusRepository;
            Room = roomRepository;
            Schedule = scheduleRepository;
            Booking = bookingRepository;
            RoomReport = roomReportRepository;
        }

        public IGenericRepository<User> User { get; }
        public IGenericRepository<Campus> Campus { get; }
        public IGenericRepository<Room> Room { get; }
        public IGenericRepository<Schedule> Schedule { get; }
        public IGenericRepository<Booking> Booking { get; }
        public IGenericRepository<RoomReport> RoomReport { get; }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}

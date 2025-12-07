using Microsoft.EntityFrameworkCore;
using UniSpace.Bo.Enums;
using UniSpace.Model.Entities;

namespace UniSpace.Model
{
    public static class DbInitializer
    {
        public static async Task SeedDataAsync(UniSpaceMvcDbContext context)
        {
            // Ki?m tra ?ã có data ch?a
            if (await context.User.AnyAsync())
            {
                return; // DB ?ã có data
            }

            // Seed Admin User
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@unispace.com",
                PasswordHash = HashPassword("Admin@123"),
                FullName = "Administrator",
                Role = RoleType.Admin,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            // Seed Sample Lecturer
            var lecturer = new User
            {
                Id = Guid.NewGuid(),
                Email = "lecturer@unispace.com",
                PasswordHash = HashPassword("Lecturer@123"),
                FullName = "Nguyen Van A",
                Role = RoleType.Lecturer,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            // Seed Sample Student
            var student = new User
            {
                Id = Guid.NewGuid(),
                Email = "student@unispace.com",
                PasswordHash = HashPassword("Student@123"),
                FullName = "Tran Thi B",
                Role = RoleType.Student,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            await context.User.AddRangeAsync(adminUser, lecturer, student);

            // Seed Sample Campus
            var campus = new Campus
            {
                Id = Guid.NewGuid(),
                Name = "FPT University HCM Campus",
                Address = "Lot E2a-7, D1 Street, High-Tech Park, Long Thanh My Ward, Thu Duc City, Ho Chi Minh City",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            await context.Campuses.AddAsync(campus);

            // Seed Sample Rooms
            var room1 = new Room
            {
                Id = Guid.NewGuid(),
                Name = "501",
                CampusId = campus.Id,
                Type = RoomType.Classroom,
                Capacity = 50,
                CurrentStatus = BookingStatus.Approved,
                RoomStatus = RoomStatus.Active,
                Description = "Standard classroom with projector",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            var room2 = new Room
            {
                Id = Guid.NewGuid(),
                Name = "Lab 101",
                CampusId = campus.Id,
                Type = RoomType.Lab,
                Capacity = 30,
                CurrentStatus = BookingStatus.Approved,
                RoomStatus = RoomStatus.Active,
                Description = "Computer lab with 30 workstations",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            var room3 = new Room
            {
                Id = Guid.NewGuid(),
                Name = "Meeting Room A",
                CampusId = campus.Id,
                Type = RoomType.Stadium,
                Capacity = 15,
                CurrentStatus = BookingStatus.Approved,
                RoomStatus = RoomStatus.Active,
                Description = "Meeting room with video conference facilities",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            await context.Rooms.AddRangeAsync(room1, room2, room3);

            // Seed Sample Schedules
            var schedule1 = new Schedule
            {
                Id = Guid.NewGuid(),
                RoomId = room1.Id,
                ScheduleType = ScheduleType.Academic_Course,
                Title = "PRN222 - Advanced Programming with .NET",
                StartTime = new TimeSpan(7, 0, 0),
                EndTime = new TimeSpan(9, 30, 0),
                DayOfWeek = 1, // Monday
                StartDate = new DateTime(2024, 1, 15),
                EndDate = new DateTime(2024, 5, 15),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            var schedule2 = new Schedule
            {
                Id = Guid.NewGuid(),
                RoomId = room2.Id,
                ScheduleType = ScheduleType.Academic_Course,
                Title = "PRN222 Lab Session",
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                DayOfWeek = 3, // Wednesday
                StartDate = new DateTime(2024, 1, 15),
                EndDate = new DateTime(2024, 5, 15),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            await context.Schedules.AddRangeAsync(schedule1, schedule2);

            // Save all changes
            await context.SaveChangesAsync();
        }

        // Simple password hasher for seeding
        private static string HashPassword(string password)
        {
            // Using simple hash for demo - in production use proper password hashing
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var salt = Guid.NewGuid().ToString();
                var bytes = System.Text.Encoding.UTF8.GetBytes(password + salt);
                var hash = sha256.ComputeHash(bytes);
                return $"{salt}:{Convert.ToBase64String(hash)}";
            }
        }
    }
}

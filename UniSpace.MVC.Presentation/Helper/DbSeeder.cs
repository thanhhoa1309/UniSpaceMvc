using Microsoft.EntityFrameworkCore;
using UniSpace.Bo.Enums;
using UniSpace.Model;
using UniSpace.Model.Entities;
using UniSpace.Service.Utils;

namespace UniSpace.Presentation.Helper
{
    public static class DbSeeder
    {
        public static async Task SeedUsersAsync(UniSpaceMvcDbContext context)
        {
            if (!await context.User.AnyAsync(u => u.Role == RoleType.Admin))
            {
                var passwordHasher = new PasswordHasher();
                var admin = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = "Admin User",
                    Email = "admin@gmail.com",
                    PasswordHash = passwordHasher.HashPassword("1@"),
                    Role = RoleType.Admin,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = Guid.Empty
                };

                await context.User.AddAsync(admin);
            }

            if (!await context.User.AnyAsync(u => u.Role == RoleType.Student))
            {
                var passwordHasher = new PasswordHasher();
                var students = new List<User>
                {
                    new User
                    {
                        Id = Guid.NewGuid(),
                        FullName = "Student 1",
                        Email = "student1@gmail.com",
                        PasswordHash = passwordHasher.HashPassword("1@"),
                        Role = RoleType.Student,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = Guid.Empty
                    },
                    new User
                    {
                        Id = Guid.NewGuid(),
                        FullName = "Student 2",
                        Email = "student2@gmail.com",
                        PasswordHash = passwordHasher.HashPassword("1@"),
                        Role = RoleType.Student,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = Guid.Empty
                    }
                };
                await context.User.AddRangeAsync(students);
            }

            if (!await context.User.AnyAsync(u => u.Role == RoleType.Lecturer))
            {
                var passwordHasher = new PasswordHasher();
                var lecturers = new List<User>
                {
                    new User
                    {
                        Id = Guid.NewGuid(),
                        FullName = "Lecturer 1",
                        Email = "lecturer1@gmail.com",
                        PasswordHash = passwordHasher.HashPassword("1@"),
                        Role = RoleType.Lecturer,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = Guid.Empty
                    },
                    new User
                    {
                        Id = Guid.NewGuid(),
                        FullName = "Lecturer 2",
                        Email = "lecturer2@gmail.com",
                        PasswordHash = passwordHasher.HashPassword("1@"),
                        Role = RoleType.Lecturer,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = Guid.Empty
                    }
                };
                await context.User.AddRangeAsync(lecturers);
            }

            await context.SaveChangesAsync();
        }

        public static async Task SeedCampusesAsync(UniSpaceMvcDbContext context)
        {
            if (!await context.Campuses.AnyAsync())
            {
                var admin = await context.User.FirstOrDefaultAsync(u => u.Role == RoleType.Admin);
                var adminId = admin?.Id ?? Guid.Empty;

                var campuses = new List<Campus>
                {
                    new Campus
                    {
                        Id = Guid.NewGuid(),
                        Name = "FPT University - Hoa Lac Campus",
                        Address = "Thach That, Hanoi, Vietnam",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminId
                    },
                    new Campus
                    {
                        Id = Guid.NewGuid(),
                        Name = "FPT University - Quy Nhon Campus",
                        Address = "Quy Nhon, Binh Dinh, Vietnam",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminId
                    },
                    new Campus
                    {
                        Id = Guid.NewGuid(),
                        Name = "FPT University - Da Nang Campus",
                        Address = "Ngu Hanh Son, Da Nang, Vietnam",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminId
                    },
                    new Campus
                    {
                        Id = Guid.NewGuid(),
                        Name = "FPT University - Ho Chi Minh Campus",
                        Address = "District 9, Ho Chi Minh City, Vietnam",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminId
                    },
                    new Campus
                    {
                        Id = Guid.NewGuid(),
                        Name = "FPT University - Can Tho Campus",
                        Address = "Ninh Kieu, Can Tho, Vietnam",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminId
                    }
                };

                await context.Campuses.AddRangeAsync(campuses);
                await context.SaveChangesAsync();
            }
        }

        public static async Task SeedRoomsAsync(UniSpaceMvcDbContext context)
        {
            if (!await context.Rooms.AnyAsync())
            {
                var admin = await context.User.FirstOrDefaultAsync(u => u.Role == RoleType.Admin);
                var adminId = admin?.Id ?? Guid.Empty;

                var campuses = await context.Campuses.ToListAsync();

                if (!campuses.Any())
                {
                    await SeedCampusesAsync(context);
                    campuses = await context.Campuses.ToListAsync();
                }

                var rooms = new List<Room>();

                foreach (var campus in campuses)
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        rooms.Add(new Room
                        {
                            Id = Guid.NewGuid(),
                            CampusId = campus.Id,
                            Name = $"Room {i}01",
                            Type = RoomType.Classroom,
                            Capacity = 40 + (i * 10),
                            CurrentStatus = BookingStatus.Approved,
                            RoomStatus = RoomStatus.Active,
                            Description = $"Standard classroom with projector and whiteboard. Capacity: {40 + (i * 10)} students.",
                            IsDeleted = false,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = adminId
                        });
                    }

                    for (int i = 1; i <= 3; i++)
                    {
                        rooms.Add(new Room
                        {
                            Id = Guid.NewGuid(),
                            CampusId = campus.Id,
                            Name = $"Lab {i}",
                            Type = RoomType.Lab,
                            Capacity = 30 + (i * 5),
                            CurrentStatus = BookingStatus.Approved,
                            RoomStatus = RoomStatus.Active,
                            Description = $"Computer lab equipped with {30 + (i * 5)} workstations. High-speed internet and development software installed.",
                            IsDeleted = false,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = adminId
                        });
                    }

                    rooms.Add(new Room
                    {
                        Id = Guid.NewGuid(),
                        CampusId = campus.Id,
                        Name = "Main Stadium",
                        Type = RoomType.Stadium,
                        Capacity = 500,
                        CurrentStatus = BookingStatus.Approved,
                        RoomStatus = RoomStatus.Active,
                        Description = "Large outdoor stadium suitable for sports events, ceremonies, and large gatherings. Capacity: 500 people.",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminId
                    });

                    rooms.Add(new Room
                    {
                        Id = Guid.NewGuid(),
                        CampusId = campus.Id,
                        Name = "Indoor Sports Hall",
                        Type = RoomType.Stadium,
                        Capacity = 200,
                        CurrentStatus = BookingStatus.Approved,
                        RoomStatus = RoomStatus.Active,
                        Description = "Indoor sports facility for basketball, volleyball, and badminton. Capacity: 200 people.",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminId
                    });
                }

                await context.Rooms.AddRangeAsync(rooms);
                await context.SaveChangesAsync();
            }
        }

        public static async Task SeedSchedulesAsync(UniSpaceMvcDbContext context)
        {
            if (!await context.Schedules.AnyAsync())
            {
                var admin = await context.User.FirstOrDefaultAsync(u => u.Role == RoleType.Admin);
                var adminId = admin?.Id ?? Guid.Empty;

                var rooms = await context.Rooms.Where(r => !r.IsDeleted).ToListAsync();

                if (!rooms.Any())
                {
                    await SeedRoomsAsync(context);
                    rooms = await context.Rooms.Where(r => !r.IsDeleted).ToListAsync();
                }

                var schedules = new List<Schedule>();
                var currentDate = DateTime.UtcNow.Date;

                // Calculate semester dates - IMPORTANT: Use DateTime.SpecifyKind to set UTC
                var fallStart = DateTime.SpecifyKind(new DateTime(currentDate.Year, 9, 1), DateTimeKind.Utc);
                var fallEnd = DateTime.SpecifyKind(new DateTime(currentDate.Year, 12, 20), DateTimeKind.Utc);

                if (currentDate > fallEnd)
                {
                    fallStart = DateTime.SpecifyKind(new DateTime(currentDate.Year + 1, 9, 1), DateTimeKind.Utc);
                    fallEnd = DateTime.SpecifyKind(new DateTime(currentDate.Year + 1, 12, 20), DateTimeKind.Utc);
                }

                var springStart = DateTime.SpecifyKind(new DateTime(currentDate.Year, 1, 15), DateTimeKind.Utc);
                var springEnd = DateTime.SpecifyKind(new DateTime(currentDate.Year, 5, 30), DateTimeKind.Utc);

                var classrooms = rooms.Where(r => r.Type == RoomType.Classroom).ToList();
                var labs = rooms.Where(r => r.Type == RoomType.Lab).ToList();
                var stadiums = rooms.Where(r => r.Type == RoomType.Stadium).ToList();

                // Academic courses for Fall semester
                var fallCourses = new[]
                {
                    new { Name = "PRN221 - Advanced C# Programming", Start = new TimeSpan(7, 30, 0), End = new TimeSpan(9, 30, 0), Day = 2 },
                    new { Name = "PRN222 - ASP.NET Core Development", Start = new TimeSpan(9, 45, 0), End = new TimeSpan(11, 45, 0), Day = 2 },
                    new { Name = "SWP391 - Software Project Management", Start = new TimeSpan(13, 0, 0), End = new TimeSpan(15, 0, 0), Day = 3 },
                    new { Name = "PRN212 - Basics of C# Programming", Start = new TimeSpan(15, 15, 0), End = new TimeSpan(17, 15, 0), Day = 3 },
                    new { Name = "SWR302 - Software Requirements", Start = new TimeSpan(7, 30, 0), End = new TimeSpan(9, 30, 0), Day = 4 },
                    new { Name = "SWT301 - Software Testing", Start = new TimeSpan(13, 0, 0), End = new TimeSpan(15, 0, 0), Day = 4 },
                    new { Name = "PRN231 - Web API Development", Start = new TimeSpan(9, 45, 0), End = new TimeSpan(11, 45, 0), Day = 5 },
                    new { Name = "IOT102 - Internet of Things", Start = new TimeSpan(15, 15, 0), End = new TimeSpan(17, 15, 0), Day = 5 },
                    new { Name = "SWD392 - SW Architecture & Design", Start = new TimeSpan(7, 30, 0), End = new TimeSpan(9, 30, 0), Day = 6 },
                    new { Name = "MLN111 - Machine Learning Basics", Start = new TimeSpan(13, 0, 0), End = new TimeSpan(15, 0, 0), Day = 6 }
                };

                // Spring courses
                var springCourses = new[]
                {
                    new { Name = "PRN231 - Building Large-scale Web Apps", Start = new TimeSpan(7, 30, 0), End = new TimeSpan(9, 30, 0), Day = 2 },
                    new { Name = "SWD392 - Advanced Software Design", Start = new TimeSpan(13, 0, 0), End = new TimeSpan(15, 0, 0), Day = 3 },
                    new { Name = "DBM301 - Database Management", Start = new TimeSpan(9, 45, 0), End = new TimeSpan(11, 45, 0), Day = 4 },
                    new { Name = "MLN121 - Deep Learning", Start = new TimeSpan(15, 15, 0), End = new TimeSpan(17, 15, 0), Day = 5 },
                    new { Name = "IOT201 - Advanced IoT Systems", Start = new TimeSpan(7, 30, 0), End = new TimeSpan(9, 30, 0), Day = 6 }
                };

                // Seed Fall courses to classrooms
                for (int i = 0; i < Math.Min(classrooms.Count, fallCourses.Length); i++)
                {
                    var classroom = classrooms[i];
                    var course = fallCourses[i];

                    schedules.Add(new Schedule
                    {
                        Id = Guid.NewGuid(),
                        RoomId = classroom.Id,
                        ScheduleType = ScheduleType.Academic_Course,
                        Title = course.Name,
                        StartTime = course.Start,
                        EndTime = course.End,
                        DayOfWeek = course.Day,
                        StartDate = fallStart,
                        EndDate = fallEnd,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminId
                    });
                }

                // Seed Spring courses to remaining classrooms
                int springClassroomStart = Math.Min(classrooms.Count, fallCourses.Length);
                for (int i = 0; i < Math.Min(classrooms.Count - springClassroomStart, springCourses.Length); i++)
                {
                    if (springClassroomStart + i < classrooms.Count)
                    {
                        var classroom = classrooms[springClassroomStart + i];
                        var course = springCourses[i];

                        schedules.Add(new Schedule
                        {
                            Id = Guid.NewGuid(),
                            RoomId = classroom.Id,
                            ScheduleType = ScheduleType.Academic_Course,
                            Title = course.Name,
                            StartTime = course.Start,
                            EndTime = course.End,
                            DayOfWeek = course.Day,
                            StartDate = springStart,
                            EndDate = springEnd,
                            IsDeleted = false,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = adminId
                        });
                    }
                }

                // Lab sessions
                var labSessions = new[]
                {
                    new { Name = "PRN221 - Lab Session A", Start = new TimeSpan(7, 30, 0), End = new TimeSpan(10, 30, 0), Day = 2, IsFall = true },
                    new { Name = "PRN221 - Lab Session B", Start = new TimeSpan(13, 0, 0), End = new TimeSpan(16, 0, 0), Day = 2, IsFall = true },
                    new { Name = "PRN222 - Lab Session A", Start = new TimeSpan(7, 30, 0), End = new TimeSpan(10, 30, 0), Day = 4, IsFall = true },
                    new { Name = "PRN222 - Lab Session B", Start = new TimeSpan(13, 0, 0), End = new TimeSpan(16, 0, 0), Day = 4, IsFall = true },
                    new { Name = "IOT102 - Hardware Lab", Start = new TimeSpan(7, 30, 0), End = new TimeSpan(10, 30, 0), Day = 6, IsFall = true },
                    new { Name = "PRN231 - Web Development Lab", Start = new TimeSpan(7, 30, 0), End = new TimeSpan(10, 30, 0), Day = 3, IsFall = false },
                    new { Name = "DBM301 - Database Lab", Start = new TimeSpan(13, 0, 0), End = new TimeSpan(16, 0, 0), Day = 5, IsFall = false },
                    new { Name = "MLN121 - AI Lab Session", Start = new TimeSpan(7, 30, 0), End = new TimeSpan(10, 30, 0), Day = 6, IsFall = false }
                };

                // Seed lab sessions
                for (int i = 0; i < Math.Min(labs.Count, labSessions.Length); i++)
                {
                    var lab = labs[i];
                    var session = labSessions[i];
                    var semesterStart = session.IsFall ? fallStart : springStart;
                    var semesterEnd = session.IsFall ? fallEnd : springEnd;

                    schedules.Add(new Schedule
                    {
                        Id = Guid.NewGuid(),
                        RoomId = lab.Id,
                        ScheduleType = ScheduleType.Academic_Course,
                        Title = session.Name,
                        StartTime = session.Start,
                        EndTime = session.End,
                        DayOfWeek = session.Day,
                        StartDate = semesterStart,
                        EndDate = semesterEnd,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminId
                    });
                }

                // Stadium events (year-round) - Use SpecifyKind for dates
                var stadiumEvents = new[]
                {
                    new { Name = "Morning Sports Training", Start = new TimeSpan(6, 0, 0), End = new TimeSpan(8, 0, 0), Day = 2 },
                    new { Name = "Student Basketball League", Start = new TimeSpan(16, 0, 0), End = new TimeSpan(18, 0, 0), Day = 4 },
                    new { Name = "Football Practice", Start = new TimeSpan(15, 0, 0), End = new TimeSpan(17, 0, 0), Day = 3 },
                    new { Name = "Volleyball Tournament", Start = new TimeSpan(14, 0, 0), End = new TimeSpan(16, 0, 0), Day = 6 }
                };

                var yearStart = DateTime.SpecifyKind(new DateTime(currentDate.Year, 1, 1), DateTimeKind.Utc);
                var yearEnd = DateTime.SpecifyKind(new DateTime(currentDate.Year, 12, 31), DateTimeKind.Utc);

                // Seed stadium events
                foreach (var stadium in stadiums)
                {
                    for (int i = 0; i < Math.Min(2, stadiumEvents.Length); i++)
                    {
                        var evt = stadiumEvents[i];
                        schedules.Add(new Schedule
                        {
                            Id = Guid.NewGuid(),
                            RoomId = stadium.Id,
                            ScheduleType = ScheduleType.Academic_Course,
                            Title = evt.Name,
                            StartTime = evt.Start,
                            EndTime = evt.End,
                            DayOfWeek = evt.Day,
                            StartDate = yearStart,
                            EndDate = yearEnd,
                            IsDeleted = false,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = adminId
                        });
                    }
                }

                // Maintenance schedules
                var maintenanceSchedules = new[]
                {
                    new { Name = "Weekly Cleaning Service", Start = new TimeSpan(18, 0, 0), End = new TimeSpan(19, 0, 0), Day = 6 },
                    new { Name = "HVAC System Check", Start = new TimeSpan(7, 0, 0), End = new TimeSpan(7, 30, 0), Day = 7 },
                    new { Name = "Equipment Inspection", Start = new TimeSpan(8, 0, 0), End = new TimeSpan(9, 0, 0), Day = 7 },
                    new { Name = "Network Infrastructure Check", Start = new TimeSpan(19, 0, 0), End = new TimeSpan(20, 0, 0), Day = 3 },
                    new { Name = "Electrical System Maintenance", Start = new TimeSpan(6, 0, 0), End = new TimeSpan(7, 0, 0), Day = 1 },
                    new { Name = "Fire Safety Inspection", Start = new TimeSpan(17, 30, 0), End = new TimeSpan(18, 30, 0), Day = 5 }
                };

                var maintenanceStart = DateTime.SpecifyKind(new DateTime(currentDate.Year, 1, 1), DateTimeKind.Utc);
                var maintenanceEnd = DateTime.SpecifyKind(new DateTime(currentDate.Year, 12, 31), DateTimeKind.Utc);

                // Seed maintenance to first 15 rooms
                var maintenanceRooms = rooms.Take(15).ToList();
                foreach (var room in maintenanceRooms)
                {
                    var random = new Random(room.Id.GetHashCode());
                    var maintenanceCount = random.Next(2, 5);
                    var selectedMaintenance = maintenanceSchedules.OrderBy(x => random.Next()).Take(maintenanceCount);

                    foreach (var maintenance in selectedMaintenance)
                    {
                        schedules.Add(new Schedule
                        {
                            Id = Guid.NewGuid(),
                            RoomId = room.Id,
                            ScheduleType = ScheduleType.Recurring_Maintenance,
                            Title = maintenance.Name,
                            StartTime = maintenance.Start,
                            EndTime = maintenance.End,
                            DayOfWeek = maintenance.Day,
                            StartDate = maintenanceStart,
                            EndDate = maintenanceEnd,
                            IsDeleted = false,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = adminId
                        });
                    }
                }

                await context.Schedules.AddRangeAsync(schedules);
                await context.SaveChangesAsync();
            }
        }

        public static async Task SeedAllAsync(UniSpaceMvcDbContext context)
        {
            await SeedUsersAsync(context);
            await SeedCampusesAsync(context);
            await SeedRoomsAsync(context);
            await SeedSchedulesAsync(context);
        }
    }
}

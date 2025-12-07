using UniSpace.Bo.Enums;
using UniSpace.Model.Entities;

public class Schedule : BaseEntity
{

    public Guid RoomId { get; set; }
    public ScheduleType ScheduleType { get; set; } // Map to schedule_type Enum (academic_course, recurring_maintenance)
    public string Title { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public int DayOfWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public Room Room { get; set; }
}
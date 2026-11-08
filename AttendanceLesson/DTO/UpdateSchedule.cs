namespace AttendanceLesson.DTO;

public class UpdateSchedule
{
    public int GroupId { get;  set; }
    public int LessonId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

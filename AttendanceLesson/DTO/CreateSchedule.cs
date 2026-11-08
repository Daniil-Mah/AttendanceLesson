namespace AttendanceLesson.DTO;

public class CreateSchedule
{
    public int GroupId { get; private set; }
    public int LessonId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
}

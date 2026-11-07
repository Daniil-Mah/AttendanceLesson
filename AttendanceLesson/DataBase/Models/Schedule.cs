namespace AttendanceLesson.DataBase.Models;

public class Schedule
{
    public int Id { get; private set; }
    public int GroupId { get; private set; }
    public Group Group { get; set; }
    public int LessonId { get; private set; }
    public Lesson Lesson { get; set; }
    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
}
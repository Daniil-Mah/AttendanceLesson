using AttendanceLesson.DataBase.Models;

namespace AttendanceLesson.Responses;

public class LogBookToGroup
{
    public string Name {get;set;}
    public Info[] Info {get;set;}
}

public class Info
{
    public DateTime AbsenceTime { get; set; }
    public int UseId { get; set; }
    public int LessonId { get; set; }
    public TimeSpan AbsenceDuration { get; set; }
    public Status? AbsenceReason {get;set;}
}
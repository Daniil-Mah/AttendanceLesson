using AttendanceLesson.DataBase.Models;
namespace AttendanceLesson.DTO;

public class CreateLogBook
{
    public int UserId { get; set; }
    public int UserPlaceId { get; set; }
    public int GroupId { get; set; }
    public int LessonId {get;set;}
    public TimeSpan AbsenceDuration {get;set;}
    public bool IsPresent {get;set;}
    public Status? AbsenceReason {get;set;}
}

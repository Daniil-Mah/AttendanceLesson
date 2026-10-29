using AttendanceLesson.DataBase.Models;

namespace AttendanceLesson.DTO;

public class UpdateLogBook
{
    public DateTime? CreatedAt { get; set; } = null;
    public int? UserPlaceId { get; set; } = null;
    public int? UserId { get; set; } = null;
    public int? GroupId { get; set; } = null;
    public int? LessonId {get;set;} = null;
    public TimeSpan? AbsenceDuration {get;set;} = null;
    public bool? IsPresent {get;set;} = null;
    public Status? AbsenceReason {get;set;} = null;
}

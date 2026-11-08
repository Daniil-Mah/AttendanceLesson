using System.ComponentModel.DataAnnotations;
using AttendanceLesson.DataBase.Models;
using AttendanceLesson.Validation;

namespace AttendanceLesson.DTO;

public class UpdateLogBook
{
    [Range(0,int.MaxValue)]
    public int? UserId { get; set; } = null;
    
    [Range(0,(int)LengthValues.GroupsMax)]
    public int? GroupId { get; set; } = null;
    
    [Range(0,(int)LengthValues.LessonMax)]
    public int? LessonId {get;set;} = null;
    
    [TimeSpanNonNegative]
    public TimeSpan? AbsenceDuration {get;set;} = null;
    
    public bool? IsPresent {get;set;} = null;
    public Status? AbsenceReason {get;set;} = null;
}

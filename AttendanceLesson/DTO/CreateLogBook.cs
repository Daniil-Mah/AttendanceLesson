using System.ComponentModel.DataAnnotations;
using AttendanceLesson.DataBase.Models;
using AttendanceLesson.Validation;
namespace AttendanceLesson.DTO;

public class CreateLogBook
{
    [Required]
    [Range(0,int.MaxValue)]
    public required int UserId { get; set; }

    [Required]
    [Range(0,int.MaxValue)]
    public required int UserPlaceId { get; set; }

    [Required]
    [Range((int)LengthValues.GroupsMin, (int)LengthValues.GroupsMax)]
    public required int GroupId { get; set; }

    [Required]
    [Range((int)LengthValues.LessonMin, (int)LengthValues.LessonMax)]
    public required int LessonId {get;set;}

    [Required]
    [TimeSpanNonNegative]
    public required TimeSpan AbsenceDuration {get;set;}

    [Required]
    public required bool IsPresent {get;set;}

    public Status? AbsenceReason {get;set;}
}

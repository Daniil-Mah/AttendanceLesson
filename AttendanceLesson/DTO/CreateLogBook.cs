using System.ComponentModel.DataAnnotations;
using AttendanceLesson.DataBase.Models;
using AttendanceLesson.Validation;
namespace AttendanceLesson.DTO;

public record CreateLogBook 
(
    [Required, Range(0,int.MaxValue)] int UserId,
    [Required, Range((int)LengthValues.GroupsMin, (int)LengthValues.GroupsMax)] int GroupId,
    [Required, Range((int)LengthValues.LessonMin, (int)LengthValues.LessonMax)] int LessonId,
    [Required, TimeSpanNonNegative] TimeSpan AbsenceDuration,
    [Required] bool IsPresent,
    Status? AbsenceReason
);

using System.ComponentModel.DataAnnotations;
using AttendanceLesson.DataBase.Models;
using AttendanceLesson.Validation;

namespace AttendanceLesson.DTO;

public record UpdateLogBook
(
    [Range(0,int.MaxValue)] int? UserId = null,
    [Range(0,(int)LengthValues.GroupsMax)] int? GroupId = null,
    [Range(0,(int)LengthValues.LessonMax)] int? LessonId = null,
    [TimeSpanNonNegative] TimeSpan? AbsenceDuration = null,
    bool? IsPresent = null,
    Status? AbsenceReason = null
);

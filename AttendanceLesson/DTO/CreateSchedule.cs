using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public record CreateSchedule
(
    [Range((int)LengthValues.GroupsMin, (int)LengthValues.GroupsMax)] int GroupId,
    [Range((int)LengthValues.LessonMin, (int)LengthValues.LessonMax)] int LessonId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime 
);

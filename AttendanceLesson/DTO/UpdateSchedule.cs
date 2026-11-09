namespace AttendanceLesson.DTO;

public record UpdateSchedule
( 
    int? GroupId = null,
    int? LessonId = null,
    DateOnly? Date = null,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null
);

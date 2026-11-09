using AttendanceLesson.DataBase.Models;

namespace AttendanceLesson.Responses;

public record LogBookToGroup (string Name, Info[] Info);

public record Info (
    string User, string UserPlace, int LessonId, 
    TimeSpan AbsenceDuration, Status? AbsenceReason, DateTime AbsenceTime 
);
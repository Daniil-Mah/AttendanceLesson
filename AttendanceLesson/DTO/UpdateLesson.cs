using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public record UpdateLesson
(
    [Required, StringLength(20 , MinimumLength = 3)] string Name
);

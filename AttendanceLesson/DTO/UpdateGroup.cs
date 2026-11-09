using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public record UpdateGroup
(
    [Required, StringLength(8 , MinimumLength = 5)] string Name
);

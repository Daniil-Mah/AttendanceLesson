using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public record Login
(
    [Required, StringLength(50, MinimumLength = 3)] string Name,
    [Required, StringLength(100, MinimumLength = 8)] string Password
);

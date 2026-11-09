using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public record CreateGroup 
(
    [Required, StringLength(8, MinimumLength = 5 )] string Name 
);

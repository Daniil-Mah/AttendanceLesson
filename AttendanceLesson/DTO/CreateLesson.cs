using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public record CreateLesson 
(
    [Required] string Name, 
    [Required] int TeacherId 
);

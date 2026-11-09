using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public class CreateLesson
{
    [Required]
    public required string Name { get; set; }

    [Required]
    public required int TeacherId { get; set; }
}

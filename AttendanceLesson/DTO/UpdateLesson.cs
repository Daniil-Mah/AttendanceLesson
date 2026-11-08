using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public class UpdateLesson
{
    [Required]
    [StringLength(20 , MinimumLength = 3)]
    public required string Name { get; set; }
}

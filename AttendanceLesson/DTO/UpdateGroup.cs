using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public class UpdateGroup
{
    [Required]
    [StringLength(8 , MinimumLength = 5)]
    public required string Name { get; set; }
}

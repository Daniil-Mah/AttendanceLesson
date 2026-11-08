using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public class Login
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public required string Name { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string Password {get;set;}
}

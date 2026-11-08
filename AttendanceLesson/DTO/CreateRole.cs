using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public class CreateRole
{
    [Required]
    [StringLength((int)LengthValues.NameMax, MinimumLength = (int)LengthValues.NameMin)]
    public required string Name {get;set;}
}

using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public record CreateRole 
(
    [Required, StringLength((int)LengthValues.NameMax, MinimumLength = (int)LengthValues.NameMin)] string Name
);

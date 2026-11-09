using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public record CreateUser 
(
    [Required, StringLength((int)LengthValues.NameMax,MinimumLength = (int)LengthValues.NameMin)] string Name,
    [Required, StringLength((int)LengthValues.PasswordMax,MinimumLength = (int)LengthValues.PasswordMin)] string Password,
    [Required, Range((int)LengthValues.RolesMin, (int)LengthValues.RolesMax)] int RoleId, 
    [Range((int)LengthValues.GroupsMin, (int)LengthValues.GroupsMax)] int? GroupId = null
);

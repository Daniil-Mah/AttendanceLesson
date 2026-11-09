using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public record UpdateUser
(
    [StringLength((int)LengthValues.NameMax, MinimumLength = (int)LengthValues.NameMin)] string? Name = null,
    [Range((int)LengthValues.RolesMin, (int)LengthValues.RolesMax)] int? RoleId = null,
    [Range((int)LengthValues.GroupsMin, (int)LengthValues.GroupsMax)] int? GroupId = null
);

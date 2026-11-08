using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public class UpdateUser
{
    [StringLength((int)LengthValues.NameMax, MinimumLength = (int)LengthValues.NameMin)]
    public string? Name { get; set; } = null;

    [Range((int)LengthValues.RolesMin, (int)LengthValues.RolesMax)]
    public int? RoleId {get;set;} = null;

    [Range((int)LengthValues.GroupsMin, (int)LengthValues.GroupsMax)]
    public int? GroupId {get;set;} = null;
}

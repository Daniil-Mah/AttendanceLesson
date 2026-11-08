using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public class CreateUser
{
    [Required]
    [StringLength((int)LengthValues.NameMax,MinimumLength = (int)LengthValues.NameMin)]
    public required string Name { get; set; }

    [Required]
    [StringLength((int)LengthValues.PasswordMax,MinimumLength = (int)LengthValues.PasswordMin)]
    public required string Password {get;set;}

    [Required]
    [Range((int)LengthValues.RolesMin, (int)LengthValues.RolesMax)]
    public required int RoleId {get;set;}
    
    [Range((int)LengthValues.GroupsMin, (int)LengthValues.GroupsMax)]
    public int? GroupId {get;set;} = null;
}

using System;

namespace AttendanceLesson.DTO;

public class CreateUser
{
    public string Name { get; set; }
    public string Password {get;set;}
    public int RoleId {get;set;}
    public int? GroupId {get;set;} = null;

}

namespace AttendanceLesson.DataBase.Models;

public class User
{
    public int Id { get; set; }
    public string Name {get;set;}
    public int RoleId { get; set; }
    public Role Role { get; set; }
    public string PasswordHash {get;set;}
    public int? GroupId {get;set;}
    public Group? Group {get;set;}
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpires {get;set;}
}

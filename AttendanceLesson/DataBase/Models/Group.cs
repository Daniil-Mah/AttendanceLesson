using AttendanceLesson.DTO;

namespace AttendanceLesson.DataBase.Models;

public class Group
{
    public int Id { get; private set; }
    private string _name;
    public string Name 
    {
        get => _name;
        private set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("name of group must not be empty");
            _name = value;
        }
    }
    private Group () {}
    public Group (string name)
    {
        Name = name;
    }
    public void SetFields (UpdateGroup dto)
    {
        var updateGroup = typeof(Group).GetProperties();
            foreach (var p in updateGroup)
            {
                var dtoProp = typeof(UpdateGroup).GetProperty(p.Name);
                if (dtoProp == null) continue;
                var dtoValue = dtoProp.GetValue(dto);
                if (dtoValue == null) continue;
                p.SetValue(typeof(Group), dtoValue);
            }
    }
}
using System.Reflection;
using AttendanceLesson.DTO;

namespace AttendanceLesson.DataBase.Models;

public class Lesson
{
    public int Id { get; private set; }
    private string _name;
    public string Name 
    {
        get => _name;
        private set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("name of lesson must not be empty");
            _name = value;
        }
    }
    private int _teacherId;
    public int TeacherId 
    {
        get => _teacherId;
        private set
        {
            if (value < 0 ) throw new ArgumentException ("teacherId must be greter than 0");
            _teacherId = value;
        }
    }
    public User Teacher {get;set;}

    private Lesson () {}
    public Lesson (string name)
    {
        Name = name;
    }
    public void SetFields (UpdateLesson dto)
    {
        var updateGroup = typeof(Group).GetProperties();
            foreach (var p in updateGroup)
            {
                var dtoProp = typeof(UpdateLesson).GetProperty(p.Name);
                if (dtoProp == null) continue;
                var dtoValue = dtoProp.GetValue(dto);
                if (dtoValue == null) continue;
                p.SetValue(this, dtoValue, BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Instance, null,null,null);

            }
    }
}
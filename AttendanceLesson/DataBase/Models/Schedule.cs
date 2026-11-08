using System.Reflection;
using AttendanceLesson.DTO;

namespace AttendanceLesson.DataBase.Models;

public class Schedule
{
    public int Id { get; private set; }
    
    private int _groupId;
    public int GroupId 
    {
        get => _groupId; 
        private set
        {
            if (value < 0) throw new ArgumentException ("groupId must be greater than 0") ;
            _groupId = value;
        }
    }
    public Group Group { get; set; }
    
    private int _lessonId;
    public int LessonId 
    { 
        get => _lessonId; 
        private set
        {
            if (value < 0) throw new ArgumentException ("lessonId must be greater than 0") ;
            _lessonId = value;
        }
    }
    
    public Lesson Lesson { get; set; }
    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }

    private Schedule () {}
    public Schedule 
    (
        int groupId, int lessonId, 
        DateOnly date, TimeOnly startTime, TimeOnly endTime 
    )
    {
        GroupId = groupId;
        LessonId = lessonId;
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
    }

    public void SetFields (UpdateSchedule dto)
    {
        var updateGroup = typeof(Schedule).GetProperties();
            foreach (var p in updateGroup)
            {
                var dtoProp = typeof(UpdateSchedule).GetProperty(p.Name);
                if (dtoProp == null) continue;
                var dtoValue = dtoProp.GetValue(dto);
                if (dtoValue == null) continue;
                p.SetValue(this, dtoValue, BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Instance, null,null,null);

            }
    }
}
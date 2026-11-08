using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.DTO;

public class CreateSchedule
{
    [Range((int)LengthValues.GroupsMin, (int)LengthValues.GroupsMax)]
    public int GroupId { get; set; }

    [Range((int)LengthValues.LessonMin, (int)LengthValues.LessonMax)]
    public int LessonId { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

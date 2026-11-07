using AttendanceLesson.DataBase;
using AttendanceLesson.DataBase.Models;
using Microsoft.EntityFrameworkCore;


namespace AttendanceLesson.Service;
public class SetAbsence (IServiceScopeFactory scopeFactory): BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            System.Console.WriteLine("backgroud gone");
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            var finishedLesson = await db.Schedules
                .Where(s=> s.Date == DateOnly.FromDateTime(now) && s.EndTime < TimeOnly.FromDateTime(now))
                .ToArrayAsync(cancellationToken: stoppingToken);

            foreach (var lesson in finishedLesson)
            {
                var students = await db.Users
                    .Include(s=> s.Group)
                    .Where(s=> s.GroupId == lesson.GroupId)
                    .ToArrayAsync(stoppingToken);
                foreach (var student in students)
                {
                    var absence = await db.Logbooks.AnyAsync(l => 
                        l.UserId == student.Id
                        && l.LessonId == lesson.LessonId
                        && DateOnly.FromDateTime(l.CreatedAt) == DateOnly.FromDateTime(now), cancellationToken: stoppingToken);
                    if (!absence)
                    {
                        await db.Logbooks.AddAsync(new Logbook(
                            1, DateTime.UtcNow, 
                            student.Id, (int)student.GroupId!,
                            lesson.LessonId, lesson.EndTime.ToTimeSpan() - lesson.StartTime.ToTimeSpan(), 
                            false,null), stoppingToken);
                    }
                }
            }
            await db.SaveChangesAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}


using System;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Localization;

namespace AttendanceLesson.DataBase.Models;

public class Logbook
{
    public int Id { get; set; }
    public int UserPlaceId { get; set; }
    public User UserPlace { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int GroupId { get; set; }
    public Group Group {get;set;}
    public int LessonId {get;set;}
    public Lesson Lesson {get;set;}
    public TimeSpan AbsenceDuration {get;set;}
    public bool IsPresent {get;set;}
    public Status? AbsenceReason {get;set;}
}

public enum Status
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    Overslept,
    PersonalReason,
    Forgot
}
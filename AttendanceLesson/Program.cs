using System.Text;
using AttendanceLesson.DataBase;
using AttendanceLesson.DTO;
using AttendanceLesson.DataBase.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using AttendanceLesson.Responses;
using Microsoft.AspNetCore.Identity;
using AttendanceLesson.Service;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using AttendanceLesson.Extension;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(opt=> opt.UseNpgsql(builder.Configuration.GetConnectionString("psql")));
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name="Authorization",
        Description="JWT",
        In=ParameterLocation.Header,
        Type=SecuritySchemeType.Http,
        Scheme=JwtBearerDefaults.AuthenticationScheme,
        BearerFormat="JWT"
    });
    opt.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer",doc)] =[]
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = true,
        ValidateLifetime=true,
        ValidateIssuer=true,
        ValidateIssuerSigningKey=true,
        ValidIssuer=builder.Configuration["JWT:Issuer"],
        ValidAudience=builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!)),
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("admin", p => p.RequireRole("admin"));
    opt.AddPolicy("teacher", p=> p.RequireRole("teacher"));
    opt.AddPolicy("headman", p=> p.RequireRole("headman"));
    opt.AddPolicy("student", p=> p.RequireRole("student"));
});
builder.Services.AddRateLimiter(option=>
    option.AddPolicy("token", http=>
    {
        var key = http.User.Identity?.Name 
            ?? http.Connection.RemoteIpAddress?.ToString() 
            ?? "anon";
        return RateLimitPartition.GetTokenBucketLimiter(key, _=> new TokenBucketRateLimiterOptions
        {
            TokenLimit=100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            TokensPerPeriod = 5,
            AutoReplenishment = true,
            QueueLimit = 5,
        });
    })
);
builder.Services.AddHostedService<SetAbsence>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IPasswordHasher<CreateUser>, PasswordHasher<CreateUser>>();

var app = builder.Build();
app.UseExceptionHandler(opt=> opt.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (statusCode, message) = ExceptionHandler.GetError(ex);
    context.Response.StatusCode = statusCode;
    await context.Response.WriteAsJsonAsync(new {Message = message});
}));
app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/roles", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.Roles.ToArrayAsync(ct)))
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Role");

app.MapPost("/roles/", async (AppDbContext db, CreateRole dto, CancellationToken ct) =>
{
    await db.Roles.AddAsync(new Role(dto.Name.ToLower()), ct);
    await db.SaveChangesAsync(ct);
    return Results.Created("/roles","Role created");
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Role");

app.MapDelete("/roles/{roleId}", async (AppDbContext db, int roleId, CancellationToken ct) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r => r.Id== roleId, ct);
    if (role == null) return Results.NotFound();
    db.Roles.Remove(role);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Role");

app.MapGet("/roles/{roleId}", async (AppDbContext db, int roleId, CancellationToken ct) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r=> r.Id==roleId, ct);
    return role != null ? Results.Ok(role) : Results.NotFound();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Role");

app.MapPatch("/roles/{roleId}",async (AppDbContext db, UpdateRole dto, int roleId, CancellationToken ct) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r=> r.Id == roleId, ct);
    if (role == null) return Results.NotFound();
    role.SetRole(dto.Name);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Role");

app.MapGet("/users", async (AppDbContext db, CancellationToken ct) =>
{
    var users = await db.Users
        .Include(u => u.Role)
        .Select(u => new UserResponses
        {
            Id = u.Id,
            Name = u.Name,
            Role = u.Role!.Name,
        }).ToArrayAsync(ct);

    return Results.Ok(users);
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("User");

app.MapPost("/users/", async (IPasswordHasher<CreateUser> hasher, AppDbContext db, CreateUser dto, CancellationToken ct) =>
{
    dto.Password = hasher.HashPassword(dto, dto.Password);
    await db.Users.AddAsync(new User (dto.Name, dto.RoleId, dto.Password), ct);
    await db.SaveChangesAsync(ct);
    return Results.Created("/users","User is created");
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("User");

app.MapDelete("/users/{userId}", async (AppDbContext db, int userId, CancellationToken ct) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    if (user == null)
        return Results.NotFound("User not found");

    db.Users.Remove(user);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("User");

app.MapGet("/users/{userId}", async (AppDbContext db, int userId, CancellationToken ct) =>
{
    var user = await db.Users
        .Include(u=> u.Role)
        .Include(u=> u.Group)        
        .FirstOrDefaultAsync(u => u.Id == userId, ct);

    return user != null
        ? Results.Ok(new {
            Id = user.Id , 
            Name = user.Name,
            Role = user.Role.Name,
            Group = user?.Group.Name ?? "not group",
            IsArchived = user.IsArchive 
            })
        : Results.NotFound("User does not exist");
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("User");

app.MapPatch("/users/{userId}", async (AppDbContext db, int userId, UpdateUser dto, CancellationToken ct) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    if (user == null)
        return Results.NotFound("User does not exist");

    user.SetFields(dto);
    await db.SaveChangesAsync(ct);

    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("User");

app.MapGet("/groups", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.Groups.ToArrayAsync(ct)))
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Group");

app.MapPost("/groups/", async (AppDbContext db, CreateGroup dto, CancellationToken ct) =>
{
    await db.Groups.AddAsync(new Group (dto.Name.ToLower()) ,ct);
    await db.SaveChangesAsync(ct);
    return Results.Created("/group", "group is created");
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Group");

app.MapDelete("/groups/{groupId}", async (AppDbContext db, int groupId, CancellationToken ct) =>
{
    var group = await db.Groups.FirstOrDefaultAsync(g => g.Id== groupId, ct);
    if (group == null) return Results.NotFound();
    db.Groups.Remove(group);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Group")
.WithDescription("Request to DELETE group by groupId");


app.MapGet("/groups/{groupId}", async (AppDbContext db, int groupId, CancellationToken ct) =>
{
    var group = await db.Groups.FirstOrDefaultAsync(g=> g.Id==groupId, ct);
    return group != null ? Results.Ok(group) : Results.NotFound();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Group")
.WithDescription("Request to GET group by groupId");


app.MapPatch("/groups/{groupId}",async (AppDbContext db, UpdateGroup dto, int groupId, CancellationToken ct) =>
{
    var group = await db.Groups.FirstOrDefaultAsync(g=> g.Id == groupId, ct);
    if (group == null) return Results.NotFound();
    group.SetFields(dto);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Group")
.WithDescription("Request to PATCH group by groupId");

app.MapGet("/lessons", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.Lessons.ToArrayAsync(ct)))
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Lesson")
.WithDescription("Request to GET all lessons ");

app.MapPost("/lessons/", async (AppDbContext db, CreateLesson dto, CancellationToken ct) =>
{
    await db.Lessons.AddAsync(new Lesson (dto.Name.ToLower()), ct);
    await db.SaveChangesAsync(ct);
    return Results.Created("/lesson", "lesson is created");
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Lesson")
.WithDescription("Request to POST (add) lesson");


app.MapDelete("/lessons/{lessonId}", async (AppDbContext db, int lessonId, CancellationToken ct) =>
{
    var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id==lessonId, ct);
    if (lesson == null) return Results.NotFound();
    db.Lessons.Remove(lesson);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Lesson")
.WithDescription("Request to DELETE lesson by lessonId");


app.MapGet("/lessons/{lessonId}", async (AppDbContext db, int lessonId, CancellationToken ct) =>
{
    var lesson = await db.Lessons.FirstOrDefaultAsync(l=> l.Id==lessonId, ct);
    return lesson != null ? Results.Ok(lesson) : Results.NotFound();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Lesson")
.WithDescription("Request to get lesson by lessonId");


app.MapPatch("/lessons/{lessonId}",async (AppDbContext db, UpdateLesson dto, int lessonId, CancellationToken ct) =>
{
    var lesson = await db.Lessons.FirstOrDefaultAsync(l=> l.Id == lessonId, ct);
    if (lesson == null) return Results.NotFound();
    lesson.SetFields(dto);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Lesson")
.WithDescription("Request to PATCH (edit) lesson by lessonId");


app.MapGet("/logbooks" , async(AppDbContext db, CancellationToken ct) =>
{
    var logs = await db.Logbooks
        .Include(l=> l.Group)
        .Include(l=> l.Lesson)
        .Include(l=> l.User)
        .ToArrayAsync(ct);
    var result = logs.GroupBy(l=> l.Group).Select( r=>  new LogBookToGroup
    {
        Name = r.Key.Name,
        Info = r.Select(a=> new Info
        {
            User = a.User.Name,
            UserPlace = a.UserPlace.Name,
            LessonId = a.LessonId,
            AbsenceDuration = a.AbsenceDuration,
            AbsenceReason = a.AbsenceReason,
            AbsenceTime = a.CreatedAt
        }).ToArray()
    }
    ).ToArray();
    return Results.Ok(result);
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Logbook")
.WithDescription("Request to GET all logs grouping by group ");


app.MapPost("/logbooks/", async (AppDbContext db ,ClaimsPrincipal claims, CreateLogBook dto, CancellationToken ct ) =>
{
    var currentUser = int.Parse(claims.FindFirstValue(ClaimTypes.NameIdentifier)!);

    await db.Logbooks.AddAsync(new Logbook
    (
        currentUser, DateTime.UtcNow, dto.UserId, dto.GroupId, dto.LessonId,
        dto.AbsenceDuration, dto.IsPresent, dto.AbsenceReason
    ), ct);

    await db.SaveChangesAsync(ct);
    return Results.Created("/logbook","Record is creared");
})
.RequireRateLimiting("token")
.RequireAuthorization(p=> p.RequireRole("admin","teacher","headman"))
.WithTags("Logbook")
.WithDescription("Request to POST (add) log");


app.MapPatch("/logbooks/{logbookId}", async (
    AppDbContext db, 
    ClaimsPrincipal cl, 
    UpdateLogBook dto, 
    int logbookId, 
    CancellationToken ct) =>
{
    var currentUser = int.Parse(cl.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var logbook = await db.Logbooks.FirstOrDefaultAsync(l=> l.Id == logbookId, ct);
    if (logbook == null) return Results.NotFound();
    
    logbook.SetFields(dto, currentUser);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization(p=> p.RequireRole("admin","teacher","headman"))
.WithTags("Logbook")
.WithDescription("Request to PATCH (edit) log by logId");

app.MapDelete("/logbooks/{logbookId}", async (AppDbContext db, int logbookId, CancellationToken ct) =>
{
   var logbook = await db.Logbooks.FirstOrDefaultAsync(l=> l.Id == logbookId, ct);
   if (logbook == null) return Results.NotFound();

    db.Logbooks.Remove(logbook);
    await db.SaveChangesAsync(ct);

    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization(p=> p.RequireRole("admin","teaсher","headman"))
.WithTags("Logbook")
.WithDescription("Request to DELETE log by logbooId");


app.MapGet("/logbooks/{groupId}", async (AppDbContext db, int groupId, CancellationToken ct) =>
{
    var logbook = await db.Logbooks
                        .Where(l=> l.GroupId == groupId)
                        .Include(l=> l.Group)
                        .Include(l=> l.Lesson)
                        .Include(l=> l.User)
                        .Include(l=> l.UserPlace)
                        .OrderBy(r=> r.CreatedAt)
                        .Select(u => new Info
                        {
                            User = u.User.Name,
                            UserPlace = u.UserPlace.Name,
                            LessonId = u.LessonId,
                            AbsenceDuration = u.AbsenceDuration,
                            AbsenceReason = u.AbsenceReason,
                            AbsenceTime = u.CreatedAt
                        })
                        .ToArrayAsync(ct);
    return Results.Ok(logbook);
})
.RequireRateLimiting("token")
.RequireAuthorization(p=> p.RequireRole("admin","teacher","headman"))
.WithTags("Logbook")
.WithDescription("Request to GET log by groupId");

app.MapPost("/login", async (
    AppDbContext db, 
    IPasswordHasher<User> hasher, 
    HttpContext http, 
    IConfiguration cfg, 
    Login dto, 
    CancellationToken ct) =>
{
    var user = await db.Users
        .Include(u=>u.Role)
        .FirstOrDefaultAsync(u=> u.Name == dto.Name, ct);

    if (user == null) return Results.Unauthorized();

    if (hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) != PasswordVerificationResult.Success)
        return Results.Unauthorized();

    user.UpdateRefreshInfo(RefreshToken.GenerateRefreshToken(), DateTime.UtcNow.AddDays(7));

    await db.SaveChangesAsync(ct);

    http.AppendCookiesInfo("refreshToken", user.RefreshToken , (DateTime)user.RefreshTokenExpires);

    return Results.Ok(new { accessToken = JWT.GenToken(user, cfg)});
})
.RequireRateLimiting("token");

app.MapPost("/refresh", async (AppDbContext db, HttpContext http, IConfiguration cfg, CancellationToken ct) =>
{
    var refreshToken = http.Request.Cookies["refreshToken"];
    if (refreshToken == null) return Results.NotFound();

    var user = await db.Users
        .Include(u=>u.Role)
        .FirstOrDefaultAsync(u=> u.RefreshToken == refreshToken, ct);
    if (user==null) return Results.Unauthorized();

    if (user.RefreshTokenExpires < DateTime.UtcNow) return Results.Unauthorized();

    user.UpdateRefreshInfo(RefreshToken.GenerateRefreshToken(), DateTime.UtcNow.AddDays(7));
    await db.SaveChangesAsync(ct);

    http.AppendCookiesInfo("refreshToken", user.RefreshToken , (DateTime)user.RefreshTokenExpires);
    return Results.Ok(new { accessToken = JWT.GenToken(user, cfg)});
})
.RequireRateLimiting("token");

app.MapGet("/users/me", async (AppDbContext db, ClaimsPrincipal claims, HttpContext http, CancellationToken ct) =>
{
    var success = int.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
    if (!success) return Results.Unauthorized();
    var user = await db.Users
        .Include(u=> u.Group)
        .Include(u=> u.Role)
        .FirstOrDefaultAsync(u=> u.Id == userId, ct);
    if (user == null) return Results.Unauthorized();
    var userInfo = new
    {
        Id = user.Id,
        Name = user.Name,
        Group = user.Group?.Name ?? "no group" ,
        Role = user.Role.Name
    };
    var json = JsonSerializer.Serialize(userInfo);
    var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

    http.AppendCookiesInfo("userInfo", base64, (DateTime)user.RefreshTokenExpires);

    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization();

app.MapPost("/users/{id}archive", async (AppDbContext db, int id, CancellationToken ct) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u=> u.Id == id, ct);
    if (user == null) return Results.NotFound();
    if (user.IsArchive == true) return Results.BadRequest();
    user.Archived();
    await db.SaveChangesAsync(ct);
    return Results.NoContent(); 
})
.RequireAuthorization(p=> p.RequireRole("admin"))
.RequireRateLimiting("token");

app.MapPost("/users/set-password", async (
    AppDbContext db, 
    ClaimsPrincipal claims, 
    IPasswordHasher <User> hasher,
    string newPassword,
    CancellationToken ct) =>
{
    var currentUser = int.Parse(claims.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var user = await db.Users.FirstOrDefaultAsync(u=> u.Id == currentUser, ct);
    if (user == null) return Results.NotFound();
    if (user.IsArchive == true) return Results.Conflict();
    user.ChangePassword(user.PasswordHash, hasher.HashPassword(user,newPassword));
    await db.SaveChangesAsync(ct);
    return Results.NoContent(); 
})
.RequireAuthorization()
.RequireRateLimiting("token");

app.MapGet("/schedules", async (AppDbContext db, CancellationToken ct) => 
{
    var schedules = await db.Schedules
        .Include(s=> s.Group)
        .Include(s=> s.Lesson)
        .Select(p=> new
        {
            Id = p.Id,
            Date = p.Date,
            Lesson = p.Lesson.Name,
            Group = p.Group.Name,
            StartAt = p.StartTime,
            EndAt = p.EndTime
        })
        .ToArrayAsync(ct);
    return Results.Ok(schedules);
})
.RequireAuthorization(p=> p.RequireRole("admin"))
.RequireRateLimiting("token");

app.MapPost("/schedules/", async (AppDbContext db, CreateSchedule dto, CancellationToken ct) =>
{
    var schedule = new Schedule(dto.GroupId, dto.LessonId, dto.Date, dto.StartTime, dto.EndTime);
    await db.Schedules.AddAsync(schedule, ct);
    await db.SaveChangesAsync(ct);
    return Results.Created("/schedules","");
})
.RequireAuthorization("admin")
.RequireRateLimiting("token");


app.Run();

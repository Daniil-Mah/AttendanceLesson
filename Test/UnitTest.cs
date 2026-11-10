using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AttendanceLesson;
using AttendanceLesson.DataBase;
using AttendanceLesson.DataBase.Models;
using AttendanceLesson.DTO;
using AttendanceLesson.Responses;
using AttendanceLesson.Service;
using AttendanceLesson.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Xunit;

namespace AttendanceLesson.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);
            var dbContextOptionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextOptionsDescriptor != null) services.Remove(dbContextOptionsDescriptor);

            // Remove background service
            var hostedServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(SetAbsence));
            if (hostedServiceDescriptor != null) services.Remove(hostedServiceDescriptor);

            // Add SQLite in-memory
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Override rate limiter to be permissive for tests
            services.Configure<RateLimiterOptions>(options =>
            {
                options.PolicyMap.Clear();
                options.AddPolicy("token", http => RateLimitPartition.GetNoLimiter("test"));
            });

            // Build the service provider and create the database schema
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}

public class UnitTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly AppDbContext _db;

    public UnitTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Reset database for each test
        _db.Database.EnsureDeleted();
        _db.Database.EnsureCreated();

        SeedRolesAndAdmin();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    private void SeedRolesAndAdmin()
    {
        _db.Roles.AddRange(
            new Role("admin"),
            new Role("teacher"),
            new Role("headman"),
            new Role("student")
        );
        _db.SaveChanges();

        var hasher = new PasswordHasher<User>();
        var tempUser = new User("admin", 1, "dummy");
        var hashed = hasher.HashPassword(tempUser, "admin123");
        var admin = new User("admin", 1, hashed);
        _db.Users.Add(admin);
        _db.SaveChanges();
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync(string username = "admin", string password = "admin123")
    {
        var client = _factory.CreateClient();
        var loginDto = new Login(username, password);
        var response = await client.PostAsJsonAsync("/login", loginDto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.accessToken);
        return client;
    }

    private record LoginResponse(string accessToken);

    private async Task<Group> CreateGroupAsync(string name = "testgroup")
    {
        var group = new Group(name);
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return group;
    }

    private async Task<Lesson> CreateLessonAsync(string name = "math", int teacherId = 1)
    {
        var lesson = new Lesson(name, teacherId);
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return lesson;
    }

    private async Task<User> CreateUserAsync(string name, int roleId, int? groupId = null, string password = "password123")
    {
        var hasher = new PasswordHasher<User>();
        var tempUser = new User(name, roleId, "dummy");
        var hashed = hasher.HashPassword(tempUser, password);
        var user = new User(name, roleId, hashed);
        if (groupId.HasValue) user.SetGroup(groupId.Value);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return user;
    }

    // ---------------------------------------------------------------
    // Role tests
    // ---------------------------------------------------------------
    [Fact]
    public async Task CreateRole_WithValidName_ReturnsCreated()
    {
        var client = await GetAuthenticatedClientAsync();
        var dto = new CreateRole("newrole");
        var response = await client.PostAsJsonAsync("/roles/", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateRole_WithInvalidName_ReturnsBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();
        var dto = new CreateRole("");
        var response = await client.PostAsJsonAsync("/roles/", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRoles_ReturnsRoles()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync("/roles");
        response.EnsureSuccessStatusCode();
        var roles = await response.Content.ReadFromJsonAsync<Role[]>();
        Assert.NotNull(roles);
        Assert.Contains(roles, r => r.Name == "admin");
    }

    [Fact]
    public async Task GetRoleById_WithValidId_ReturnsRole()
    {
        var client = await GetAuthenticatedClientAsync();
        var role = await _db.Roles.FirstAsync(r => r.Name == "admin");
        var response = await client.GetAsync($"/roles/{role.Id}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Role>();
        Assert.Equal("admin", result!.Name);
    }

    [Fact]
    public async Task GetRoleById_WithInvalidId_ReturnsNotFound()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync("/roles/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRole_WithValidName_ReturnsNoContent()
    {
        var client = await GetAuthenticatedClientAsync();
        var role = await _db.Roles.FirstAsync(r => r.Name == "admin");
        var dto = new UpdateRole("updatedadmin");
        var response = await client.PatchAsJsonAsync($"/roles/{role.Id}", dto);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _db.ChangeTracker.Clear();
        var updated = await _db.Roles.FindAsync(role.Id);
        Assert.Equal("updatedadmin", updated!.Name);
    }

    [Fact]
    public async Task DeleteRole_WithValidId_ReturnsNoContent()
    {
        var client = await GetAuthenticatedClientAsync();
        var role = new Role("temp");
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        var response = await client.DeleteAsync($"/roles/{role.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _db.ChangeTracker.Clear();
        Assert.Null(await _db.Roles.FindAsync(role.Id));
    }

    [Fact]
    public async Task RoleEndpoints_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/roles");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------
    // User tests
    // ---------------------------------------------------------------
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        var client = await GetAuthenticatedClientAsync();
        var dto = new CreateUser("newuser", "password123", 4);
        var response = await client.PostAsJsonAsync("/users/", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithInvalidData_ReturnsBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();
        var dto = new CreateUser("", "short", 999);
        var response = await client.PostAsJsonAsync("/users/", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ReturnsUsers()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync("/users");
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<UserResponses[]>();
        Assert.NotNull(users);
        Assert.Contains(users, u => u.Name == "admin");
    }

    [Fact]
    public async Task GetUserById_WithValidId_ReturnsUser()
    {
        var client = await GetAuthenticatedClientAsync();
        var user = await _db.Users.FirstAsync(u => u.Name == "admin");
        var response = await client.GetAsync($"/users/{user.Id}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("admin", content);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsNoContent()
    {
        var client = await GetAuthenticatedClientAsync();
        var user = await _db.Users.FirstAsync(u => u.Name == "admin");
        var dto = new UpdateUser("adminUpdated");
        var response = await client.PatchAsJsonAsync($"/users/{user.Id}", dto);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _db.ChangeTracker.Clear();
        var updated = await _db.Users.FindAsync(user.Id);
        Assert.Equal("adminUpdated", updated!.Name);
    }

    [Fact]
    public async Task ArchiveUser_WithValidId_ReturnsNoContent()
    {
        var client = await GetAuthenticatedClientAsync();
        var user = await CreateUserAsync("tempuser", 4);
        var response = await client.PostAsync($"/users/{user.Id}archive", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _db.ChangeTracker.Clear();
        var archived = await _db.Users.FindAsync(user.Id);
        Assert.True(archived!.IsArchive);
    }

    [Fact]
    public async Task ArchiveUser_AlreadyArchived_ReturnsBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();
        var user = await CreateUserAsync("tempuser2", 4);
        user.Archived();
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        var response = await client.PostAsync($"/users/{user.Id}archive", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetPassword_WithValidPassword_ReturnsNoContent()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.PostAsync("/users/set-password?newPassword=newpassword123", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_ReturnsUserInfo()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync("/users/me");
        response.EnsureSuccessStatusCode();
        var cookies = response.Headers.GetValues("Set-Cookie");
        Assert.Contains(cookies, c => c.StartsWith("userInfo="));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var client = _factory.CreateClient();
        var dto = new Login("admin", "admin123");
        var response = await client.PostAsJsonAsync("/login", dto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.accessToken));
        var cookies = response.Headers.GetValues("Set-Cookie");
        Assert.Contains(cookies, c => c.StartsWith("refreshToken="));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var dto = new Login("admin", "wrongpassword");
        var response = await client.PostAsJsonAsync("/login", dto);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewToken()
    {
        var client = _factory.CreateClient();
        var loginDto = new Login("admin", "admin123");
        var loginResponse = await client.PostAsJsonAsync("/login", loginDto);
        loginResponse.EnsureSuccessStatusCode();
        var refreshCookie = loginResponse.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("refreshToken="));
        var refreshToken = refreshCookie.Split(';')[0].Split('=')[1];

        client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={refreshToken}");

        var refreshResponse = await client.PostAsync("/refresh", null);
        refreshResponse.EnsureSuccessStatusCode();
        var result = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.accessToken));
    }

    [Fact]
    public async Task RefreshToken_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------
    // Group tests
    // ---------------------------------------------------------------
    [Fact]
    public async Task CreateGroup_WithValidName_ReturnsCreated()
    {
        var client = await GetAuthenticatedClientAsync();
        var dto = new CreateGroup("group1");
        var response = await client.PostAsJsonAsync("/groups/", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroup_WithInvalidName_ReturnsBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();
        var dto = new CreateGroup("a");
        var response = await client.PostAsJsonAsync("/groups/", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetGroups_ReturnsGroups()
    {
        var client = await GetAuthenticatedClientAsync();
        await CreateGroupAsync("testgroup");
        var response = await client.GetAsync("/groups");
        response.EnsureSuccessStatusCode();
        var groups = await response.Content.ReadFromJsonAsync<Group[]>();
        Assert.Contains(groups, g => g.Name == "testgroup");
    }

    [Fact]
    public async Task UpdateGroup_WithValidName_ReturnsNoContent()
    {
        var client = await GetAuthenticatedClientAsync();
        var group = await CreateGroupAsync("oldname");
        var dto = new UpdateGroup("newname");
        var response = await client.PatchAsJsonAsync($"/groups/{group.Id}", dto);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _db.ChangeTracker.Clear();
        var updated = await _db.Groups.FindAsync(group.Id);
        Assert.Equal("newname", updated!.Name);
    }

    [Fact]
    public async Task DeleteGroup_WithValidId_ReturnsNoContent()
    {
        var client = await GetAuthenticatedClientAsync();
        var group = await CreateGroupAsync("todelete");
        var response = await client.DeleteAsync($"/groups/{group.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _db.ChangeTracker.Clear();
        Assert.Null(await _db.Groups.FindAsync(group.Id));
    }

    [Fact]
    public async Task GroupEndpoints_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/groups");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------
    // Lesson tests
    // ---------------------------------------------------------------
    [Fact]
    public async Task CreateLesson_WithValidData_ReturnsCreated()
    {
        var client = await GetAuthenticatedClientAsync();
        var teacher = await CreateUserAsync("teacher1", 2);
        var dto = new CreateLesson("physics", teacher.Id);
        var response = await client.PostAsJsonAsync("/lessons/", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateLesson_WithEmptyName_ReturnsBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();
        var teacher = await CreateUserAsync("teacher2", 2);
        var dto = new CreateLesson("", teacher.Id);
        var response = await client.PostAsJsonAsync("/lessons/", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetLessons_ReturnsLessons()
    {
        var client = await GetAuthenticatedClientAsync();
        await CreateLessonAsync("math");
        var response = await client.GetAsync("/lessons");
        response.EnsureSuccessStatusCode();
        var lessons = await response.Content.ReadFromJsonAsync<Lesson[]>();
        Assert.Contains(lessons, l => l.Name == "math");
    }

    [Fact]
    public async Task UpdateLesson_WithValidName_ReturnsNoContent()
    {
        var client = await GetAuthenticatedClientAsync();
        var lesson = await CreateLessonAsync("oldlesson");
        var dto = new UpdateLesson("newlesson");
        var response = await client.PatchAsJsonAsync($"/lessons/{lesson.Id}", dto);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _db.ChangeTracker.Clear();
        var updated = await _db.Lessons.FindAsync(lesson.Id);
        Assert.Equal("newlesson", updated!.Name);
    }

    [Fact]
    public async Task DeleteLesson_WithValidId_ReturnsNoContent()
    {
        var client = await GetAuthenticatedClientAsync();
        var lesson = await CreateLessonAsync("todelete");
        var response = await client.DeleteAsync($"/lessons/{lesson.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _db.ChangeTracker.Clear();
        Assert.Null(await _db.Lessons.FindAsync(lesson.Id));
    }

    [Fact]
    public async Task LessonEndpoints_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/lessons");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------
    // Logbook tests
    // ---------------------------------------------------------------
    [Fact]
    public async Task CreateLogbook_WithValidData_ReturnsCreated()
    {
        var client = await GetAuthenticatedClientAsync();
        var group = await CreateGroupAsync("g1");
        var lesson = await CreateLessonAsync("l1");
        var student = await CreateUserAsync("student1", 4, group.Id);

        var dto = new CreateLogBook(student.Id, group.Id, lesson.Id, TimeSpan.Zero, true, null);
        var response = await client.PostAsJsonAsync("/logbooks/", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateLogbook_WithNegativeAbsenceDuration_ReturnsBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();
        var group = await CreateGroupAsync("g1");
        var lesson = await CreateLessonAsync("l1");
        var student = await CreateUserAsync("student1", 4, group.Id);

        var dto = new CreateLogBook(student.Id, group.Id, lesson.Id, TimeSpan.FromMinutes(-5), false, Status.Forgot);
        var response = await client.PostAsJsonAsync("/logbooks/", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetLogbooks_ReturnsGroupedLogbooks()
    {
        var client = await GetAuthenticatedClientAsync();
        var group = await CreateGroupAsync("g1");
        var lesson = await CreateLessonAsync("l1");
        var student = await CreateUserAsync("student1", 4, group.Id);

        var dto = new CreateLogBook(student.Id, group.Id, lesson.Id, TimeSpan.Zero, true, null);
        await client.PostAsJsonAsync("/logbooks/", dto);

        var response = await client.GetAsync("/logbooks");
        response.EnsureSuccessStatusCode();
        var logbooks = await response.Content.ReadFromJsonAsync<LogBookToGroup[]>();
        Assert.NotNull(logbooks);
        Assert.Contains(logbooks, g => g.Name == "g1");
    }

    [Fact]
    public async Task UpdateLogbook_WithValidData_ReturnsNoContent()
    {
        var client = await GetAuthenticatedClientAsync();
        var group = await CreateGroupAsync("g1");
        var lesson = await CreateLessonAsync("l1");
        var student = await CreateUserAsync("student1", 4, group.Id);

        var logbook = new Logbook(1, DateTime.UtcNow, student.Id, group.Id, lesson.Id, TimeSpan.Zero, true, null);
        _db.Logbooks.Add(logbook);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var dto = new UpdateLogBook(IsPresent: false, AbsenceReason: Status.Forgot, AbsenceDuration: TimeSpan.FromMinutes(10));
        var response = await client.PatchAsJsonAsync($"/logbooks/{logbook.Id}", dto);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _db.ChangeTracker.Clear();
        var updated = await _db.Logbooks.FindAsync(logbook.Id);
        Assert.False(updated!.IsPresent);
        Assert.Equal(Status.Forgot, updated.AbsenceReason);
    }

    [Fact]
    public async Task DeleteLogbook_WithValidId_ReturnsNoContent()
    {
        var client = await GetAuthenticatedClientAsync();
        var group = await CreateGroupAsync("g1");
        var lesson = await CreateLessonAsync("l1");
        var student = await CreateUserAsync("student1", 4, group.Id);

        var logbook = new Logbook(1, DateTime.UtcNow, student.Id, group.Id, lesson.Id, TimeSpan.Zero, true, null);
        _db.Logbooks.Add(logbook);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var response = await client.DeleteAsync($"/logbooks/{logbook.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _db.ChangeTracker.Clear();
        Assert.Null(await _db.Logbooks.FindAsync(logbook.Id));
    }

    [Fact]
    public async Task LogbookEndpoints_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/logbooks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------
    // Schedule tests
    // ---------------------------------------------------------------
    [Fact]
    public async Task CreateSchedule_WithValidData_ReturnsCreated()
    {
        var client = await GetAuthenticatedClientAsync();
        var group = await CreateGroupAsync("g1");
        var lesson = await CreateLessonAsync("l1");
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(10, 0);
        var dto = new CreateSchedule(group.Id, lesson.Id, date, start, end);
        var response = await client.PostAsJsonAsync("/schedules/", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetSchedules_ReturnsSchedules()
    {
        var client = await GetAuthenticatedClientAsync();
        var group = await CreateGroupAsync("g1");
        var lesson = await CreateLessonAsync("l1");
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(10, 0);
        var schedule = new Schedule(group.Id, lesson.Id, date, start, end);
        _db.Schedules.Add(schedule);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var response = await client.GetAsync("/schedules");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("g1", content);
    }

    [Fact]
    public async Task ScheduleEndpoints_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/schedules");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------
    // Validation attribute tests
    // ---------------------------------------------------------------
    [Fact]
    public void TimeSpanNonNegative_WithNegativeValue_ReturnsError()
    {
        var attribute = new TimeSpanNonNegativeAttribute();
        var result = attribute.GetValidationResult(TimeSpan.FromMinutes(-1), new ValidationContext(this));
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void TimeSpanNonNegative_WithPositiveValue_ReturnsSuccess()
    {
        var attribute = new TimeSpanNonNegativeAttribute();
        var result = attribute.GetValidationResult(TimeSpan.FromMinutes(1), new ValidationContext(this));
        Assert.Equal(ValidationResult.Success, result);
    }

    // ---------------------------------------------------------------
    // Exception handler tests
    // ---------------------------------------------------------------
    [Theory]
    [InlineData("23502", 400, "Обязательное поле не заполнено")]
    [InlineData("23503", 400, "Связанная запись не найдена")]
    [InlineData("23505", 409, "Запись уже существует")]
    [InlineData("23514", 400, "Нарушено ограничение")]
    [InlineData("99999", 500, "what")]
    public void ExceptionHandler_ReturnsExpectedStatusAndMessage(string sqlState, int expectedStatusCode, string expectedMessage)
    {
        var ex = new PostgresException("msg", "ERROR", sqlState);
        var (statusCode, message) = ExceptionHandler.GetError(ex);
        Assert.Equal(expectedStatusCode, statusCode);
        Assert.Equal(expectedMessage, message);
    }

    [Fact]
    public void ExceptionHandler_UnwrapsDbUpdateException()
    {
        var pgEx = new PostgresException("msg", "ERROR", "23505");
        var dbEx = new DbUpdateException("msg", pgEx);
        var (statusCode, message) = ExceptionHandler.GetError(dbEx);
        Assert.Equal(409, statusCode);
        Assert.Equal("Запись уже существует", message);
    }

    // ---------------------------------------------------------------
    // Database constraint tests (direct DB access)
    // ---------------------------------------------------------------
    [Fact]
    public async Task Logbook_CheckConstraint_ThrowsWhenAbsenceReasonNullAndNotPresent()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var group = new Group("g1");
        var lesson = new Lesson("l1", 1);
        db.Groups.Add(group);
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();

        var logbook = new Logbook(1, DateTime.UtcNow, 1, group.Id, lesson.Id, TimeSpan.Zero, false, null);
        db.Logbooks.Add(logbook);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Schedule_CheckConstraint_ThrowsWhenStartTimeAfterEndTime()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var group = new Group("g1");
        var lesson = new Lesson("l1", 1);
        db.Groups.Add(group);
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var start = new TimeOnly(10, 0);
        var end = new TimeOnly(9, 0); // end before start
        var schedule = new Schedule(group.Id, lesson.Id, date, start, end);
        db.Schedules.Add(schedule);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    // ---------------------------------------------------------------
    // Service tests
    // ---------------------------------------------------------------
    [Fact]
    public void RefreshToken_GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var token = RefreshToken.GenerateRefreshToken();
        Assert.False(string.IsNullOrWhiteSpace(token));
    }
}

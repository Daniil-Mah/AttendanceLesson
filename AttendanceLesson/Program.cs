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

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(opt=> opt.UseNpgsql(builder.Configuration.GetConnectionString("psql")));
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name="Authoriation",
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
    opt.AddPolicy("admin",p => p.RequireRole("admin"));
    opt.AddPolicy("revizor", p=> p.RequireRole("revizor"));
});

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IPasswordHasher<CreateUser>, PasswordHasher<CreateUser>>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/roles", async (AppDbContext db) => Results.Ok(await db.Roles.ToArrayAsync()));

app.MapPost("/roles/", async (AppDbContext db, CreateRole dto) =>
{
    await db.Roles.AddAsync(new Role {Name=dto.Name.ToLower()});
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireAuthorization("admin")
.WithTags("Role");

app.MapDelete("/roles/{roleId}", async (AppDbContext db, int roleId) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r => r.Id== roleId);
    if (role == null) return Results.NotFound();
    db.Roles.Remove(role);
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireAuthorization("admin")
.WithTags("Role");

app.MapGet("/roles/{roleId}", async (AppDbContext db, int roleId) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r=> r.Id==roleId);
    return role != null ? Results.Ok(role) : Results.NotFound();
})
.RequireAuthorization("admin")
.WithTags("Role");

app.MapPatch("/roles/{roleId}",async (AppDbContext db, int roleId, UpdateRole dto ) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r=> r.Id == roleId);
    if (role == null) return Results.NotFound();
    if (dto.Name != null) role.Name=dto.Name; 
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireAuthorization("admin")
.WithTags("Role");

app.MapGet("/users", async (AppDbContext db) =>
{
    var users = await db.Users
        .Include(u => u.Role)
        .Select(u => new UserResponses
        {
            Id = u.Id,
            Name = u.Name,
            Role = u.Role!.Name,
        }).ToArrayAsync();

    return Results.Ok(users);
})
.RequireAuthorization("admin")
.WithTags("User");

app.MapPost("/users/", async (IPasswordHasher<CreateUser> hasher, AppDbContext db, CreateUser dto) =>
{
    dto.Password = hasher.HashPassword(dto, dto.Password);
    await db.Users.AddAsync(new User
    {
        Name = dto.Name,
        RoleId = dto.RoleId,
        GroupId = dto.GroupId,
        PasswordHash = dto.Password
    });
    await db.SaveChangesAsync();

    return Results.Ok("User is created");
})
.RequireAuthorization("admin")
.WithTags("User");

app.MapDelete("/users/{userId}", async (AppDbContext db, int userId) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null)
        return Results.NotFound("User not found");

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.Ok("User is deleted");
})
.RequireAuthorization("admin")
.WithTags("User");

app.MapGet("/users/{userId}", async (AppDbContext db, int userId) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

    return user != null
        ? Results.Ok(user)
        : Results.NotFound("User does not exist");
})
.RequireAuthorization("admin")
.WithTags("User");

app.MapPatch("/users/{userId}", async (AppDbContext db, int userId, UpdateUser dto) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null)
        return Results.NotFound("User does not exist");

    if (dto.Name != null) user.Name = dto.Name;
    if (dto.GroupId != null) user.GroupId = dto.GroupId;
    if (dto.RoleId != null) user.RoleId = (int)dto.RoleId;

    await db.SaveChangesAsync();

    return Results.Ok("User is edited");
})
.RequireAuthorization("admin")
.WithTags("User");



app.Run();

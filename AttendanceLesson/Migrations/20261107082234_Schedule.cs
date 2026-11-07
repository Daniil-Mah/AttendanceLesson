using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AttendanceLesson.Migrations
{
    /// <inheritdoc />
    public partial class Schedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsArchive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TeacherId",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    LessonId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedules_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Schedules_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_TeacherId",
                table: "Lessons",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_Date",
                table: "Schedules",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_EndTime",
                table: "Schedules",
                column: "EndTime",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_GroupId",
                table: "Schedules",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_LessonId",
                table: "Schedules",
                column: "LessonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_StartTime",
                table: "Schedules",
                column: "StartTime",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Users_TeacherId",
                table: "Lessons",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Users_TeacherId",
                table: "Lessons");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_TeacherId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsArchive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "Lessons");
        }
    }
}

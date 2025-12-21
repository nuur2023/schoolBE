using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schoolBE.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDayOfWeekToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DayOfWeek",
                table: "ClassSubjects",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_SchoolId",
                table: "Exams",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_Schools_SchoolId",
                table: "Exams",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exams_Schools_SchoolId",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Exams_SchoolId",
                table: "Exams");

            migrationBuilder.AlterColumn<int>(
                name: "DayOfWeek",
                table: "ClassSubjects",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

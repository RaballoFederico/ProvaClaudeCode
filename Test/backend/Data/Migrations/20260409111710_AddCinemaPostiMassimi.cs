using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCinemaPostiMassimi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PostiMassimi",
                table: "cinemas",
                type: "int",
                nullable: false,
                defaultValue: 120);

            migrationBuilder.Sql("UPDATE `cinemas` SET `PostiMassimi` = 120 WHERE `PostiMassimi` <= 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostiMassimi",
                table: "cinemas");
        }
    }
}

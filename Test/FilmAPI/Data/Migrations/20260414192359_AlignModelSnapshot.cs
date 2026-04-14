using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_proiezioni_shows_ShowId",
                table: "proiezioni");

            migrationBuilder.AlterColumn<int>(
                name: "ShowId",
                table: "proiezioni",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_proiezioni_shows_ShowId",
                table: "proiezioni",
                column: "ShowId",
                principalTable: "shows",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_proiezioni_shows_ShowId",
                table: "proiezioni");

            migrationBuilder.AlterColumn<int>(
                name: "ShowId",
                table: "proiezioni",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_proiezioni_shows_ShowId",
                table: "proiezioni",
                column: "ShowId",
                principalTable: "shows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

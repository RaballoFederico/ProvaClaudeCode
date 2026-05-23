using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFilmRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "film_ratings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FilmId = table.Column<int>(type: "int", nullable: false),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    Valutazione = table.Column<int>(type: "int", nullable: false),
                    Commento = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_film_ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_film_ratings_films_FilmId",
                        column: x => x.FilmId,
                        principalTable: "films",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_film_ratings_utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_film_ratings_FilmId",
                table: "film_ratings",
                column: "FilmId");

            migrationBuilder.CreateIndex(
                name: "IX_film_ratings_FilmId_UtenteId",
                table: "film_ratings",
                columns: new[] { "FilmId", "UtenteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_film_ratings_UtenteId",
                table: "film_ratings",
                column: "UtenteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "film_ratings");
        }
    }
}

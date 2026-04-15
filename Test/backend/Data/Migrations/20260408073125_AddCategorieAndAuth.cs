using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorieAndAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prenotazioni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    ProiezioneId = table.Column<int>(type: "int", nullable: false),
                    DataPrenotazione = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NumeroPosti = table.Column<int>(type: "int", nullable: false),
                    DataAnnullamento = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prenotazioni", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prenotazioni_proiezioni_ProiezioneId",
                        column: x => x.ProiezioneId,
                        principalTable: "proiezioni",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prenotazioni_utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Token = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_prenotazioni_ProiezioneId",
                table: "prenotazioni",
                column: "ProiezioneId");

            migrationBuilder.CreateIndex(
                name: "IX_prenotazioni_UtenteId",
                table: "prenotazioni",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_Token",
                table: "refresh_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UtenteId",
                table: "refresh_tokens",
                column: "UtenteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prenotazioni");

            migrationBuilder.DropTable(
                name: "refresh_tokens");
        }
    }
}

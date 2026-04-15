using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descrizione = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorie", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ruoli",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descrizione = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ruoli", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "utenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cognome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataRegistrazione = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataUltimoAccesso = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Attivo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RefreshToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utenti", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "films_categorie",
                columns: table => new
                {
                    FilmId = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_films_categorie", x => new { x.FilmId, x.CategoriaId });
                    table.ForeignKey(
                        name: "FK_films_categorie_categorie_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "categorie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_films_categorie_films_FilmId",
                        column: x => x.FilmId,
                        principalTable: "films",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "proiezioni_salvate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    ProiezioneId = table.Column<int>(type: "int", nullable: false),
                    DataSalvataggio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Prenotato = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataPrenotazione = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NumeroPosti = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proiezioni_salvate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proiezioni_salvate_proiezioni_ProiezioneId",
                        column: x => x.ProiezioneId,
                        principalTable: "proiezioni",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proiezioni_salvate_utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "utenti_ruoli",
                columns: table => new
                {
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    RuoloId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utenti_ruoli", x => new { x.UtenteId, x.RuoloId });
                    table.ForeignKey(
                        name: "FK_utenti_ruoli_ruoli_RuoloId",
                        column: x => x.RuoloId,
                        principalTable: "ruoli",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_utenti_ruoli_utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_categorie_Nome",
                table: "categorie",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_films_categorie_CategoriaId",
                table: "films_categorie",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_proiezioni_salvate_ProiezioneId",
                table: "proiezioni_salvate",
                column: "ProiezioneId");

            migrationBuilder.CreateIndex(
                name: "IX_proiezioni_salvate_UtenteId",
                table: "proiezioni_salvate",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_ruoli_Nome",
                table: "ruoli",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_utenti_Email",
                table: "utenti",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_utenti_Username",
                table: "utenti",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_utenti_ruoli_RuoloId",
                table: "utenti_ruoli",
                column: "RuoloId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "films_categorie");

            migrationBuilder.DropTable(
                name: "proiezioni_salvate");

            migrationBuilder.DropTable(
                name: "utenti_ruoli");

            migrationBuilder.DropTable(
                name: "categorie");

            migrationBuilder.DropTable(
                name: "ruoli");

            migrationBuilder.DropTable(
                name: "utenti");
        }
    }
}

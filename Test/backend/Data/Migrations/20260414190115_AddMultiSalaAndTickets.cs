using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiSalaAndTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredCinemaId",
                table: "utenti",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShowId",
                table: "proiezioni_salvate",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShowId",
                table: "proiezioni",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShowId",
                table: "prenotazioni",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cast",
                table: "films",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataRilascio",
                table: "films",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descrizione",
                table: "films",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "Featured",
                table: "films",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Genere",
                table: "films",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RegistaNome",
                table: "films",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodiceLocale",
                table: "cinemas",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitudine",
                table: "cinemas",
                type: "decimal(10,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitudine",
                table: "cinemas",
                type: "decimal(11,8)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "crediti_utente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DataUltimoAggiornamento = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crediti_utente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crediti_utente_utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sale",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CinemaId = table.Column<int>(type: "int", nullable: false),
                    NumeroSala = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipologia = table.Column<int>(type: "int", nullable: false),
                    NumeroFile = table.Column<int>(type: "int", nullable: false),
                    PostiPerFila = table.Column<int>(type: "int", nullable: true),
                    PostiTotali = table.Column<int>(type: "int", nullable: false),
                    ConfigurazionePosti = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Attiva = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sale_cinemas_CinemaId",
                        column: x => x.CinemaId,
                        principalTable: "cinemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "shows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SalaId = table.Column<int>(type: "int", nullable: false),
                    FilmId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    OraInizio = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    OraFine = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    PrezzoBase = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shows_films_FilmId",
                        column: x => x.FilmId,
                        principalTable: "films",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shows_sale_SalaId",
                        column: x => x.SalaId,
                        principalTable: "sale",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "acquisti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    ShowId = table.Column<int>(type: "int", nullable: false),
                    DataAcquisto = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ImportoTotale = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreditoUsato = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    StripeChargeId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    CodiceConferma = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acquisti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_acquisti_shows_ShowId",
                        column: x => x.ShowId,
                        principalTable: "shows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_acquisti_utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "prenotazioni_temporanee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CodiceTemporaneo = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShowId = table.Column<int>(type: "int", nullable: false),
                    Posto = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataScadenza = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prenotazioni_temporanee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prenotazioni_temporanee_shows_ShowId",
                        column: x => x.ShowId,
                        principalTable: "shows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prenotazioni_temporanee_utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "biglietti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AcquistoId = table.Column<int>(type: "int", nullable: false),
                    ShowId = table.Column<int>(type: "int", nullable: false),
                    Posto = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SalaNumero = table.Column<int>(type: "int", nullable: false),
                    TipologiaSala = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Prezzo = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CodiceUnivoco = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodiceHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Validato = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataValidazione = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CinemaId = table.Column<int>(type: "int", nullable: false),
                    QRCodeUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_biglietti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_biglietti_acquisti_AcquistoId",
                        column: x => x.AcquistoId,
                        principalTable: "acquisti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_biglietti_cinemas_CinemaId",
                        column: x => x.CinemaId,
                        principalTable: "cinemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_biglietti_shows_ShowId",
                        column: x => x.ShowId,
                        principalTable: "shows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "transazioni_credito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Importo = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SaldoPrecedente = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SaldoSuccessivo = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DataTransazione = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OperatoreId = table.Column<int>(type: "int", nullable: true),
                    CinemaId = table.Column<int>(type: "int", nullable: true),
                    Descrizione = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AcquistoId = table.Column<int>(type: "int", nullable: true),
                    CreditoUtenteId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transazioni_credito", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transazioni_credito_acquisti_AcquistoId",
                        column: x => x.AcquistoId,
                        principalTable: "acquisti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_transazioni_credito_cinemas_CinemaId",
                        column: x => x.CinemaId,
                        principalTable: "cinemas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_transazioni_credito_crediti_utente_CreditoUtenteId",
                        column: x => x.CreditoUtenteId,
                        principalTable: "crediti_utente",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_transazioni_credito_utenti_OperatoreId",
                        column: x => x.OperatoreId,
                        principalTable: "utenti",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_transazioni_credito_utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_utenti_PreferredCinemaId",
                table: "utenti",
                column: "PreferredCinemaId");

            migrationBuilder.CreateIndex(
                name: "IX_proiezioni_salvate_ShowId",
                table: "proiezioni_salvate",
                column: "ShowId");

            migrationBuilder.CreateIndex(
                name: "IX_proiezioni_ShowId",
                table: "proiezioni",
                column: "ShowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prenotazioni_ShowId",
                table: "prenotazioni",
                column: "ShowId");

            migrationBuilder.CreateIndex(
                name: "IX_acquisti_ShowId",
                table: "acquisti",
                column: "ShowId");

            migrationBuilder.CreateIndex(
                name: "IX_acquisti_UtenteId",
                table: "acquisti",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_biglietti_AcquistoId",
                table: "biglietti",
                column: "AcquistoId");

            migrationBuilder.CreateIndex(
                name: "IX_biglietti_CinemaId",
                table: "biglietti",
                column: "CinemaId");

            migrationBuilder.CreateIndex(
                name: "IX_biglietti_CodiceHash",
                table: "biglietti",
                column: "CodiceHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_biglietti_CodiceUnivoco",
                table: "biglietti",
                column: "CodiceUnivoco",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_biglietti_ShowId",
                table: "biglietti",
                column: "ShowId");

            migrationBuilder.CreateIndex(
                name: "IX_crediti_utente_UtenteId",
                table: "crediti_utente",
                column: "UtenteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prenotazioni_temporanee_DataScadenza",
                table: "prenotazioni_temporanee",
                column: "DataScadenza");

            migrationBuilder.CreateIndex(
                name: "IX_prenotazioni_temporanee_SessionId",
                table: "prenotazioni_temporanee",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_prenotazioni_temporanee_ShowId_Posto",
                table: "prenotazioni_temporanee",
                columns: new[] { "ShowId", "Posto" });

            migrationBuilder.CreateIndex(
                name: "IX_prenotazioni_temporanee_UtenteId",
                table: "prenotazioni_temporanee",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_sale_CinemaId_NumeroSala",
                table: "sale",
                columns: new[] { "CinemaId", "NumeroSala" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shows_FilmId",
                table: "shows",
                column: "FilmId");

            migrationBuilder.CreateIndex(
                name: "IX_shows_SalaId_Data_OraInizio",
                table: "shows",
                columns: new[] { "SalaId", "Data", "OraInizio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transazioni_credito_AcquistoId",
                table: "transazioni_credito",
                column: "AcquistoId");

            migrationBuilder.CreateIndex(
                name: "IX_transazioni_credito_CinemaId",
                table: "transazioni_credito",
                column: "CinemaId");

            migrationBuilder.CreateIndex(
                name: "IX_transazioni_credito_CreditoUtenteId",
                table: "transazioni_credito",
                column: "CreditoUtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_transazioni_credito_OperatoreId",
                table: "transazioni_credito",
                column: "OperatoreId");

            migrationBuilder.CreateIndex(
                name: "IX_transazioni_credito_UtenteId",
                table: "transazioni_credito",
                column: "UtenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_prenotazioni_shows_ShowId",
                table: "prenotazioni",
                column: "ShowId",
                principalTable: "shows",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_proiezioni_shows_ShowId",
                table: "proiezioni",
                column: "ShowId",
                principalTable: "shows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_proiezioni_salvate_shows_ShowId",
                table: "proiezioni_salvate",
                column: "ShowId",
                principalTable: "shows",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_utenti_cinemas_PreferredCinemaId",
                table: "utenti",
                column: "PreferredCinemaId",
                principalTable: "cinemas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prenotazioni_shows_ShowId",
                table: "prenotazioni");

            migrationBuilder.DropForeignKey(
                name: "FK_proiezioni_shows_ShowId",
                table: "proiezioni");

            migrationBuilder.DropForeignKey(
                name: "FK_proiezioni_salvate_shows_ShowId",
                table: "proiezioni_salvate");

            migrationBuilder.DropForeignKey(
                name: "FK_utenti_cinemas_PreferredCinemaId",
                table: "utenti");

            migrationBuilder.DropTable(
                name: "biglietti");

            migrationBuilder.DropTable(
                name: "prenotazioni_temporanee");

            migrationBuilder.DropTable(
                name: "transazioni_credito");

            migrationBuilder.DropTable(
                name: "acquisti");

            migrationBuilder.DropTable(
                name: "crediti_utente");

            migrationBuilder.DropTable(
                name: "shows");

            migrationBuilder.DropTable(
                name: "sale");

            migrationBuilder.DropIndex(
                name: "IX_utenti_PreferredCinemaId",
                table: "utenti");

            migrationBuilder.DropIndex(
                name: "IX_proiezioni_salvate_ShowId",
                table: "proiezioni_salvate");

            migrationBuilder.DropIndex(
                name: "IX_proiezioni_ShowId",
                table: "proiezioni");

            migrationBuilder.DropIndex(
                name: "IX_prenotazioni_ShowId",
                table: "prenotazioni");

            migrationBuilder.DropColumn(
                name: "PreferredCinemaId",
                table: "utenti");

            migrationBuilder.DropColumn(
                name: "ShowId",
                table: "proiezioni_salvate");

            migrationBuilder.DropColumn(
                name: "ShowId",
                table: "proiezioni");

            migrationBuilder.DropColumn(
                name: "ShowId",
                table: "prenotazioni");

            migrationBuilder.DropColumn(
                name: "Cast",
                table: "films");

            migrationBuilder.DropColumn(
                name: "DataRilascio",
                table: "films");

            migrationBuilder.DropColumn(
                name: "Descrizione",
                table: "films");

            migrationBuilder.DropColumn(
                name: "Featured",
                table: "films");

            migrationBuilder.DropColumn(
                name: "Genere",
                table: "films");

            migrationBuilder.DropColumn(
                name: "RegistaNome",
                table: "films");

            migrationBuilder.DropColumn(
                name: "CodiceLocale",
                table: "cinemas");

            migrationBuilder.DropColumn(
                name: "Latitudine",
                table: "cinemas");

            migrationBuilder.DropColumn(
                name: "Longitudine",
                table: "cinemas");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "support_tickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    AssegnatoAId = table.Column<int>(type: "int", nullable: true),
                    Oggetto = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Categoria = table.Column<int>(type: "int", nullable: false),
                    Priorita = table.Column<int>(type: "int", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    CreatoIl = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AggiornatoIl = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ChiusoIl = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_support_tickets_utenti_AssegnatoAId",
                        column: x => x.AssegnatoAId,
                        principalTable: "utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_support_tickets_utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "support_ticket_messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SupportTicketId = table.Column<int>(type: "int", nullable: false),
                    AutoreId = table.Column<int>(type: "int", nullable: false),
                    Staff = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Messaggio = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatoIl = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_ticket_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_support_ticket_messages_support_tickets_SupportTicketId",
                        column: x => x.SupportTicketId,
                        principalTable: "support_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_support_ticket_messages_utenti_AutoreId",
                        column: x => x.AutoreId,
                        principalTable: "utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_support_ticket_messages_AutoreId",
                table: "support_ticket_messages",
                column: "AutoreId");

            migrationBuilder.CreateIndex(
                name: "IX_support_ticket_messages_SupportTicketId_CreatoIl",
                table: "support_ticket_messages",
                columns: new[] { "SupportTicketId", "CreatoIl" });

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_AssegnatoAId",
                table: "support_tickets",
                column: "AssegnatoAId");

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_Stato_Priorita_AggiornatoIl",
                table: "support_tickets",
                columns: new[] { "Stato", "Priorita", "AggiornatoIl" });

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_UtenteId",
                table: "support_tickets",
                column: "UtenteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "support_ticket_messages");

            migrationBuilder.DropTable(
                name: "support_tickets");
        }
    }
}

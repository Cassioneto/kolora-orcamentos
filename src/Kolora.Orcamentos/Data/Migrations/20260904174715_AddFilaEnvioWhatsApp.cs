using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kolora.Orcamentos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFilaEnvioWhatsApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FilaEnvioWhatsApp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraficaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrcamentoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TelefoneDestino = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    JidDestino = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PdfPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Legenda = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Pendente"),
                    Tentativas = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimoErro = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EnviadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilaEnvioWhatsApp", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FilaEnvioWhatsApp_GraficaId_Status",
                table: "FilaEnvioWhatsApp",
                columns: new[] { "GraficaId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FilaEnvioWhatsApp");
        }
    }
}

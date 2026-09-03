using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kolora.Orcamentos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Iban",
                table: "ConfiguracoesGrafica",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MulticaixaExpressNumero",
                table: "ConfiguracoesGrafica",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Iban",
                table: "ConfiguracoesGrafica");

            migrationBuilder.DropColumn(
                name: "MulticaixaExpressNumero",
                table: "ConfiguracoesGrafica");
        }
    }
}

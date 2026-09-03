using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kolora.Orcamentos.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Graficas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Localizacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LogoPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Graficas", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ConfiguracoesGrafica",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraficaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LogoPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NomeExibicaoPdf = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MensagemRodapePdf = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MargemPadraoGlobal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ValidadePadraoDias = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 3),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesGrafica", x => x.Id);
                    table.ForeignKey("FK_ConfiguracoesGrafica_Graficas_GraficaId", x => x.GraficaId, "Graficas", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraficaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TipoCalculo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PrecoCustoBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MargemPadrao = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Produtos", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Materiais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraficaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Unidade = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StockAtual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockMinimo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Materiais", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraficaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Clientes", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Orcamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraficaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClienteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ItensJson = table.Column<string>(type: "TEXT", nullable: false),
                    Validade = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CaminhoPdf = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Orcamentos", x => x.Id));

            migrationBuilder.CreateTable(
                name: "PedidosOrcamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraficaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClienteNome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ClienteTelefone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TextoOriginalCliente = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ProdutoDesejado = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DescricaoPedido = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Quantidade = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OrcamentoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEmOrigem = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecebidoLocalEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_PedidosOrcamento", x => x.Id));

            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Entidade = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntidadeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TipoOperacao = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TentativasEnvio = table.Column<int>(type: "INTEGER", nullable: false),
                    StatusSync = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UltimoErro = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_OutboxEvents", x => x.Id));

            migrationBuilder.CreateTable(
                name: "SyncState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimoPullCatalogo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UltimoPullPedidos = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VersaoSchemaLocal = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_SyncState", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ProdutoMateriais",
                columns: table => new
                {
                    ProdutoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MaterialId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConsumoPorUnidade = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutoMateriais", x => new { x.ProdutoId, x.MaterialId });
                    table.ForeignKey("FK_ProdutoMateriais_Materiais_MaterialId", x => x.MaterialId, "Materiais", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_ProdutoMateriais_Produtos_ProdutoId", x => x.ProdutoId, "Produtos", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_ConfiguracoesGrafica_GraficaId", "ConfiguracoesGrafica", "GraficaId", unique: true);
            migrationBuilder.CreateIndex("IX_OutboxEvents_CriadoEm", "OutboxEvents", "CriadoEm");
            migrationBuilder.CreateIndex("IX_OutboxEvents_StatusSync", "OutboxEvents", "StatusSync");
            migrationBuilder.CreateIndex("IX_ProdutoMateriais_MaterialId", "ProdutoMateriais", "MaterialId");

            migrationBuilder.InsertData("SyncState", new[] { "Id", "VersaoSchemaLocal" }, new object[] { 1, 1 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProdutoMateriais");
            migrationBuilder.DropTable(name: "SyncState");
            migrationBuilder.DropTable(name: "OutboxEvents");
            migrationBuilder.DropTable(name: "PedidosOrcamento");
            migrationBuilder.DropTable(name: "Orcamentos");
            migrationBuilder.DropTable(name: "Clientes");
            migrationBuilder.DropTable(name: "Materiais");
            migrationBuilder.DropTable(name: "Produtos");
            migrationBuilder.DropTable(name: "ConfiguracoesGrafica");
            migrationBuilder.DropTable(name: "Graficas");
        }
    }
}

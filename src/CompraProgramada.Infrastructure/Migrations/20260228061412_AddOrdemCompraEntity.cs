using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompraProgramada.Infrastructure.Migrations
{
    public partial class AddOrdemCompraEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Cestas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataDesativacao = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cestas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CPF = table.Column<string>(type: "varchar(14)", maxLength: 14, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValorMensal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataAdesao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataSaida = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Distribuicoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Distribuicoes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CestaItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Ticker = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Percentual = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CestaId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CestaItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CestaItem_Cestas_CestaId",
                        column: x => x.CestaId,
                        principalTable: "Cestas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ContasGraficas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroConta = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ClienteId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasGraficas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasGraficas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Custodias",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ContaGraficaId = table.Column<long>(type: "bigint", nullable: false),
                    ClienteId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Custodias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Custodias_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrdensCompra",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataConclusao = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdensCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdensCompra_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ValorMensalHistorico",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ValorAnterior = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ValorNovo = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ClienteId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValorMensalHistorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValorMensalHistorico_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DistribuicaoItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Ticker = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantidade = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DistribuicaoId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistribuicaoItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistribuicaoItem_Distribuicoes_DistribuicaoId",
                        column: x => x.DistribuicaoId,
                        principalTable: "Distribuicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CustodiaItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Ticker = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantidade = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecoMedio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CustodiaId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodiaItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustodiaItem_Custodias_CustodiaId",
                        column: x => x.CustodiaId,
                        principalTable: "Custodias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrdemCompraItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrdemCompraId = table.Column<long>(type: "bigint", nullable: false),
                    OrdemCompraId1 = table.Column<long>(type: "bigint", nullable: true),
                    Ticker = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantidade = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorItem = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Fracionario = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdemCompraItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdemCompraItem_OrdensCompra_OrdemCompraId",
                        column: x => x.OrdemCompraId,
                        principalTable: "OrdensCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdemCompraItem_OrdensCompra_OrdemCompraId1",
                        column: x => x.OrdemCompraId1,
                        principalTable: "OrdensCompra",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CestaItem_CestaId",
                table: "CestaItem",
                column: "CestaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasGraficas_ClienteId",
                table: "ContasGraficas",
                column: "ClienteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustodiaItem_CustodiaId",
                table: "CustodiaItem",
                column: "CustodiaId");

            migrationBuilder.CreateIndex(
                name: "IX_Custodias_ClienteId",
                table: "Custodias",
                column: "ClienteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistribuicaoItem_DistribuicaoId",
                table: "DistribuicaoItem",
                column: "DistribuicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdemCompraItem_OrdemCompraId",
                table: "OrdemCompraItem",
                column: "OrdemCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdemCompraItem_OrdemCompraId1",
                table: "OrdemCompraItem",
                column: "OrdemCompraId1");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensCompra_ClienteId",
                table: "OrdensCompra",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorMensalHistorico_ClienteId",
                table: "ValorMensalHistorico",
                column: "ClienteId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CestaItem");

            migrationBuilder.DropTable(
                name: "ContasGraficas");

            migrationBuilder.DropTable(
                name: "CustodiaItem");

            migrationBuilder.DropTable(
                name: "DistribuicaoItem");

            migrationBuilder.DropTable(
                name: "OrdemCompraItem");

            migrationBuilder.DropTable(
                name: "ValorMensalHistorico");

            migrationBuilder.DropTable(
                name: "Cestas");

            migrationBuilder.DropTable(
                name: "Custodias");

            migrationBuilder.DropTable(
                name: "Distribuicoes");

            migrationBuilder.DropTable(
                name: "OrdensCompra");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}

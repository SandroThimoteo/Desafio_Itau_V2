using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompraProgramada.Infrastructure.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdemCompraItem_OrdensCompra_OrdemCompraId1",
                table: "OrdemCompraItem");

            migrationBuilder.DropIndex(
                name: "IX_OrdemCompraItem_OrdemCompraId1",
                table: "OrdemCompraItem");

            migrationBuilder.DropColumn(
                name: "OrdemCompraId1",
                table: "OrdemCompraItem");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OrdemCompraId1",
                table: "OrdemCompraItem",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdemCompraItem_OrdemCompraId1",
                table: "OrdemCompraItem",
                column: "OrdemCompraId1");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdemCompraItem_OrdensCompra_OrdemCompraId1",
                table: "OrdemCompraItem",
                column: "OrdemCompraId1",
                principalTable: "OrdensCompra",
                principalColumn: "Id");
        }
    }
}

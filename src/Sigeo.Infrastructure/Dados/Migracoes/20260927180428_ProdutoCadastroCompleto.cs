using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigeo.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class ProdutoCadastroCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescricaoCompleta",
                schema: "qh",
                table: "Produtos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ean",
                schema: "qh",
                table: "Produtos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                schema: "qh",
                table: "Produtos",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ncm",
                schema: "qh",
                table: "Produtos",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigemSistema",
                schema: "qh",
                table: "Produtos",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantidadePorEmbalagem",
                schema: "qh",
                table: "Produtos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Referencia",
                schema: "qh",
                table: "Produtos",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Ncm",
                schema: "qh",
                table: "Produtos",
                column: "Ncm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Produtos_Ncm",
                schema: "qh",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "DescricaoCompleta",
                schema: "qh",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Ean",
                schema: "qh",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Marca",
                schema: "qh",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Ncm",
                schema: "qh",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "OrigemSistema",
                schema: "qh",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "QuantidadePorEmbalagem",
                schema: "qh",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Referencia",
                schema: "qh",
                table: "Produtos");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualyHub.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class PadraoCatalogoGrupos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FasePadrao",
                schema: "qh",
                table: "CategoriasProduto",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoCustoPadrao",
                schema: "qh",
                table: "CategoriasProduto",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FasePadrao",
                schema: "qh",
                table: "CategoriasProduto");

            migrationBuilder.DropColumn(
                name: "TipoCustoPadrao",
                schema: "qh",
                table: "CategoriasProduto");
        }
    }
}

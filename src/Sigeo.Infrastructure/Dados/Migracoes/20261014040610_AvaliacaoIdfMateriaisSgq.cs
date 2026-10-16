using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigeo.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class AvaliacaoIdfMateriaisSgq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Idf",
                schema: "qh",
                table: "Recebimentos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NotaDocumentacao",
                schema: "qh",
                table: "Recebimentos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NotaPrazo",
                schema: "qh",
                table: "Recebimentos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NotaQualidade",
                schema: "qh",
                table: "Recebimentos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Armazenamento",
                schema: "qh",
                table: "MateriaisControlados",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CriterioQualificacao",
                schema: "qh",
                table: "MateriaisControlados",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormasQualificacao",
                schema: "qh",
                table: "MateriaisControlados",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Norma",
                schema: "qh",
                table: "MateriaisControlados",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServicosQueUtilizam",
                schema: "qh",
                table: "MateriaisControlados",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IdfMedio",
                schema: "qh",
                table: "AvaliacoesSemestrais",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QtdEntregasIdf",
                schema: "qh",
                table: "AvaliacoesSemestrais",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Idf",
                schema: "qh",
                table: "Recebimentos");

            migrationBuilder.DropColumn(
                name: "NotaDocumentacao",
                schema: "qh",
                table: "Recebimentos");

            migrationBuilder.DropColumn(
                name: "NotaPrazo",
                schema: "qh",
                table: "Recebimentos");

            migrationBuilder.DropColumn(
                name: "NotaQualidade",
                schema: "qh",
                table: "Recebimentos");

            migrationBuilder.DropColumn(
                name: "Armazenamento",
                schema: "qh",
                table: "MateriaisControlados");

            migrationBuilder.DropColumn(
                name: "CriterioQualificacao",
                schema: "qh",
                table: "MateriaisControlados");

            migrationBuilder.DropColumn(
                name: "FormasQualificacao",
                schema: "qh",
                table: "MateriaisControlados");

            migrationBuilder.DropColumn(
                name: "Norma",
                schema: "qh",
                table: "MateriaisControlados");

            migrationBuilder.DropColumn(
                name: "ServicosQueUtilizam",
                schema: "qh",
                table: "MateriaisControlados");

            migrationBuilder.DropColumn(
                name: "IdfMedio",
                schema: "qh",
                table: "AvaliacoesSemestrais");

            migrationBuilder.DropColumn(
                name: "QtdEntregasIdf",
                schema: "qh",
                table: "AvaliacoesSemestrais");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Groundkey.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class FornecedorCadastroCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CapitalSocial",
                schema: "qh",
                table: "Fornecedores",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataFundacao",
                schema: "qh",
                table: "Fornecedores",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Mei",
                schema: "qh",
                table: "Fornecedores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OptanteSimples",
                schema: "qh",
                table: "Fornecedores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OrigemAlteradoEm",
                schema: "qh",
                table: "Fornecedores",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigemAlteradoPor",
                schema: "qh",
                table: "Fornecedores",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OrigemCriadoEm",
                schema: "qh",
                table: "Fornecedores",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigemCriadoPor",
                schema: "qh",
                table: "Fornecedores",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigemSistema",
                schema: "qh",
                table: "Fornecedores",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Site",
                schema: "qh",
                table: "Fornecedores",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefone0800",
                schema: "qh",
                table: "Fornecedores",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FornecedorContatos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Funcao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Telefone = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Celular = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Principal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FornecedorContatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FornecedorContatos_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorContatos_FornecedorId",
                schema: "qh",
                table: "FornecedorContatos",
                column: "FornecedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FornecedorContatos",
                schema: "qh");

            migrationBuilder.DropColumn(
                name: "CapitalSocial",
                schema: "qh",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "DataFundacao",
                schema: "qh",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "Mei",
                schema: "qh",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "OptanteSimples",
                schema: "qh",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "OrigemAlteradoEm",
                schema: "qh",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "OrigemAlteradoPor",
                schema: "qh",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "OrigemCriadoEm",
                schema: "qh",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "OrigemCriadoPor",
                schema: "qh",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "OrigemSistema",
                schema: "qh",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "Site",
                schema: "qh",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "Telefone0800",
                schema: "qh",
                table: "Fornecedores");
        }
    }
}

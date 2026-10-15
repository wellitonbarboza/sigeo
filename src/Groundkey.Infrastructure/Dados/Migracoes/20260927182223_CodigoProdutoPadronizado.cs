using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Groundkey.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class CodigoProdutoPadronizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoAnterior",
                schema: "qh",
                table: "Produtos",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoriasProduto",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaiId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Sigla = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CriadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AlteradoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AlteradoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Excluido = table.Column<bool>(type: "bit", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExcluidoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasProduto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoriasProduto_CategoriasProduto_PaiId",
                        column: x => x.PaiId,
                        principalSchema: "qh",
                        principalTable: "CategoriasProduto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasProduto_EmpresaId",
                schema: "qh",
                table: "CategoriasProduto",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasProduto_EmpresaId_PaiId_Nome",
                schema: "qh",
                table: "CategoriasProduto",
                columns: new[] { "EmpresaId", "PaiId", "Nome" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasProduto_EmpresaId_PaiId_Sigla",
                schema: "qh",
                table: "CategoriasProduto",
                columns: new[] { "EmpresaId", "PaiId", "Sigla" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasProduto_PaiId",
                schema: "qh",
                table: "CategoriasProduto",
                column: "PaiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoriasProduto",
                schema: "qh");

            migrationBuilder.DropColumn(
                name: "CodigoAnterior",
                schema: "qh",
                table: "Produtos");
        }
    }
}

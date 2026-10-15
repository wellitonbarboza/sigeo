using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Groundkey.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class PerfisDeAcessoEModulos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExcecoesAcesso",
                schema: "qh",
                table: "Usuarios",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PerfilAcessoId",
                schema: "qh",
                table: "Usuarios",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelasBloqueadas",
                schema: "qh",
                table: "Empresas",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PerfisAcesso",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Base = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Regras = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_PerfisAcesso", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_PerfilAcessoId",
                schema: "qh",
                table: "Usuarios",
                column: "PerfilAcessoId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfisAcesso_EmpresaId",
                schema: "qh",
                table: "PerfisAcesso",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfisAcesso_EmpresaId_Nome",
                schema: "qh",
                table: "PerfisAcesso",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_PerfisAcesso_PerfilAcessoId",
                schema: "qh",
                table: "Usuarios",
                column: "PerfilAcessoId",
                principalSchema: "qh",
                principalTable: "PerfisAcesso",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_PerfisAcesso_PerfilAcessoId",
                schema: "qh",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "PerfisAcesso",
                schema: "qh");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_PerfilAcessoId",
                schema: "qh",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ExcecoesAcesso",
                schema: "qh",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "PerfilAcessoId",
                schema: "qh",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TelasBloqueadas",
                schema: "qh",
                table: "Empresas");
        }
    }
}

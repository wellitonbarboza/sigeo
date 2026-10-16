using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigeo.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class PlataformaEEstruturaLocais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Empresas_Cnpj",
                schema: "qh",
                table: "Empresas");

            migrationBuilder.AddColumn<bool>(
                name: "AdministradorPlataforma",
                schema: "qh",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ContaId",
                schema: "qh",
                table: "Usuarios",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SuporteAte",
                schema: "qh",
                table: "Usuarios",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CriadaEm",
                schema: "qh",
                table: "Empresas",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeloEstrutura",
                schema: "qh",
                table: "Empresas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomesNiveis",
                schema: "qh",
                table: "Empresas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RegistrosPlataforma",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quando = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Quem = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Empresa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Detalhe = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosPlataforma", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ContaId",
                schema: "qh",
                table: "Usuarios",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Cnpj",
                schema: "qh",
                table: "Empresas",
                column: "Cnpj",
                unique: true,
                filter: "[Cnpj] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPlataforma_Quando",
                schema: "qh",
                table: "RegistrosPlataforma",
                column: "Quando");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosPlataforma",
                schema: "qh");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ContaId",
                schema: "qh",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Empresas_Cnpj",
                schema: "qh",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "AdministradorPlataforma",
                schema: "qh",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ContaId",
                schema: "qh",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "SuporteAte",
                schema: "qh",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CriadaEm",
                schema: "qh",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "ModeloEstrutura",
                schema: "qh",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "NomesNiveis",
                schema: "qh",
                table: "Empresas");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Cnpj",
                schema: "qh",
                table: "Empresas",
                column: "Cnpj",
                unique: true);
        }
    }
}

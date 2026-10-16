using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigeo.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class ObraPropria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DonoCusto",
                schema: "qh",
                table: "Obras",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Spe");

            migrationBuilder.AddColumn<Guid>(
                name: "EstabelecimentoDonoId",
                schema: "qh",
                table: "Obras",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Obras_EstabelecimentoDonoId",
                schema: "qh",
                table: "Obras",
                column: "EstabelecimentoDonoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Obras_Estabelecimentos_EstabelecimentoDonoId",
                schema: "qh",
                table: "Obras",
                column: "EstabelecimentoDonoId",
                principalSchema: "qh",
                principalTable: "Estabelecimentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Obras_Estabelecimentos_EstabelecimentoDonoId",
                schema: "qh",
                table: "Obras");

            migrationBuilder.DropIndex(
                name: "IX_Obras_EstabelecimentoDonoId",
                schema: "qh",
                table: "Obras");

            migrationBuilder.DropColumn(
                name: "DonoCusto",
                schema: "qh",
                table: "Obras");

            migrationBuilder.DropColumn(
                name: "EstabelecimentoDonoId",
                schema: "qh",
                table: "Obras");
        }
    }
}

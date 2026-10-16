using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigeo.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class InsumoSubstituidoPor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubstituidoPorId",
                schema: "qh",
                table: "Produtos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_SubstituidoPorId",
                schema: "qh",
                table: "Produtos",
                column: "SubstituidoPorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Produtos_Produtos_SubstituidoPorId",
                schema: "qh",
                table: "Produtos",
                column: "SubstituidoPorId",
                principalSchema: "qh",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Produtos_Produtos_SubstituidoPorId",
                schema: "qh",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_SubstituidoPorId",
                schema: "qh",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "SubstituidoPorId",
                schema: "qh",
                table: "Produtos");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigeo.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class SpeEngenheiro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EngenheiroId",
                schema: "qh",
                table: "Spes",
                type: "uniqueidentifier",
                nullable: true);

            // Cada SPE passa a ter o próprio engenheiro responsável; herda o da obra onde já havia.
            migrationBuilder.Sql("EXEC(N'UPDATE s SET s.EngenheiroId = o.EngenheiroId FROM qh.Spes s JOIN qh.Obras o ON o.Id = s.ObraId WHERE s.EngenheiroId IS NULL AND o.EngenheiroId IS NOT NULL;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EngenheiroId",
                schema: "qh",
                table: "Spes");
        }
    }
}

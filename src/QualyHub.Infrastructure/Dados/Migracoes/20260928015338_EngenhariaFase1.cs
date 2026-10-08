using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualyHub.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class EngenhariaFase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Locais_ObraId",
                schema: "qh",
                table: "Locais");

            migrationBuilder.AddColumn<Guid>(
                name: "SpeId",
                schema: "qh",
                table: "Requisicoes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SpeId",
                schema: "qh",
                table: "Pedidos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AreaPrivativa",
                schema: "qh",
                table: "Locais",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FracaoIdeal",
                schema: "qh",
                table: "Locais",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SpeId",
                schema: "qh",
                table: "Locais",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TipologiaId",
                schema: "qh",
                table: "Locais",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SpeId",
                schema: "qh",
                table: "Contratos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Spes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RazaoSocial = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    NomeFantasia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Cnpj = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InscricaoEstadual = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    EnderecoFaturamento = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    EmailFiscal = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    PatrimonioAfetacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
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
                    table.PrimaryKey("PK_Spes", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Spe_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Tipologias",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Dormitorios = table.Column<int>(type: "int", nullable: false),
                    Suites = table.Column<int>(type: "int", nullable: false),
                    AreaPrivativa = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AreaEquivalente = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Tipologias", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Locais_ObraId_SpeId_Tipo",
                schema: "qh",
                table: "Locais",
                columns: new[] { "ObraId", "SpeId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_Locais_SpeId",
                schema: "qh",
                table: "Locais",
                column: "SpeId");

            migrationBuilder.CreateIndex(
                name: "IX_Locais_TipologiaId",
                schema: "qh",
                table: "Locais",
                column: "TipologiaId");

            migrationBuilder.CreateIndex(
                name: "IX_Spes_EmpresaId",
                schema: "qh",
                table: "Spes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Spes_EmpresaId_Cnpj",
                schema: "qh",
                table: "Spes",
                columns: new[] { "EmpresaId", "Cnpj" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Spes_ObraId_Codigo",
                schema: "qh",
                table: "Spes",
                columns: new[] { "ObraId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Tipologias_EmpresaId",
                schema: "qh",
                table: "Tipologias",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tipologias_ObraId_Codigo",
                schema: "qh",
                table: "Tipologias",
                columns: new[] { "ObraId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Locais_Spes_SpeId",
                schema: "qh",
                table: "Locais",
                column: "SpeId",
                principalSchema: "qh",
                principalTable: "Spes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Locais_Tipologias_TipologiaId",
                schema: "qh",
                table: "Locais",
                column: "TipologiaId",
                principalSchema: "qh",
                principalTable: "Tipologias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Locais_Spes_SpeId",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropForeignKey(
                name: "FK_Locais_Tipologias_TipologiaId",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropTable(
                name: "Spes",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Spe_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Tipologias",
                schema: "qh");

            migrationBuilder.DropIndex(
                name: "IX_Locais_ObraId_SpeId_Tipo",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropIndex(
                name: "IX_Locais_SpeId",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropIndex(
                name: "IX_Locais_TipologiaId",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "SpeId",
                schema: "qh",
                table: "Requisicoes");

            migrationBuilder.DropColumn(
                name: "SpeId",
                schema: "qh",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "AreaPrivativa",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "FracaoIdeal",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "SpeId",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "TipologiaId",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "SpeId",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.CreateIndex(
                name: "IX_Locais_ObraId",
                schema: "qh",
                table: "Locais",
                column: "ObraId");
        }
    }
}

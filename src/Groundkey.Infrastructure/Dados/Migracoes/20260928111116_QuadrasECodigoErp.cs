using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Groundkey.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class QuadrasECodigoErp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Padroniza a coluna de chave externa do ERP para "CodigoErp" (bancos criados antes desta versão).
            // Localiza pelo prefixo e só renomeia onde "CodigoErp" ainda não existe — idempotente; tabelas temporais propagam ao histórico.
            migrationBuilder.Sql(@"
DECLARE @t sysname, @c sysname, @sql nvarchar(400);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT t.name, c.name FROM sys.columns c
    JOIN sys.tables t ON t.object_id = c.object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'qh' AND t.temporal_type IN (0, 2) AND c.name LIKE 'CodigoObra[_A-Z]%' AND c.name <> 'CodigoObra'
      AND NOT EXISTS (SELECT 1 FROM sys.columns x WHERE x.object_id = t.object_id AND x.name = 'CodigoErp');
OPEN cur; FETCH NEXT FROM cur INTO @t, @c;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'qh.' + QUOTENAME(@t) + N'.' + QUOTENAME(@c);
    EXEC sp_rename @sql, N'CodigoErp', N'COLUMN';
    FETCH NEXT FROM cur INTO @t, @c;
END
CLOSE cur; DEALLOCATE cur;");

            migrationBuilder.AddColumn<Guid>(
                name: "QuadraId",
                schema: "qh",
                table: "Spes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Quadras",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NomeCondominio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CnpjCondominio = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Endereco = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Matricula = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AreaTerreno = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Observacoes = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Quadras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quadras_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Quadra_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_Spes_QuadraId",
                schema: "qh",
                table: "Spes",
                column: "QuadraId");

            migrationBuilder.CreateIndex(
                name: "IX_Quadras_EmpresaId",
                schema: "qh",
                table: "Quadras",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Quadras_LocalId",
                schema: "qh",
                table: "Quadras",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_Quadras_ObraId_Codigo",
                schema: "qh",
                table: "Quadras",
                columns: new[] { "ObraId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Spes_Quadras_QuadraId",
                schema: "qh",
                table: "Spes",
                column: "QuadraId",
                principalSchema: "qh",
                principalTable: "Quadras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Spes_Quadras_QuadraId",
                schema: "qh",
                table: "Spes");

            migrationBuilder.DropTable(
                name: "Quadras",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Quadra_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropIndex(
                name: "IX_Spes_QuadraId",
                schema: "qh",
                table: "Spes");

            migrationBuilder.DropColumn(
                name: "QuadraId",
                schema: "qh",
                table: "Spes");
        }
    }
}

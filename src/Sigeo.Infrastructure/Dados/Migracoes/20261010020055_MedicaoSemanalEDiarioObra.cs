using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigeo.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class MedicaoSemanalEDiarioObra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiariosObra",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    ClimaManha = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ClimaTarde = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Condicao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Trabalhado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time", nullable: true),
                    HoraTermino = table.Column<TimeOnly>(type: "time", nullable: true),
                    ObservacaoClima = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ocorrencias = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Visitas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Equipamentos = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AssinadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssinadoPorNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AssinadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiariosObra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiariosObra_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DiarioObra_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "MedicoesSemanais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtividadeChave = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Servico = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Semana = table.Column<DateOnly>(type: "date", nullable: false),
                    DataMedicao = table.Column<DateOnly>(type: "date", nullable: false),
                    Percentual = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    PercentualAnterior = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    FvsPendente = table.Column<bool>(type: "bit", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MedidoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
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
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicoesSemanais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicoesSemanais_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicoesSemanais_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "MedicaoSemanal_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "DiariosObraAtividades",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtividadeChave = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiariosObraAtividades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiariosObraAtividades_DiariosObra_DiarioId",
                        column: x => x.DiarioId,
                        principalSchema: "qh",
                        principalTable: "DiariosObra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiariosObraAtividades_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiariosObraEfetivo",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Terceiro = table.Column<bool>(type: "bit", nullable: false),
                    FuncaoObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiariosObraEfetivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiariosObraEfetivo_DiariosObra_DiarioId",
                        column: x => x.DiarioId,
                        principalSchema: "qh",
                        principalTable: "DiariosObra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiariosObra_EmpresaId",
                schema: "qh",
                table: "DiariosObra",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_DiariosObra_EmpresaId_ObraId",
                schema: "qh",
                table: "DiariosObra",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_DiariosObra_ObraId_Data",
                schema: "qh",
                table: "DiariosObra",
                columns: new[] { "ObraId", "Data" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DiariosObra_ObraId_Numero",
                schema: "qh",
                table: "DiariosObra",
                columns: new[] { "ObraId", "Numero" });

            migrationBuilder.CreateIndex(
                name: "IX_DiariosObraAtividades_DiarioId",
                schema: "qh",
                table: "DiariosObraAtividades",
                column: "DiarioId");

            migrationBuilder.CreateIndex(
                name: "IX_DiariosObraAtividades_LocalId",
                schema: "qh",
                table: "DiariosObraAtividades",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_DiariosObraEfetivo_DiarioId",
                schema: "qh",
                table: "DiariosObraEfetivo",
                column: "DiarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicoesSemanais_EmpresaId",
                schema: "qh",
                table: "MedicoesSemanais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicoesSemanais_EmpresaId_ObraId",
                schema: "qh",
                table: "MedicoesSemanais",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicoesSemanais_LocalId",
                schema: "qh",
                table: "MedicoesSemanais",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicoesSemanais_ObraId_AtividadeChave_Semana",
                schema: "qh",
                table: "MedicoesSemanais",
                columns: new[] { "ObraId", "AtividadeChave", "Semana" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MedicoesSemanais_ObraId_Semana",
                schema: "qh",
                table: "MedicoesSemanais",
                columns: new[] { "ObraId", "Semana" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiariosObraAtividades",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "DiariosObraEfetivo",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "MedicoesSemanais",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "MedicaoSemanal_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "DiariosObra",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DiarioObra_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigeo.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class PlanejamentoRedes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Feriados",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Feriados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feriados_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RedesPlanejamento",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Cor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    Pulmao = table.Column<int>(type: "int", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_RedesPlanejamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RedesPlanejamento_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VersoesCronograma",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InicioProjeto = table.Column<DateOnly>(type: "date", nullable: false),
                    DataStatus = table.Column<DateOnly>(type: "date", nullable: true),
                    TerminoPrevisto = table.Column<DateOnly>(type: "date", nullable: true),
                    TerminoBase = table.Column<DateOnly>(type: "date", nullable: true),
                    CustoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EnviadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EnviadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AprovadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AprovadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MotivoDevolucao = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_VersoesCronograma", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VersoesCronograma_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "VersaoCronograma_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "RedeLocais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedeLocais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RedeLocais_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RedeLocais_RedesPlanejamento_RedeId",
                        column: x => x.RedeId,
                        principalSchema: "qh",
                        principalTable: "RedesPlanejamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RedePacotes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ComposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DuracaoPorLocal = table.Column<int>(type: "int", nullable: false),
                    Equipes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedePacotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RedePacotes_Composicoes_ComposicaoId",
                        column: x => x.ComposicaoId,
                        principalSchema: "qh",
                        principalTable: "Composicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RedePacotes_RedesPlanejamento_RedeId",
                        column: x => x.RedeId,
                        principalSchema: "qh",
                        principalTable: "RedesPlanejamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AtividadesCronograma",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaiId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Eap = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Duracao = table.Column<int>(type: "int", nullable: false),
                    Inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Termino = table.Column<DateOnly>(type: "date", nullable: false),
                    InicioBase = table.Column<DateOnly>(type: "date", nullable: true),
                    TerminoBase = table.Column<DateOnly>(type: "date", nullable: true),
                    InicioMinimo = table.Column<DateOnly>(type: "date", nullable: true),
                    Avanco = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    Custo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ComposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PacoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Chave = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Resumo = table.Column<bool>(type: "bit", nullable: false),
                    Folga = table.Column<int>(type: "int", nullable: true),
                    Critica = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtividadesCronograma", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtividadesCronograma_AtividadesCronograma_PaiId",
                        column: x => x.PaiId,
                        principalSchema: "qh",
                        principalTable: "AtividadesCronograma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AtividadesCronograma_Composicoes_ComposicaoId",
                        column: x => x.ComposicaoId,
                        principalSchema: "qh",
                        principalTable: "Composicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AtividadesCronograma_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AtividadesCronograma_VersoesCronograma_VersaoId",
                        column: x => x.VersaoId,
                        principalSchema: "qh",
                        principalTable: "VersoesCronograma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DesembolsoVersao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mes = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorBase = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesembolsoVersao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesembolsoVersao_VersoesCronograma_VersaoId",
                        column: x => x.VersaoId,
                        principalSchema: "qh",
                        principalTable: "VersoesCronograma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RedePacoteLocais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Incluido = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedePacoteLocais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RedePacoteLocais_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RedePacoteLocais_RedePacotes_PacoteId",
                        column: x => x.PacoteId,
                        principalSchema: "qh",
                        principalTable: "RedePacotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VinculosRede",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacoteOrigemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacoteDestinoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Modo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Deslocamento = table.Column<int>(type: "int", nullable: false),
                    Lag = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinculosRede", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VinculosRede_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VinculosRede_RedePacotes_PacoteDestinoId",
                        column: x => x.PacoteDestinoId,
                        principalSchema: "qh",
                        principalTable: "RedePacotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VinculosRede_RedePacotes_PacoteOrigemId",
                        column: x => x.PacoteOrigemId,
                        principalSchema: "qh",
                        principalTable: "RedePacotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VinculosAtividade",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PredecessoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SucessoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Lag = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinculosAtividade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VinculosAtividade_AtividadesCronograma_PredecessoraId",
                        column: x => x.PredecessoraId,
                        principalSchema: "qh",
                        principalTable: "AtividadesCronograma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VinculosAtividade_AtividadesCronograma_SucessoraId",
                        column: x => x.SucessoraId,
                        principalSchema: "qh",
                        principalTable: "AtividadesCronograma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VinculosAtividade_VersoesCronograma_VersaoId",
                        column: x => x.VersaoId,
                        principalSchema: "qh",
                        principalTable: "VersoesCronograma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtividadesCronograma_ComposicaoId",
                schema: "qh",
                table: "AtividadesCronograma",
                column: "ComposicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_AtividadesCronograma_LocalId",
                schema: "qh",
                table: "AtividadesCronograma",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_AtividadesCronograma_PaiId",
                schema: "qh",
                table: "AtividadesCronograma",
                column: "PaiId");

            migrationBuilder.CreateIndex(
                name: "IX_AtividadesCronograma_VersaoId_PaiId_Ordem",
                schema: "qh",
                table: "AtividadesCronograma",
                columns: new[] { "VersaoId", "PaiId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_DesembolsoVersao_VersaoId_Mes",
                schema: "qh",
                table: "DesembolsoVersao",
                columns: new[] { "VersaoId", "Mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_EmpresaId",
                schema: "qh",
                table: "Feriados",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_EmpresaId_Data",
                schema: "qh",
                table: "Feriados",
                columns: new[] { "EmpresaId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_ObraId",
                schema: "qh",
                table: "Feriados",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_RedeLocais_LocalId",
                schema: "qh",
                table: "RedeLocais",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_RedeLocais_RedeId_LocalId",
                schema: "qh",
                table: "RedeLocais",
                columns: new[] { "RedeId", "LocalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RedePacoteLocais_LocalId",
                schema: "qh",
                table: "RedePacoteLocais",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_RedePacoteLocais_PacoteId_LocalId",
                schema: "qh",
                table: "RedePacoteLocais",
                columns: new[] { "PacoteId", "LocalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RedePacotes_ComposicaoId",
                schema: "qh",
                table: "RedePacotes",
                column: "ComposicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_RedePacotes_RedeId_Ordem",
                schema: "qh",
                table: "RedePacotes",
                columns: new[] { "RedeId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_RedesPlanejamento_EmpresaId",
                schema: "qh",
                table: "RedesPlanejamento",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_RedesPlanejamento_EmpresaId_ObraId",
                schema: "qh",
                table: "RedesPlanejamento",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_RedesPlanejamento_ObraId",
                schema: "qh",
                table: "RedesPlanejamento",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_VersoesCronograma_EmpresaId",
                schema: "qh",
                table: "VersoesCronograma",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_VersoesCronograma_EmpresaId_ObraId",
                schema: "qh",
                table: "VersoesCronograma",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_VersoesCronograma_ObraId_Numero",
                schema: "qh",
                table: "VersoesCronograma",
                columns: new[] { "ObraId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosAtividade_PredecessoraId_SucessoraId",
                schema: "qh",
                table: "VinculosAtividade",
                columns: new[] { "PredecessoraId", "SucessoraId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VinculosAtividade_SucessoraId",
                schema: "qh",
                table: "VinculosAtividade",
                column: "SucessoraId");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosAtividade_VersaoId",
                schema: "qh",
                table: "VinculosAtividade",
                column: "VersaoId");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosRede_ObraId",
                schema: "qh",
                table: "VinculosRede",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosRede_PacoteDestinoId",
                schema: "qh",
                table: "VinculosRede",
                column: "PacoteDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosRede_PacoteOrigemId",
                schema: "qh",
                table: "VinculosRede",
                column: "PacoteOrigemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DesembolsoVersao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Feriados",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "RedeLocais",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "RedePacoteLocais",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "VinculosAtividade",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "VinculosRede",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "AtividadesCronograma",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "RedePacotes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "VersoesCronograma",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "VersaoCronograma_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "RedesPlanejamento",
                schema: "qh");
        }
    }
}

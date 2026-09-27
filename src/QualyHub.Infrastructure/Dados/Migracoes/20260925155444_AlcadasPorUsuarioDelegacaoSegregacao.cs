using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualyHub.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class AlcadasPorUsuarioDelegacaoSegregacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SolicitanteNome",
                schema: "qh",
                table: "SolicitacoesAprovacao",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SolicitanteUsuarioId",
                schema: "qh",
                table: "SolicitacoesAprovacao",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DelegacaoAprovacaoId",
                schema: "qh",
                table: "EtapasAprovacao",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmNomeDeNome",
                schema: "qh",
                table: "EtapasAprovacao",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EmNomeDeUsuarioId",
                schema: "qh",
                table: "EtapasAprovacao",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DelegacoesAprovacao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitularUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubstitutoUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Inicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Fim = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Entidade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Revogada = table.Column<bool>(type: "bit", nullable: false),
                    RevogadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_DelegacoesAprovacao", x => x.Id);
                    table.CheckConstraint("CK_Delegacao_Periodo", "[Fim] > [Inicio]");
                    table.CheckConstraint("CK_Delegacao_Pessoas", "[TitularUsuarioId] <> [SubstitutoUsuarioId]");
                    table.ForeignKey(
                        name: "FK_DelegacoesAprovacao_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegacoesAprovacao_Usuarios_SubstitutoUsuarioId",
                        column: x => x.SubstitutoUsuarioId,
                        principalSchema: "qh",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegacoesAprovacao_Usuarios_TitularUsuarioId",
                        column: x => x.TitularUsuarioId,
                        principalSchema: "qh",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DelegacaoAprovacao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "LimitesAprovacao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Entidade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValorMaximo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_LimitesAprovacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LimitesAprovacao_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LimitesAprovacao_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "qh",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "LimiteAprovacao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesAprovacao_SolicitanteUsuarioId",
                schema: "qh",
                table: "SolicitacoesAprovacao",
                column: "SolicitanteUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EtapasAprovacao_DelegacaoAprovacaoId",
                schema: "qh",
                table: "EtapasAprovacao",
                column: "DelegacaoAprovacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_EtapasAprovacao_UsuarioAprovadorId",
                schema: "qh",
                table: "EtapasAprovacao",
                column: "UsuarioAprovadorId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegacoesAprovacao_EmpresaId",
                schema: "qh",
                table: "DelegacoesAprovacao",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegacoesAprovacao_ObraId",
                schema: "qh",
                table: "DelegacoesAprovacao",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegacoesAprovacao_SubstitutoUsuarioId_Inicio_Fim",
                schema: "qh",
                table: "DelegacoesAprovacao",
                columns: new[] { "SubstitutoUsuarioId", "Inicio", "Fim" });

            migrationBuilder.CreateIndex(
                name: "IX_DelegacoesAprovacao_TitularUsuarioId",
                schema: "qh",
                table: "DelegacoesAprovacao",
                column: "TitularUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_LimitesAprovacao_EmpresaId",
                schema: "qh",
                table: "LimitesAprovacao",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LimitesAprovacao_ObraId",
                schema: "qh",
                table: "LimitesAprovacao",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_LimitesAprovacao_UsuarioId_Entidade_ObraId",
                schema: "qh",
                table: "LimitesAprovacao",
                columns: new[] { "UsuarioId", "Entidade", "ObraId" },
                unique: true,
                filter: "[Excluido] = 0 AND [Ativo] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_EtapasAprovacao_DelegacoesAprovacao_DelegacaoAprovacaoId",
                schema: "qh",
                table: "EtapasAprovacao",
                column: "DelegacaoAprovacaoId",
                principalSchema: "qh",
                principalTable: "DelegacoesAprovacao",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EtapasAprovacao_DelegacoesAprovacao_DelegacaoAprovacaoId",
                schema: "qh",
                table: "EtapasAprovacao");

            migrationBuilder.DropTable(
                name: "DelegacoesAprovacao",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DelegacaoAprovacao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "LimitesAprovacao",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "LimiteAprovacao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropIndex(
                name: "IX_SolicitacoesAprovacao_SolicitanteUsuarioId",
                schema: "qh",
                table: "SolicitacoesAprovacao");

            migrationBuilder.DropIndex(
                name: "IX_EtapasAprovacao_DelegacaoAprovacaoId",
                schema: "qh",
                table: "EtapasAprovacao");

            migrationBuilder.DropIndex(
                name: "IX_EtapasAprovacao_UsuarioAprovadorId",
                schema: "qh",
                table: "EtapasAprovacao");

            migrationBuilder.DropColumn(
                name: "SolicitanteNome",
                schema: "qh",
                table: "SolicitacoesAprovacao");

            migrationBuilder.DropColumn(
                name: "SolicitanteUsuarioId",
                schema: "qh",
                table: "SolicitacoesAprovacao");

            migrationBuilder.DropColumn(
                name: "DelegacaoAprovacaoId",
                schema: "qh",
                table: "EtapasAprovacao");

            migrationBuilder.DropColumn(
                name: "EmNomeDeNome",
                schema: "qh",
                table: "EtapasAprovacao");

            migrationBuilder.DropColumn(
                name: "EmNomeDeUsuarioId",
                schema: "qh",
                table: "EtapasAprovacao");
        }
    }
}

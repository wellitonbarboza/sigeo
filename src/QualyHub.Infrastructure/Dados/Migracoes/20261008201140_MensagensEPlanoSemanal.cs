using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualyHub.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class MensagensEPlanoSemanal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvisosEmail",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Evento = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Para = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NomeDestinatario = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Assunto = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Link = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Tentativas = table.Column<int>(type: "int", nullable: false),
                    ProximaTentativaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EnviadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Detalhe = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_AvisosEmail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conversas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Assunto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RegistroEntidade = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    RegistroId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RegistroDescricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RemetenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemetenteNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UltimaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                    table.PrimaryKey("PK_Conversas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversas_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Conversa_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "PlanosSemanais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Semana = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Ppc = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
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
                    table.PrimaryKey("PK_PlanosSemanais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanosSemanais_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PlanoSemanal_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Restricoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AtividadeChave = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AtividadeNome = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    InicioPrevisto = table.Column<DateOnly>(type: "date", nullable: true),
                    ResponsavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsavelNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DataLimite = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Resolucao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemovidaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RemovidaPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
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
                    table.PrimaryKey("PK_Restricoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Restricoes_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Restricoes_Usuarios_ResponsavelId",
                        column: x => x.ResponsavelId,
                        principalSchema: "qh",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Restricao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "MensagensConversa",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AutorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AutorNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnviadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagensConversa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensagensConversa_Conversas_ConversaId",
                        column: x => x.ConversaId,
                        principalSchema: "qh",
                        principalTable: "Conversas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantesConversa",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Papel = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Arquivada = table.Column<bool>(type: "bit", nullable: false),
                    LidaAte = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_ParticipantesConversa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipantesConversa_Conversas_ConversaId",
                        column: x => x.ConversaId,
                        principalSchema: "qh",
                        principalTable: "Conversas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantesConversa_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "qh",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompromissosSemanais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    AtividadeChave = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Meta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Responsavel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ForaDaMeta = table.Column<bool>(type: "bit", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Causa = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ObservacaoCausa = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompromissosSemanais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompromissosSemanais_PlanosSemanais_PlanoId",
                        column: x => x.PlanoId,
                        principalSchema: "qh",
                        principalTable: "PlanosSemanais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeiturasMensagem",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MensagemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LidaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeiturasMensagem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeiturasMensagem_MensagensConversa_MensagemId",
                        column: x => x.MensagemId,
                        principalSchema: "qh",
                        principalTable: "MensagensConversa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvisosEmail_EmpresaId",
                schema: "qh",
                table: "AvisosEmail",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_AvisosEmail_EmpresaId_Chave",
                schema: "qh",
                table: "AvisosEmail",
                columns: new[] { "EmpresaId", "Chave" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AvisosEmail_Situacao_ProximaTentativaEm",
                schema: "qh",
                table: "AvisosEmail",
                columns: new[] { "Situacao", "ProximaTentativaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_CompromissosSemanais_PlanoId_Ordem",
                schema: "qh",
                table: "CompromissosSemanais",
                columns: new[] { "PlanoId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversas_EmpresaId",
                schema: "qh",
                table: "Conversas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversas_EmpresaId_Numero",
                schema: "qh",
                table: "Conversas",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Conversas_ObraId",
                schema: "qh",
                table: "Conversas",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversas_RegistroEntidade_RegistroId",
                schema: "qh",
                table: "Conversas",
                columns: new[] { "RegistroEntidade", "RegistroId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeiturasMensagem_MensagemId_UsuarioId",
                schema: "qh",
                table: "LeiturasMensagem",
                columns: new[] { "MensagemId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MensagensConversa_ConversaId_EnviadaEm",
                schema: "qh",
                table: "MensagensConversa",
                columns: new[] { "ConversaId", "EnviadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantesConversa_ConversaId_UsuarioId",
                schema: "qh",
                table: "ParticipantesConversa",
                columns: new[] { "ConversaId", "UsuarioId" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantesConversa_EmpresaId",
                schema: "qh",
                table: "ParticipantesConversa",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantesConversa_UsuarioId_Arquivada",
                schema: "qh",
                table: "ParticipantesConversa",
                columns: new[] { "UsuarioId", "Arquivada" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanosSemanais_EmpresaId",
                schema: "qh",
                table: "PlanosSemanais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosSemanais_EmpresaId_ObraId",
                schema: "qh",
                table: "PlanosSemanais",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanosSemanais_ObraId_Semana",
                schema: "qh",
                table: "PlanosSemanais",
                columns: new[] { "ObraId", "Semana" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Restricoes_EmpresaId",
                schema: "qh",
                table: "Restricoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Restricoes_EmpresaId_ObraId",
                schema: "qh",
                table: "Restricoes",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Restricoes_ObraId_AtividadeChave",
                schema: "qh",
                table: "Restricoes",
                columns: new[] { "ObraId", "AtividadeChave" });

            migrationBuilder.CreateIndex(
                name: "IX_Restricoes_ObraId_Status_DataLimite",
                schema: "qh",
                table: "Restricoes",
                columns: new[] { "ObraId", "Status", "DataLimite" });

            migrationBuilder.CreateIndex(
                name: "IX_Restricoes_ResponsavelId",
                schema: "qh",
                table: "Restricoes",
                column: "ResponsavelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvisosEmail",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "CompromissosSemanais",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "LeiturasMensagem",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ParticipantesConversa",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Restricoes",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Restricao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "PlanosSemanais",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PlanoSemanal_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "MensagensConversa",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Conversas",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Conversa_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}

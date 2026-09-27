using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualyHub.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "qh");

            migrationBuilder.CreateTable(
                name: "Alcadas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Entidade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    PerfilAprovador = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    UsuarioAprovadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValorMinimo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ValorMaximo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
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
                    table.PrimaryKey("PK_Alcadas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Auditoria",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quando = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Entidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntidadeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Operacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValoresAnterioresJson = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: true),
                    ValoresNovosJson = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: true),
                    CamposAlterados = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: true),
                    Origem = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosSgq",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ProcessoResponsavel = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevisaoVigente = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PrazoRetencaoAnos = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_DocumentosSgq", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DocumentoSgq_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Empresas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RazaoSocial = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    NomeFantasia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Cnpj = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    NivelSiac = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fornecedores",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RazaoSocial = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    NomeFantasia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CpfCnpj = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Telefone = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Cidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    FornecedorMaterialControlado = table.Column<bool>(type: "bit", nullable: false),
                    PrestadorServicoControlado = table.Column<bool>(type: "bit", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DataQualificacao = table.Column<DateOnly>(type: "date", nullable: true),
                    ProximaReavaliacao = table.Column<DateOnly>(type: "date", nullable: true),
                    CodigoObraPrima = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    UltimaImportacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Fornecedores", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Fornecedor_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "LayoutsImportacao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    LinhaCabecalho = table.Column<int>(type: "int", nullable: false),
                    NomePlanilha = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Delimitador = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    MapeamentoJson = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: false),
                    Padrao = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_LayoutsImportacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LotesImportacao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LayoutImportacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ArquivoNomeOriginal = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ArquivoBlob = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ArquivoHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TotalLinhas = table.Column<int>(type: "int", nullable: false),
                    LinhasInserir = table.Column<int>(type: "int", nullable: false),
                    LinhasAtualizar = table.Column<int>(type: "int", nullable: false),
                    LinhasSemAlteracao = table.Column<int>(type: "int", nullable: false),
                    LinhasComErro = table.Column<int>(type: "int", nullable: false),
                    ConfirmadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConfirmadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("PK_LotesImportacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Obras",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Endereco = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Cidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: true),
                    DataPrevistaTermino = table.Column<DateOnly>(type: "date", nullable: true),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    CodigoObraPrima = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    UltimaImportacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Obras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacoesAprovacao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Entidade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EntidadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Titulo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EtapaAtual = table.Column<int>(type: "int", nullable: false),
                    EnviadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConcluidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_SolicitacoesAprovacao", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SolicitacaoAprovacao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "TiposDocumento",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AplicaA = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ValidadePadraoDias = table.Column<int>(type: "int", nullable: true),
                    DiasAlertaAntesVencimento = table.Column<int>(type: "int", nullable: false),
                    Obrigatorio = table.Column<bool>(type: "bit", nullable: false),
                    BloqueiaQuandoVencido = table.Column<bool>(type: "bit", nullable: false),
                    ContemDadoSensivel = table.Column<bool>(type: "bit", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_TiposDocumento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntraObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Perfil = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormModelos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Versao = table.Column<int>(type: "int", nullable: false),
                    DocumentoSgqOrigemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Vigente = table.Column<bool>(type: "bit", nullable: false),
                    ExigeFotoGeral = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_FormModelos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormModelos_DocumentosSgq_DocumentoSgqOrigemId",
                        column: x => x.DocumentoSgqOrigemId,
                        principalSchema: "qh",
                        principalTable: "DocumentosSgq",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "FormModelo_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "VersoesDocumentoSgq",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoSgqId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revisao = table.Column<int>(type: "int", nullable: false),
                    DescricaoAlteracao = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: false),
                    ArquivoBlob = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ArquivoHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ElaboradoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AnalisadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AprovadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AprovadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_VersoesDocumentoSgq", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VersoesDocumentoSgq_DocumentosSgq_DocumentoSgqId",
                        column: x => x.DocumentoSgqId,
                        principalSchema: "qh",
                        principalTable: "DocumentosSgq",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "VersaoDocumentoSgq_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "FornecedorAvaliacoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataAvaliacao = table.Column<DateOnly>(type: "date", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    NotaPrazo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NotaQualidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NotaAtendimento = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NotaDocumentacao = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NotaFinal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StatusAprovacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("PK_FornecedorAvaliacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FornecedorAvaliacoes_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Funcionarios",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CpfHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CpfMascarado = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Funcao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Terceiro = table.Column<bool>(type: "bit", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CodigoObraPrima = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    UltimaImportacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Funcionarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Funcionarios_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LinhasImportacao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoteImportacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroLinha = table.Column<int>(type: "int", nullable: false),
                    ChaveNatural = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DadosJson = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Erros = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: true),
                    EntidadeAfetadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinhasImportacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinhasImportacao_LotesImportacao_LoteImportacaoId",
                        column: x => x.LoteImportacaoId,
                        principalSchema: "qh",
                        principalTable: "LotesImportacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Locais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalPaiId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
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
                    table.PrimaryKey("PK_Locais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locais_Locais_LocalPaiId",
                        column: x => x.LocalPaiId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Locais_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Medicoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    NumeroContrato = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodoInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodoFim = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorMedido = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorRetencao = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Bloqueios = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: true),
                    CodigoObraPrima = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    UltimaImportacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Medicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medicoes_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Medicoes_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Medicao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Pedidos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DataPedido = table.Column<DateOnly>(type: "date", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrevisaoEntrega = table.Column<DateOnly>(type: "date", nullable: true),
                    Solicitante = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Bloqueios = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: true),
                    CodigoObraPrima = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    UltimaImportacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedidos_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pedidos_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Pedido_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "EtapasAprovacao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitacaoAprovacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    PerfilAprovador = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    UsuarioAprovadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Decisao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DecididoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecididoPorNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DecididoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Justificativa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapasAprovacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtapasAprovacao_SolicitacoesAprovacao_SolicitacaoAprovacaoId",
                        column: x => x.SolicitacaoAprovacaoId,
                        principalSchema: "qh",
                        principalTable: "SolicitacoesAprovacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FornecedorDocumentos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoDocumentoExigidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataEmissao = table.Column<DateOnly>(type: "date", nullable: true),
                    DataValidade = table.Column<DateOnly>(type: "date", nullable: true),
                    ArquivoBlob = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoNomeOriginal = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("PK_FornecedorDocumentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FornecedorDocumentos_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FornecedorDocumentos_TiposDocumento_TipoDocumentoExigidoId",
                        column: x => x.TipoDocumentoExigidoId,
                        principalSchema: "qh",
                        principalTable: "TiposDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "FornecedorDocumento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "UsuariosObras",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosObras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosObras_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuariosObras_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "qh",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormModeloId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Grupo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ItemVerificacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CriterioAceitacao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MetodoVerificacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TipoResposta = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ToleranciaMin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ToleranciaMax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Obrigatorio = table.Column<bool>(type: "bit", nullable: false),
                    ExigeFotoSeNaoConforme = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormItens_FormModelos_FormModeloId",
                        column: x => x.FormModeloId,
                        principalSchema: "qh",
                        principalTable: "FormModelos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MateriaisControlados",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CodigosInsumo = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: true),
                    FormModeloFvmId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExigeFvmParaAceite = table.Column<bool>(type: "bit", nullable: false),
                    ExigeFornecedorQualificado = table.Column<bool>(type: "bit", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CodigoObraPrima = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    UltimaImportacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_MateriaisControlados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MateriaisControlados_FormModelos_FormModeloFvmId",
                        column: x => x.FormModeloFvmId,
                        principalSchema: "qh",
                        principalTable: "FormModelos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServicosControlados",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CodigosItemOrcamento = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DocumentoPeisId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FormModeloFvsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExigeFvsParaMedicao = table.Column<bool>(type: "bit", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CodigoObraPrima = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    UltimaImportacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ServicosControlados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicosControlados_DocumentosSgq_DocumentoPeisId",
                        column: x => x.DocumentoPeisId,
                        principalSchema: "qh",
                        principalTable: "DocumentosSgq",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServicosControlados_FormModelos_FormModeloFvsId",
                        column: x => x.FormModeloFvsId,
                        principalSchema: "qh",
                        principalTable: "FormModelos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Distribuicoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersaoDocumentoSgqId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Destinatario = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CopiaImpressa = table.Column<bool>(type: "bit", nullable: false),
                    DistribuidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecolhidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Distribuicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Distribuicoes_VersoesDocumentoSgq_VersaoDocumentoSgqId",
                        column: x => x.VersaoDocumentoSgqId,
                        principalSchema: "qh",
                        principalTable: "VersoesDocumentoSgq",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FuncionarioDocumentos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoDocumentoExigidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataRealizacao = table.Column<DateOnly>(type: "date", nullable: true),
                    DataValidade = table.Column<DateOnly>(type: "date", nullable: true),
                    Apto = table.Column<bool>(type: "bit", nullable: true),
                    CargaHoraria = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoBlob = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_FuncionarioDocumentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncionarioDocumentos_Funcionarios_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalSchema: "qh",
                        principalTable: "Funcionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuncionarioDocumentos_TiposDocumento_TipoDocumentoExigidoId",
                        column: x => x.TipoDocumentoExigidoId,
                        principalSchema: "qh",
                        principalTable: "TiposDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "FuncionarioDocumento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "FuncionariosObras",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataIntegracao = table.Column<DateOnly>(type: "date", nullable: true),
                    AcessoLiberado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncionariosObras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncionariosObras_Funcionarios_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalSchema: "qh",
                        principalTable: "Funcionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoInsumo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MaterialControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuantidadeRecebidaAceita = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoItens_MateriaisControlados_MaterialControladoId",
                        column: x => x.MaterialControladoId,
                        principalSchema: "qh",
                        principalTable: "MateriaisControlados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoItens_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalSchema: "qh",
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicaoItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoItem = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    QuantidadeMedida = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocalTexto = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ServicoControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicaoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicaoItens_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicaoItens_Medicoes_MedicaoId",
                        column: x => x.MedicaoId,
                        principalSchema: "qh",
                        principalTable: "Medicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicaoItens_ServicosControlados_ServicoControladoId",
                        column: x => x.ServicoControladoId,
                        principalSchema: "qh",
                        principalTable: "ServicosControlados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Fichas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    FormModeloId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersaoModelo = table.Column<int>(type: "int", nullable: false),
                    ServicoControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrestadorFornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaterialControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PedidoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NotaFiscal = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Lote = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    QuantidadeRecebida = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    QuantidadeAceita = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FichaOrigemReinspecaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspetorNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    InspetorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SincronizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DispositivoId = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: true),
                    AprovadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AprovadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_Fichas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fichas_Fichas_FichaOrigemReinspecaoId",
                        column: x => x.FichaOrigemReinspecaoId,
                        principalSchema: "qh",
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fichas_FormModelos_FormModeloId",
                        column: x => x.FormModeloId,
                        principalSchema: "qh",
                        principalTable: "FormModelos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fichas_Fornecedores_PrestadorFornecedorId",
                        column: x => x.PrestadorFornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fichas_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fichas_MateriaisControlados_MaterialControladoId",
                        column: x => x.MaterialControladoId,
                        principalSchema: "qh",
                        principalTable: "MateriaisControlados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fichas_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fichas_PedidoItens_PedidoItemId",
                        column: x => x.PedidoItemId,
                        principalSchema: "qh",
                        principalTable: "PedidoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fichas_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalSchema: "qh",
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fichas_ServicosControlados_ServicoControladoId",
                        column: x => x.ServicoControladoId,
                        principalSchema: "qh",
                        principalTable: "ServicosControlados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Ficha_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "FichaEvidencias",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ArquivoBlob = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ArquivoHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    CapturadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UploadConcluido = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichaEvidencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FichaEvidencias_Fichas_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "qh",
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FichaEvidencias_FormItens_FormItemId",
                        column: x => x.FormItemId,
                        principalSchema: "qh",
                        principalTable: "FormItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FichaRespostas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ValorNumerico = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ValorTexto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichaRespostas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FichaRespostas_Fichas_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "qh",
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FichaRespostas_FormItens_FormItemId",
                        column: x => x.FormItemId,
                        principalSchema: "qh",
                        principalTable: "FormItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rncs",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: false),
                    AcaoImediata = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AnaliseCausa = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: true),
                    AcaoCorretiva = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: true),
                    Responsavel = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    PrazoTratativa = table.Column<DateOnly>(type: "date", nullable: true),
                    VerificacaoEficacia = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: true),
                    Eficaz = table.Column<bool>(type: "bit", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EncerradaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_Rncs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rncs_Fichas_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "qh",
                        principalTable: "Fichas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rncs_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rncs_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Rnc_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_Alcadas_EmpresaId",
                schema: "qh",
                table: "Alcadas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Entidade_EntidadeId",
                schema: "qh",
                table: "Auditoria",
                columns: new[] { "Entidade", "EntidadeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Quando",
                schema: "qh",
                table: "Auditoria",
                column: "Quando");

            migrationBuilder.CreateIndex(
                name: "IX_Distribuicoes_VersaoDocumentoSgqId",
                schema: "qh",
                table: "Distribuicoes",
                column: "VersaoDocumentoSgqId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosSgq_EmpresaId",
                schema: "qh",
                table: "DocumentosSgq",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosSgq_EmpresaId_Codigo",
                schema: "qh",
                table: "DocumentosSgq",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Cnpj",
                schema: "qh",
                table: "Empresas",
                column: "Cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EtapasAprovacao_PerfilAprovador_Decisao",
                schema: "qh",
                table: "EtapasAprovacao",
                columns: new[] { "PerfilAprovador", "Decisao" });

            migrationBuilder.CreateIndex(
                name: "IX_EtapasAprovacao_SolicitacaoAprovacaoId",
                schema: "qh",
                table: "EtapasAprovacao",
                column: "SolicitacaoAprovacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_FichaEvidencias_FichaId",
                schema: "qh",
                table: "FichaEvidencias",
                column: "FichaId");

            migrationBuilder.CreateIndex(
                name: "IX_FichaEvidencias_FormItemId",
                schema: "qh",
                table: "FichaEvidencias",
                column: "FormItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FichaRespostas_FichaId_FormItemId",
                schema: "qh",
                table: "FichaRespostas",
                columns: new[] { "FichaId", "FormItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FichaRespostas_FormItemId",
                schema: "qh",
                table: "FichaRespostas",
                column: "FormItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_EmpresaId",
                schema: "qh",
                table: "Fichas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_EmpresaId_Numero",
                schema: "qh",
                table: "Fichas",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_EmpresaId_ObraId",
                schema: "qh",
                table: "Fichas",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_FichaOrigemReinspecaoId",
                schema: "qh",
                table: "Fichas",
                column: "FichaOrigemReinspecaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_FormModeloId",
                schema: "qh",
                table: "Fichas",
                column: "FormModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_LocalId",
                schema: "qh",
                table: "Fichas",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_MaterialControladoId",
                schema: "qh",
                table: "Fichas",
                column: "MaterialControladoId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_ObraId_Tipo_Status",
                schema: "qh",
                table: "Fichas",
                columns: new[] { "ObraId", "Tipo", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_PedidoId",
                schema: "qh",
                table: "Fichas",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_PedidoItemId",
                schema: "qh",
                table: "Fichas",
                column: "PedidoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_PrestadorFornecedorId",
                schema: "qh",
                table: "Fichas",
                column: "PrestadorFornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Fichas_ServicoControladoId_LocalId",
                schema: "qh",
                table: "Fichas",
                columns: new[] { "ServicoControladoId", "LocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormItens_FormModeloId",
                schema: "qh",
                table: "FormItens",
                column: "FormModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_FormModelos_DocumentoSgqOrigemId",
                schema: "qh",
                table: "FormModelos",
                column: "DocumentoSgqOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_FormModelos_EmpresaId",
                schema: "qh",
                table: "FormModelos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_FormModelos_EmpresaId_Codigo_Versao",
                schema: "qh",
                table: "FormModelos",
                columns: new[] { "EmpresaId", "Codigo", "Versao" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorAvaliacoes_EmpresaId",
                schema: "qh",
                table: "FornecedorAvaliacoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorAvaliacoes_FornecedorId",
                schema: "qh",
                table: "FornecedorAvaliacoes",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorDocumentos_DataValidade",
                schema: "qh",
                table: "FornecedorDocumentos",
                column: "DataValidade");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorDocumentos_EmpresaId",
                schema: "qh",
                table: "FornecedorDocumentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorDocumentos_FornecedorId",
                schema: "qh",
                table: "FornecedorDocumentos",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorDocumentos_TipoDocumentoExigidoId",
                schema: "qh",
                table: "FornecedorDocumentos",
                column: "TipoDocumentoExigidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_EmpresaId",
                schema: "qh",
                table: "Fornecedores",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_EmpresaId_CpfCnpj",
                schema: "qh",
                table: "Fornecedores",
                columns: new[] { "EmpresaId", "CpfCnpj" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FuncionarioDocumentos_DataValidade",
                schema: "qh",
                table: "FuncionarioDocumentos",
                column: "DataValidade");

            migrationBuilder.CreateIndex(
                name: "IX_FuncionarioDocumentos_EmpresaId",
                schema: "qh",
                table: "FuncionarioDocumentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncionarioDocumentos_FuncionarioId",
                schema: "qh",
                table: "FuncionarioDocumentos",
                column: "FuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncionarioDocumentos_TipoDocumentoExigidoId",
                schema: "qh",
                table: "FuncionarioDocumentos",
                column: "TipoDocumentoExigidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Funcionarios_EmpresaId",
                schema: "qh",
                table: "Funcionarios",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Funcionarios_EmpresaId_CpfHash",
                schema: "qh",
                table: "Funcionarios",
                columns: new[] { "EmpresaId", "CpfHash" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Funcionarios_FornecedorId",
                schema: "qh",
                table: "Funcionarios",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncionariosObras_FuncionarioId",
                schema: "qh",
                table: "FuncionariosObras",
                column: "FuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_LayoutsImportacao_EmpresaId",
                schema: "qh",
                table: "LayoutsImportacao",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LinhasImportacao_LoteImportacaoId",
                schema: "qh",
                table: "LinhasImportacao",
                column: "LoteImportacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Locais_EmpresaId",
                schema: "qh",
                table: "Locais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Locais_EmpresaId_ObraId",
                schema: "qh",
                table: "Locais",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Locais_LocalPaiId",
                schema: "qh",
                table: "Locais",
                column: "LocalPaiId");

            migrationBuilder.CreateIndex(
                name: "IX_Locais_ObraId",
                schema: "qh",
                table: "Locais",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesImportacao_EmpresaId",
                schema: "qh",
                table: "LotesImportacao",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesImportacao_EmpresaId_ArquivoHashSha256",
                schema: "qh",
                table: "LotesImportacao",
                columns: new[] { "EmpresaId", "ArquivoHashSha256" });

            migrationBuilder.CreateIndex(
                name: "IX_MateriaisControlados_EmpresaId",
                schema: "qh",
                table: "MateriaisControlados",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_MateriaisControlados_EmpresaId_Codigo",
                schema: "qh",
                table: "MateriaisControlados",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MateriaisControlados_FormModeloFvmId",
                schema: "qh",
                table: "MateriaisControlados",
                column: "FormModeloFvmId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicaoItens_LocalId",
                schema: "qh",
                table: "MedicaoItens",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicaoItens_MedicaoId",
                schema: "qh",
                table: "MedicaoItens",
                column: "MedicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicaoItens_ServicoControladoId",
                schema: "qh",
                table: "MedicaoItens",
                column: "ServicoControladoId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_EmpresaId",
                schema: "qh",
                table: "Medicoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_EmpresaId_Numero",
                schema: "qh",
                table: "Medicoes",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_EmpresaId_ObraId",
                schema: "qh",
                table: "Medicoes",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_FornecedorId",
                schema: "qh",
                table: "Medicoes",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_ObraId",
                schema: "qh",
                table: "Medicoes",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Obras_EmpresaId",
                schema: "qh",
                table: "Obras",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Obras_EmpresaId_Codigo",
                schema: "qh",
                table: "Obras",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItens_CodigoInsumo",
                schema: "qh",
                table: "PedidoItens",
                column: "CodigoInsumo");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItens_MaterialControladoId",
                schema: "qh",
                table: "PedidoItens",
                column: "MaterialControladoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItens_PedidoId",
                schema: "qh",
                table: "PedidoItens",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_EmpresaId",
                schema: "qh",
                table: "Pedidos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_EmpresaId_Numero",
                schema: "qh",
                table: "Pedidos",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_EmpresaId_ObraId",
                schema: "qh",
                table: "Pedidos",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_FornecedorId",
                schema: "qh",
                table: "Pedidos",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_ObraId",
                schema: "qh",
                table: "Pedidos",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Rncs_EmpresaId",
                schema: "qh",
                table: "Rncs",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rncs_EmpresaId_Numero",
                schema: "qh",
                table: "Rncs",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Rncs_EmpresaId_ObraId",
                schema: "qh",
                table: "Rncs",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Rncs_FichaId",
                schema: "qh",
                table: "Rncs",
                column: "FichaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rncs_FornecedorId",
                schema: "qh",
                table: "Rncs",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Rncs_ObraId",
                schema: "qh",
                table: "Rncs",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicosControlados_DocumentoPeisId",
                schema: "qh",
                table: "ServicosControlados",
                column: "DocumentoPeisId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicosControlados_EmpresaId",
                schema: "qh",
                table: "ServicosControlados",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicosControlados_EmpresaId_Codigo",
                schema: "qh",
                table: "ServicosControlados",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ServicosControlados_FormModeloFvsId",
                schema: "qh",
                table: "ServicosControlados",
                column: "FormModeloFvsId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesAprovacao_EmpresaId",
                schema: "qh",
                table: "SolicitacoesAprovacao",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesAprovacao_EmpresaId_Status",
                schema: "qh",
                table: "SolicitacoesAprovacao",
                columns: new[] { "EmpresaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesAprovacao_Entidade_EntidadeId",
                schema: "qh",
                table: "SolicitacoesAprovacao",
                columns: new[] { "Entidade", "EntidadeId" });

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumento_EmpresaId",
                schema: "qh",
                table: "TiposDocumento",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmpresaId",
                schema: "qh",
                table: "Usuarios",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EntraObjectId",
                schema: "qh",
                table: "Usuarios",
                column: "EntraObjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosObras_ObraId",
                schema: "qh",
                table: "UsuariosObras",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosObras_UsuarioId_ObraId",
                schema: "qh",
                table: "UsuariosObras",
                columns: new[] { "UsuarioId", "ObraId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VersoesDocumentoSgq_DocumentoSgqId_Revisao",
                schema: "qh",
                table: "VersoesDocumentoSgq",
                columns: new[] { "DocumentoSgqId", "Revisao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VersoesDocumentoSgq_EmpresaId",
                schema: "qh",
                table: "VersoesDocumentoSgq",
                column: "EmpresaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alcadas",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Auditoria",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Distribuicoes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Empresas",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "EtapasAprovacao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "FichaEvidencias",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "FichaRespostas",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "FornecedorAvaliacoes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "FornecedorDocumentos",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "FornecedorDocumento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "FuncionarioDocumentos",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "FuncionarioDocumento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "FuncionariosObras",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "LayoutsImportacao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "LinhasImportacao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "MedicaoItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Rncs",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Rnc_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "UsuariosObras",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "VersoesDocumentoSgq",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "VersaoDocumentoSgq_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "SolicitacoesAprovacao",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SolicitacaoAprovacao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "FormItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "TiposDocumento",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Funcionarios",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "LotesImportacao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Medicoes",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Medicao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Fichas",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Ficha_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Usuarios",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Locais",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "PedidoItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ServicosControlados",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "MateriaisControlados",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Pedidos",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Pedido_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "FormModelos",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "FormModelo_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Fornecedores",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Fornecedor_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Obras",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "DocumentosSgq",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DocumentoSgq_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}

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
                name: "AcoesCorretivas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    RncsOrigem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OcorrenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Problema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetodoAnalise = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AnaliseCausa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CausaRaiz = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsavelNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Prazo = table.Column<DateOnly>(type: "date", nullable: false),
                    DataEficacia = table.Column<DateOnly>(type: "date", nullable: true),
                    CriterioEficacia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ResultadoEficacia = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AtualizaSgq = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AcoesCorretivas", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AcaoCorretiva_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Alcadas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Entidade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    NomeEtapa = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    PerfilAprovador = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    UsuarioAprovadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValorMinimo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ValorMaximo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Condicao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    EngenheiroDaObra = table.Column<bool>(type: "bit", nullable: false),
                    SubstitutoUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrazoHoras = table.Column<int>(type: "int", nullable: true),
                    EscalonaPara = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
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
                name: "Anexos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Entidade = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EntidadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    NomeOriginal = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ArquivoBlob = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ArquivoHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_Anexos", x => x.Id);
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
                name: "AuditoriasInternas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Escopo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Criterio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Fim = table.Column<DateOnly>(type: "date", nullable: false),
                    Auditores = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Independencia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ResumoAnaliseCritica = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_AuditoriasInternas", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AuditoriaInterna_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Colaboradores",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CpfHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CpfMascarado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Vinculo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FuncaoObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataSaida = table.Column<DateOnly>(type: "date", nullable: true),
                    Telefone = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ConsentimentoTelefone = table.Column<bool>(type: "bit", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Restricoes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Pendencias = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QrToken = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
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
                    table.PrimaryKey("PK_Colaboradores", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Colaborador_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "DocumentosColaborador",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExigenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataEmissao = table.Column<DateOnly>(type: "date", nullable: false),
                    Validade = table.Column<DateOnly>(type: "date", nullable: true),
                    ConclusaoAso = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    AptoAtividades = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ArquivoBlob = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoRestrito = table.Column<bool>(type: "bit", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Validacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MotivoRecusa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ValidadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_DocumentosColaborador", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DocumentoColaborador_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "DocumentosExternos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Identificacao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Edicao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Emissor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DataVerificacao = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Relacionados = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArquivoOuLink = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Substitui = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("PK_DocumentosExternos", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DocumentoExterno_Historico")
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
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    EnderecoEntregaPadrao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    NivelSiac = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntregasEpi",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EpiId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DataEntrega = table.Column<DateOnly>(type: "date", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TrocaPrevista = table.Column<DateOnly>(type: "date", nullable: true),
                    Assinatura = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntreguePor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
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
                    table.PrimaryKey("PK_EntregasEpi", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EntregaEpi_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Epis",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ca = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ValidadeCa = table.Column<DateOnly>(type: "date", nullable: false),
                    VidaUtilDias = table.Column<int>(type: "int", nullable: true),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Epis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipamentos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TipoControle = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PadraoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PeriodicidadeMeses = table.Column<int>(type: "int", nullable: false),
                    Criterio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Responsavel = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    UltimaData = table.Column<DateOnly>(type: "date", nullable: true),
                    UltimoResultado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    UltimoCertificado = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Baixado = table.Column<bool>(type: "bit", nullable: false),
                    EmCalibracao = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Equipamentos", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EquipamentoMedicao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "EventosDocumento",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Evento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevisaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CopiaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Referencia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Detalhe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Origem = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosDocumento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Exigencias",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TreinamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CargaInicial = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ValidadeMeses = table.Column<int>(type: "int", nullable: true),
                    EfeitoVencimento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Atividades = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Fonte = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Exigencias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fornecedores",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CpfCnpj = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    RazaoSocial = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    NomeFantasia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    InscricaoEstadual = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    InscricaoMunicipal = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Cep = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: true),
                    Endereco = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Numero = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Complemento = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Bairro = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Cidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Telefones = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Emails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NaturezaJuridica = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Porte = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    SituacaoCadastral = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Cnae = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    TipoFornecimento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StatusQualificacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    NotaMedia = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CodigoErp = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
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
                name: "Funcoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cbo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Riscos = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Funcoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InspecoesSeguranca",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ModeloInspecaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspetorNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Data = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Conformidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Paralisacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspecoesSeguranca", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "InspecaoSeguranca_Historico")
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
                name: "Listas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Listas", x => x.Id);
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
                name: "MateriaisControlados",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    GrupoCatalogo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    EspecificacaoModelo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentosExigidos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExigeEspecificacao = table.Column<bool>(type: "bit", nullable: false),
                    ExigeFvmParaAceite = table.Column<bool>(type: "bit", nullable: false),
                    ExigeFornecedorQualificado = table.Column<bool>(type: "bit", nullable: false),
                    ConsultarPsq = table.Column<bool>(type: "bit", nullable: false),
                    SubGrupos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ensaios = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConstaPsq = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CodigoErp = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "ModelosInspecao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ServicoControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaterialControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NormaReferencia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Amostragem = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    QuarentenaAteEnsaio = table.Column<bool>(type: "bit", nullable: false),
                    RastrearAplicacao = table.Column<bool>(type: "bit", nullable: false),
                    DocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevisaoDocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Revisao = table.Column<int>(type: "int", nullable: false),
                    MomentoInspecao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    NivelLocal = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    LiberaMedicao = table.Column<bool>(type: "bit", nullable: false),
                    ServicoSeguinteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_ModelosInspecao", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ModeloInspecao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Notas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Entidade = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EntidadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_Notas", x => x.Id);
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
                    EmpresaSpe = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    EnderecoEntrega = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    EngenheiroId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AdministrativoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AlmoxarifeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PqoDocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    CodigoErp = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
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
                name: "Ocorrencias",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiasPerdidos = table.Column<int>(type: "int", nullable: false),
                    Cat = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoRestritoBlob = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    PermissaoTrabalhoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspecaoSegurancaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcaoCorretivaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ocorrencias", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Ocorrencia_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Parametros",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Grupo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("PK_Parametros", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Parametro_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Pastas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    PastaPaiId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ordem = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Pastas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pastas_Pastas_PastaPaiId",
                        column: x => x.PastaPaiId,
                        principalSchema: "qh",
                        principalTable: "Pastas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermissoesTrabalho",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Atividade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Inicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Fim = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Medidas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MedidasMarcadas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Emitente = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ResponsavelArea = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EncerradaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EncerradaPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("PK_PermissoesTrabalho", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PermissaoTrabalho_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "RegrasNotificacao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Evento = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Antecedencia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Repeticao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Destinatarios = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModeloMensagem = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_RegrasNotificacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegrasRetencao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Identificacao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Formulario = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Armazenamento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recuperacao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Protecao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TempoRetencao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    TempoRetencaoAnos = table.Column<int>(type: "int", nullable: true),
                    Descarte = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_RegrasRetencao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rncs",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocaisAfetados = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Classificacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Gravidade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Disposicao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Concessao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsavelNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Prazo = table.Column<DateOnly>(type: "date", nullable: false),
                    ExigeAcao = table.Column<bool>(type: "bit", nullable: false),
                    AcaoCorretivaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EncerradaPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rncs", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Rnc_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Sequencias",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Ultimo = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sequencias", x => x.Id);
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
                    SolicitanteUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitanteNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
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
                name: "Tarefas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Prioridade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsavelNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    PrazoOriginal = table.Column<DateOnly>(type: "date", nullable: false),
                    Prazo = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Resolucao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcluidaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConcluidaPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    OrigemEntidade = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    OrigemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrigemDescricao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("PK_Tarefas", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Tarefa_Historico")
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
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidadePadraoDias = table.Column<int>(type: "int", nullable: true),
                    DiasAlertaAntesVencimento = table.Column<int>(type: "int", nullable: false),
                    Obrigatorio = table.Column<bool>(type: "bit", nullable: false),
                    ExigidoPara = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PorEntrega = table.Column<bool>(type: "bit", nullable: false),
                    Momento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MaterialControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServicoControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiasLembrete = table.Column<int>(type: "int", nullable: false),
                    Fonte = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                name: "Treinamentos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CargaHoraria = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidadeMeses = table.Column<int>(type: "int", nullable: true),
                    MetodoEficacia = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_Treinamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Turmas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    TreinamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Origem = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Data = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Instrutor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Local = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
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
                    table.PrimaryKey("PK_Turmas", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Turma_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

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
                    Funcao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AcessoTodasObras = table.Column<bool>(type: "bit", nullable: false),
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
                name: "AcaoCorretivaItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcaoCorretivaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Responsavel = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Prazo = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Resultado = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcaoCorretivaItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcaoCorretivaItens_AcoesCorretivas_AcaoCorretivaId",
                        column: x => x.AcaoCorretivaId,
                        principalSchema: "qh",
                        principalTable: "AcoesCorretivas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Constatacoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditoriaInternaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Requisito = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Pergunta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Evidencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RncId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Constatacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Constatacoes_AuditoriasInternas_AuditoriaInternaId",
                        column: x => x.AuditoriaInternaId,
                        principalSchema: "qh",
                        principalTable: "AuditoriasInternas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Calibracoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipamentoMedicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Executante = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    LaboratorioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Certificado = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Erro = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AvaliacaoImpacto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistradoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calibracoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calibracoes_Equipamentos_EquipamentoMedicaoId",
                        column: x => x.EquipamentoMedicaoId,
                        principalSchema: "qh",
                        principalTable: "Equipamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AvaliacoesSemestrais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QtdFvm = table.Column<int>(type: "int", nullable: false),
                    PercFvmAprovadas = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    QtdMedicoes = table.Column<int>(type: "int", nullable: false),
                    NotaMedia = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PlanosAbertos = table.Column<int>(type: "int", nullable: false),
                    QtdRnc = table.Column<int>(type: "int", nullable: false),
                    Decisao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Participantes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataReuniao = table.Column<DateOnly>(type: "date", nullable: true),
                    DataComunicado = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_AvaliacoesSemestrais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvaliacoesSemestrais_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AvaliacaoSemestral_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "LinksPortal",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ValidoAte = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Revogado = table.Column<bool>(type: "bit", nullable: false),
                    UltimoAcessoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UltimoAcessoIp = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    EmailDestino = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ItensSolicitados = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnviadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_LinksPortal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinksPortal_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FuncaoExigencias",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuncaoObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExigenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Obrigatoriedade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncaoExigencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncaoExigencias_Exigencias_ExigenciaId",
                        column: x => x.ExigenciaId,
                        principalSchema: "qh",
                        principalTable: "Exigencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuncaoExigencias_Funcoes_FuncaoObraId",
                        column: x => x.FuncaoObraId,
                        principalSchema: "qh",
                        principalTable: "Funcoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InspecaoSegurancaItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspecaoSegurancaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemVerificacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Resultado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Gravidade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Responsavel = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Prazo = table.Column<DateOnly>(type: "date", nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Corrigido = table.Column<bool>(type: "bit", nullable: false),
                    RncId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspecaoSegurancaItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspecaoSegurancaItens_InspecoesSeguranca_InspecaoSegurancaId",
                        column: x => x.InspecaoSegurancaId,
                        principalSchema: "qh",
                        principalTable: "InspecoesSeguranca",
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
                name: "Produtos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Grupo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    SubGrupo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    LeadTimeDias = table.Column<int>(type: "int", nullable: false),
                    PrecoReferencia = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaterialControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CodigoErp = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
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
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produtos_MateriaisControlados_MaterialControladoId",
                        column: x => x.MaterialControladoId,
                        principalSchema: "qh",
                        principalTable: "MateriaisControlados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EnsaiosModelo",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModeloInspecaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ensaio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Norma = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Momento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PrazoDias = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Frequencia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Executante = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Criterio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    AcaoReprovacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnsaiosModelo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnsaiosModelo_ModelosInspecao_ModeloInspecaoId",
                        column: x => x.ModeloInspecaoId,
                        principalSchema: "qh",
                        principalTable: "ModelosInspecao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensVerificacao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModeloInspecaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metodo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ExigeEquipamento = table.Column<bool>(type: "bit", nullable: false),
                    Criterio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ValorMin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ValorMax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Foto = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Bloqueia = table.Column<bool>(type: "bit", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensVerificacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensVerificacao_ModelosInspecao_ModeloInspecaoId",
                        column: x => x.ModeloInspecaoId,
                        principalSchema: "qh",
                        principalTable: "ModelosInspecao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CentrosCusto",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Orcamento = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
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
                    table.PrimaryKey("PK_CentrosCusto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CentrosCusto_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
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
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    QrToken = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
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
                name: "PlanosAcao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Origem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OrigemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TipoPlano = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Problema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Responsavel = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Prazo = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Eficacia = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EficaciaComentario = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PlanosAcao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanosAcao_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanosAcao_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PlanoAcaoFornecedor_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "SolicitacoesCopia",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    SolicitadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    SolicitadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Copiadora = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Programacao = table.Column<DateOnly>(type: "date", nullable: false),
                    LiberadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    LiberadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MotivoRecusa = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_SolicitacoesCopia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacoesCopia_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutantesPt",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissaoTrabalhoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Papel = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Liberado = table.Column<bool>(type: "bit", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutantesPt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutantesPt_PermissoesTrabalho_PermissaoTrabalhoId",
                        column: x => x.PermissaoTrabalhoId,
                        principalSchema: "qh",
                        principalTable: "PermissoesTrabalho",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TarefaLeituras",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TarefaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    LidaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarefaLeituras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarefaLeituras_Tarefas_TarefaId",
                        column: x => x.TarefaId,
                        principalSchema: "qh",
                        principalTable: "Tarefas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TarefaProrrogacoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TarefaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrazoAnterior = table.Column<DateOnly>(type: "date", nullable: false),
                    NovoPrazo = table.Column<DateOnly>(type: "date", nullable: false),
                    Dias = table.Column<int>(type: "int", nullable: false),
                    Justificativa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Em = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarefaProrrogacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarefaProrrogacoes_Tarefas_TarefaId",
                        column: x => x.TarefaId,
                        principalSchema: "qh",
                        principalTable: "Tarefas",
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
                    Numero = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DataEmissao = table.Column<DateOnly>(type: "date", nullable: true),
                    DataValidade = table.Column<DateOnly>(type: "date", nullable: true),
                    Bloqueia = table.Column<bool>(type: "bit", nullable: false),
                    ArquivoBlob = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoNomeOriginal = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EnviadoPeloPortal = table.Column<bool>(type: "bit", nullable: false),
                    ResponsavelEnvio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    MotivoRecusa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Conferencia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ValidadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "Participantes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurmaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Presenca = table.Column<bool>(type: "bit", nullable: false),
                    Avaliacao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Resultado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ValidoAte = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participantes_Turmas_TurmaId",
                        column: x => x.TurmaId,
                        principalSchema: "qh",
                        principalTable: "Turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "Permissoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Escopo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AlvoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Perfil = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PodeVer = table.Column<bool>(type: "bit", nullable: false),
                    PodeBaixar = table.Column<bool>(type: "bit", nullable: false),
                    PodeImprimir = table.Column<bool>(type: "bit", nullable: false),
                    PodeEditar = table.Column<bool>(type: "bit", nullable: false),
                    PodeAprovar = table.Column<bool>(type: "bit", nullable: false),
                    PodeVerObsoletos = table.Column<bool>(type: "bit", nullable: false),
                    ValidoAte = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_Permissoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "qh",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PermissaoDocumento_Historico")
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
                name: "Requisicoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    EnderecoEntrega = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CentroCustoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CategoriaServico = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Atividade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DataEntrega = table.Column<DateOnly>(type: "date", nullable: false),
                    PrazoCronogramaDias = table.Column<int>(type: "int", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SolicitacaoAprovacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AprovadoEngenheiroPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AprovadoEngenheiroEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AprovadoDiretorPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AprovadoDiretorEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MotivoReprovacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SemOrcamento = table.Column<bool>(type: "bit", nullable: false),
                    JustificativaSemOrcamento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChaveNFe = table.Column<string>(type: "nvarchar(44)", maxLength: 44, nullable: true),
                    SolicitanteUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitanteNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
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
                    table.PrimaryKey("PK_Requisicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requisicoes_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalSchema: "qh",
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requisicoes_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Requisicao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Fvs",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModeloInspecaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisaoModelo = table.Column<int>(type: "int", nullable: false),
                    ServicoControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspetorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspetorNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DataInspecao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EquipamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Disposicao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    PrazoCorrecao = table.Column<DateOnly>(type: "date", nullable: true),
                    RncId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FvsOrigemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataReinspecao = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LiberadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LiberadoPorNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DataLiberacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Origem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_Fvs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fvs_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fvs_ModelosInspecao_ModeloInspecaoId",
                        column: x => x.ModeloInspecaoId,
                        principalSchema: "qh",
                        principalTable: "ModelosInspecao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Fvs_Historico")
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
                    Justificativa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmNomeDeUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmNomeDeNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DelegacaoAprovacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapasAprovacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtapasAprovacao_DelegacoesAprovacao_DelegacaoAprovacaoId",
                        column: x => x.DelegacaoAprovacaoId,
                        principalSchema: "qh",
                        principalTable: "DelegacoesAprovacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EtapasAprovacao_SolicitacoesAprovacao_SolicitacaoAprovacaoId",
                        column: x => x.SolicitacaoAprovacaoId,
                        principalSchema: "qh",
                        principalTable: "SolicitacoesAprovacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cotacoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    RequisicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Frete = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EnderecoEntrega = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ValorEstimado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PropostasExigidas = table.Column<int>(type: "int", nullable: false),
                    PropostasRecebidas = table.Column<int>(type: "int", nullable: false),
                    JustificativaPropostas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FornecedorVencedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CriterioSelecao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    SolicitacaoAprovacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AprovadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AprovadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Concluido = table.Column<bool>(type: "bit", nullable: false),
                    SemOrcamento = table.Column<bool>(type: "bit", nullable: false),
                    ChaveNFe = table.Column<string>(type: "nvarchar(44)", maxLength: 44, nullable: true),
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
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cotacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cotacoes_Fornecedores_FornecedorVencedorId",
                        column: x => x.FornecedorVencedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cotacoes_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cotacoes_Requisicoes_RequisicaoId",
                        column: x => x.RequisicaoId,
                        principalSchema: "qh",
                        principalTable: "Requisicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Cotacao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "FvsItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FvsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemVerificacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metodo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Criterio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ValorMin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ValorMax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Foto = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Bloqueia = table.Column<bool>(type: "bit", nullable: false),
                    ExigeEquipamento = table.Column<bool>(type: "bit", nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ValorMedido = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultadoReinspecao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FvsItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FvsItens_Fvs_FvsId",
                        column: x => x.FvsId,
                        principalSchema: "qh",
                        principalTable: "Fvs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contratos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CentroCustoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CotacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Objeto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequisitosLaboratorio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequisitosDesempenho = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequisitosEquipamento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetencaoPerc = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenciaFim = table.Column<DateOnly>(type: "date", nullable: false),
                    Formalizacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SolicitacaoAprovacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AprovadoDiretoriaPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AprovadoDiretoriaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AssinaturaEmpresa = table.Column<DateOnly>(type: "date", nullable: true),
                    AssinaturaFornecedor = table.Column<DateOnly>(type: "date", nullable: true),
                    EnviadoFornecedorEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contratos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contratos_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalSchema: "qh",
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contratos_Cotacoes_CotacaoId",
                        column: x => x.CotacaoId,
                        principalSchema: "qh",
                        principalTable: "Cotacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contratos_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contratos_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contratos_Requisicoes_RequisicaoId",
                        column: x => x.RequisicaoId,
                        principalSchema: "qh",
                        principalTable: "Requisicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Contrato_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "CotacaoFornecedores",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CotacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkResposta = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DataResposta = table.Column<DateOnly>(type: "date", nullable: true),
                    ValorFrete = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    QualificacaoNaData = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PrazoEntregaOfertado = table.Column<DateOnly>(type: "date", nullable: true),
                    CondicaoPagamento = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ValidadeProposta = table.Column<DateOnly>(type: "date", nullable: true),
                    TemAnexoProposta = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CotacaoFornecedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CotacaoFornecedores_Cotacoes_CotacaoId",
                        column: x => x.CotacaoId,
                        principalSchema: "qh",
                        principalTable: "Cotacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CotacaoFornecedores_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Aditivos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Justificativa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NovaVigencia = table.Column<DateOnly>(type: "date", nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PercAcumulado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SolicitacaoAprovacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AprovadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AprovadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AssinaturaEmpresa = table.Column<DateOnly>(type: "date", nullable: true),
                    AssinaturaFornecedor = table.Column<DateOnly>(type: "date", nullable: true),
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
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aditivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aditivos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Aditivo_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Medicoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroMedicao = table.Column<int>(type: "int", nullable: false),
                    PeriodoInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodoFim = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorPeriodo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorAcumulado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorRetencao = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NotaEscopo = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    NotaMaterial = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    NotaServico = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    NotaLimpeza = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    NotaDocSeg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    NotaFinal = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Faixa = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DataReuniao = table.Column<DateOnly>(type: "date", nullable: true),
                    AvaliadorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AvaliadorNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AvaliadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SolicitacaoAprovacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AprovadoDiretorPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AprovadoDiretorEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MotivoReprovacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChaveNFe = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Bloqueios = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanoAcaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                        name: "FK_Medicoes_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Medicoes_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Medicoes_PlanosAcao_PlanoAcaoId",
                        column: x => x.PlanoAcaoId,
                        principalSchema: "qh",
                        principalTable: "PlanosAcao",
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
                    Numero = table.Column<int>(type: "int", nullable: false),
                    CotacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequisicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CondicaoPagamento = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Frete = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Transportadora = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ValorFrete = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Entrega = table.Column<DateOnly>(type: "date", nullable: false),
                    EnderecoEntrega = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    TipoDocumento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspecificacoesIncluidas = table.Column<bool>(type: "bit", nullable: false),
                    SolicitacaoAprovacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AprovadoDiretorPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AprovadoDiretorEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EnviadoFornecedorEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EmailsCopia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SemOrcamento = table.Column<bool>(type: "bit", nullable: false),
                    ChaveNFe = table.Column<string>(type: "nvarchar(44)", maxLength: 44, nullable: true),
                    Bloqueios = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodigoErp = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
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
                        name: "FK_Pedidos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pedidos_Cotacoes_CotacaoId",
                        column: x => x.CotacaoId,
                        principalSchema: "qh",
                        principalTable: "Cotacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_Pedidos_Requisicoes_RequisicaoId",
                        column: x => x.RequisicaoId,
                        principalSchema: "qh",
                        principalTable: "Requisicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Pedido_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "AditivoItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AditivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Item = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ServicoControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QtdOriginal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QtdDelta = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecoOriginal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AditivoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AditivoItens_Aditivos_AditivoId",
                        column: x => x.AditivoId,
                        principalSchema: "qh",
                        principalTable: "Aditivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Grds",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Contato = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnviadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EnviadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    TokenConfirmacao = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_Grds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grds_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Grds_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Grds_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalSchema: "qh",
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagamentos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Origem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ChaveNFe = table.Column<string>(type: "nvarchar(44)", maxLength: 44, nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MedicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValorNF = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorLiberado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    JustificativaDiferenca = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecebimentoOk = table.Column<bool>(type: "bit", nullable: false),
                    EncaminhadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    EncaminhadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SolicitacaoAprovacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LiberadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    LiberadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Bloqueios = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Pagamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Medicoes_MedicaoId",
                        column: x => x.MedicaoId,
                        principalSchema: "qh",
                        principalTable: "Medicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalSchema: "qh",
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "LiberacaoPagamento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Recebimentos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    PedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChaveNFe = table.Column<string>(type: "nvarchar(44)", maxLength: 44, nullable: true),
                    NumeroNotaFiscal = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DataRecebimento = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecebidoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecebidoPorNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    FvmPrazo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FvmQualidade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FvmAtendimento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ocorrencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Destino = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ComunicadoA = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ComunicadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModeloFvmId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusLote = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    QuarentenaAteEnsaio = table.Column<bool>(type: "bit", nullable: false),
                    RastrearAplicacao = table.Column<bool>(type: "bit", nullable: false),
                    RncId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Recebimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recebimentos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recebimentos_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recebimentos_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recebimentos_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalSchema: "qh",
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Recebimento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "AplicacoesLote",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecebimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    FvsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RegistradoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    RegistradoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AplicacoesLote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AplicacoesLote_Recebimentos_RecebimentoId",
                        column: x => x.RecebimentoId,
                        principalSchema: "qh",
                        principalTable: "Recebimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EnsaiosRecebimento",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecebimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnsaioModeloId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ensaio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Criterio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    AcaoReprovacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Amostra = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DataColeta = table.Column<DateOnly>(type: "date", nullable: false),
                    DataPrevista = table.Column<DateOnly>(type: "date", nullable: true),
                    LaboratorioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Resultado = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RncId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_EnsaiosRecebimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnsaiosRecebimento_Recebimentos_RecebimentoId",
                        column: x => x.RecebimentoId,
                        principalSchema: "qh",
                        principalTable: "Recebimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EnsaioRecebimento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "FvmItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecebimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecebimentoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemVerificacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Criterio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ValorMin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ValorMax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Bloqueia = table.Column<bool>(type: "bit", nullable: false),
                    ValorMedido = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Resultado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FvmItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FvmItens_Recebimentos_RecebimentoId",
                        column: x => x.RecebimentoId,
                        principalSchema: "qh",
                        principalTable: "Recebimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Checkouts",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoRevisaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CheckoutEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Prazo = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckinEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LiberadoForcadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("PK_Checkouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ciencias",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoRevisaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Funcao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Prazo = table.Column<DateOnly>(type: "date", nullable: false),
                    LidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExigeTreinamento = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Ciencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ciencias_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "qh",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContratoItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Item = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServicoControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContratoItens_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
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
                    ContratoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QtdAnterior = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QtdPeriodo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QtdAcumulada = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PercExecutado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorPeriodo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FvsRef = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    FvsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicaoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicaoItens_ContratoItens_ContratoItemId",
                        column: x => x.ContratoItemId,
                        principalSchema: "qh",
                        principalTable: "ContratoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                });

            migrationBuilder.CreateTable(
                name: "ContratoServicos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicoControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratoServicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContratoServicos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Copias",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroCopia = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    DocumentoRevisaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoCopia = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Destinatario = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SolicitacaoCopiaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GrdId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntregueEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RecebidoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    QrToken = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PrazoRecolhimento = table.Column<DateOnly>(type: "date", nullable: true),
                    RecolhidaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RecolhidaPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DestinoObsoleta = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
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
                    table.PrimaryKey("PK_Copias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Copias_Grds_GrdId",
                        column: x => x.GrdId,
                        principalSchema: "qh",
                        principalTable: "Grds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Copias_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Copias_SolicitacoesCopia_SolicitacaoCopiaId",
                        column: x => x.SolicitacaoCopiaId,
                        principalSchema: "qh",
                        principalTable: "SolicitacoesCopia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CopiaControlada_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "CotacaoItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CotacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisicaoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ValorOfertado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorNegociado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Entrega = table.Column<DateOnly>(type: "date", nullable: true),
                    MaterialControlado = table.Column<bool>(type: "bit", nullable: false),
                    AtendeEspecificacao = table.Column<bool>(type: "bit", nullable: true),
                    PrecoUltimo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PrecoReferencia = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CotacaoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CotacaoItens_Cotacoes_CotacaoId",
                        column: x => x.CotacaoId,
                        principalSchema: "qh",
                        principalTable: "Cotacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CotacaoItens_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Documentos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    PastaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitoSiac = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Origem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Abrangencia = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsavelUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsavelNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AprovadorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AprovadorNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    RevisaoVigenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataVigencia = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PeriodicidadeMeses = table.Column<int>(type: "int", nullable: false),
                    ProximaAnalise = table.Column<DateOnly>(type: "date", nullable: true),
                    NivelAcesso = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DistribuicaoPadrao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LeituraObrigatoria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeraRegistro = table.Column<bool>(type: "bit", nullable: false),
                    RegraRetencaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documentos_Pastas_PastaId",
                        column: x => x.PastaId,
                        principalSchema: "qh",
                        principalTable: "Pastas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documentos_RegrasRetencao_RegraRetencaoId",
                        column: x => x.RegraRetencaoId,
                        principalSchema: "qh",
                        principalTable: "RegrasRetencao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Documento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "DocumentosRelacionados",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelacionadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosRelacionados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosRelacionados_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalSchema: "qh",
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosRelacionados_Documentos_RelacionadoId",
                        column: x => x.RelacionadoId,
                        principalSchema: "qh",
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Revisoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revisao = table.Column<int>(type: "int", nullable: false),
                    ArquivoEditavelBlob = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoEditavelNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoPublicadoBlob = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoPublicadoNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    HashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DescricaoAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MotivoRevisao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OrigemRevisao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ElaboradoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElaboradoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ElaboradoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AnalisadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AnalisadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AprovadoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AprovadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DataVigencia = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ObsoletoEm = table.Column<DateOnly>(type: "date", nullable: true),
                    RetidoAte = table.Column<DateOnly>(type: "date", nullable: true),
                    RecolhimentoOk = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Revisoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Revisoes_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalSchema: "qh",
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DocumentoRevisao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ServicosControlados",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DocumentoItsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExigeFvsParaMedicao = table.Column<bool>(type: "bit", nullable: false),
                    AvaliadoNaMedicao = table.Column<bool>(type: "bit", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CodigoErp = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
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
                        name: "FK_ServicosControlados_Documentos_DocumentoItsId",
                        column: x => x.DocumentoItsId,
                        principalSchema: "qh",
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EtapasRevisao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoRevisaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Etapa = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ResponsavelUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsavelNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Prazo = table.Column<DateOnly>(type: "date", nullable: true),
                    Decisao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataDecisao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Assinatura = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Historico = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapasRevisao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtapasRevisao_Revisoes_DocumentoRevisaoId",
                        column: x => x.DocumentoRevisaoId,
                        principalSchema: "qh",
                        principalTable: "Revisoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GrdItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrdId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoRevisaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrdItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrdItens_Grds_GrdId",
                        column: x => x.GrdId,
                        principalSchema: "qh",
                        principalTable: "Grds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GrdItens_Revisoes_DocumentoRevisaoId",
                        column: x => x.DocumentoRevisaoId,
                        principalSchema: "qh",
                        principalTable: "Revisoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacaoCopiaItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitacaoCopiaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoRevisaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    Destinatario = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacaoCopiaItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacaoCopiaItens_Revisoes_DocumentoRevisaoId",
                        column: x => x.DocumentoRevisaoId,
                        principalSchema: "qh",
                        principalTable: "Revisoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitacaoCopiaItens_SolicitacoesCopia_SolicitacaoCopiaId",
                        column: x => x.SolicitacaoCopiaId,
                        principalSchema: "qh",
                        principalTable: "SolicitacoesCopia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Qualificacoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoFornecimento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaterialControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServicoControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CriterioQualificacao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Psq = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DataConsultaPsq = table.Column<DateOnly>(type: "date", nullable: true),
                    CertificadoSbac = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    LaboratorioEnquadramento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AvaliadorLaboratorio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DataQualificacao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataValidade = table.Column<DateOnly>(type: "date", nullable: true),
                    Resultado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ResponsavelUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsavelNome = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    QualificadoPorHistorico = table.Column<bool>(type: "bit", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Qualificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Qualificacoes_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Qualificacoes_MateriaisControlados_MaterialControladoId",
                        column: x => x.MaterialControladoId,
                        principalSchema: "qh",
                        principalTable: "MateriaisControlados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Qualificacoes_ServicosControlados_ServicoControladoId",
                        column: x => x.ServicoControladoId,
                        principalSchema: "qh",
                        principalTable: "ServicosControlados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "FornecedorQualificacao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "RequisicaoItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    MesmaMarca = table.Column<bool>(type: "bit", nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ValorReferencia = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    LeadTimeDias = table.Column<int>(type: "int", nullable: false),
                    ServicoControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaterialControlado = table.Column<bool>(type: "bit", nullable: false),
                    MaterialControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspecificacaoTecnica = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentosExigidos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataNecessidade = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisicaoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisicaoItens_MateriaisControlados_MaterialControladoId",
                        column: x => x.MaterialControladoId,
                        principalSchema: "qh",
                        principalTable: "MateriaisControlados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisicaoItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalSchema: "qh",
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisicaoItens_Requisicoes_RequisicaoId",
                        column: x => x.RequisicaoId,
                        principalSchema: "qh",
                        principalTable: "Requisicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisicaoItens_ServicosControlados_ServicoControladoId",
                        column: x => x.ServicoControladoId,
                        principalSchema: "qh",
                        principalTable: "ServicosControlados",
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
                    RequisicaoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CodigoInsumo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MaterialControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspecificacaoTecnica = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentosExigidos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuantidadeRecebidaAceita = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
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
                    table.ForeignKey(
                        name: "FK_PedidoItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalSchema: "qh",
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoItens_RequisicaoItens_RequisicaoItemId",
                        column: x => x.RequisicaoItemId,
                        principalSchema: "qh",
                        principalTable: "RequisicaoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecebimentoItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecebimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PedidoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    QtdPedida = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QtdRecebida = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    MaterialControlado = table.Column<bool>(type: "bit", nullable: false),
                    MaterialControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Lote = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DocumentosRecebidos = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecebimentoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecebimentoItens_PedidoItens_PedidoItemId",
                        column: x => x.PedidoItemId,
                        principalSchema: "qh",
                        principalTable: "PedidoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecebimentoItens_Recebimentos_RecebimentoId",
                        column: x => x.RecebimentoId,
                        principalSchema: "qh",
                        principalTable: "Recebimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcaoCorretivaItens_AcaoCorretivaId",
                schema: "qh",
                table: "AcaoCorretivaItens",
                column: "AcaoCorretivaId");

            migrationBuilder.CreateIndex(
                name: "IX_AcoesCorretivas_EmpresaId",
                schema: "qh",
                table: "AcoesCorretivas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_AditivoItens_AditivoId",
                schema: "qh",
                table: "AditivoItens",
                column: "AditivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Aditivos_ContratoId_Numero",
                schema: "qh",
                table: "Aditivos",
                columns: new[] { "ContratoId", "Numero" });

            migrationBuilder.CreateIndex(
                name: "IX_Aditivos_EmpresaId",
                schema: "qh",
                table: "Aditivos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Aditivos_EmpresaId_ObraId",
                schema: "qh",
                table: "Aditivos",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Alcadas_EmpresaId",
                schema: "qh",
                table: "Alcadas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Anexos_EmpresaId",
                schema: "qh",
                table: "Anexos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Anexos_Entidade_EntidadeId",
                schema: "qh",
                table: "Anexos",
                columns: new[] { "Entidade", "EntidadeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AplicacoesLote_RecebimentoId",
                schema: "qh",
                table: "AplicacoesLote",
                column: "RecebimentoId");

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
                name: "IX_AuditoriasInternas_EmpresaId",
                schema: "qh",
                table: "AuditoriasInternas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacoesSemestrais_EmpresaId",
                schema: "qh",
                table: "AvaliacoesSemestrais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacoesSemestrais_EmpresaId_Periodo_FornecedorId",
                schema: "qh",
                table: "AvaliacoesSemestrais",
                columns: new[] { "EmpresaId", "Periodo", "FornecedorId" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacoesSemestrais_FornecedorId",
                schema: "qh",
                table: "AvaliacoesSemestrais",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Calibracoes_EquipamentoMedicaoId",
                schema: "qh",
                table: "Calibracoes",
                column: "EquipamentoMedicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCusto_EmpresaId",
                schema: "qh",
                table: "CentrosCusto",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCusto_EmpresaId_Codigo",
                schema: "qh",
                table: "CentrosCusto",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCusto_ObraId",
                schema: "qh",
                table: "CentrosCusto",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Checkouts_DocumentoRevisaoId",
                schema: "qh",
                table: "Checkouts",
                column: "DocumentoRevisaoId",
                unique: true,
                filter: "[CheckinEm] IS NULL AND [LiberadoForcadoPor] IS NULL AND [Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Checkouts_EmpresaId",
                schema: "qh",
                table: "Checkouts",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ciencias_DocumentoRevisaoId_UsuarioId",
                schema: "qh",
                table: "Ciencias",
                columns: new[] { "DocumentoRevisaoId", "UsuarioId" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Ciencias_EmpresaId",
                schema: "qh",
                table: "Ciencias",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ciencias_UsuarioId",
                schema: "qh",
                table: "Ciencias",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_EmpresaId",
                schema: "qh",
                table: "Colaboradores",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_EmpresaId_ObraId",
                schema: "qh",
                table: "Colaboradores",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_ObraId_CpfHash",
                schema: "qh",
                table: "Colaboradores",
                columns: new[] { "ObraId", "CpfHash" });

            migrationBuilder.CreateIndex(
                name: "IX_Constatacoes_AuditoriaInternaId",
                schema: "qh",
                table: "Constatacoes",
                column: "AuditoriaInternaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoItens_ContratoId",
                schema: "qh",
                table: "ContratoItens",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoItens_ServicoControladoId",
                schema: "qh",
                table: "ContratoItens",
                column: "ServicoControladoId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_CentroCustoId",
                schema: "qh",
                table: "Contratos",
                column: "CentroCustoId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_CotacaoId",
                schema: "qh",
                table: "Contratos",
                column: "CotacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_EmpresaId",
                schema: "qh",
                table: "Contratos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_EmpresaId_Numero",
                schema: "qh",
                table: "Contratos",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_EmpresaId_ObraId",
                schema: "qh",
                table: "Contratos",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_FornecedorId",
                schema: "qh",
                table: "Contratos",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_ObraId",
                schema: "qh",
                table: "Contratos",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_RequisicaoId",
                schema: "qh",
                table: "Contratos",
                column: "RequisicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoServicos_ContratoId_ServicoControladoId",
                schema: "qh",
                table: "ContratoServicos",
                columns: new[] { "ContratoId", "ServicoControladoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContratoServicos_ServicoControladoId",
                schema: "qh",
                table: "ContratoServicos",
                column: "ServicoControladoId");

            migrationBuilder.CreateIndex(
                name: "IX_Copias_DocumentoRevisaoId",
                schema: "qh",
                table: "Copias",
                column: "DocumentoRevisaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Copias_EmpresaId",
                schema: "qh",
                table: "Copias",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Copias_EmpresaId_NumeroCopia",
                schema: "qh",
                table: "Copias",
                columns: new[] { "EmpresaId", "NumeroCopia" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Copias_GrdId",
                schema: "qh",
                table: "Copias",
                column: "GrdId");

            migrationBuilder.CreateIndex(
                name: "IX_Copias_ObraId",
                schema: "qh",
                table: "Copias",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Copias_QrToken",
                schema: "qh",
                table: "Copias",
                column: "QrToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Copias_SolicitacaoCopiaId",
                schema: "qh",
                table: "Copias",
                column: "SolicitacaoCopiaId");

            migrationBuilder.CreateIndex(
                name: "IX_CotacaoFornecedores_CotacaoId_FornecedorId",
                schema: "qh",
                table: "CotacaoFornecedores",
                columns: new[] { "CotacaoId", "FornecedorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CotacaoFornecedores_FornecedorId",
                schema: "qh",
                table: "CotacaoFornecedores",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_CotacaoFornecedores_LinkResposta",
                schema: "qh",
                table: "CotacaoFornecedores",
                column: "LinkResposta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CotacaoItens_CotacaoId",
                schema: "qh",
                table: "CotacaoItens",
                column: "CotacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_CotacaoItens_FornecedorId",
                schema: "qh",
                table: "CotacaoItens",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_CotacaoItens_RequisicaoItemId",
                schema: "qh",
                table: "CotacaoItens",
                column: "RequisicaoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotacoes_EmpresaId",
                schema: "qh",
                table: "Cotacoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotacoes_EmpresaId_Numero",
                schema: "qh",
                table: "Cotacoes",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Cotacoes_EmpresaId_ObraId",
                schema: "qh",
                table: "Cotacoes",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cotacoes_FornecedorVencedorId",
                schema: "qh",
                table: "Cotacoes",
                column: "FornecedorVencedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotacoes_ObraId",
                schema: "qh",
                table: "Cotacoes",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotacoes_RequisicaoId",
                schema: "qh",
                table: "Cotacoes",
                column: "RequisicaoId");

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
                name: "IX_Documentos_EmpresaId",
                schema: "qh",
                table: "Documentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_EmpresaId_Codigo",
                schema: "qh",
                table: "Documentos",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_ObraId",
                schema: "qh",
                table: "Documentos",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_PastaId",
                schema: "qh",
                table: "Documentos",
                column: "PastaId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_ProximaAnalise",
                schema: "qh",
                table: "Documentos",
                column: "ProximaAnalise");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_RegraRetencaoId",
                schema: "qh",
                table: "Documentos",
                column: "RegraRetencaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_RevisaoVigenteId",
                schema: "qh",
                table: "Documentos",
                column: "RevisaoVigenteId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosColaborador_EmpresaId",
                schema: "qh",
                table: "DocumentosColaborador",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosExternos_EmpresaId",
                schema: "qh",
                table: "DocumentosExternos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosExternos_EmpresaId_Identificacao_Edicao",
                schema: "qh",
                table: "DocumentosExternos",
                columns: new[] { "EmpresaId", "Identificacao", "Edicao" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosRelacionados_DocumentoId_RelacionadoId",
                schema: "qh",
                table: "DocumentosRelacionados",
                columns: new[] { "DocumentoId", "RelacionadoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosRelacionados_RelacionadoId",
                schema: "qh",
                table: "DocumentosRelacionados",
                column: "RelacionadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Cnpj",
                schema: "qh",
                table: "Empresas",
                column: "Cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnsaiosModelo_ModeloInspecaoId",
                schema: "qh",
                table: "EnsaiosModelo",
                column: "ModeloInspecaoId");

            migrationBuilder.CreateIndex(
                name: "IX_EnsaiosRecebimento_EmpresaId",
                schema: "qh",
                table: "EnsaiosRecebimento",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_EnsaiosRecebimento_RecebimentoId",
                schema: "qh",
                table: "EnsaiosRecebimento",
                column: "RecebimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregasEpi_EmpresaId",
                schema: "qh",
                table: "EntregasEpi",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Epis_EmpresaId",
                schema: "qh",
                table: "Epis",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamentos_EmpresaId",
                schema: "qh",
                table: "Equipamentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamentos_EmpresaId_Codigo",
                schema: "qh",
                table: "Equipamentos",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EtapasAprovacao_DelegacaoAprovacaoId",
                schema: "qh",
                table: "EtapasAprovacao",
                column: "DelegacaoAprovacaoId");

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
                name: "IX_EtapasAprovacao_UsuarioAprovadorId",
                schema: "qh",
                table: "EtapasAprovacao",
                column: "UsuarioAprovadorId");

            migrationBuilder.CreateIndex(
                name: "IX_EtapasRevisao_DocumentoRevisaoId",
                schema: "qh",
                table: "EtapasRevisao",
                column: "DocumentoRevisaoId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosDocumento_DocumentoId",
                schema: "qh",
                table: "EventosDocumento",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosDocumento_EmpresaId_DataHora",
                schema: "qh",
                table: "EventosDocumento",
                columns: new[] { "EmpresaId", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutantesPt_PermissaoTrabalhoId",
                schema: "qh",
                table: "ExecutantesPt",
                column: "PermissaoTrabalhoId");

            migrationBuilder.CreateIndex(
                name: "IX_Exigencias_EmpresaId",
                schema: "qh",
                table: "Exigencias",
                column: "EmpresaId");

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
                filter: "[Excluido] = 0 AND [CpfCnpj] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_EmpresaId_RazaoSocial",
                schema: "qh",
                table: "Fornecedores",
                columns: new[] { "EmpresaId", "RazaoSocial" });

            migrationBuilder.CreateIndex(
                name: "IX_FuncaoExigencias_ExigenciaId",
                schema: "qh",
                table: "FuncaoExigencias",
                column: "ExigenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_FuncaoExigencias_FuncaoObraId",
                schema: "qh",
                table: "FuncaoExigencias",
                column: "FuncaoObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Funcoes_EmpresaId",
                schema: "qh",
                table: "Funcoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_FvmItens_RecebimentoId",
                schema: "qh",
                table: "FvmItens",
                column: "RecebimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Fvs_EmpresaId",
                schema: "qh",
                table: "Fvs",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Fvs_EmpresaId_Numero",
                schema: "qh",
                table: "Fvs",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Fvs_EmpresaId_ObraId",
                schema: "qh",
                table: "Fvs",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Fvs_LocalId_ServicoControladoId_Status",
                schema: "qh",
                table: "Fvs",
                columns: new[] { "LocalId", "ServicoControladoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Fvs_ModeloInspecaoId",
                schema: "qh",
                table: "Fvs",
                column: "ModeloInspecaoId");

            migrationBuilder.CreateIndex(
                name: "IX_FvsItens_FvsId",
                schema: "qh",
                table: "FvsItens",
                column: "FvsId");

            migrationBuilder.CreateIndex(
                name: "IX_GrdItens_DocumentoRevisaoId",
                schema: "qh",
                table: "GrdItens",
                column: "DocumentoRevisaoId");

            migrationBuilder.CreateIndex(
                name: "IX_GrdItens_GrdId",
                schema: "qh",
                table: "GrdItens",
                column: "GrdId");

            migrationBuilder.CreateIndex(
                name: "IX_Grds_ContratoId",
                schema: "qh",
                table: "Grds",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Grds_EmpresaId",
                schema: "qh",
                table: "Grds",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Grds_EmpresaId_Numero",
                schema: "qh",
                table: "Grds",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Grds_FornecedorId",
                schema: "qh",
                table: "Grds",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Grds_PedidoId",
                schema: "qh",
                table: "Grds",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Grds_TokenConfirmacao",
                schema: "qh",
                table: "Grds",
                column: "TokenConfirmacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InspecaoSegurancaItens_InspecaoSegurancaId",
                schema: "qh",
                table: "InspecaoSegurancaItens",
                column: "InspecaoSegurancaId");

            migrationBuilder.CreateIndex(
                name: "IX_InspecoesSeguranca_EmpresaId",
                schema: "qh",
                table: "InspecoesSeguranca",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_InspecoesSeguranca_EmpresaId_ObraId",
                schema: "qh",
                table: "InspecoesSeguranca",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensVerificacao_ModeloInspecaoId",
                schema: "qh",
                table: "ItensVerificacao",
                column: "ModeloInspecaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LayoutsImportacao_EmpresaId",
                schema: "qh",
                table: "LayoutsImportacao",
                column: "EmpresaId");

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

            migrationBuilder.CreateIndex(
                name: "IX_LinhasImportacao_LoteImportacaoId",
                schema: "qh",
                table: "LinhasImportacao",
                column: "LoteImportacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LinksPortal_EmpresaId",
                schema: "qh",
                table: "LinksPortal",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LinksPortal_FornecedorId",
                schema: "qh",
                table: "LinksPortal",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_LinksPortal_TokenHash",
                schema: "qh",
                table: "LinksPortal",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listas_EmpresaId",
                schema: "qh",
                table: "Listas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Listas_EmpresaId_Tipo_Nome",
                schema: "qh",
                table: "Listas",
                columns: new[] { "EmpresaId", "Tipo", "Nome" },
                unique: true,
                filter: "[Excluido] = 0");

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
                name: "IX_MedicaoItens_ContratoItemId",
                schema: "qh",
                table: "MedicaoItens",
                column: "ContratoItemId");

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
                name: "IX_Medicoes_ContratoId_NumeroMedicao",
                schema: "qh",
                table: "Medicoes",
                columns: new[] { "ContratoId", "NumeroMedicao" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_EmpresaId",
                schema: "qh",
                table: "Medicoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_EmpresaId_ObraId",
                schema: "qh",
                table: "Medicoes",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_ObraId",
                schema: "qh",
                table: "Medicoes",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicoes_PlanoAcaoId",
                schema: "qh",
                table: "Medicoes",
                column: "PlanoAcaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelosInspecao_EmpresaId",
                schema: "qh",
                table: "ModelosInspecao",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelosInspecao_EmpresaId_Codigo",
                schema: "qh",
                table: "ModelosInspecao",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Notas_EmpresaId",
                schema: "qh",
                table: "Notas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Notas_Entidade_EntidadeId",
                schema: "qh",
                table: "Notas",
                columns: new[] { "Entidade", "EntidadeId" });

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
                name: "IX_Ocorrencias_EmpresaId",
                schema: "qh",
                table: "Ocorrencias",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_EmpresaId_ObraId",
                schema: "qh",
                table: "Ocorrencias",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_ChaveNFe",
                schema: "qh",
                table: "Pagamentos",
                column: "ChaveNFe");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_ContratoId",
                schema: "qh",
                table: "Pagamentos",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_EmpresaId",
                schema: "qh",
                table: "Pagamentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_EmpresaId_Numero",
                schema: "qh",
                table: "Pagamentos",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_FornecedorId",
                schema: "qh",
                table: "Pagamentos",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_MedicaoId",
                schema: "qh",
                table: "Pagamentos",
                column: "MedicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_ObraId",
                schema: "qh",
                table: "Pagamentos",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_PedidoId",
                schema: "qh",
                table: "Pagamentos",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Parametros_EmpresaId",
                schema: "qh",
                table: "Parametros",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Parametros_EmpresaId_Chave",
                schema: "qh",
                table: "Parametros",
                columns: new[] { "EmpresaId", "Chave" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Participantes_TurmaId",
                schema: "qh",
                table: "Participantes",
                column: "TurmaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pastas_EmpresaId",
                schema: "qh",
                table: "Pastas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pastas_PastaPaiId",
                schema: "qh",
                table: "Pastas",
                column: "PastaPaiId");

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
                name: "IX_PedidoItens_ProdutoId",
                schema: "qh",
                table: "PedidoItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItens_RequisicaoItemId",
                schema: "qh",
                table: "PedidoItens",
                column: "RequisicaoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_ContratoId",
                schema: "qh",
                table: "Pedidos",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_CotacaoId",
                schema: "qh",
                table: "Pedidos",
                column: "CotacaoId");

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
                name: "IX_Pedidos_RequisicaoId",
                schema: "qh",
                table: "Pedidos",
                column: "RequisicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissoes_EmpresaId",
                schema: "qh",
                table: "Permissoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissoes_Escopo_AlvoId",
                schema: "qh",
                table: "Permissoes",
                columns: new[] { "Escopo", "AlvoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissoes_UsuarioId",
                schema: "qh",
                table: "Permissoes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissoesTrabalho_EmpresaId",
                schema: "qh",
                table: "PermissoesTrabalho",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissoesTrabalho_EmpresaId_ObraId",
                schema: "qh",
                table: "PermissoesTrabalho",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanosAcao_EmpresaId",
                schema: "qh",
                table: "PlanosAcao",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosAcao_EmpresaId_Numero",
                schema: "qh",
                table: "PlanosAcao",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosAcao_FornecedorId_Status",
                schema: "qh",
                table: "PlanosAcao",
                columns: new[] { "FornecedorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanosAcao_ObraId",
                schema: "qh",
                table: "PlanosAcao",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_EmpresaId",
                schema: "qh",
                table: "Produtos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_EmpresaId_Codigo",
                schema: "qh",
                table: "Produtos",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_MaterialControladoId",
                schema: "qh",
                table: "Produtos",
                column: "MaterialControladoId");

            migrationBuilder.CreateIndex(
                name: "IX_Qualificacoes_DataValidade",
                schema: "qh",
                table: "Qualificacoes",
                column: "DataValidade");

            migrationBuilder.CreateIndex(
                name: "IX_Qualificacoes_EmpresaId",
                schema: "qh",
                table: "Qualificacoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Qualificacoes_FornecedorId_DataQualificacao",
                schema: "qh",
                table: "Qualificacoes",
                columns: new[] { "FornecedorId", "DataQualificacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Qualificacoes_MaterialControladoId",
                schema: "qh",
                table: "Qualificacoes",
                column: "MaterialControladoId");

            migrationBuilder.CreateIndex(
                name: "IX_Qualificacoes_ServicoControladoId",
                schema: "qh",
                table: "Qualificacoes",
                column: "ServicoControladoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecebimentoItens_PedidoItemId",
                schema: "qh",
                table: "RecebimentoItens",
                column: "PedidoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RecebimentoItens_RecebimentoId",
                schema: "qh",
                table: "RecebimentoItens",
                column: "RecebimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_ContratoId",
                schema: "qh",
                table: "Recebimentos",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_EmpresaId",
                schema: "qh",
                table: "Recebimentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_EmpresaId_Numero",
                schema: "qh",
                table: "Recebimentos",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_EmpresaId_ObraId",
                schema: "qh",
                table: "Recebimentos",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_FornecedorId_DataRecebimento",
                schema: "qh",
                table: "Recebimentos",
                columns: new[] { "FornecedorId", "DataRecebimento" });

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_ObraId",
                schema: "qh",
                table: "Recebimentos",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_PedidoId",
                schema: "qh",
                table: "Recebimentos",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegrasNotificacao_EmpresaId",
                schema: "qh",
                table: "RegrasNotificacao",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegrasRetencao_EmpresaId",
                schema: "qh",
                table: "RegrasRetencao",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisicaoItens_MaterialControladoId",
                schema: "qh",
                table: "RequisicaoItens",
                column: "MaterialControladoId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisicaoItens_ProdutoId",
                schema: "qh",
                table: "RequisicaoItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisicaoItens_RequisicaoId",
                schema: "qh",
                table: "RequisicaoItens",
                column: "RequisicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisicaoItens_ServicoControladoId",
                schema: "qh",
                table: "RequisicaoItens",
                column: "ServicoControladoId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisicoes_CentroCustoId",
                schema: "qh",
                table: "Requisicoes",
                column: "CentroCustoId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisicoes_EmpresaId",
                schema: "qh",
                table: "Requisicoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisicoes_EmpresaId_Numero",
                schema: "qh",
                table: "Requisicoes",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Requisicoes_EmpresaId_ObraId",
                schema: "qh",
                table: "Requisicoes",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Requisicoes_EmpresaId_Status",
                schema: "qh",
                table: "Requisicoes",
                columns: new[] { "EmpresaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Requisicoes_ObraId",
                schema: "qh",
                table: "Requisicoes",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Revisoes_DocumentoId_Revisao",
                schema: "qh",
                table: "Revisoes",
                columns: new[] { "DocumentoId", "Revisao" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Revisoes_EmpresaId",
                schema: "qh",
                table: "Revisoes",
                column: "EmpresaId");

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
                name: "IX_Rncs_FornecedorId_Status",
                schema: "qh",
                table: "Rncs",
                columns: new[] { "FornecedorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Sequencias_EmpresaId_Chave",
                schema: "qh",
                table: "Sequencias",
                columns: new[] { "EmpresaId", "Chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicosControlados_DocumentoItsId",
                schema: "qh",
                table: "ServicosControlados",
                column: "DocumentoItsId");

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
                name: "IX_SolicitacaoCopiaItens_DocumentoRevisaoId",
                schema: "qh",
                table: "SolicitacaoCopiaItens",
                column: "DocumentoRevisaoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoCopiaItens_SolicitacaoCopiaId",
                schema: "qh",
                table: "SolicitacaoCopiaItens",
                column: "SolicitacaoCopiaId");

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
                name: "IX_SolicitacoesAprovacao_SolicitanteUsuarioId",
                schema: "qh",
                table: "SolicitacoesAprovacao",
                column: "SolicitanteUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesCopia_EmpresaId",
                schema: "qh",
                table: "SolicitacoesCopia",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesCopia_EmpresaId_Numero",
                schema: "qh",
                table: "SolicitacoesCopia",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesCopia_ObraId",
                schema: "qh",
                table: "SolicitacoesCopia",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_TarefaLeituras_TarefaId_UsuarioId",
                schema: "qh",
                table: "TarefaLeituras",
                columns: new[] { "TarefaId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TarefaProrrogacoes_TarefaId",
                schema: "qh",
                table: "TarefaProrrogacoes",
                column: "TarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tarefas_EmpresaId",
                schema: "qh",
                table: "Tarefas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tarefas_EmpresaId_Numero",
                schema: "qh",
                table: "Tarefas",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Tarefas_OrigemEntidade_OrigemId",
                schema: "qh",
                table: "Tarefas",
                columns: new[] { "OrigemEntidade", "OrigemId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tarefas_ResponsavelId_Status",
                schema: "qh",
                table: "Tarefas",
                columns: new[] { "ResponsavelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumento_EmpresaId",
                schema: "qh",
                table: "TiposDocumento",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Treinamentos_EmpresaId",
                schema: "qh",
                table: "Treinamentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_EmpresaId",
                schema: "qh",
                table: "Turmas",
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

            migrationBuilder.AddForeignKey(
                name: "FK_Checkouts_Revisoes_DocumentoRevisaoId",
                schema: "qh",
                table: "Checkouts",
                column: "DocumentoRevisaoId",
                principalSchema: "qh",
                principalTable: "Revisoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ciencias_Revisoes_DocumentoRevisaoId",
                schema: "qh",
                table: "Ciencias",
                column: "DocumentoRevisaoId",
                principalSchema: "qh",
                principalTable: "Revisoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContratoItens_ServicosControlados_ServicoControladoId",
                schema: "qh",
                table: "ContratoItens",
                column: "ServicoControladoId",
                principalSchema: "qh",
                principalTable: "ServicosControlados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContratoServicos_ServicosControlados_ServicoControladoId",
                schema: "qh",
                table: "ContratoServicos",
                column: "ServicoControladoId",
                principalSchema: "qh",
                principalTable: "ServicosControlados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Copias_Revisoes_DocumentoRevisaoId",
                schema: "qh",
                table: "Copias",
                column: "DocumentoRevisaoId",
                principalSchema: "qh",
                principalTable: "Revisoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CotacaoItens_RequisicaoItens_RequisicaoItemId",
                schema: "qh",
                table: "CotacaoItens",
                column: "RequisicaoItemId",
                principalSchema: "qh",
                principalTable: "RequisicaoItens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documentos_Revisoes_RevisaoVigenteId",
                schema: "qh",
                table: "Documentos",
                column: "RevisaoVigenteId",
                principalSchema: "qh",
                principalTable: "Revisoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documentos_Obras_ObraId",
                schema: "qh",
                table: "Documentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Documentos_Revisoes_RevisaoVigenteId",
                schema: "qh",
                table: "Documentos");

            migrationBuilder.DropTable(
                name: "AcaoCorretivaItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "AditivoItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Alcadas",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Anexos",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "AplicacoesLote",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Auditoria",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "AvaliacoesSemestrais",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AvaliacaoSemestral_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Calibracoes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Checkouts",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Ciencias",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Colaboradores",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Colaborador_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Constatacoes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ContratoServicos",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Copias",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CopiaControlada_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "CotacaoFornecedores",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "CotacaoItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "DocumentosColaborador",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DocumentoColaborador_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "DocumentosExternos",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DocumentoExterno_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "DocumentosRelacionados",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Empresas",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "EnsaiosModelo",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "EnsaiosRecebimento",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EnsaioRecebimento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "EntregasEpi",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EntregaEpi_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Epis",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "EtapasAprovacao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "EtapasRevisao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "EventosDocumento",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ExecutantesPt",
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
                name: "FuncaoExigencias",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "FvmItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "FvsItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "GrdItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "InspecaoSegurancaItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ItensVerificacao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "LayoutsImportacao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "LimitesAprovacao",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "LimiteAprovacao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "LinhasImportacao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "LinksPortal",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Listas",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "MedicaoItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Notas",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Ocorrencias",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Ocorrencia_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Pagamentos",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "LiberacaoPagamento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Parametros",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Parametro_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Participantes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Permissoes",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PermissaoDocumento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Qualificacoes",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "FornecedorQualificacao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "RecebimentoItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "RegrasNotificacao",
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
                name: "Sequencias",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "SolicitacaoCopiaItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "TarefaLeituras",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "TarefaProrrogacoes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Treinamentos",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "UsuariosObras",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "AcoesCorretivas",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AcaoCorretiva_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Aditivos",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Aditivo_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Equipamentos",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EquipamentoMedicao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "AuditoriasInternas",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AuditoriaInterna_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "DelegacoesAprovacao",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DelegacaoAprovacao_Historico")
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
                name: "PermissoesTrabalho",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PermissaoTrabalho_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "TiposDocumento",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Exigencias",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Funcoes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Fvs",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Fvs_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Grds",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "InspecoesSeguranca",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "InspecaoSeguranca_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "LotesImportacao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ContratoItens",
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
                name: "Turmas",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Turma_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "PedidoItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Recebimentos",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Recebimento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "SolicitacoesCopia",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Tarefas",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Tarefa_Historico")
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
                name: "ModelosInspecao",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ModeloInspecao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "PlanosAcao",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PlanoAcaoFornecedor_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "RequisicaoItens",
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
                name: "Produtos",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ServicosControlados",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Contratos",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Contrato_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "MateriaisControlados",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Cotacoes",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Cotacao_Historico")
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
                name: "Requisicoes",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Requisicao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "CentrosCusto",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Obras",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Revisoes",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "DocumentoRevisao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Documentos",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Documento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Pastas",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "RegrasRetencao",
                schema: "qh");
        }
    }
}

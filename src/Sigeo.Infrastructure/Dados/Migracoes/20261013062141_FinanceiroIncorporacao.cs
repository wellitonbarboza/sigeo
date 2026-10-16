using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigeo.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class FinanceiroIncorporacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AliquotasRegime",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Regime = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Irpj = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Csll = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Pis = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Cofins = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Cbs = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Ibs = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    CodigoReceita = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_AliquotasRegime", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Compradores",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CpfCnpj = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Telefone = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Endereco = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Conjuge = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Compradores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CondicoesPagamento",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EntradaPerc = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Prazos = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_CondicoesPagamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContasBancarias",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Banco = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Agencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SaldoInicial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DataSaldoInicial = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.PrimaryKey("PK_ContasBancarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasBancarias_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalSchema: "qh",
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContasGerenciais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Natureza = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_ContasGerenciais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Incorporacoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MemorialRegistro = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DataRegistroMemorial = table.Column<DateOnly>(type: "date", nullable: true),
                    PatrimonioAfetacao = table.Column<bool>(type: "bit", nullable: false),
                    DataAverbacao = table.Column<DateOnly>(type: "date", nullable: true),
                    CnpjIncorporacao = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true),
                    Regime = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DataOpcaoRet = table.Column<DateOnly>(type: "date", nullable: true),
                    OpcaoTransicaoIbsCbs = table.Column<bool>(type: "bit", nullable: false),
                    PrevisaoChaves = table.Column<DateOnly>(type: "date", nullable: true),
                    IndiceAteChaves = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IndiceAposChaves = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JurosAposChavesMes = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    DiaVencimentoRet = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Incorporacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incorporacoes_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IndicesEconomicos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Indice = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    VariacaoPerc = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
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
                    table.PrimaryKey("PK_IndicesEconomicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnidadesComerciais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrecoTabela = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Vagas = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_UnidadesComerciais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnidadesComerciais_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Titulos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    DocumentoTipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    DocumentoNumero = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Pessoa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CentroCustoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContaGerencialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Emissao = table.Column<DateOnly>(type: "date", nullable: false),
                    Competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorBruto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RetencaoIss = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RetencaoInss = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RetencaoIr = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RetencaoPcc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OrigemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChaveOrigem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Historico = table.Column<bool>(type: "bit", nullable: false),
                    Cancelado = table.Column<bool>(type: "bit", nullable: false),
                    MotivoCancelamento = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Titulos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Titulos_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalSchema: "qh",
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Titulos_Compradores_CompradorId",
                        column: x => x.CompradorId,
                        principalSchema: "qh",
                        principalTable: "Compradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Titulos_ContasGerenciais_ContaGerencialId",
                        column: x => x.ContaGerencialId,
                        principalSchema: "qh",
                        principalTable: "ContasGerenciais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Titulos_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalSchema: "qh",
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Titulos_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Titulos_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Titulo_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ApuracoesRet",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncorporacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    Regime = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReceitaRecebida = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Irpj = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Csll = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Pis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Cofins = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Cbs = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Ibs = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AliquotaTotal = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TituloId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ApuracoesRet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApuracoesRet_Incorporacoes_IncorporacaoId",
                        column: x => x.IncorporacaoId,
                        principalSchema: "qh",
                        principalTable: "Incorporacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApuracoesRet_Titulos_TituloId",
                        column: x => x.TituloId,
                        principalSchema: "qh",
                        principalTable: "Titulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContratosVenda",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    UnidadeComercialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataContrato = table.Column<DateOnly>(type: "date", nullable: false),
                    DataBase = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorVenda = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IndiceAteChaves = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IndiceAposChaves = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JurosAposChavesMes = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    DataChaves = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TituloId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataDistrato = table.Column<DateOnly>(type: "date", nullable: true),
                    ValorDevolvido = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_ContratosVenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContratosVenda_Compradores_CompradorId",
                        column: x => x.CompradorId,
                        principalSchema: "qh",
                        principalTable: "Compradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContratosVenda_Titulos_TituloId",
                        column: x => x.TituloId,
                        principalSchema: "qh",
                        principalTable: "Titulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContratosVenda_UnidadesComerciais_UnidadeComercialId",
                        column: x => x.UnidadeComercialId,
                        principalSchema: "qh",
                        principalTable: "UnidadesComerciais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TituloParcelas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TituloId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TipoVenda = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FornecedorPermutaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataPagamento = table.Column<DateOnly>(type: "date", nullable: true),
                    Correcao = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Juros = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Multa = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Desconto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ContaBancariaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Forma = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Comprovante = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_TituloParcelas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TituloParcelas_ContasBancarias_ContaBancariaId",
                        column: x => x.ContaBancariaId,
                        principalSchema: "qh",
                        principalTable: "ContasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TituloParcelas_Fornecedores_FornecedorPermutaId",
                        column: x => x.FornecedorPermutaId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TituloParcelas_Titulos_TituloId",
                        column: x => x.TituloId,
                        principalSchema: "qh",
                        principalTable: "Titulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "TituloParcela_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "BlocosCondicaoVenda",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoVendaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    ValorParcela = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrimeiroVencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    IntervaloMeses = table.Column<int>(type: "int", nullable: false),
                    FornecedorPermutaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlocosCondicaoVenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlocosCondicaoVenda_ContratosVenda_ContratoVendaId",
                        column: x => x.ContratoVendaId,
                        principalSchema: "qh",
                        principalTable: "ContratosVenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlocosCondicaoVenda_Fornecedores_FornecedorPermutaId",
                        column: x => x.FornecedorPermutaId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComissoesVenda",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoVendaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Papel = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Percentual = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Forma = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Parcelas = table.Column<int>(type: "int", nullable: false),
                    PercentualPagoMinimo = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    TituloId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Estornada = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComissoesVenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComissoesVenda_ContratosVenda_ContratoVendaId",
                        column: x => x.ContratoVendaId,
                        principalSchema: "qh",
                        principalTable: "ContratosVenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComissoesVenda_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComissoesVenda_Titulos_TituloId",
                        column: x => x.TituloId,
                        principalSchema: "qh",
                        principalTable: "Titulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimentosBancarios",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContaBancariaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Historico = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TituloParcelaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContaGerencialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Conciliado = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_MovimentosBancarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentosBancarios_ContasBancarias_ContaBancariaId",
                        column: x => x.ContaBancariaId,
                        principalSchema: "qh",
                        principalTable: "ContasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosBancarios_TituloParcelas_TituloParcelaId",
                        column: x => x.TituloParcelaId,
                        principalSchema: "qh",
                        principalTable: "TituloParcelas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AliquotasRegime_EmpresaId",
                schema: "qh",
                table: "AliquotasRegime",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_AliquotasRegime_EmpresaId_Regime_VigenciaInicio",
                schema: "qh",
                table: "AliquotasRegime",
                columns: new[] { "EmpresaId", "Regime", "VigenciaInicio" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ApuracoesRet_EmpresaId",
                schema: "qh",
                table: "ApuracoesRet",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ApuracoesRet_IncorporacaoId_Competencia",
                schema: "qh",
                table: "ApuracoesRet",
                columns: new[] { "IncorporacaoId", "Competencia" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ApuracoesRet_TituloId",
                schema: "qh",
                table: "ApuracoesRet",
                column: "TituloId");

            migrationBuilder.CreateIndex(
                name: "IX_BlocosCondicaoVenda_ContratoVendaId",
                schema: "qh",
                table: "BlocosCondicaoVenda",
                column: "ContratoVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_BlocosCondicaoVenda_FornecedorPermutaId",
                schema: "qh",
                table: "BlocosCondicaoVenda",
                column: "FornecedorPermutaId");

            migrationBuilder.CreateIndex(
                name: "IX_ComissoesVenda_ContratoVendaId",
                schema: "qh",
                table: "ComissoesVenda",
                column: "ContratoVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_ComissoesVenda_FornecedorId",
                schema: "qh",
                table: "ComissoesVenda",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_ComissoesVenda_TituloId",
                schema: "qh",
                table: "ComissoesVenda",
                column: "TituloId");

            migrationBuilder.CreateIndex(
                name: "IX_Compradores_EmpresaId",
                schema: "qh",
                table: "Compradores",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CondicoesPagamento_EmpresaId",
                schema: "qh",
                table: "CondicoesPagamento",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CondicoesPagamento_EmpresaId_Nome",
                schema: "qh",
                table: "CondicoesPagamento",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_EmpresaId",
                schema: "qh",
                table: "ContasBancarias",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_EstabelecimentoId",
                schema: "qh",
                table: "ContasBancarias",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasGerenciais_EmpresaId",
                schema: "qh",
                table: "ContasGerenciais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasGerenciais_EmpresaId_Codigo",
                schema: "qh",
                table: "ContasGerenciais",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosVenda_CompradorId",
                schema: "qh",
                table: "ContratosVenda",
                column: "CompradorId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosVenda_EmpresaId",
                schema: "qh",
                table: "ContratosVenda",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosVenda_EmpresaId_Numero",
                schema: "qh",
                table: "ContratosVenda",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosVenda_EmpresaId_ObraId",
                schema: "qh",
                table: "ContratosVenda",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_ContratosVenda_TituloId",
                schema: "qh",
                table: "ContratosVenda",
                column: "TituloId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosVenda_UnidadeComercialId",
                schema: "qh",
                table: "ContratosVenda",
                column: "UnidadeComercialId");

            migrationBuilder.CreateIndex(
                name: "IX_Incorporacoes_EmpresaId",
                schema: "qh",
                table: "Incorporacoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Incorporacoes_ObraId",
                schema: "qh",
                table: "Incorporacoes",
                column: "ObraId",
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_IndicesEconomicos_EmpresaId",
                schema: "qh",
                table: "IndicesEconomicos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_IndicesEconomicos_EmpresaId_Indice_Competencia",
                schema: "qh",
                table: "IndicesEconomicos",
                columns: new[] { "EmpresaId", "Indice", "Competencia" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosBancarios_ContaBancariaId_Data",
                schema: "qh",
                table: "MovimentosBancarios",
                columns: new[] { "ContaBancariaId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosBancarios_EmpresaId",
                schema: "qh",
                table: "MovimentosBancarios",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosBancarios_TituloParcelaId",
                schema: "qh",
                table: "MovimentosBancarios",
                column: "TituloParcelaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosBancarios_TransferenciaId",
                schema: "qh",
                table: "MovimentosBancarios",
                column: "TransferenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_TituloParcelas_ContaBancariaId",
                schema: "qh",
                table: "TituloParcelas",
                column: "ContaBancariaId");

            migrationBuilder.CreateIndex(
                name: "IX_TituloParcelas_DataPagamento",
                schema: "qh",
                table: "TituloParcelas",
                column: "DataPagamento");

            migrationBuilder.CreateIndex(
                name: "IX_TituloParcelas_EmpresaId",
                schema: "qh",
                table: "TituloParcelas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_TituloParcelas_FornecedorPermutaId",
                schema: "qh",
                table: "TituloParcelas",
                column: "FornecedorPermutaId");

            migrationBuilder.CreateIndex(
                name: "IX_TituloParcelas_Status_Vencimento",
                schema: "qh",
                table: "TituloParcelas",
                columns: new[] { "Status", "Vencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_TituloParcelas_TituloId",
                schema: "qh",
                table: "TituloParcelas",
                column: "TituloId");

            migrationBuilder.CreateIndex(
                name: "IX_Titulos_CentroCustoId",
                schema: "qh",
                table: "Titulos",
                column: "CentroCustoId");

            migrationBuilder.CreateIndex(
                name: "IX_Titulos_CompradorId",
                schema: "qh",
                table: "Titulos",
                column: "CompradorId");

            migrationBuilder.CreateIndex(
                name: "IX_Titulos_ContaGerencialId",
                schema: "qh",
                table: "Titulos",
                column: "ContaGerencialId");

            migrationBuilder.CreateIndex(
                name: "IX_Titulos_EmpresaId",
                schema: "qh",
                table: "Titulos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Titulos_EmpresaId_ChaveOrigem",
                schema: "qh",
                table: "Titulos",
                columns: new[] { "EmpresaId", "ChaveOrigem" },
                unique: true,
                filter: "[Excluido] = 0 AND [ChaveOrigem] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Titulos_EmpresaId_Tipo_Numero",
                schema: "qh",
                table: "Titulos",
                columns: new[] { "EmpresaId", "Tipo", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Titulos_EstabelecimentoId",
                schema: "qh",
                table: "Titulos",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Titulos_FornecedorId",
                schema: "qh",
                table: "Titulos",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Titulos_ObraId",
                schema: "qh",
                table: "Titulos",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Titulos_Origem_OrigemId",
                schema: "qh",
                table: "Titulos",
                columns: new[] { "Origem", "OrigemId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesComerciais_EmpresaId",
                schema: "qh",
                table: "UnidadesComerciais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesComerciais_EmpresaId_ObraId",
                schema: "qh",
                table: "UnidadesComerciais",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesComerciais_LocalId",
                schema: "qh",
                table: "UnidadesComerciais",
                column: "LocalId",
                unique: true,
                filter: "[Excluido] = 0");

            // Módulos adicionais (Financeiro e Incorporação) nascem bloqueados em todas as construtoras: a plataforma libera quem contratar.
            migrationBuilder.Sql("""
                UPDATE qh.Empresas SET TelasBloqueadas = CONCAT(TelasBloqueadas, CASE WHEN ISNULL(TelasBloqueadas, N'') = N'' THEN N'' ELSE N';' END,
                    N'financeiro/pagar;financeiro/receber;financeiro/fluxo;financeiro/bancos;financeiro/cadastros;incorporacao/empreendimento;incorporacao/espelho;incorporacao/vendas;incorporacao/carteira;incorporacao/ret')
                WHERE ISNULL(TelasBloqueadas, N'') NOT LIKE N'%financeiro/pagar%';
                """);
            // Perfis padrão: Financeiro faz tudo nos dois módulos; Diretoria vê, aprova e exporta.
            migrationBuilder.Sql("""
                UPDATE qh.PerfisAcesso SET Regras = CONCAT(Regras, CASE WHEN Regras = N'' THEN N'' ELSE N';' END,
                    N'financeiro/pagar=VIEAX;financeiro/receber=VIEX;financeiro/fluxo=VX;financeiro/bancos=VIEX;financeiro/cadastros=VIEX;incorporacao/empreendimento=VIEX;incorporacao/espelho=VIEX;incorporacao/vendas=VIEX;incorporacao/carteira=VIX;incorporacao/ret=VIEX')
                WHERE Base = N'Financeiro' AND Regras NOT LIKE N'%financeiro/pagar=%';
                UPDATE qh.PerfisAcesso SET Regras = CONCAT(Regras, CASE WHEN Regras = N'' THEN N'' ELSE N';' END,
                    N'financeiro/pagar=VAX;financeiro/receber=VX;financeiro/fluxo=VX;financeiro/bancos=VX;financeiro/cadastros=VX;incorporacao/empreendimento=VX;incorporacao/espelho=VX;incorporacao/vendas=VX;incorporacao/carteira=VX;incorporacao/ret=VX')
                WHERE Base = N'Diretoria' AND Regras NOT LIKE N'%financeiro/pagar=%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AliquotasRegime",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ApuracoesRet",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "BlocosCondicaoVenda",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ComissoesVenda",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "CondicoesPagamento",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "IndicesEconomicos",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "MovimentosBancarios",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Incorporacoes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ContratosVenda",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "TituloParcelas",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "TituloParcela_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "UnidadesComerciais",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ContasBancarias",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Titulos",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Titulo_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Compradores",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ContasGerenciais",
                schema: "qh");
        }
    }
}

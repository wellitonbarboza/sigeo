using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualyHub.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class CondominioOrcamentoFornecedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Spes_QuadraId",
                schema: "qh",
                table: "Spes");

            // 1 condomínio = 1 SPE: se houver mais de uma SPE na mesma quadra (modelo antigo, 1 prédio = 1 SPE), só a primeira continua vinculada.
            migrationBuilder.Sql(@"WITH x AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY QuadraId ORDER BY CriadoEm) AS N FROM qh.Spes WHERE QuadraId IS NOT NULL AND Excluido = 0)
UPDATE s SET QuadraId = NULL FROM qh.Spes s JOIN x ON x.Id = s.Id WHERE x.N > 1;");

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                schema: "qh",
                table: "TiposDocumento",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeFuncionario",
                schema: "qh",
                table: "TiposDocumento",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DiaLimite",
                schema: "qh",
                table: "TiposDocumento",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Frequencia",
                schema: "qh",
                table: "TiposDocumento",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Validade");

            migrationBuilder.AddColumn<int>(
                name: "Ordem",
                schema: "qh",
                table: "TiposDocumento",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Observacoes",
                schema: "qh",
                table: "Tipologias",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pavimentos",
                schema: "qh",
                table: "Tipologias",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PeDireitoPadrao",
                schema: "qh",
                table: "Tipologias",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrimeiroTerreo",
                schema: "qh",
                table: "Tipologias",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                schema: "qh",
                table: "Tipologias",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Apartamento");

            migrationBuilder.AddColumn<string>(
                name: "Sigla",
                schema: "qh",
                table: "ServicosControlados",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Requisicoes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JustificativaAcimaOrcado",
                schema: "qh",
                table: "Requisicoes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ComposicaoId",
                schema: "qh",
                table: "RequisicaoItens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AlmoxarifadoCentral",
                schema: "qh",
                table: "Recebimentos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "EstabelecimentoId",
                schema: "qh",
                table: "Recebimentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImplantacaoId",
                schema: "qh",
                table: "Quadras",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EntregaAlmoxarifadoCentral",
                schema: "qh",
                table: "Pedidos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Pedidos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoriaProdutoId",
                schema: "qh",
                table: "MateriaisControlados",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AmbienteModeloId",
                schema: "qh",
                table: "Locais",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AreaParede",
                schema: "qh",
                table: "Locais",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AreaPiso",
                schema: "qh",
                table: "Locais",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                schema: "qh",
                table: "Locais",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PeDireito",
                schema: "qh",
                table: "Locais",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Perimetro",
                schema: "qh",
                table: "Locais",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncluirDocsFuncionarios",
                schema: "qh",
                table: "Fornecedores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PreCadastro",
                schema: "qh",
                table: "Fornecedores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Competencia",
                schema: "qh",
                table: "FornecedorDocumentos",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Contratos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ComposicaoId",
                schema: "qh",
                table: "ContratoItens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoErp",
                schema: "qh",
                table: "CentrosCusto",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstabelecimentosPermitidos",
                schema: "qh",
                table: "CentrosCusto",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaiId",
                schema: "qh",
                table: "CentrosCusto",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Composicoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Grupo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Regra = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Fator = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ServicoControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Composicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Composicoes_ServicosControlados_ServicoControladoId",
                        column: x => x.ServicoControladoId,
                        principalSchema: "qh",
                        principalTable: "ServicosControlados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Composicao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ContratoItemLocais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratoItemLocais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContratoItemLocais_ContratoItens_ContratoItemId",
                        column: x => x.ContratoItemId,
                        principalSchema: "qh",
                        principalTable: "ContratoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContratoItemLocais_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Equipes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Encarregado = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Equipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Estabelecimentos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RazaoSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NomeFantasia = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Cnpj = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Raiz = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    InscricaoEstadual = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Endereco = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Atividade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    SpeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FaturaMateriais = table.Column<bool>(type: "bit", nullable: false),
                    AlmoxarifadoCentral = table.Column<bool>(type: "bit", nullable: false),
                    EnderecoEntrega = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("PK_Estabelecimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Estabelecimentos_Spes_SpeId",
                        column: x => x.SpeId,
                        principalSchema: "qh",
                        principalTable: "Spes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FornecedorExigencias",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoDocumentoExigidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Obrigatorio = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FornecedorExigencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FornecedorExigencias_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FornecedorExigencias_TiposDocumento_TipoDocumentoExigidoId",
                        column: x => x.TipoDocumentoExigidoId,
                        principalSchema: "qh",
                        principalTable: "TiposDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FornecedorGrupos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoriaProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subgrupos = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FornecedorGrupos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FornecedorGrupos_CategoriasProduto_CategoriaProdutoId",
                        column: x => x.CategoriaProdutoId,
                        principalSchema: "qh",
                        principalTable: "CategoriasProduto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FornecedorGrupos_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FornecedorServicos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicoControladoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FornecedorServicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FornecedorServicos_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FornecedorServicos_ServicosControlados_ServicoControladoId",
                        column: x => x.ServicoControladoId,
                        principalSchema: "qh",
                        principalTable: "ServicosControlados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequisicaoItemLocais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisicaoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisicaoItemLocais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisicaoItemLocais_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisicaoItemLocais_RequisicaoItens_RequisicaoItemId",
                        column: x => x.RequisicaoItemId,
                        principalSchema: "qh",
                        principalTable: "RequisicaoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TipologiaAmbientes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipologiaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Escopo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Sigla = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    AreaPiso = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Perimetro = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PeDireito = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    AreaVaos = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PavimentoDe = table.Column<int>(type: "int", nullable: true),
                    PavimentoAte = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipologiaAmbientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TipologiaAmbientes_Tipologias_TipologiaId",
                        column: x => x.TipologiaId,
                        principalSchema: "qh",
                        principalTable: "Tipologias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TipologiaMix",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipologiaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    PavimentoDe = table.Column<int>(type: "int", nullable: false),
                    PavimentoAte = table.Column<int>(type: "int", nullable: false),
                    ApartamentoTipologiaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipologiaMix", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TipologiaMix_Tipologias_ApartamentoTipologiaId",
                        column: x => x.ApartamentoTipologiaId,
                        principalSchema: "qh",
                        principalTable: "Tipologias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TipologiaMix_Tipologias_TipologiaId",
                        column: x => x.TipologiaId,
                        principalSchema: "qh",
                        principalTable: "Tipologias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transferencias",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    RecebimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrigemEstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SpeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ConferidoPor = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ConferidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transferencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transferencias_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transferencias_Recebimentos_RecebimentoId",
                        column: x => x.RecebimentoId,
                        principalSchema: "qh",
                        principalTable: "Recebimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transferencias_Spes_SpeId",
                        column: x => x.SpeId,
                        principalSchema: "qh",
                        principalTable: "Spes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "TransferenciaMaterial_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ComposicaoItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FuncaoObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Coeficiente = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PerdaPerc = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    CustoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComposicaoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComposicaoItens_Composicoes_ComposicaoId",
                        column: x => x.ComposicaoId,
                        principalSchema: "qh",
                        principalTable: "Composicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComposicaoItens_Funcoes_FuncaoObraId",
                        column: x => x.FuncaoObraId,
                        principalSchema: "qh",
                        principalTable: "Funcoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComposicaoItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalSchema: "qh",
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServicoAplicacoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipologiaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Alvo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AmbienteModeloId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuantidadeFixa = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
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
                    table.PrimaryKey("PK_ServicoAplicacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicoAplicacoes_Composicoes_ComposicaoId",
                        column: x => x.ComposicaoId,
                        principalSchema: "qh",
                        principalTable: "Composicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServicoAplicacoes_Tipologias_TipologiaId",
                        column: x => x.TipologiaId,
                        principalSchema: "qh",
                        principalTable: "Tipologias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServicosLocais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CustoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Custo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Travado = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ServicosLocais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicosLocais_Composicoes_ComposicaoId",
                        column: x => x.ComposicaoId,
                        principalSchema: "qh",
                        principalTable: "Composicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServicosLocais_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Apropriacoes",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    EquipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Horas = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    QuantidadeExecutada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Lancamento = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_Apropriacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Apropriacoes_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalSchema: "qh",
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Apropriacoes_Composicoes_ComposicaoId",
                        column: x => x.ComposicaoId,
                        principalSchema: "qh",
                        principalTable: "Composicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Apropriacoes_Equipes_EquipeId",
                        column: x => x.EquipeId,
                        principalSchema: "qh",
                        principalTable: "Equipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Apropriacoes_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustosMaoObra",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    EquipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FuncaoObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pessoas = table.Column<int>(type: "int", nullable: false),
                    Salario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EncargosPerc = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Beneficios = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Outros = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HorasMes = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_CustosMaoObra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustosMaoObra_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalSchema: "qh",
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustosMaoObra_Equipes_EquipeId",
                        column: x => x.EquipeId,
                        principalSchema: "qh",
                        principalTable: "Equipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustosMaoObra_Funcoes_FuncaoObraId",
                        column: x => x.FuncaoObraId,
                        principalSchema: "qh",
                        principalTable: "Funcoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipeMembros",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Desde = table.Column<DateOnly>(type: "date", nullable: false),
                    Ate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipeMembros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipeMembros_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalSchema: "qh",
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipeMembros_Equipes_EquipeId",
                        column: x => x.EquipeId,
                        principalSchema: "qh",
                        principalTable: "Equipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FluxosEstabelecimento",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrigemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Base = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_FluxosEstabelecimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FluxosEstabelecimento_Estabelecimentos_DestinoId",
                        column: x => x.DestinoId,
                        principalSchema: "qh",
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FluxosEstabelecimento_Estabelecimentos_OrigemId",
                        column: x => x.OrigemId,
                        principalSchema: "qh",
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferenciaItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecebimentoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantidadeRecebida = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CustoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Lote = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferenciaItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferenciaItens_Transferencias_TransferenciaId",
                        column: x => x.TransferenciaId,
                        principalSchema: "qh",
                        principalTable: "Transferencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Spes_QuadraId",
                schema: "qh",
                table: "Spes",
                column: "QuadraId",
                unique: true,
                filter: "[Excluido] = 0 AND [QuadraId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Requisicoes_EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Requisicoes",
                column: "EstabelecimentoFaturamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisicaoItens_ComposicaoId",
                schema: "qh",
                table: "RequisicaoItens",
                column: "ComposicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_EstabelecimentoId",
                schema: "qh",
                table: "Recebimentos",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Quadras_ImplantacaoId",
                schema: "qh",
                table: "Quadras",
                column: "ImplantacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Pedidos",
                column: "EstabelecimentoFaturamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_MateriaisControlados_CategoriaProdutoId",
                schema: "qh",
                table: "MateriaisControlados",
                column: "CategoriaProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Locais_AmbienteModeloId",
                schema: "qh",
                table: "Locais",
                column: "AmbienteModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Contratos",
                column: "EstabelecimentoFaturamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoItens_ComposicaoId",
                schema: "qh",
                table: "ContratoItens",
                column: "ComposicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCusto_PaiId",
                schema: "qh",
                table: "CentrosCusto",
                column: "PaiId");

            migrationBuilder.CreateIndex(
                name: "IX_Apropriacoes_ColaboradorId",
                schema: "qh",
                table: "Apropriacoes",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_Apropriacoes_ComposicaoId",
                schema: "qh",
                table: "Apropriacoes",
                column: "ComposicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Apropriacoes_EmpresaId",
                schema: "qh",
                table: "Apropriacoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Apropriacoes_EmpresaId_ObraId",
                schema: "qh",
                table: "Apropriacoes",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Apropriacoes_EquipeId",
                schema: "qh",
                table: "Apropriacoes",
                column: "EquipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Apropriacoes_LocalId",
                schema: "qh",
                table: "Apropriacoes",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_Apropriacoes_ObraId_Data",
                schema: "qh",
                table: "Apropriacoes",
                columns: new[] { "ObraId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_ComposicaoItens_ComposicaoId",
                schema: "qh",
                table: "ComposicaoItens",
                column: "ComposicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ComposicaoItens_FuncaoObraId",
                schema: "qh",
                table: "ComposicaoItens",
                column: "FuncaoObraId");

            migrationBuilder.CreateIndex(
                name: "IX_ComposicaoItens_ProdutoId",
                schema: "qh",
                table: "ComposicaoItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Composicoes_EmpresaId",
                schema: "qh",
                table: "Composicoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Composicoes_EmpresaId_Codigo",
                schema: "qh",
                table: "Composicoes",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Composicoes_ServicoControladoId",
                schema: "qh",
                table: "Composicoes",
                column: "ServicoControladoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoItemLocais_ContratoItemId",
                schema: "qh",
                table: "ContratoItemLocais",
                column: "ContratoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoItemLocais_LocalId",
                schema: "qh",
                table: "ContratoItemLocais",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosMaoObra_ColaboradorId",
                schema: "qh",
                table: "CustosMaoObra",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosMaoObra_EmpresaId",
                schema: "qh",
                table: "CustosMaoObra",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosMaoObra_EmpresaId_ObraId",
                schema: "qh",
                table: "CustosMaoObra",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustosMaoObra_EquipeId",
                schema: "qh",
                table: "CustosMaoObra",
                column: "EquipeId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosMaoObra_FuncaoObraId",
                schema: "qh",
                table: "CustosMaoObra",
                column: "FuncaoObraId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosMaoObra_ObraId_Competencia",
                schema: "qh",
                table: "CustosMaoObra",
                columns: new[] { "ObraId", "Competencia" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipeMembros_ColaboradorId",
                schema: "qh",
                table: "EquipeMembros",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipeMembros_EquipeId",
                schema: "qh",
                table: "EquipeMembros",
                column: "EquipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipes_EmpresaId",
                schema: "qh",
                table: "Equipes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipes_EmpresaId_ObraId",
                schema: "qh",
                table: "Equipes",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Equipes_ObraId_Codigo",
                schema: "qh",
                table: "Equipes",
                columns: new[] { "ObraId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_EmpresaId",
                schema: "qh",
                table: "Estabelecimentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_EmpresaId_Cnpj",
                schema: "qh",
                table: "Estabelecimentos",
                columns: new[] { "EmpresaId", "Cnpj" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_EmpresaId_Codigo",
                schema: "qh",
                table: "Estabelecimentos",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_SpeId",
                schema: "qh",
                table: "Estabelecimentos",
                column: "SpeId");

            migrationBuilder.CreateIndex(
                name: "IX_FluxosEstabelecimento_DestinoId",
                schema: "qh",
                table: "FluxosEstabelecimento",
                column: "DestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_FluxosEstabelecimento_EmpresaId",
                schema: "qh",
                table: "FluxosEstabelecimento",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_FluxosEstabelecimento_OrigemId",
                schema: "qh",
                table: "FluxosEstabelecimento",
                column: "OrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorExigencias_FornecedorId_TipoDocumentoExigidoId",
                schema: "qh",
                table: "FornecedorExigencias",
                columns: new[] { "FornecedorId", "TipoDocumentoExigidoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorExigencias_TipoDocumentoExigidoId",
                schema: "qh",
                table: "FornecedorExigencias",
                column: "TipoDocumentoExigidoId");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorGrupos_CategoriaProdutoId",
                schema: "qh",
                table: "FornecedorGrupos",
                column: "CategoriaProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorGrupos_FornecedorId_CategoriaProdutoId",
                schema: "qh",
                table: "FornecedorGrupos",
                columns: new[] { "FornecedorId", "CategoriaProdutoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorServicos_FornecedorId_ServicoControladoId",
                schema: "qh",
                table: "FornecedorServicos",
                columns: new[] { "FornecedorId", "ServicoControladoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorServicos_ServicoControladoId",
                schema: "qh",
                table: "FornecedorServicos",
                column: "ServicoControladoId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisicaoItemLocais_LocalId",
                schema: "qh",
                table: "RequisicaoItemLocais",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisicaoItemLocais_RequisicaoItemId",
                schema: "qh",
                table: "RequisicaoItemLocais",
                column: "RequisicaoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicoAplicacoes_ComposicaoId",
                schema: "qh",
                table: "ServicoAplicacoes",
                column: "ComposicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicoAplicacoes_EmpresaId",
                schema: "qh",
                table: "ServicoAplicacoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicoAplicacoes_TipologiaId_ComposicaoId_Alvo_AmbienteModeloId",
                schema: "qh",
                table: "ServicoAplicacoes",
                columns: new[] { "TipologiaId", "ComposicaoId", "Alvo", "AmbienteModeloId" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ServicosLocais_ComposicaoId_LocalId",
                schema: "qh",
                table: "ServicosLocais",
                columns: new[] { "ComposicaoId", "LocalId" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ServicosLocais_EmpresaId",
                schema: "qh",
                table: "ServicosLocais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicosLocais_EmpresaId_ObraId",
                schema: "qh",
                table: "ServicosLocais",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicosLocais_LocalId",
                schema: "qh",
                table: "ServicosLocais",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicosLocais_ObraId_LocalId",
                schema: "qh",
                table: "ServicosLocais",
                columns: new[] { "ObraId", "LocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_TipologiaAmbientes_TipologiaId",
                schema: "qh",
                table: "TipologiaAmbientes",
                column: "TipologiaId");

            migrationBuilder.CreateIndex(
                name: "IX_TipologiaMix_ApartamentoTipologiaId",
                schema: "qh",
                table: "TipologiaMix",
                column: "ApartamentoTipologiaId");

            migrationBuilder.CreateIndex(
                name: "IX_TipologiaMix_TipologiaId",
                schema: "qh",
                table: "TipologiaMix",
                column: "TipologiaId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciaItens_TransferenciaId",
                schema: "qh",
                table: "TransferenciaItens",
                column: "TransferenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_EmpresaId",
                schema: "qh",
                table: "Transferencias",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_EmpresaId_Numero",
                schema: "qh",
                table: "Transferencias",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_EmpresaId_ObraId",
                schema: "qh",
                table: "Transferencias",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_ObraId",
                schema: "qh",
                table: "Transferencias",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_RecebimentoId",
                schema: "qh",
                table: "Transferencias",
                column: "RecebimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_SpeId",
                schema: "qh",
                table: "Transferencias",
                column: "SpeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CentrosCusto_CentrosCusto_PaiId",
                schema: "qh",
                table: "CentrosCusto",
                column: "PaiId",
                principalSchema: "qh",
                principalTable: "CentrosCusto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContratoItens_Composicoes_ComposicaoId",
                schema: "qh",
                table: "ContratoItens",
                column: "ComposicaoId",
                principalSchema: "qh",
                principalTable: "Composicoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contratos_Estabelecimentos_EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Contratos",
                column: "EstabelecimentoFaturamentoId",
                principalSchema: "qh",
                principalTable: "Estabelecimentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MateriaisControlados_CategoriasProduto_CategoriaProdutoId",
                schema: "qh",
                table: "MateriaisControlados",
                column: "CategoriaProdutoId",
                principalSchema: "qh",
                principalTable: "CategoriasProduto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Estabelecimentos_EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Pedidos",
                column: "EstabelecimentoFaturamentoId",
                principalSchema: "qh",
                principalTable: "Estabelecimentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quadras_Tipologias_ImplantacaoId",
                schema: "qh",
                table: "Quadras",
                column: "ImplantacaoId",
                principalSchema: "qh",
                principalTable: "Tipologias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recebimentos_Estabelecimentos_EstabelecimentoId",
                schema: "qh",
                table: "Recebimentos",
                column: "EstabelecimentoId",
                principalSchema: "qh",
                principalTable: "Estabelecimentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequisicaoItens_Composicoes_ComposicaoId",
                schema: "qh",
                table: "RequisicaoItens",
                column: "ComposicaoId",
                principalSchema: "qh",
                principalTable: "Composicoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requisicoes_Estabelecimentos_EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Requisicoes",
                column: "EstabelecimentoFaturamentoId",
                principalSchema: "qh",
                principalTable: "Estabelecimentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Nova estrutura: Condomínio (SPE) › Torre › Pavimento › Apartamento › Cômodo. Renomeia os níveis gerados antes.
            migrationBuilder.Sql(@"UPDATE qh.Locais SET Tipo = N'Condomínio' WHERE Tipo = N'Quadra';
UPDATE qh.Locais SET Tipo = N'Torre' WHERE Tipo IN (N'Prédio', N'Predio', N'Bloco');
UPDATE qh.Locais SET Tipo = N'Apartamento' WHERE Tipo = N'Unidade';
UPDATE qh.Locais SET Tipo = N'Cômodo' WHERE Tipo = N'Ambiente';
UPDATE qh.ModelosInspecao SET NivelLocal = N'Torre' WHERE NivelLocal IN (N'Prédio', N'Predio', N'Bloco');
UPDATE qh.ModelosInspecao SET NivelLocal = N'Apartamento' WHERE NivelLocal = N'Unidade';
UPDATE qh.ModelosInspecao SET NivelLocal = N'Cômodo' WHERE NivelLocal = N'Ambiente';
UPDATE qh.Quadras SET Tipo = N'Condomínio' WHERE Tipo = N'Quadra';");
            // EXEC: colunas criadas neste mesmo script.
            migrationBuilder.Sql(@"EXEC(N'UPDATE qh.TiposDocumento SET Frequencia = CASE WHEN PorEntrega = 1 THEN N''PorEntrega'' WHEN ValidadePadraoDias > 0 THEN N''Validade'' ELSE N''NoCadastro'' END');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CentrosCusto_CentrosCusto_PaiId",
                schema: "qh",
                table: "CentrosCusto");

            migrationBuilder.DropForeignKey(
                name: "FK_ContratoItens_Composicoes_ComposicaoId",
                schema: "qh",
                table: "ContratoItens");

            migrationBuilder.DropForeignKey(
                name: "FK_Contratos_Estabelecimentos_EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.DropForeignKey(
                name: "FK_MateriaisControlados_CategoriasProduto_CategoriaProdutoId",
                schema: "qh",
                table: "MateriaisControlados");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Estabelecimentos_EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_Quadras_Tipologias_ImplantacaoId",
                schema: "qh",
                table: "Quadras");

            migrationBuilder.DropForeignKey(
                name: "FK_Recebimentos_Estabelecimentos_EstabelecimentoId",
                schema: "qh",
                table: "Recebimentos");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisicaoItens_Composicoes_ComposicaoId",
                schema: "qh",
                table: "RequisicaoItens");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisicoes_Estabelecimentos_EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Requisicoes");

            migrationBuilder.DropTable(
                name: "Apropriacoes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ComposicaoItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ContratoItemLocais",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "CustosMaoObra",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "EquipeMembros",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "FluxosEstabelecimento",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "FornecedorExigencias",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "FornecedorGrupos",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "FornecedorServicos",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "RequisicaoItemLocais",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ServicoAplicacoes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ServicosLocais",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "TipologiaAmbientes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "TipologiaMix",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "TransferenciaItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Equipes",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Estabelecimentos",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Composicoes",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Composicao_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Transferencias",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "TransferenciaMaterial_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropIndex(
                name: "IX_Spes_QuadraId",
                schema: "qh",
                table: "Spes");

            migrationBuilder.DropIndex(
                name: "IX_Requisicoes_EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Requisicoes");

            migrationBuilder.DropIndex(
                name: "IX_RequisicaoItens_ComposicaoId",
                schema: "qh",
                table: "RequisicaoItens");

            migrationBuilder.DropIndex(
                name: "IX_Recebimentos_EstabelecimentoId",
                schema: "qh",
                table: "Recebimentos");

            migrationBuilder.DropIndex(
                name: "IX_Quadras_ImplantacaoId",
                schema: "qh",
                table: "Quadras");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_MateriaisControlados_CategoriaProdutoId",
                schema: "qh",
                table: "MateriaisControlados");

            migrationBuilder.DropIndex(
                name: "IX_Locais_AmbienteModeloId",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropIndex(
                name: "IX_Contratos_EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.DropIndex(
                name: "IX_ContratoItens_ComposicaoId",
                schema: "qh",
                table: "ContratoItens");

            migrationBuilder.DropIndex(
                name: "IX_CentrosCusto_PaiId",
                schema: "qh",
                table: "CentrosCusto");

            migrationBuilder.DropColumn(
                name: "Categoria",
                schema: "qh",
                table: "TiposDocumento");

            migrationBuilder.DropColumn(
                name: "DeFuncionario",
                schema: "qh",
                table: "TiposDocumento");

            migrationBuilder.DropColumn(
                name: "DiaLimite",
                schema: "qh",
                table: "TiposDocumento");

            migrationBuilder.DropColumn(
                name: "Frequencia",
                schema: "qh",
                table: "TiposDocumento");

            migrationBuilder.DropColumn(
                name: "Ordem",
                schema: "qh",
                table: "TiposDocumento");

            migrationBuilder.DropColumn(
                name: "Observacoes",
                schema: "qh",
                table: "Tipologias");

            migrationBuilder.DropColumn(
                name: "Pavimentos",
                schema: "qh",
                table: "Tipologias");

            migrationBuilder.DropColumn(
                name: "PeDireitoPadrao",
                schema: "qh",
                table: "Tipologias");

            migrationBuilder.DropColumn(
                name: "PrimeiroTerreo",
                schema: "qh",
                table: "Tipologias");

            migrationBuilder.DropColumn(
                name: "Tipo",
                schema: "qh",
                table: "Tipologias");

            migrationBuilder.DropColumn(
                name: "Sigla",
                schema: "qh",
                table: "ServicosControlados");

            migrationBuilder.DropColumn(
                name: "EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Requisicoes");

            migrationBuilder.DropColumn(
                name: "JustificativaAcimaOrcado",
                schema: "qh",
                table: "Requisicoes");

            migrationBuilder.DropColumn(
                name: "ComposicaoId",
                schema: "qh",
                table: "RequisicaoItens");

            migrationBuilder.DropColumn(
                name: "AlmoxarifadoCentral",
                schema: "qh",
                table: "Recebimentos");

            migrationBuilder.DropColumn(
                name: "EstabelecimentoId",
                schema: "qh",
                table: "Recebimentos");

            migrationBuilder.DropColumn(
                name: "ImplantacaoId",
                schema: "qh",
                table: "Quadras");

            migrationBuilder.DropColumn(
                name: "EntregaAlmoxarifadoCentral",
                schema: "qh",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CategoriaProdutoId",
                schema: "qh",
                table: "MateriaisControlados");

            migrationBuilder.DropColumn(
                name: "AmbienteModeloId",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "AreaParede",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "AreaPiso",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "Categoria",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "PeDireito",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "Perimetro",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "IncluirDocsFuncionarios",
                schema: "qh",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "PreCadastro",
                schema: "qh",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "Competencia",
                schema: "qh",
                table: "FornecedorDocumentos");

            migrationBuilder.DropColumn(
                name: "EstabelecimentoFaturamentoId",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "ComposicaoId",
                schema: "qh",
                table: "ContratoItens");

            migrationBuilder.DropColumn(
                name: "CodigoErp",
                schema: "qh",
                table: "CentrosCusto");

            migrationBuilder.DropColumn(
                name: "EstabelecimentosPermitidos",
                schema: "qh",
                table: "CentrosCusto");

            migrationBuilder.DropColumn(
                name: "PaiId",
                schema: "qh",
                table: "CentrosCusto");

            migrationBuilder.CreateIndex(
                name: "IX_Spes_QuadraId",
                schema: "qh",
                table: "Spes",
                column: "QuadraId");
        }
    }
}

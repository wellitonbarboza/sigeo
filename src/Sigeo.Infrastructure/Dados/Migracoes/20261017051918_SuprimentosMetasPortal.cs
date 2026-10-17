using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigeo.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class SuprimentosMetasPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PodeAdiar",
                schema: "qh",
                table: "TiposDocumento",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AceitaEm",
                schema: "qh",
                table: "Tarefas",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AceitaPor",
                schema: "qh",
                table: "Tarefas",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Aceite",
                schema: "qh",
                table: "Tarefas",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "EquipeId",
                schema: "qh",
                table: "Tarefas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MetaId",
                schema: "qh",
                table: "Tarefas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoDevolucao",
                schema: "qh",
                table: "Tarefas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Emergencial",
                schema: "qh",
                table: "Requisicoes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LoteCompraId",
                schema: "qh",
                table: "RequisicaoItens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCancelamento",
                schema: "qh",
                table: "RequisicaoItens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QtdCancelada",
                schema: "qh",
                table: "RequisicaoItens",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CatalogoVersao",
                schema: "qh",
                table: "PerfisAcesso",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SubstituiDocumentoId",
                schema: "qh",
                table: "FornecedorDocumentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RecebeAvisos",
                schema: "qh",
                table: "FornecedorContatos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LoteCompraId",
                schema: "qh",
                table: "Cotacoes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PrazoResposta",
                schema: "qh",
                table: "Cotacoes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantidadeVencida",
                schema: "qh",
                table: "CotacaoItens",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Vencedor",
                schema: "qh",
                table: "CotacaoItens",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MotivoDeclinio",
                schema: "qh",
                table: "CotacaoFornecedores",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendenciaNaData",
                schema: "qh",
                table: "CotacaoFornecedores",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RascunhoEm",
                schema: "qh",
                table: "CotacaoFornecedores",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RascunhoJson",
                schema: "qh",
                table: "CotacaoFornecedores",
                type: "nvarchar(max)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VisualizadoEm",
                schema: "qh",
                table: "CotacaoFornecedores",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AguardandoValidacao",
                schema: "qh",
                table: "Colaboradores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CadastradoPeloFornecedor",
                schema: "qh",
                table: "Colaboradores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidadoEm",
                schema: "qh",
                table: "Colaboradores",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidadoPor",
                schema: "qh",
                table: "Colaboradores",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDias",
                schema: "qh",
                table: "CategoriasProduto",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CarteirasComprador",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SubstitutoUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubstitutoNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LimiteCarga = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_CarteirasComprador", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetenciasDocumentais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mes = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EnviadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EnviadaPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ConferidaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConferidaPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MotivoReabertura = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_CompetenciasDocumentais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetenciasDocumentais_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetenciasDocumentais_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetenciasDocumentais_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LotesCompra",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Letra = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompradorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompradorNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TitularUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TitularNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DataLimiteCompra = table.Column<DateOnly>(type: "date", nullable: true),
                    AtribuidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MotivoReatribuicao = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_LotesCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LotesCompra_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LotesCompra_Requisicoes_RequisicaoId",
                        column: x => x.RequisicaoId,
                        principalSchema: "qh",
                        principalTable: "Requisicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Metas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Dono = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Setor = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    EquipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EquipeNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Servico = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Fim = table.Column<DateOnly>(type: "date", nullable: false),
                    Indicador = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Alvo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MenorMelhor = table.Column<bool>(type: "bit", nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Peso = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorManual = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Evidencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Metas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropostasVersao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CotacaoFornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Versao = table.Column<int>(type: "int", nullable: false),
                    EnviadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EnviadaPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ValorItens = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorFrete = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PrazoEntrega = table.Column<DateOnly>(type: "date", nullable: true),
                    CondicaoPagamento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Validade = table.Column<DateOnly>(type: "date", nullable: true),
                    ItensJson = table.Column<string>(type: "nvarchar(max)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropostasVersao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropostasVersao_CotacaoFornecedores_CotacaoFornecedorId",
                        column: x => x.CotacaoFornecedorId,
                        principalSchema: "qh",
                        principalTable: "CotacaoFornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AusenciasComprador",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarteiraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Fim = table.Column<DateOnly>(type: "date", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TransferirAbertos = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AusenciasComprador", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AusenciasComprador_CarteirasComprador_CarteiraId",
                        column: x => x.CarteiraId,
                        principalSchema: "qh",
                        principalTable: "CarteirasComprador",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CarteirasCategoria",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarteiraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Grupo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SubGrupo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CategoriaServico = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarteirasCategoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarteirasCategoria_CarteirasComprador_CarteiraId",
                        column: x => x.CarteiraId,
                        principalSchema: "qh",
                        principalTable: "CarteirasComprador",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensCompetencia",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoDocumentoExigidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PodeAdiar = table.Column<bool>(type: "bit", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ArquivoBlob = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArquivoNome = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    EnviadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EnviadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FornecedorDocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MotivoRecusa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConferidoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ConferidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    JustificativaAdiamento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdiadoAte = table.Column<DateOnly>(type: "date", nullable: true),
                    AdiamentoAprovadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AdiamentoAprovadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensCompetencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensCompetencia_CompetenciasDocumentais_CompetenciaId",
                        column: x => x.CompetenciaId,
                        principalSchema: "qh",
                        principalTable: "CompetenciasDocumentais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensCompetencia_TiposDocumento_TipoDocumentoExigidoId",
                        column: x => x.TipoDocumentoExigidoId,
                        principalSchema: "qh",
                        principalTable: "TiposDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MetasApuracao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mes = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PercAtingido = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ApuradoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApuradoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetasApuracao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetasApuracao_Metas_MetaId",
                        column: x => x.MetaId,
                        principalSchema: "qh",
                        principalTable: "Metas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AusenciasComprador_CarteiraId",
                schema: "qh",
                table: "AusenciasComprador",
                column: "CarteiraId");

            migrationBuilder.CreateIndex(
                name: "IX_CarteirasCategoria_CarteiraId",
                schema: "qh",
                table: "CarteirasCategoria",
                column: "CarteiraId");

            migrationBuilder.CreateIndex(
                name: "IX_CarteirasComprador_EmpresaId",
                schema: "qh",
                table: "CarteirasComprador",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CarteirasComprador_EmpresaId_UsuarioId",
                schema: "qh",
                table: "CarteirasComprador",
                columns: new[] { "EmpresaId", "UsuarioId" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CompetenciasDocumentais_ContratoId_Mes",
                schema: "qh",
                table: "CompetenciasDocumentais",
                columns: new[] { "ContratoId", "Mes" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CompetenciasDocumentais_EmpresaId",
                schema: "qh",
                table: "CompetenciasDocumentais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetenciasDocumentais_EmpresaId_ObraId",
                schema: "qh",
                table: "CompetenciasDocumentais",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetenciasDocumentais_FornecedorId_Situacao",
                schema: "qh",
                table: "CompetenciasDocumentais",
                columns: new[] { "FornecedorId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetenciasDocumentais_ObraId",
                schema: "qh",
                table: "CompetenciasDocumentais",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensCompetencia_CompetenciaId_TipoDocumentoExigidoId_ColaboradorId",
                schema: "qh",
                table: "ItensCompetencia",
                columns: new[] { "CompetenciaId", "TipoDocumentoExigidoId", "ColaboradorId" },
                unique: true,
                filter: "[ColaboradorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItensCompetencia_TipoDocumentoExigidoId",
                schema: "qh",
                table: "ItensCompetencia",
                column: "TipoDocumentoExigidoId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesCompra_CompradorUsuarioId_Situacao",
                schema: "qh",
                table: "LotesCompra",
                columns: new[] { "CompradorUsuarioId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_LotesCompra_EmpresaId",
                schema: "qh",
                table: "LotesCompra",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesCompra_EmpresaId_ObraId",
                schema: "qh",
                table: "LotesCompra",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_LotesCompra_ObraId",
                schema: "qh",
                table: "LotesCompra",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesCompra_RequisicaoId_Letra",
                schema: "qh",
                table: "LotesCompra",
                columns: new[] { "RequisicaoId", "Letra" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Metas_EmpresaId",
                schema: "qh",
                table: "Metas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Metas_EmpresaId_Inicio_Fim",
                schema: "qh",
                table: "Metas",
                columns: new[] { "EmpresaId", "Inicio", "Fim" });

            migrationBuilder.CreateIndex(
                name: "IX_MetasApuracao_MetaId_Mes",
                schema: "qh",
                table: "MetasApuracao",
                columns: new[] { "MetaId", "Mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropostasVersao_CotacaoFornecedorId_Versao",
                schema: "qh",
                table: "PropostasVersao",
                columns: new[] { "CotacaoFornecedorId", "Versao" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AusenciasComprador",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "CarteirasCategoria",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ItensCompetencia",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "LotesCompra",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "MetasApuracao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "PropostasVersao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "CarteirasComprador",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "CompetenciasDocumentais",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "Metas",
                schema: "qh");

            migrationBuilder.DropColumn(
                name: "PodeAdiar",
                schema: "qh",
                table: "TiposDocumento");

            migrationBuilder.DropColumn(
                name: "AceitaEm",
                schema: "qh",
                table: "Tarefas");

            migrationBuilder.DropColumn(
                name: "AceitaPor",
                schema: "qh",
                table: "Tarefas");

            migrationBuilder.DropColumn(
                name: "Aceite",
                schema: "qh",
                table: "Tarefas");

            migrationBuilder.DropColumn(
                name: "EquipeId",
                schema: "qh",
                table: "Tarefas");

            migrationBuilder.DropColumn(
                name: "MetaId",
                schema: "qh",
                table: "Tarefas");

            migrationBuilder.DropColumn(
                name: "MotivoDevolucao",
                schema: "qh",
                table: "Tarefas");

            migrationBuilder.DropColumn(
                name: "Emergencial",
                schema: "qh",
                table: "Requisicoes");

            migrationBuilder.DropColumn(
                name: "LoteCompraId",
                schema: "qh",
                table: "RequisicaoItens");

            migrationBuilder.DropColumn(
                name: "MotivoCancelamento",
                schema: "qh",
                table: "RequisicaoItens");

            migrationBuilder.DropColumn(
                name: "QtdCancelada",
                schema: "qh",
                table: "RequisicaoItens");

            migrationBuilder.DropColumn(
                name: "CatalogoVersao",
                schema: "qh",
                table: "PerfisAcesso");

            migrationBuilder.DropColumn(
                name: "SubstituiDocumentoId",
                schema: "qh",
                table: "FornecedorDocumentos");

            migrationBuilder.DropColumn(
                name: "RecebeAvisos",
                schema: "qh",
                table: "FornecedorContatos");

            migrationBuilder.DropColumn(
                name: "LoteCompraId",
                schema: "qh",
                table: "Cotacoes");

            migrationBuilder.DropColumn(
                name: "PrazoResposta",
                schema: "qh",
                table: "Cotacoes");

            migrationBuilder.DropColumn(
                name: "QuantidadeVencida",
                schema: "qh",
                table: "CotacaoItens");

            migrationBuilder.DropColumn(
                name: "Vencedor",
                schema: "qh",
                table: "CotacaoItens");

            migrationBuilder.DropColumn(
                name: "MotivoDeclinio",
                schema: "qh",
                table: "CotacaoFornecedores");

            migrationBuilder.DropColumn(
                name: "PendenciaNaData",
                schema: "qh",
                table: "CotacaoFornecedores");

            migrationBuilder.DropColumn(
                name: "RascunhoEm",
                schema: "qh",
                table: "CotacaoFornecedores");

            migrationBuilder.DropColumn(
                name: "RascunhoJson",
                schema: "qh",
                table: "CotacaoFornecedores");

            migrationBuilder.DropColumn(
                name: "VisualizadoEm",
                schema: "qh",
                table: "CotacaoFornecedores");

            migrationBuilder.DropColumn(
                name: "AguardandoValidacao",
                schema: "qh",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "CadastradoPeloFornecedor",
                schema: "qh",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "ValidadoEm",
                schema: "qh",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "ValidadoPor",
                schema: "qh",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "LeadTimeDias",
                schema: "qh",
                table: "CategoriasProduto");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Groundkey.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class EstoqueEquipamentosRecorrentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DestinoAlmoxarifadoId",
                schema: "qh",
                table: "Transferencias",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrigemAlmoxarifadoId",
                schema: "qh",
                table: "Transferencias",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrigemId",
                schema: "qh",
                table: "Restricoes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AlmoxarifadoId",
                schema: "qh",
                table: "Recebimentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProdutoId",
                schema: "qh",
                table: "RecebimentoItens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CautelaColaboradorId",
                schema: "qh",
                table: "Equipamentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CautelaDesde",
                schema: "qh",
                table: "Equipamentos",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CautelaFornecedorId",
                schema: "qh",
                table: "Equipamentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComQuem",
                schema: "qh",
                table: "Equipamentos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dono",
                schema: "qh",
                table: "Equipamentos",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "FornecedorId",
                schema: "qh",
                table: "Equipamentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrToken",
                schema: "qh",
                table: "Equipamentos",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TipoMedicaoId",
                schema: "qh",
                table: "Equipamentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatasCombinadas",
                schema: "qh",
                table: "Contratos",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiaCorte",
                schema: "qh",
                table: "Contratos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiaVencimento",
                schema: "qh",
                table: "Contratos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ExigeOsPorVisita",
                schema: "qh",
                table: "Contratos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FormaCobranca",
                schema: "qh",
                table: "Contratos",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Periodicidade",
                schema: "qh",
                table: "Contratos",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Reajuste",
                schema: "qh",
                table: "Contratos",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Recorrente",
                schema: "qh",
                table: "Contratos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ToleranciaPerc",
                schema: "qh",
                table: "Contratos",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecoMensal",
                schema: "qh",
                table: "ContratoItens",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Disciplina",
                schema: "qh",
                table: "Composicoes",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Almoxarifados",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Endereco = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("PK_Almoxarifados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Almoxarifados_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalSchema: "qh",
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Almoxarifados_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CautelasFerramenta",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipamentoMedicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Para = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DevolvidaEm = table.Column<DateOnly>(type: "date", nullable: true),
                    EntreguePor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
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
                    table.PrimaryKey("PK_CautelasFerramenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CautelasFerramenta_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalSchema: "qh",
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CautelasFerramenta_Equipamentos_EquipamentoMedicaoId",
                        column: x => x.EquipamentoMedicaoId,
                        principalSchema: "qh",
                        principalTable: "Equipamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CautelasFerramenta_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FamiliasEquipamento",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsaHorimetro = table.Column<bool>(type: "bit", nullable: false),
                    UsaHodometro = table.Column<bool>(type: "bit", nullable: false),
                    CamposExtras = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CriticidadePadrao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_FamiliasEquipamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KitsMaterial",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Etapa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Disciplina = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_KitsMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitsMaterial_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TiposEquipamentoMedicao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Prefixo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PeriodicidadeMeses = table.Column<int>(type: "int", nullable: false),
                    Criterio = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Metodo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    TipoControle = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_TiposEquipamentoMedicao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsosContrato",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
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
                    table.PrimaryKey("PK_UsosContrato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsosContrato_ContratoItens_ContratoItemId",
                        column: x => x.ContratoItemId,
                        principalSchema: "qh",
                        principalTable: "ContratoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsosContrato_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsosContrato_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventariosEstoque",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    AlmoxarifadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AprovadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
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
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventariosEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventariosEstoque_Almoxarifados_AlmoxarifadoId",
                        column: x => x.AlmoxarifadoId,
                        principalSchema: "qh",
                        principalTable: "Almoxarifados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "InventarioEstoque_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "SaldosEstoque",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlmoxarifadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Disponivel = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Bloqueado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CustoMedio = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Minimo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PontoPedido = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Maximo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UltimoMovimento = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_SaldosEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaldosEstoque_Almoxarifados_AlmoxarifadoId",
                        column: x => x.AlmoxarifadoId,
                        principalSchema: "qh",
                        principalTable: "Almoxarifados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaldosEstoque_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalSchema: "qh",
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipamentosPatrimonio",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Patrimonio = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FamiliaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Propriedade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContratoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Criticidade = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Fabricante = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Modelo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    NumeroSerie = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Ano = table.Column<int>(type: "int", nullable: true),
                    ValorAquisicao = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DataAquisicao = table.Column<DateOnly>(type: "date", nullable: true),
                    DiariaInterna = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    AlmoxarifadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Horimetro = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Hodometro = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CamposExtras = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DevolverAte = table.Column<DateOnly>(type: "date", nullable: true),
                    QrToken = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_EquipamentosPatrimonio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipamentosPatrimonio_Almoxarifados_AlmoxarifadoId",
                        column: x => x.AlmoxarifadoId,
                        principalSchema: "qh",
                        principalTable: "Almoxarifados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipamentosPatrimonio_ContratoItens_ContratoItemId",
                        column: x => x.ContratoItemId,
                        principalSchema: "qh",
                        principalTable: "ContratoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipamentosPatrimonio_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipamentosPatrimonio_FamiliasEquipamento_FamiliaId",
                        column: x => x.FamiliaId,
                        principalSchema: "qh",
                        principalTable: "FamiliasEquipamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipamentosPatrimonio_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Equipamento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "KitMaterialItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitMaterialItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitMaterialItens_KitsMaterial_KitId",
                        column: x => x.KitId,
                        principalSchema: "qh",
                        principalTable: "KitsMaterial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KitMaterialItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalSchema: "qh",
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimentosEstoque",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlmoxarifadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Bloqueado = table.Column<bool>(type: "bit", nullable: false),
                    CustoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SaldoApos = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    OrigemTipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    OrigemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    KitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntreguePara = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Lote = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
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
                    table.PrimaryKey("PK_MovimentosEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentosEstoque_Almoxarifados_AlmoxarifadoId",
                        column: x => x.AlmoxarifadoId,
                        principalSchema: "qh",
                        principalTable: "Almoxarifados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosEstoque_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalSchema: "qh",
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosEstoque_Composicoes_ComposicaoId",
                        column: x => x.ComposicaoId,
                        principalSchema: "qh",
                        principalTable: "Composicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosEstoque_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosEstoque_KitsMaterial_KitId",
                        column: x => x.KitId,
                        principalSchema: "qh",
                        principalTable: "KitsMaterial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosEstoque_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosEstoque_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalSchema: "qh",
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventarioItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaldoSistema = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Contado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CustoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventarioItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventarioItens_InventariosEstoque_InventarioId",
                        column: x => x.InventarioId,
                        principalSchema: "qh",
                        principalTable: "InventariosEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventarioItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalSchema: "qh",
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiariosObraEquipamentos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Horas = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Parado = table.Column<bool>(type: "bit", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiariosObraEquipamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiariosObraEquipamentos_DiariosObra_DiarioId",
                        column: x => x.DiarioId,
                        principalSchema: "qh",
                        principalTable: "DiariosObra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiariosObraEquipamentos_EquipamentosPatrimonio_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalSchema: "qh",
                        principalTable: "EquipamentosPatrimonio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipamentoDocumentos",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "date", nullable: true),
                    Bloqueia = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipamentoDocumentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipamentoDocumentos_EquipamentosPatrimonio_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalSchema: "qh",
                        principalTable: "EquipamentosPatrimonio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimentosEquipamento",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DeAlmoxarifadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParaAlmoxarifadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Checklist = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_MovimentosEquipamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentosEquipamento_Almoxarifados_DeAlmoxarifadoId",
                        column: x => x.DeAlmoxarifadoId,
                        principalSchema: "qh",
                        principalTable: "Almoxarifados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosEquipamento_Almoxarifados_ParaAlmoxarifadoId",
                        column: x => x.ParaAlmoxarifadoId,
                        principalSchema: "qh",
                        principalTable: "Almoxarifados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosEquipamento_EquipamentosPatrimonio_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalSchema: "qh",
                        principalTable: "EquipamentosPatrimonio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanosManutencao",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Gatilho = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IntervaloDias = table.Column<int>(type: "int", nullable: true),
                    IntervaloUso = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    UltimaData = table.Column<DateOnly>(type: "date", nullable: true),
                    UltimaLeitura = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Executante = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Checklist = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DiasAntecedencia = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PlanosManutencao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanosManutencao_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanosManutencao_EquipamentosPatrimonio_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalSchema: "qh",
                        principalTable: "EquipamentosPatrimonio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdensServico",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    EquipamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AbertaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AbertaPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ParadoDesde = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ParadoAte = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModoFalha = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Causa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CustoMaoObra = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CustoPecas = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CustoTerceiros = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ChecklistResultado = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Leitura = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ExecutadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ConcluidaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VerificadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    VerificadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RequisicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_OrdensServico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdensServico_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdensServico_EquipamentosPatrimonio_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalSchema: "qh",
                        principalTable: "EquipamentosPatrimonio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdensServico_PlanosManutencao_PlanoId",
                        column: x => x.PlanoId,
                        principalSchema: "qh",
                        principalTable: "PlanosManutencao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdensServico_Requisicoes_RequisicaoId",
                        column: x => x.RequisicaoId,
                        principalSchema: "qh",
                        principalTable: "Requisicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrdemServico_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ParcelasContrato",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Fim = table.Column<DateOnly>(type: "date", nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorPrevisto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Composicao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotaNumero = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    NotaValor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NotaFiscalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NotaOrigem = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Justificativa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrdemServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConferidoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ConferidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AprovadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AprovadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LiberadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LiberadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PagoEm = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_ParcelasContrato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParcelasContrato_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalSchema: "qh",
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParcelasContrato_NotasFiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalSchema: "qh",
                        principalTable: "NotasFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParcelasContrato_Obras_ObraId",
                        column: x => x.ObraId,
                        principalSchema: "qh",
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParcelasContrato_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalSchema: "qh",
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ParcelaContrato_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_DestinoAlmoxarifadoId",
                schema: "qh",
                table: "Transferencias",
                column: "DestinoAlmoxarifadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_OrigemAlmoxarifadoId",
                schema: "qh",
                table: "Transferencias",
                column: "OrigemAlmoxarifadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_AlmoxarifadoId",
                schema: "qh",
                table: "Recebimentos",
                column: "AlmoxarifadoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecebimentoItens_ProdutoId",
                schema: "qh",
                table: "RecebimentoItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamentos_CautelaColaboradorId",
                schema: "qh",
                table: "Equipamentos",
                column: "CautelaColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamentos_CautelaFornecedorId",
                schema: "qh",
                table: "Equipamentos",
                column: "CautelaFornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamentos_FornecedorId",
                schema: "qh",
                table: "Equipamentos",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamentos_QrToken",
                schema: "qh",
                table: "Equipamentos",
                column: "QrToken");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamentos_TipoMedicaoId",
                schema: "qh",
                table: "Equipamentos",
                column: "TipoMedicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Almoxarifados_EmpresaId",
                schema: "qh",
                table: "Almoxarifados",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Almoxarifados_EmpresaId_Codigo",
                schema: "qh",
                table: "Almoxarifados",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Almoxarifados_EstabelecimentoId",
                schema: "qh",
                table: "Almoxarifados",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Almoxarifados_ObraId",
                schema: "qh",
                table: "Almoxarifados",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "UX_Almoxarifados_Obra",
                schema: "qh",
                table: "Almoxarifados",
                columns: new[] { "EmpresaId", "ObraId" },
                unique: true,
                filter: "[Excluido] = 0 AND [ObraId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CautelasFerramenta_ColaboradorId",
                schema: "qh",
                table: "CautelasFerramenta",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_CautelasFerramenta_EmpresaId",
                schema: "qh",
                table: "CautelasFerramenta",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CautelasFerramenta_EquipamentoMedicaoId_Data",
                schema: "qh",
                table: "CautelasFerramenta",
                columns: new[] { "EquipamentoMedicaoId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_CautelasFerramenta_FornecedorId",
                schema: "qh",
                table: "CautelasFerramenta",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_DiariosObraEquipamentos_DiarioId",
                schema: "qh",
                table: "DiariosObraEquipamentos",
                column: "DiarioId");

            migrationBuilder.CreateIndex(
                name: "IX_DiariosObraEquipamentos_EquipamentoId",
                schema: "qh",
                table: "DiariosObraEquipamentos",
                column: "EquipamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipamentoDocumentos_EquipamentoId",
                schema: "qh",
                table: "EquipamentoDocumentos",
                column: "EquipamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipamentosPatrimonio_AlmoxarifadoId",
                schema: "qh",
                table: "EquipamentosPatrimonio",
                column: "AlmoxarifadoId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipamentosPatrimonio_ContratoId",
                schema: "qh",
                table: "EquipamentosPatrimonio",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipamentosPatrimonio_ContratoItemId",
                schema: "qh",
                table: "EquipamentosPatrimonio",
                column: "ContratoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipamentosPatrimonio_EmpresaId",
                schema: "qh",
                table: "EquipamentosPatrimonio",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipamentosPatrimonio_EmpresaId_Patrimonio",
                schema: "qh",
                table: "EquipamentosPatrimonio",
                columns: new[] { "EmpresaId", "Patrimonio" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EquipamentosPatrimonio_FamiliaId",
                schema: "qh",
                table: "EquipamentosPatrimonio",
                column: "FamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipamentosPatrimonio_FornecedorId",
                schema: "qh",
                table: "EquipamentosPatrimonio",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipamentosPatrimonio_QrToken",
                schema: "qh",
                table: "EquipamentosPatrimonio",
                column: "QrToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamiliasEquipamento_EmpresaId",
                schema: "qh",
                table: "FamiliasEquipamento",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_FamiliasEquipamento_EmpresaId_Nome",
                schema: "qh",
                table: "FamiliasEquipamento",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventarioItens_InventarioId",
                schema: "qh",
                table: "InventarioItens",
                column: "InventarioId");

            migrationBuilder.CreateIndex(
                name: "IX_InventarioItens_ProdutoId",
                schema: "qh",
                table: "InventarioItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosEstoque_AlmoxarifadoId",
                schema: "qh",
                table: "InventariosEstoque",
                column: "AlmoxarifadoId");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosEstoque_EmpresaId",
                schema: "qh",
                table: "InventariosEstoque",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosEstoque_EmpresaId_Numero",
                schema: "qh",
                table: "InventariosEstoque",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_KitMaterialItens_KitId",
                schema: "qh",
                table: "KitMaterialItens",
                column: "KitId");

            migrationBuilder.CreateIndex(
                name: "IX_KitMaterialItens_ProdutoId",
                schema: "qh",
                table: "KitMaterialItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_KitsMaterial_EmpresaId",
                schema: "qh",
                table: "KitsMaterial",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_KitsMaterial_EmpresaId_Codigo",
                schema: "qh",
                table: "KitsMaterial",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_KitsMaterial_ObraId",
                schema: "qh",
                table: "KitsMaterial",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEquipamento_DeAlmoxarifadoId",
                schema: "qh",
                table: "MovimentosEquipamento",
                column: "DeAlmoxarifadoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEquipamento_EmpresaId",
                schema: "qh",
                table: "MovimentosEquipamento",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEquipamento_EquipamentoId_Data",
                schema: "qh",
                table: "MovimentosEquipamento",
                columns: new[] { "EquipamentoId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEquipamento_ParaAlmoxarifadoId",
                schema: "qh",
                table: "MovimentosEquipamento",
                column: "ParaAlmoxarifadoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_AlmoxarifadoId_ProdutoId_Data",
                schema: "qh",
                table: "MovimentosEstoque",
                columns: new[] { "AlmoxarifadoId", "ProdutoId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_ColaboradorId",
                schema: "qh",
                table: "MovimentosEstoque",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_ComposicaoId",
                schema: "qh",
                table: "MovimentosEstoque",
                column: "ComposicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_EmpresaId",
                schema: "qh",
                table: "MovimentosEstoque",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_FornecedorId",
                schema: "qh",
                table: "MovimentosEstoque",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_KitId",
                schema: "qh",
                table: "MovimentosEstoque",
                column: "KitId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_LocalId",
                schema: "qh",
                table: "MovimentosEstoque",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_OrigemTipo_OrigemId",
                schema: "qh",
                table: "MovimentosEstoque",
                columns: new[] { "OrigemTipo", "OrigemId" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_ProdutoId",
                schema: "qh",
                table: "MovimentosEstoque",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_ContratoId",
                schema: "qh",
                table: "OrdensServico",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_EmpresaId",
                schema: "qh",
                table: "OrdensServico",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_EmpresaId_Numero",
                schema: "qh",
                table: "OrdensServico",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_EquipamentoId_Status",
                schema: "qh",
                table: "OrdensServico",
                columns: new[] { "EquipamentoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_PlanoId",
                schema: "qh",
                table: "OrdensServico",
                column: "PlanoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_RequisicaoId",
                schema: "qh",
                table: "OrdensServico",
                column: "RequisicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelasContrato_ContratoId_Numero",
                schema: "qh",
                table: "ParcelasContrato",
                columns: new[] { "ContratoId", "Numero" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelasContrato_EmpresaId",
                schema: "qh",
                table: "ParcelasContrato",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelasContrato_EmpresaId_ObraId",
                schema: "qh",
                table: "ParcelasContrato",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_ParcelasContrato_NotaFiscalId",
                schema: "qh",
                table: "ParcelasContrato",
                column: "NotaFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelasContrato_ObraId",
                schema: "qh",
                table: "ParcelasContrato",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelasContrato_OrdemServicoId",
                schema: "qh",
                table: "ParcelasContrato",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosManutencao_ContratoId",
                schema: "qh",
                table: "PlanosManutencao",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosManutencao_EmpresaId",
                schema: "qh",
                table: "PlanosManutencao",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosManutencao_EquipamentoId",
                schema: "qh",
                table: "PlanosManutencao",
                column: "EquipamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_SaldosEstoque_AlmoxarifadoId_ProdutoId",
                schema: "qh",
                table: "SaldosEstoque",
                columns: new[] { "AlmoxarifadoId", "ProdutoId" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SaldosEstoque_EmpresaId",
                schema: "qh",
                table: "SaldosEstoque",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_SaldosEstoque_ProdutoId",
                schema: "qh",
                table: "SaldosEstoque",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposEquipamentoMedicao_EmpresaId",
                schema: "qh",
                table: "TiposEquipamentoMedicao",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposEquipamentoMedicao_EmpresaId_Nome",
                schema: "qh",
                table: "TiposEquipamentoMedicao",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UsosContrato_ContratoId_Data",
                schema: "qh",
                table: "UsosContrato",
                columns: new[] { "ContratoId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_UsosContrato_ContratoItemId",
                schema: "qh",
                table: "UsosContrato",
                column: "ContratoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UsosContrato_EmpresaId",
                schema: "qh",
                table: "UsosContrato",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsosContrato_EmpresaId_ObraId",
                schema: "qh",
                table: "UsosContrato",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_UsosContrato_ObraId",
                schema: "qh",
                table: "UsosContrato",
                column: "ObraId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipamentos_Colaboradores_CautelaColaboradorId",
                schema: "qh",
                table: "Equipamentos",
                column: "CautelaColaboradorId",
                principalSchema: "qh",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipamentos_Fornecedores_CautelaFornecedorId",
                schema: "qh",
                table: "Equipamentos",
                column: "CautelaFornecedorId",
                principalSchema: "qh",
                principalTable: "Fornecedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipamentos_Fornecedores_FornecedorId",
                schema: "qh",
                table: "Equipamentos",
                column: "FornecedorId",
                principalSchema: "qh",
                principalTable: "Fornecedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipamentos_TiposEquipamentoMedicao_TipoMedicaoId",
                schema: "qh",
                table: "Equipamentos",
                column: "TipoMedicaoId",
                principalSchema: "qh",
                principalTable: "TiposEquipamentoMedicao",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecebimentoItens_Produtos_ProdutoId",
                schema: "qh",
                table: "RecebimentoItens",
                column: "ProdutoId",
                principalSchema: "qh",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recebimentos_Almoxarifados_AlmoxarifadoId",
                schema: "qh",
                table: "Recebimentos",
                column: "AlmoxarifadoId",
                principalSchema: "qh",
                principalTable: "Almoxarifados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transferencias_Almoxarifados_DestinoAlmoxarifadoId",
                schema: "qh",
                table: "Transferencias",
                column: "DestinoAlmoxarifadoId",
                principalSchema: "qh",
                principalTable: "Almoxarifados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transferencias_Almoxarifados_OrigemAlmoxarifadoId",
                schema: "qh",
                table: "Transferencias",
                column: "OrigemAlmoxarifadoId",
                principalSchema: "qh",
                principalTable: "Almoxarifados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Equipamentos de medição já cadastrados ganham o token do QR da etiqueta.
            migrationBuilder.Sql("UPDATE qh.Equipamentos SET QrToken = LOWER(CONVERT(varchar(20), CRYPT_GEN_RANDOM(10), 2)) WHERE QrToken = N'';");
            migrationBuilder.Sql("UPDATE qh.Equipamentos SET ComQuem = Responsavel WHERE ComQuem IS NULL AND Responsavel IS NOT NULL;");

            // Telas novas de Suprimentos: perfis de acesso, exceções e bloqueios já gravados passam a valer igual à tela de origem
            // (estoque, kits e equipamentos seguem "Recebimento"; parcelas seguem "Recebimento", com Aprovar de quem aprova pagamentos).
            migrationBuilder.Sql("""
                UPDATE p SET Regras = p.Regras
                    + ISNULL(N';estoque=' + rc.v, N'') + ISNULL(N';estoque/kits=' + rc.v, N'') + ISNULL(N';equipamentos=' + rc.v, N'')
                    + ISNULL(N';contratos/parcelas=' + CASE WHEN pg.v LIKE N'%A%' THEN N'VIAX' ELSE rc.v END, N'')
                FROM qh.PerfisAcesso p
                OUTER APPLY (SELECT TOP (1) SUBSTRING(LTRIM(s.value), 14, 20) AS v FROM STRING_SPLIT(p.Regras, ';') s WHERE LTRIM(s.value) LIKE N'recebimentos=%') rc
                OUTER APPLY (SELECT TOP (1) SUBSTRING(LTRIM(s.value), 12, 20) AS v FROM STRING_SPLIT(p.Regras, ';') s WHERE LTRIM(s.value) LIKE N'pagamentos=%') pg
                WHERE p.Regras NOT LIKE N'%estoque=%' AND (rc.v IS NOT NULL OR pg.v IS NOT NULL);
                """);
            migrationBuilder.Sql("""
                UPDATE u SET ExcecoesAcesso = u.ExcecoesAcesso + N';estoque=' + x.v + N';estoque/kits=' + x.v + N';equipamentos=' + x.v + N';contratos/parcelas=' + x.v
                FROM qh.Usuarios u
                CROSS APPLY (SELECT TOP (1) SUBSTRING(LTRIM(s.value), 14, 20) AS v FROM STRING_SPLIT(u.ExcecoesAcesso, ';') s WHERE LTRIM(s.value) LIKE N'recebimentos=%') x
                WHERE u.ExcecoesAcesso NOT LIKE N'%estoque=%' AND LEN(u.ExcecoesAcesso) + 4 * LEN(x.v) + 80 <= 2000;
                """);
            migrationBuilder.Sql("""
                UPDATE qh.Empresas SET TelasBloqueadas = TelasBloqueadas + N';estoque;estoque/kits;equipamentos'
                WHERE N';' + TelasBloqueadas + N';' LIKE N'%;recebimentos;%' AND TelasBloqueadas NOT LIKE N'%estoque%' AND LEN(TelasBloqueadas) + 60 <= 2000;
                UPDATE qh.Empresas SET TelasBloqueadas = TelasBloqueadas + N';contratos/parcelas'
                WHERE N';' + TelasBloqueadas + N';' LIKE N'%;contratos;%' AND TelasBloqueadas NOT LIKE N'%contratos/parcelas%' AND LEN(TelasBloqueadas) + 30 <= 2000;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipamentos_Colaboradores_CautelaColaboradorId",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipamentos_Fornecedores_CautelaFornecedorId",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipamentos_Fornecedores_FornecedorId",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipamentos_TiposEquipamentoMedicao_TipoMedicaoId",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_RecebimentoItens_Produtos_ProdutoId",
                schema: "qh",
                table: "RecebimentoItens");

            migrationBuilder.DropForeignKey(
                name: "FK_Recebimentos_Almoxarifados_AlmoxarifadoId",
                schema: "qh",
                table: "Recebimentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Transferencias_Almoxarifados_DestinoAlmoxarifadoId",
                schema: "qh",
                table: "Transferencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Transferencias_Almoxarifados_OrigemAlmoxarifadoId",
                schema: "qh",
                table: "Transferencias");

            migrationBuilder.DropTable(
                name: "CautelasFerramenta",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "DiariosObraEquipamentos",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "EquipamentoDocumentos",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "InventarioItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "KitMaterialItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "MovimentosEquipamento",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "MovimentosEstoque",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "ParcelasContrato",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ParcelaContrato_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "SaldosEstoque",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "TiposEquipamentoMedicao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "UsosContrato",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "InventariosEstoque",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "InventarioEstoque_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "KitsMaterial",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "OrdensServico",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrdemServico_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "PlanosManutencao",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "EquipamentosPatrimonio",
                schema: "qh")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "Equipamento_Historico")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "qh_hist")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Almoxarifados",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "FamiliasEquipamento",
                schema: "qh");

            migrationBuilder.DropIndex(
                name: "IX_Transferencias_DestinoAlmoxarifadoId",
                schema: "qh",
                table: "Transferencias");

            migrationBuilder.DropIndex(
                name: "IX_Transferencias_OrigemAlmoxarifadoId",
                schema: "qh",
                table: "Transferencias");

            migrationBuilder.DropIndex(
                name: "IX_Recebimentos_AlmoxarifadoId",
                schema: "qh",
                table: "Recebimentos");

            migrationBuilder.DropIndex(
                name: "IX_RecebimentoItens_ProdutoId",
                schema: "qh",
                table: "RecebimentoItens");

            migrationBuilder.DropIndex(
                name: "IX_Equipamentos_CautelaColaboradorId",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropIndex(
                name: "IX_Equipamentos_CautelaFornecedorId",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropIndex(
                name: "IX_Equipamentos_FornecedorId",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropIndex(
                name: "IX_Equipamentos_QrToken",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropIndex(
                name: "IX_Equipamentos_TipoMedicaoId",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "DestinoAlmoxarifadoId",
                schema: "qh",
                table: "Transferencias");

            migrationBuilder.DropColumn(
                name: "OrigemAlmoxarifadoId",
                schema: "qh",
                table: "Transferencias");

            migrationBuilder.DropColumn(
                name: "OrigemId",
                schema: "qh",
                table: "Restricoes");

            migrationBuilder.DropColumn(
                name: "AlmoxarifadoId",
                schema: "qh",
                table: "Recebimentos");

            migrationBuilder.DropColumn(
                name: "ProdutoId",
                schema: "qh",
                table: "RecebimentoItens");

            migrationBuilder.DropColumn(
                name: "CautelaColaboradorId",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "CautelaDesde",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "CautelaFornecedorId",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "ComQuem",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "Dono",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "FornecedorId",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "QrToken",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "TipoMedicaoId",
                schema: "qh",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "DatasCombinadas",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "DiaCorte",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "DiaVencimento",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "ExigeOsPorVisita",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "FormaCobranca",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "Periodicidade",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "Reajuste",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "Recorrente",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "ToleranciaPerc",
                schema: "qh",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "PrecoMensal",
                schema: "qh",
                table: "ContratoItens");

            migrationBuilder.DropColumn(
                name: "Disciplina",
                schema: "qh",
                table: "Composicoes");
        }
    }
}

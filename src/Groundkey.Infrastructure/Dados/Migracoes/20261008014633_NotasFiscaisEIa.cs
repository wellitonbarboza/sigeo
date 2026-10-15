using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Groundkey.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class NotasFiscaisEIa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnaliseIa",
                schema: "qh",
                table: "Cotacoes",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnaliseIaEm",
                schema: "qh",
                table: "Cotacoes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportacoesNota",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Cnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    De = table.Column<DateOnly>(type: "date", nullable: true),
                    Ate = table.Column<DateOnly>(type: "date", nullable: true),
                    Lidas = table.Column<int>(type: "int", nullable: false),
                    Novas = table.Column<int>(type: "int", nullable: false),
                    Canceladas = table.Column<int>(type: "int", nullable: false),
                    Detalhe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Falhou = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ImportacoesNota", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotasFiscais",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Chave = table.Column<string>(type: "nchar(44)", fixedLength: true, maxLength: 44, nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Serie = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Emissao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EmitenteCnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    EmitenteNome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DestinatarioCnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    DestinatarioNome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorFrete = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    InformacoesComplementares = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PedidosCitados = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ArquivoXml = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecebimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VinculoOrigem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    VinculoDetalhe = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AnaliseIa = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
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
                    table.PrimaryKey("PK_NotasFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalSchema: "qh",
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_Recebimentos_RecebimentoId",
                        column: x => x.RecebimentoId,
                        principalSchema: "qh",
                        principalTable: "Recebimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsosIa",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Funcao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TokensEntrada = table.Column<int>(type: "int", nullable: false),
                    TokensSaida = table.Column<int>(type: "int", nullable: false),
                    Paginas = table.Column<int>(type: "int", nullable: false),
                    Falhou = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_UsosIa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotaFiscalItens",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotaFiscalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Item = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ncm = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Cfop = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Unidade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PedidoCitado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ItemPedidoCitado = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PedidoItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VinculoOrigem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Confianca = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaFiscalItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaFiscalItens_NotasFiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalSchema: "qh",
                        principalTable: "NotasFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotaFiscalItens_PedidoItens_PedidoItemId",
                        column: x => x.PedidoItemId,
                        principalSchema: "qh",
                        principalTable: "PedidoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportacoesNota_EmpresaId",
                schema: "qh",
                table: "ImportacoesNota",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportacoesNota_EmpresaId_CriadoEm",
                schema: "qh",
                table: "ImportacoesNota",
                columns: new[] { "EmpresaId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalItens_NotaFiscalId",
                schema: "qh",
                table: "NotaFiscalItens",
                column: "NotaFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalItens_PedidoItemId",
                schema: "qh",
                table: "NotaFiscalItens",
                column: "PedidoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_EmpresaId",
                schema: "qh",
                table: "NotasFiscais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_EmpresaId_Chave",
                schema: "qh",
                table: "NotasFiscais",
                columns: new[] { "EmpresaId", "Chave" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_EmpresaId_Situacao_Emissao",
                schema: "qh",
                table: "NotasFiscais",
                columns: new[] { "EmpresaId", "Situacao", "Emissao" });

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_PedidoId",
                schema: "qh",
                table: "NotasFiscais",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_RecebimentoId",
                schema: "qh",
                table: "NotasFiscais",
                column: "RecebimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsosIa_EmpresaId",
                schema: "qh",
                table: "UsosIa",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsosIa_EmpresaId_CriadoEm",
                schema: "qh",
                table: "UsosIa",
                columns: new[] { "EmpresaId", "CriadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportacoesNota",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "NotaFiscalItens",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "UsosIa",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "NotasFiscais",
                schema: "qh");

            migrationBuilder.DropColumn(
                name: "AnaliseIa",
                schema: "qh",
                table: "Cotacoes");

            migrationBuilder.DropColumn(
                name: "AnaliseIaEm",
                schema: "qh",
                table: "Cotacoes");
        }
    }
}

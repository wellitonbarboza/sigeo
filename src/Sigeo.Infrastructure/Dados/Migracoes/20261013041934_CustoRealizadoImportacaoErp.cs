using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigeo.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class CustoRealizadoImportacaoErp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoCusto",
                schema: "qh",
                table: "Produtos",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataCorteSistema",
                schema: "qh",
                table: "Obras",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustosRealizados",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    TipoCusto = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Fase = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LocalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FornecedorNome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DocumentoTipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    DocumentoNumero = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    PedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecebimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MedicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Origem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ChaveOrigem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Historico = table.Column<bool>(type: "bit", nullable: false),
                    LoteImportacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_CustosRealizados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustosRealizados_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustosRealizados_Locais_LocalId",
                        column: x => x.LocalId,
                        principalSchema: "qh",
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustosRealizados_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalSchema: "qh",
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustosRealizados_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalSchema: "qh",
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VinculosOrigem",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Sistema = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ObraId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Chave = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TextoOrigem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DestinoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinoTexto = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Ignorar = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_VinculosOrigem", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustosRealizados_EmpresaId",
                schema: "qh",
                table: "CustosRealizados",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosRealizados_EmpresaId_ObraId",
                schema: "qh",
                table: "CustosRealizados",
                columns: new[] { "EmpresaId", "ObraId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustosRealizados_FornecedorId",
                schema: "qh",
                table: "CustosRealizados",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosRealizados_LocalId",
                schema: "qh",
                table: "CustosRealizados",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosRealizados_MedicaoId",
                schema: "qh",
                table: "CustosRealizados",
                column: "MedicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosRealizados_ObraId_ChaveOrigem",
                schema: "qh",
                table: "CustosRealizados",
                columns: new[] { "ObraId", "ChaveOrigem" },
                unique: true,
                filter: "[Excluido] = 0 AND [ChaveOrigem] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustosRealizados_ObraId_Data",
                schema: "qh",
                table: "CustosRealizados",
                columns: new[] { "ObraId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_CustosRealizados_ObraId_Fase",
                schema: "qh",
                table: "CustosRealizados",
                columns: new[] { "ObraId", "Fase" });

            migrationBuilder.CreateIndex(
                name: "IX_CustosRealizados_PedidoId",
                schema: "qh",
                table: "CustosRealizados",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosRealizados_ProdutoId",
                schema: "qh",
                table: "CustosRealizados",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosRealizados_RecebimentoId",
                schema: "qh",
                table: "CustosRealizados",
                column: "RecebimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosOrigem_EmpresaId",
                schema: "qh",
                table: "VinculosOrigem",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosOrigem_EmpresaId_Sistema_Tipo_ObraId_Chave",
                schema: "qh",
                table: "VinculosOrigem",
                columns: new[] { "EmpresaId", "Sistema", "Tipo", "ObraId", "Chave" },
                unique: true,
                filter: "[Excluido] = 0");

            // Unidades: grafias equivalentes viram a padrão (mesma regra de UnidadesPadrao no cadastro e nas importações).
            migrationBuilder.Sql("""
                UPDATE p SET Unidade = m.Padrao
                FROM qh.Produtos p
                JOIN (VALUES (N'UND',N'UN'),(N'UNID',N'UN'),(N'UNI',N'UN'),(N'UNIDADE',N'UN'),(N'U',N'UN'),(N'PEC',N'PC'),(N'PECA',N'PC'),(N'PEÇA',N'PC'),
                             (N'PÇ',N'PC'),(N'PCS',N'PC'),(N'MT',N'M'),(N'MTS',N'M'),(N'METRO',N'M'),(N'M²',N'M2'),(N'MT2',N'M2'),(N'M³',N'M3'),(N'MT3',N'M3'),
                             (N'LT',N'L'),(N'LTS',N'L'),(N'LITRO',N'L'),(N'KGS',N'KG'),(N'CXA',N'CX'),(N'CAIXA',N'CX'),(N'ROLO',N'RL'),(N'SACO',N'SC'),
                             (N'GALAO',N'GL'),(N'BARRA',N'BR'),(N'MÊS',N'MES'),(N'HR',N'H'),(N'HORA',N'H'),(N'PARES',N'PAR'),(N'PR',N'PAR')) m(Origem, Padrao)
                  ON UPPER(LTRIM(RTRIM(p.Unidade))) = m.Origem COLLATE Latin1_General_CI_AS
                WHERE p.Unidade <> m.Padrao;
                UPDATE qh.Produtos SET Unidade = UPPER(LTRIM(RTRIM(Unidade))) WHERE Unidade <> UPPER(LTRIM(RTRIM(Unidade))) COLLATE Latin1_General_CS_AS;
                """);

            // Telas novas (Custo Realizado e Importar Histórico): perfis, exceções e bloqueios seguem a Mão de Obra Própria.
            foreach (var rota in new[] { "engenharia/custo-realizado", "engenharia/importar-erp" })
                migrationBuilder.Sql($"""
                    UPDATE p SET Regras = p.Regras + N';{rota}=' + x.v
                    FROM qh.PerfisAcesso p
                    CROSS APPLY (SELECT TOP (1) SUBSTRING(LTRIM(s.value), CHARINDEX(N'=', LTRIM(s.value)) + 1, 20) AS v FROM STRING_SPLIT(p.Regras, ';') s WHERE LTRIM(s.value) LIKE N'engenharia/mao-de-obra=%') x
                    WHERE p.Regras NOT LIKE N'%{rota}=%';
                    UPDATE u SET ExcecoesAcesso = u.ExcecoesAcesso + N';{rota}=' + x.v
                    FROM qh.Usuarios u
                    CROSS APPLY (SELECT TOP (1) SUBSTRING(LTRIM(s.value), CHARINDEX(N'=', LTRIM(s.value)) + 1, 20) AS v FROM STRING_SPLIT(u.ExcecoesAcesso, ';') s WHERE LTRIM(s.value) LIKE N'engenharia/mao-de-obra=%') x
                    WHERE u.ExcecoesAcesso NOT LIKE N'%{rota}=%' AND LEN(u.ExcecoesAcesso) + LEN(x.v) + 30 <= 2000;
                    UPDATE qh.Empresas SET TelasBloqueadas = TelasBloqueadas + N';{rota}'
                    WHERE N';' + TelasBloqueadas + N';' LIKE N'%;engenharia/mao-de-obra;%' AND TelasBloqueadas NOT LIKE N'%{rota}%' AND LEN(TelasBloqueadas) + 30 <= 2000;
                    """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustosRealizados",
                schema: "qh");

            migrationBuilder.DropTable(
                name: "VinculosOrigem",
                schema: "qh");

            migrationBuilder.DropColumn(
                name: "TipoCusto",
                schema: "qh",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "DataCorteSistema",
                schema: "qh",
                table: "Obras");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Groundkey.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class MarcasExclusaoAcessoObras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Marca",
                schema: "qh",
                table: "RecebimentoItens",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                schema: "qh",
                table: "PedidoItens",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                schema: "qh",
                table: "CotacaoItens",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Marcas",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Fabricante = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Marcas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_EmpresaId",
                schema: "qh",
                table: "Marcas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_EmpresaId_Nome",
                schema: "qh",
                table: "Marcas",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true,
                filter: "[Excluido] = 0");

            // Marcas já digitadas nos produtos e requisições viram o cadastro inicial (uma por grafia, por construtora).
            migrationBuilder.Sql("""
                INSERT INTO qh.Marcas (Id, Nome, Fabricante, Observacao, Ativa, EmpresaId, CriadoEm, CriadoPor, Excluido)
                SELECT NEWID(), x.Nome, NULL, N'Importada dos produtos/requisições', 1, x.EmpresaId, SYSDATETIMEOFFSET(), N'Sistema', 0
                FROM (
                    SELECT EmpresaId, LEFT(LTRIM(RTRIM(Marca)), 120) AS Nome FROM qh.Produtos WHERE Excluido = 0 AND Marca IS NOT NULL AND LTRIM(RTRIM(Marca)) <> N''
                    UNION
                    SELECT r.EmpresaId, LEFT(LTRIM(RTRIM(i.Marca)), 120) FROM qh.RequisicaoItens i JOIN qh.Requisicoes r ON r.Id = i.RequisicaoId
                    WHERE i.Marca IS NOT NULL AND LTRIM(RTRIM(i.Marca)) <> N''
                ) x
                GROUP BY x.EmpresaId, x.Nome;
                """);

            // Acesso por obra: só Administrador e Diretoria veem todas as obras sem marcar. Qualidade sem obra marcada
            // mantém o que tinha (todas), para ninguém perder acesso com a mudança.
            migrationBuilder.Sql("""
                UPDATE u SET AcessoTodasObras = 1 FROM qh.Usuarios u
                WHERE u.Perfil = N'Qualidade' AND u.AcessoTodasObras = 0 AND NOT EXISTS (SELECT 1 FROM qh.UsuariosObras o WHERE o.UsuarioId = u.Id);
                """);

            // Tela nova "Marcas": perfis de acesso, exceções e bloqueios seguem a tela de Produtos.
            migrationBuilder.Sql("""
                UPDATE p SET Regras = p.Regras + N';cadastros/marcas=' + x.v
                FROM qh.PerfisAcesso p
                CROSS APPLY (SELECT TOP (1) SUBSTRING(LTRIM(s.value), 20, 20) AS v FROM STRING_SPLIT(p.Regras, ';') s WHERE LTRIM(s.value) LIKE N'cadastros/produtos=%') x
                WHERE p.Regras NOT LIKE N'%cadastros/marcas=%';
                UPDATE u SET ExcecoesAcesso = u.ExcecoesAcesso + N';cadastros/marcas=' + x.v
                FROM qh.Usuarios u
                CROSS APPLY (SELECT TOP (1) SUBSTRING(LTRIM(s.value), 20, 20) AS v FROM STRING_SPLIT(u.ExcecoesAcesso, ';') s WHERE LTRIM(s.value) LIKE N'cadastros/produtos=%') x
                WHERE u.ExcecoesAcesso NOT LIKE N'%cadastros/marcas=%' AND LEN(u.ExcecoesAcesso) + LEN(x.v) + 20 <= 2000;
                UPDATE qh.Empresas SET TelasBloqueadas = TelasBloqueadas + N';cadastros/marcas'
                WHERE N';' + TelasBloqueadas + N';' LIKE N'%;cadastros/produtos;%' AND TelasBloqueadas NOT LIKE N'%cadastros/marcas%' AND LEN(TelasBloqueadas) + 20 <= 2000;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Marcas",
                schema: "qh");

            migrationBuilder.DropColumn(
                name: "Marca",
                schema: "qh",
                table: "RecebimentoItens");

            migrationBuilder.DropColumn(
                name: "Marca",
                schema: "qh",
                table: "PedidoItens");

            migrationBuilder.DropColumn(
                name: "Marca",
                schema: "qh",
                table: "CotacaoItens");
        }
    }
}

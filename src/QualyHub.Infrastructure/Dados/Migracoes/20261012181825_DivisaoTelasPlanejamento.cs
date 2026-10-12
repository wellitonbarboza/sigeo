using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualyHub.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class DivisaoTelasPlanejamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "Planejamento" virou três telas: Indicadores (engenharia/planejamento), Cronograma e Reprogramação
            // (engenharia/cronograma) e Last Planner (engenharia/last-planner). Perfis de acesso, exceções por usuário e
            // telas bloqueadas pela plataforma que já existem passam a valer igual para as duas telas novas.
            migrationBuilder.Sql("""
                UPDATE p SET Regras = p.Regras + N';engenharia/cronograma=' + x.v + N';engenharia/last-planner=' + x.v
                FROM qh.PerfisAcesso p
                CROSS APPLY (SELECT TOP (1) SUBSTRING(LTRIM(s.value), 25, 50) AS v FROM STRING_SPLIT(p.Regras, ';') s
                             WHERE LTRIM(s.value) LIKE N'engenharia/planejamento=%') x
                WHERE p.Regras NOT LIKE N'%engenharia/cronograma=%';
                """);
            migrationBuilder.Sql("""
                UPDATE u SET ExcecoesAcesso = u.ExcecoesAcesso + N';engenharia/cronograma=' + x.v + N';engenharia/last-planner=' + x.v
                FROM qh.Usuarios u
                CROSS APPLY (SELECT TOP (1) SUBSTRING(LTRIM(s.value), 25, 50) AS v FROM STRING_SPLIT(u.ExcecoesAcesso, ';') s
                             WHERE LTRIM(s.value) LIKE N'engenharia/planejamento=%') x
                WHERE u.ExcecoesAcesso NOT LIKE N'%engenharia/cronograma=%'
                  AND LEN(u.ExcecoesAcesso) + 2 * LEN(x.v) + 50 <= 2000;
                """);
            migrationBuilder.Sql("""
                UPDATE qh.Empresas SET TelasBloqueadas = TelasBloqueadas + N';engenharia/cronograma;engenharia/last-planner'
                WHERE N';' + TelasBloqueadas + N';' LIKE N'%;engenharia/planejamento;%'
                  AND TelasBloqueadas NOT LIKE N'%engenharia/cronograma%'
                  AND LEN(TelasBloqueadas) + 50 <= 2000;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sem volta automática: as entradas das telas novas ficam no texto e são ignoradas por uma versão anterior.
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Groundkey.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class ObraSemEmpresaSpe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmpresaSpe",
                schema: "qh",
                table: "Obras",
                newName: "Observacoes");

            // O texto livre "Empresa / SPE" deu lugar ao cadastro de SPEs; o conteúdo antigo fica nas observações da obra.
            // EXEC: a coluna só existe depois do rename — compilar em lote separado evita erro no script idempotente.
            migrationBuilder.Sql("EXEC(N'UPDATE qh.Obras SET Observacoes = N''Empresa/SPE (cadastro antigo): '' + Observacoes WHERE Observacoes IS NOT NULL AND LEN(Observacoes) > 0')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Observacoes",
                schema: "qh",
                table: "Obras",
                newName: "EmpresaSpe");
        }
    }
}

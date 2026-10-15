using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Groundkey.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class LoginPorSenha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_EntraObjectId",
                schema: "qh",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EntraObjectId",
                schema: "qh",
                table: "Usuarios");

            migrationBuilder.AddColumn<string>(
                name: "Login",
                schema: "qh",
                table: "Usuarios",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            // Usuários já cadastrados: login = e-mail (minúsculo). Se o mesmo e-mail existir em mais de uma empresa,
            // só o primeiro recebe; os demais ficam sem login até o administrador definir.
            // EXEC: a coluna Login foi criada neste mesmo script — compilar em lote separado evita erro no script idempotente.
            migrationBuilder.Sql(@"EXEC(N'
WITH x AS (SELECT Id, LOWER(LEFT(LTRIM(RTRIM(Email)), 60)) AS L, ROW_NUMBER() OVER (PARTITION BY LOWER(LTRIM(RTRIM(Email))) ORDER BY CriadoEm) AS N
           FROM qh.Usuarios WHERE Excluido = 0 AND LEN(Email) > 0)
UPDATE u SET Login = x.L FROM qh.Usuarios u JOIN x ON x.Id = u.Id WHERE x.N = 1 AND LEN(u.Login) = 0;')");

            migrationBuilder.CreateTable(
                name: "CredenciaisUsuario",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenhaHash = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TrocarSenhaNoProximoAcesso = table.Column<bool>(type: "bit", nullable: false),
                    TentativasFalhas = table.Column<int>(type: "int", nullable: false),
                    BloqueadoAte = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UltimoAcessoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SenhaAlteradaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_CredenciaisUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CredenciaisUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "qh",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Login",
                schema: "qh",
                table: "Usuarios",
                column: "Login",
                unique: true,
                filter: "[Excluido] = 0 AND [Login] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_CredenciaisUsuario_EmpresaId",
                schema: "qh",
                table: "CredenciaisUsuario",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CredenciaisUsuario_UsuarioId",
                schema: "qh",
                table: "CredenciaisUsuario",
                column: "UsuarioId",
                unique: true,
                filter: "[Excluido] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CredenciaisUsuario",
                schema: "qh");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Login",
                schema: "qh",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Login",
                schema: "qh",
                table: "Usuarios");

            migrationBuilder.AddColumn<Guid>(
                name: "EntraObjectId",
                schema: "qh",
                table: "Usuarios",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EntraObjectId",
                schema: "qh",
                table: "Usuarios",
                column: "EntraObjectId",
                unique: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigeo.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class AreaFornecedorCodigo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodigosAcessoFornecedor",
                schema: "qh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContatoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Destino = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CodigoHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Tentativas = table.Column<int>(type: "int", nullable: false),
                    UsadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigosAcessoFornecedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodigosAcessoFornecedor_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalSchema: "qh",
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodigosAcessoFornecedor_Documento_CriadoEm",
                schema: "qh",
                table: "CodigosAcessoFornecedor",
                columns: new[] { "Documento", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_CodigosAcessoFornecedor_FornecedorId",
                schema: "qh",
                table: "CodigosAcessoFornecedor",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_CodigosAcessoFornecedor_Ip_CriadoEm",
                schema: "qh",
                table: "CodigosAcessoFornecedor",
                columns: new[] { "Ip", "CriadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodigosAcessoFornecedor",
                schema: "qh");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Groundkey.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class RevisaoIntegridade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Dados: deixa a base consistente ANTES dos índices únicos (cada passo é idempotente). ──
            // 1) Custos de mão de obra repetidos no mês: mantém o mais recente.
            migrationBuilder.Sql(@"EXEC(N'
WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY ObraId, Competencia, EquipeId, ColaboradorId, FuncaoObraId, LEFT(Descricao, 120) ORDER BY COALESCE(AlteradoEm, CriadoEm) DESC) n
           FROM qh.CustosMaoObra WHERE Excluido = 0)
UPDATE c SET Excluido = 1, ExcluidoEm = SYSDATETIMEOFFSET(), ExcluidoPor = N''migracao'' FROM qh.CustosMaoObra c JOIN d ON d.Id = c.Id WHERE d.n > 1;
UPDATE qh.CustosMaoObra SET Descricao = LEFT(Descricao, 120) WHERE LEN(Descricao) > 120;')");
            // 2) Só uma fábrica que fatura materiais e um almoxarifado central por empresa (mantém o mais recente).
            migrationBuilder.Sql(@"EXEC(N'
WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY EmpresaId ORDER BY COALESCE(AlteradoEm, CriadoEm) DESC) n FROM qh.Estabelecimentos WHERE Excluido = 0 AND FaturaMateriais = 1)
UPDATE e SET FaturaMateriais = 0 FROM qh.Estabelecimentos e JOIN d ON d.Id = e.Id WHERE d.n > 1;
WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY EmpresaId ORDER BY COALESCE(AlteradoEm, CriadoEm) DESC) n FROM qh.Estabelecimentos WHERE Excluido = 0 AND AlmoxarifadoCentral = 1)
UPDATE e SET AlmoxarifadoCentral = 0 FROM qh.Estabelecimentos e JOIN d ON d.Id = e.Id WHERE d.n > 1;
WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY SpeId ORDER BY COALESCE(AlteradoEm, CriadoEm) DESC) n FROM qh.Estabelecimentos WHERE Excluido = 0 AND SpeId IS NOT NULL)
UPDATE e SET SpeId = NULL FROM qh.Estabelecimentos e JOIN d ON d.Id = e.Id WHERE d.n > 1;')");
            // 3) Documento mensal: uma entrega ativa por competência (as anteriores vão para o histórico).
            migrationBuilder.Sql(@"EXEC(N'
UPDATE qh.FornecedorDocumentos SET Competencia = DATEFROMPARTS(YEAR(Competencia), MONTH(Competencia), 1) WHERE Competencia IS NOT NULL AND DAY(Competencia) <> 1;
WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY FornecedorId, TipoDocumentoExigidoId, Competencia ORDER BY CriadoEm DESC) n FROM qh.FornecedorDocumentos WHERE Excluido = 0 AND Competencia IS NOT NULL)
UPDATE f SET Excluido = 1, ExcluidoEm = SYSDATETIMEOFFSET(), ExcluidoPor = N''migracao'' FROM qh.FornecedorDocumentos f JOIN d ON d.Id = f.Id WHERE d.n > 1;')");
            // 4) Quebras por local repetidas: soma na primeira linha e apaga as demais; membros de equipe repetidos.
            migrationBuilder.Sql(@"EXEC(N'
WITH g AS (SELECT RequisicaoItemId, LocalId, ComposicaoId, SUM(Quantidade) q, MIN(CAST(Id AS nvarchar(36))) manter FROM qh.RequisicaoItemLocais WHERE ComposicaoId IS NOT NULL GROUP BY RequisicaoItemId, LocalId, ComposicaoId HAVING COUNT(*) > 1)
UPDATE r SET Quantidade = g.q FROM qh.RequisicaoItemLocais r JOIN g ON CAST(r.Id AS nvarchar(36)) = g.manter;
DELETE r FROM qh.RequisicaoItemLocais r WHERE r.ComposicaoId IS NOT NULL AND EXISTS (SELECT 1 FROM qh.RequisicaoItemLocais o WHERE o.RequisicaoItemId = r.RequisicaoItemId AND o.LocalId = r.LocalId AND o.ComposicaoId = r.ComposicaoId AND CAST(o.Id AS nvarchar(36)) < CAST(r.Id AS nvarchar(36)));
WITH g AS (SELECT ContratoItemId, LocalId, SUM(Quantidade) q, MIN(CAST(Id AS nvarchar(36))) manter FROM qh.ContratoItemLocais GROUP BY ContratoItemId, LocalId HAVING COUNT(*) > 1)
UPDATE c SET Quantidade = g.q FROM qh.ContratoItemLocais c JOIN g ON CAST(c.Id AS nvarchar(36)) = g.manter;
DELETE c FROM qh.ContratoItemLocais c WHERE EXISTS (SELECT 1 FROM qh.ContratoItemLocais o WHERE o.ContratoItemId = c.ContratoItemId AND o.LocalId = c.LocalId AND CAST(o.Id AS nvarchar(36)) < CAST(c.Id AS nvarchar(36)));
DELETE m FROM qh.EquipeMembros m WHERE EXISTS (SELECT 1 FROM qh.EquipeMembros o WHERE o.EquipeId = m.EquipeId AND o.ColaboradorId = m.ColaboradorId AND CAST(o.Id AS nvarchar(36)) < CAST(m.Id AS nvarchar(36)));')");
            // 5) Sigla de ambiente repetida na tipologia: numera as repetidas (BWC, BWC2…).
            migrationBuilder.Sql(@"EXEC(N'
WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY TipologiaId, Sigla ORDER BY Ordem, Id) n FROM qh.TipologiaAmbientes WHERE Sigla IS NOT NULL)
UPDATE a SET Sigla = LEFT(a.Sigla, 6) + CAST(d.n AS nvarchar(2)) FROM qh.TipologiaAmbientes a JOIN d ON d.Id = a.Id WHERE d.n > 1;')");
            // 6) Uma transferência ativa por recebimento: as repetidas (rascunho) são canceladas.
            migrationBuilder.Sql(@"EXEC(N'
WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY RecebimentoId ORDER BY CASE Status WHEN N''Recebida'' THEN 0 WHEN N''Enviada'' THEN 1 ELSE 2 END, CriadoEm) n
           FROM qh.Transferencias WHERE Excluido = 0 AND RecebimentoId IS NOT NULL AND Status <> N''Cancelada'')
UPDATE t SET Status = N''Cancelada'', Observacao = CONCAT(Observacao, N'' [cancelada na migração: transferência repetida]'') FROM qh.Transferencias t JOIN d ON d.Id = t.Id WHERE d.n > 1;')");
            // 7) Condomínio = SPE: toda a árvore do condomínio (e as células do orçamento) passa a ser da SPE do condomínio.
            migrationBuilder.Sql(@"EXEC(N'
WITH arv AS (
    SELECT l.Id, s.Id AS SpeId FROM qh.Quadras q JOIN qh.Spes s ON s.QuadraId = q.Id AND s.Excluido = 0 JOIN qh.Locais l ON l.Id = q.LocalId WHERE q.Excluido = 0
    UNION ALL
    SELECT f.Id, a.SpeId FROM qh.Locais f JOIN arv a ON f.LocalPaiId = a.Id)
UPDATE l SET SpeId = a.SpeId FROM qh.Locais l JOIN arv a ON a.Id = l.Id WHERE l.SpeId IS NULL OR l.SpeId <> a.SpeId
OPTION (MAXRECURSION 50);
UPDATE c SET SpeId = l.SpeId FROM qh.ServicosLocais c JOIN qh.Locais l ON l.Id = c.LocalId WHERE (c.SpeId IS NULL AND l.SpeId IS NOT NULL) OR c.SpeId <> l.SpeId;')");
            // 8) Fração ideal: era por prédio, agora soma 100% por condomínio. Só recalcula (pela área privativa) onde a soma não fecha.
            migrationBuilder.Sql(@"EXEC(N'
WITH arv AS (
    SELECT l.Id, q.Id AS QuadraId FROM qh.Quadras q JOIN qh.Locais l ON l.Id = q.LocalId WHERE q.Excluido = 0
    UNION ALL
    SELECT f.Id, a.QuadraId FROM qh.Locais f JOIN arv a ON f.LocalPaiId = a.Id),
ap AS (SELECT l.Id, a.QuadraId, COALESCE(l.AreaPrivativa, 0) Area, l.FracaoIdeal, l.Codigo FROM qh.Locais l JOIN arv a ON a.Id = l.Id WHERE l.Tipo = N''Apartamento'' AND l.Excluido = 0),
tot AS (SELECT QuadraId, SUM(Area) Area, SUM(COALESCE(FracaoIdeal, 0)) Soma FROM ap GROUP BY QuadraId),
rec AS (SELECT ap.Id, ap.QuadraId, ROUND(ap.Area / tot.Area, 6) f, ROW_NUMBER() OVER (PARTITION BY ap.QuadraId ORDER BY ap.Codigo DESC) ult
        FROM ap JOIN tot ON tot.QuadraId = ap.QuadraId WHERE tot.Area > 0 AND ABS(tot.Soma - 1) > 0.0001)
UPDATE l SET FracaoIdeal = rec.f FROM qh.Locais l JOIN rec ON rec.Id = l.Id
OPTION (MAXRECURSION 50);
WITH arv AS (
    SELECT l.Id, q.Id AS QuadraId FROM qh.Quadras q JOIN qh.Locais l ON l.Id = q.LocalId WHERE q.Excluido = 0
    UNION ALL
    SELECT f.Id, a.QuadraId FROM qh.Locais f JOIN arv a ON f.LocalPaiId = a.Id),
ap AS (SELECT l.Id, a.QuadraId, l.FracaoIdeal, ROW_NUMBER() OVER (PARTITION BY a.QuadraId ORDER BY l.Codigo DESC) ult FROM qh.Locais l JOIN arv a ON a.Id = l.Id WHERE l.Tipo = N''Apartamento'' AND l.Excluido = 0),
tot AS (SELECT QuadraId, SUM(COALESCE(FracaoIdeal, 0)) Soma FROM ap GROUP BY QuadraId)
UPDATE l SET FracaoIdeal = l.FracaoIdeal + (1 - tot.Soma) FROM qh.Locais l JOIN ap ON ap.Id = l.Id AND ap.ult = 1 JOIN tot ON tot.QuadraId = ap.QuadraId
WHERE ABS(tot.Soma - 1) > 0 AND ABS(tot.Soma - 1) < 0.001
OPTION (MAXRECURSION 50);')");

            migrationBuilder.DropIndex(
                name: "IX_Transferencias_RecebimentoId",
                schema: "qh",
                table: "Transferencias");

            migrationBuilder.DropIndex(
                name: "IX_RequisicaoItemLocais_RequisicaoItemId",
                schema: "qh",
                table: "RequisicaoItemLocais");

            migrationBuilder.DropIndex(
                name: "IX_FornecedorDocumentos_FornecedorId",
                schema: "qh",
                table: "FornecedorDocumentos");

            migrationBuilder.DropIndex(
                name: "IX_Estabelecimentos_SpeId",
                schema: "qh",
                table: "Estabelecimentos");

            migrationBuilder.DropIndex(
                name: "IX_EquipeMembros_EquipeId",
                schema: "qh",
                table: "EquipeMembros");

            migrationBuilder.DropIndex(
                name: "IX_ContratoItemLocais_ContratoItemId",
                schema: "qh",
                table: "ContratoItemLocais");

            migrationBuilder.AlterColumn<string>(
                name: "Descricao",
                schema: "qh",
                table: "CustosMaoObra",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_OrigemEstabelecimentoId",
                schema: "qh",
                table: "Transferencias",
                column: "OrigemEstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_PedidoId",
                schema: "qh",
                table: "Transferencias",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "UX_Transferencias_Recebimento",
                schema: "qh",
                table: "Transferencias",
                column: "RecebimentoId",
                unique: true,
                filter: "[Excluido] = 0 AND [RecebimentoId] IS NOT NULL AND [Status] <> N'Cancelada'");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciaItens_LocalId",
                schema: "qh",
                table: "TransferenciaItens",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciaItens_ProdutoId",
                schema: "qh",
                table: "TransferenciaItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciaItens_RecebimentoItemId",
                schema: "qh",
                table: "TransferenciaItens",
                column: "RecebimentoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TipologiaAmbientes_TipologiaId_Sigla",
                schema: "qh",
                table: "TipologiaAmbientes",
                columns: new[] { "TipologiaId", "Sigla" },
                unique: true,
                filter: "[Sigla] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServicosLocais_SpeId",
                schema: "qh",
                table: "ServicosLocais",
                column: "SpeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicoAplicacoes_AmbienteModeloId",
                schema: "qh",
                table: "ServicoAplicacoes",
                column: "AmbienteModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicoAplicacoes_ObraId",
                schema: "qh",
                table: "ServicoAplicacoes",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisicaoItemLocais_ComposicaoId",
                schema: "qh",
                table: "RequisicaoItemLocais",
                column: "ComposicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisicaoItemLocais_RequisicaoItemId_LocalId_ComposicaoId",
                schema: "qh",
                table: "RequisicaoItemLocais",
                columns: new[] { "RequisicaoItemId", "LocalId", "ComposicaoId" },
                unique: true,
                filter: "[ComposicaoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_FornecedorDocumentos_Competencia",
                schema: "qh",
                table: "FornecedorDocumentos",
                columns: new[] { "FornecedorId", "TipoDocumentoExigidoId", "Competencia" },
                unique: true,
                filter: "[Excluido] = 0 AND [Competencia] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Estabelecimentos_Almoxarifado",
                schema: "qh",
                table: "Estabelecimentos",
                columns: new[] { "EmpresaId", "AlmoxarifadoCentral" },
                unique: true,
                filter: "[Excluido] = 0 AND [AlmoxarifadoCentral] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Estabelecimentos_FaturaMateriais",
                schema: "qh",
                table: "Estabelecimentos",
                columns: new[] { "EmpresaId", "FaturaMateriais" },
                unique: true,
                filter: "[Excluido] = 0 AND [FaturaMateriais] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Estabelecimentos_Spe",
                schema: "qh",
                table: "Estabelecimentos",
                column: "SpeId",
                unique: true,
                filter: "[Excluido] = 0 AND [SpeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EquipeMembros_EquipeId_ColaboradorId",
                schema: "qh",
                table: "EquipeMembros",
                columns: new[] { "EquipeId", "ColaboradorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CustosMaoObra_Linha",
                schema: "qh",
                table: "CustosMaoObra",
                columns: new[] { "ObraId", "Competencia", "EquipeId", "ColaboradorId", "FuncaoObraId", "Descricao" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoItemLocais_ContratoItemId_LocalId",
                schema: "qh",
                table: "ContratoItemLocais",
                columns: new[] { "ContratoItemId", "LocalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Apropriacoes_Lancamento",
                schema: "qh",
                table: "Apropriacoes",
                column: "Lancamento");

            migrationBuilder.AddForeignKey(
                name: "FK_Locais_TipologiaAmbientes_AmbienteModeloId",
                schema: "qh",
                table: "Locais",
                column: "AmbienteModeloId",
                principalSchema: "qh",
                principalTable: "TipologiaAmbientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequisicaoItemLocais_Composicoes_ComposicaoId",
                schema: "qh",
                table: "RequisicaoItemLocais",
                column: "ComposicaoId",
                principalSchema: "qh",
                principalTable: "Composicoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServicoAplicacoes_Obras_ObraId",
                schema: "qh",
                table: "ServicoAplicacoes",
                column: "ObraId",
                principalSchema: "qh",
                principalTable: "Obras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServicoAplicacoes_TipologiaAmbientes_AmbienteModeloId",
                schema: "qh",
                table: "ServicoAplicacoes",
                column: "AmbienteModeloId",
                principalSchema: "qh",
                principalTable: "TipologiaAmbientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServicosLocais_Spes_SpeId",
                schema: "qh",
                table: "ServicosLocais",
                column: "SpeId",
                principalSchema: "qh",
                principalTable: "Spes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransferenciaItens_Locais_LocalId",
                schema: "qh",
                table: "TransferenciaItens",
                column: "LocalId",
                principalSchema: "qh",
                principalTable: "Locais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransferenciaItens_Produtos_ProdutoId",
                schema: "qh",
                table: "TransferenciaItens",
                column: "ProdutoId",
                principalSchema: "qh",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransferenciaItens_RecebimentoItens_RecebimentoItemId",
                schema: "qh",
                table: "TransferenciaItens",
                column: "RecebimentoItemId",
                principalSchema: "qh",
                principalTable: "RecebimentoItens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transferencias_Estabelecimentos_OrigemEstabelecimentoId",
                schema: "qh",
                table: "Transferencias",
                column: "OrigemEstabelecimentoId",
                principalSchema: "qh",
                principalTable: "Estabelecimentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transferencias_Pedidos_PedidoId",
                schema: "qh",
                table: "Transferencias",
                column: "PedidoId",
                principalSchema: "qh",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Locais_TipologiaAmbientes_AmbienteModeloId",
                schema: "qh",
                table: "Locais");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisicaoItemLocais_Composicoes_ComposicaoId",
                schema: "qh",
                table: "RequisicaoItemLocais");

            migrationBuilder.DropForeignKey(
                name: "FK_ServicoAplicacoes_Obras_ObraId",
                schema: "qh",
                table: "ServicoAplicacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_ServicoAplicacoes_TipologiaAmbientes_AmbienteModeloId",
                schema: "qh",
                table: "ServicoAplicacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_ServicosLocais_Spes_SpeId",
                schema: "qh",
                table: "ServicosLocais");

            migrationBuilder.DropForeignKey(
                name: "FK_TransferenciaItens_Locais_LocalId",
                schema: "qh",
                table: "TransferenciaItens");

            migrationBuilder.DropForeignKey(
                name: "FK_TransferenciaItens_Produtos_ProdutoId",
                schema: "qh",
                table: "TransferenciaItens");

            migrationBuilder.DropForeignKey(
                name: "FK_TransferenciaItens_RecebimentoItens_RecebimentoItemId",
                schema: "qh",
                table: "TransferenciaItens");

            migrationBuilder.DropForeignKey(
                name: "FK_Transferencias_Estabelecimentos_OrigemEstabelecimentoId",
                schema: "qh",
                table: "Transferencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Transferencias_Pedidos_PedidoId",
                schema: "qh",
                table: "Transferencias");

            migrationBuilder.DropIndex(
                name: "IX_Transferencias_OrigemEstabelecimentoId",
                schema: "qh",
                table: "Transferencias");

            migrationBuilder.DropIndex(
                name: "IX_Transferencias_PedidoId",
                schema: "qh",
                table: "Transferencias");

            migrationBuilder.DropIndex(
                name: "UX_Transferencias_Recebimento",
                schema: "qh",
                table: "Transferencias");

            migrationBuilder.DropIndex(
                name: "IX_TransferenciaItens_LocalId",
                schema: "qh",
                table: "TransferenciaItens");

            migrationBuilder.DropIndex(
                name: "IX_TransferenciaItens_ProdutoId",
                schema: "qh",
                table: "TransferenciaItens");

            migrationBuilder.DropIndex(
                name: "IX_TransferenciaItens_RecebimentoItemId",
                schema: "qh",
                table: "TransferenciaItens");

            migrationBuilder.DropIndex(
                name: "IX_TipologiaAmbientes_TipologiaId_Sigla",
                schema: "qh",
                table: "TipologiaAmbientes");

            migrationBuilder.DropIndex(
                name: "IX_ServicosLocais_SpeId",
                schema: "qh",
                table: "ServicosLocais");

            migrationBuilder.DropIndex(
                name: "IX_ServicoAplicacoes_AmbienteModeloId",
                schema: "qh",
                table: "ServicoAplicacoes");

            migrationBuilder.DropIndex(
                name: "IX_ServicoAplicacoes_ObraId",
                schema: "qh",
                table: "ServicoAplicacoes");

            migrationBuilder.DropIndex(
                name: "IX_RequisicaoItemLocais_ComposicaoId",
                schema: "qh",
                table: "RequisicaoItemLocais");

            migrationBuilder.DropIndex(
                name: "IX_RequisicaoItemLocais_RequisicaoItemId_LocalId_ComposicaoId",
                schema: "qh",
                table: "RequisicaoItemLocais");

            migrationBuilder.DropIndex(
                name: "UX_FornecedorDocumentos_Competencia",
                schema: "qh",
                table: "FornecedorDocumentos");

            migrationBuilder.DropIndex(
                name: "UX_Estabelecimentos_Almoxarifado",
                schema: "qh",
                table: "Estabelecimentos");

            migrationBuilder.DropIndex(
                name: "UX_Estabelecimentos_FaturaMateriais",
                schema: "qh",
                table: "Estabelecimentos");

            migrationBuilder.DropIndex(
                name: "UX_Estabelecimentos_Spe",
                schema: "qh",
                table: "Estabelecimentos");

            migrationBuilder.DropIndex(
                name: "IX_EquipeMembros_EquipeId_ColaboradorId",
                schema: "qh",
                table: "EquipeMembros");

            migrationBuilder.DropIndex(
                name: "UX_CustosMaoObra_Linha",
                schema: "qh",
                table: "CustosMaoObra");

            migrationBuilder.DropIndex(
                name: "IX_ContratoItemLocais_ContratoItemId_LocalId",
                schema: "qh",
                table: "ContratoItemLocais");

            migrationBuilder.DropIndex(
                name: "IX_Apropriacoes_Lancamento",
                schema: "qh",
                table: "Apropriacoes");

            migrationBuilder.AlterColumn<string>(
                name: "Descricao",
                schema: "qh",
                table: "CustosMaoObra",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_RecebimentoId",
                schema: "qh",
                table: "Transferencias",
                column: "RecebimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisicaoItemLocais_RequisicaoItemId",
                schema: "qh",
                table: "RequisicaoItemLocais",
                column: "RequisicaoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FornecedorDocumentos_FornecedorId",
                schema: "qh",
                table: "FornecedorDocumentos",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_SpeId",
                schema: "qh",
                table: "Estabelecimentos",
                column: "SpeId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipeMembros_EquipeId",
                schema: "qh",
                table: "EquipeMembros",
                column: "EquipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoItemLocais_ContratoItemId",
                schema: "qh",
                table: "ContratoItemLocais",
                column: "ContratoItemId");
        }
    }
}

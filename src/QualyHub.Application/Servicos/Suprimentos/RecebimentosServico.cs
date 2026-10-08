using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Regras;
using QualyHub.Domain.Suprimentos;
using QualyHub.Domain.Qualidade;

namespace QualyHub.Application.Servicos;

/// <summary>Recebimento de material com FVM (T13).</summary>
public sealed class RecebimentosServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── T13 Recebimento + FVM ─────────

    public async Task<Guid> RegistrarRecebimentoAsync(Recebimento r, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (r.PedidoId is { } pid)
        {
            var ped = await db.Pedidos.Include(p => p.Itens).FirstAsync(p => p.Id == pid, ct);
            r.FornecedorId = ped.FornecedorId;
            r.ObraId = ped.ObraId;
            if (ped.EntregaAlmoxarifadoCentral)
            {
                r.AlmoxarifadoCentral = true;
                r.EstabelecimentoId ??= await db.Estabelecimentos.Where(e => e.AlmoxarifadoCentral).Select(e => (Guid?)e.Id).FirstOrDefaultAsync(ct);
            }
        }
        r.AplicarChecklist();
        ValidacaoException.Se(r.Validar());
        r.Numero = await Numerador.ProximoAsync(db, "Recebimento", ct);
        // Modelo de FVM vigente do material controlado (Q01): quarentena, rastreabilidade e ensaios previstos (Q12 → Q14).
        var matId = r.Itens.Select(i => i.MaterialControladoId).FirstOrDefault(i => i is not null);
        if (matId is not null)
        {
            var modelo = await db.ModelosInspecao.AsNoTracking().Include(m => m.Ensaios)
                .FirstOrDefaultAsync(m => m.Tipo == TipoModelo.FVM && m.MaterialControladoId == matId && m.Situacao == SituacaoModelo.Vigente, ct);
            if (modelo is not null)
            {
                r.ModeloFvmId = modelo.Id; r.QuarentenaAteEnsaio = modelo.QuarentenaAteEnsaio; r.RastrearAplicacao = modelo.RastrearAplicacao;
                if (r.Destino != DestinoRecebimento.Devolvido)
                    foreach (var x in modelo.Ensaios)
                        r.Ensaios.Add(new Domain.Qualidade.EnsaioRecebimento
                        {
                            RecebimentoId = r.Id, EnsaioModeloId = x.Id, Ensaio = x.Ensaio, Criterio = x.Criterio, AcaoReprovacao = x.AcaoReprovacao,
                            Amostra = $"{r.Numero}-{x.Ensaio[..Math.Min(3, x.Ensaio.Length)].ToUpperInvariant()}", DataColeta = Hoje,
                            DataPrevista = Hoje.AddDays(x.Momento == MomentoEnsaio.Posterior ? x.MaiorPrazo() : 0)
                        });
            }
        }
        foreach (var i in r.FvmItens) i.RecebimentoId = r.Id;
        r.StatusLote = Domain.Qualidade.RegrasQualidadeObra.StatusDoLote(r.FvmAprovada == true, r.Destino == DestinoRecebimento.Devolvido,
            r.Destino == DestinoRecebimento.AceitoComRessalva, r.QuarentenaAteEnsaio, [.. r.Ensaios]);
        if (r.FvmAprovada == false)
        {
            var rnc = new Domain.Qualidade.Rnc
            {
                Numero = $"RNC-{await Numerador.ProximoAsync(db, "Rnc", ct):0000}", Origem = OrigemRnc.FvmRecebimento, Referencia = $"Recebimento {r.Numero} · NF {r.NumeroNotaFiscal ?? r.ChaveNFe}",
                ReferenciaId = r.Id, ObraId = r.ObraId, FornecedorId = r.FornecedorId, Classificacao = ClassificacaoRnc.Material, Gravidade = Gravidade.Media,
                Descricao = r.Ocorrencia ?? "FVM reprovada", Disposicao = r.Destino == DestinoRecebimento.Devolvido ? "Devolução ao fornecedor" : "Aceite sob concessão",
                Concessao = r.Destino == DestinoRecebimento.AceitoComRessalva ? "Aceito com ressalva no recebimento — confirmar com engenharia" : null,
                ResponsavelId = usuario.UsuarioId, ResponsavelNome = usuario.Nome, Prazo = Hoje.AddDays(7), Status = StatusRnc.EmTratamento
            };
            db.Rncs.Add(rnc); r.RncId = rnc.Id;
        }
        r.RecebidoPorUsuarioId = usuario.UsuarioId;
        r.RecebidoPorNome = usuario.Nome;
        if (r.FvmAprovada == false || r.TemDivergencia)
        {
            r.ComunicadoA ??= "Engenheiro da obra; Suprimentos; Qualidade" + (r.Destino == DestinoRecebimento.Devolvido ? "; Comprador; Financeiro" : "");
            r.ComunicadoEm = relogio.GetUtcNow();
        }
        db.Recebimentos.Add(r);

        if (r.PedidoId is { } pedidoId && r.Destino != DestinoRecebimento.Devolvido)
        {
            var ped = await db.Pedidos.Include(p => p.Itens).FirstAsync(p => p.Id == pedidoId, ct);
            foreach (var ri in r.Itens.Where(i => i.PedidoItemId is not null))
            {
                var pi = ped.Itens.First(i => i.Id == ri.PedidoItemId);
                pi.QuantidadeRecebidaAceita += ri.QtdRecebida;
            }
            ped.Status = ped.Itens.All(i => i.QuantidadeRecebidaAceita >= i.Quantidade) ? StatusPedido.Entregue : StatusPedido.EntregueParcial;
        }
        if (RegrasAvaliacao.PlanoPorFvm(r, Hoje, await AvaliacaoFornecedoresServico.NumeroPlanoAsync(db, ct)) is { } plano) db.PlanosAcao.Add(plano);
        await Fiscal.NotasFiscaisServico.MarcarRecebidaAsync(db, r.Id, r.ChaveNFe, ct); // nota importada (SIEG/XML) sai da fila
        await db.SaveChangesAsync(ct);
        return r.Id;
    }
}

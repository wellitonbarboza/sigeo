using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Servicos;

/// <summary>Recebimento de material com FVM (T13).</summary>
public sealed class RecebimentosServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── T13 Recebimento + FVM ─────────

    public async Task<Guid> RegistrarRecebimentoAsync(Recebimento r, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var marcas = await MarcasServico.NormalizarAsync(db, r.Itens.Select(i => i.Marca), ct);
        foreach (var i in r.Itens) i.Marca = MarcasServico.Oficial(marcas, i.Marca);
        if (r.PedidoId is { } pid)
        {
            var ped = await db.Pedidos.Include(p => p.Itens).FirstAsync(p => p.Id == pid, ct);
            r.FornecedorId = ped.FornecedorId;
            r.ObraId = ped.ObraId;
            // Onde está recebendo: o escolhido na tela; senão o local de entrega do pedido (central ou obra).
            if (r.AlmoxarifadoId is null && ped.EntregaAlmoxarifadoCentral) r.AlmoxarifadoId = (await MotorEstoque.CentralAsync(db, ct))?.Id;
            r.AlmoxarifadoId ??= (await MotorEstoque.DaObraAsync(db, r.ObraId, ct)).Id;
            foreach (var ri in r.Itens.Where(i => i.PedidoItemId is not null)) ri.ProdutoId ??= ped.Itens.FirstOrDefault(i => i.Id == ri.PedidoItemId)?.ProdutoId;
        }
        else if (r.ObraId != Guid.Empty) r.AlmoxarifadoId ??= (await MotorEstoque.DaObraAsync(db, r.ObraId, ct)).Id;
        if (r.AlmoxarifadoId is { } almoxId)
        {
            var almox = db.Almoxarifados.Local.FirstOrDefault(a => a.Id == almoxId) ?? await db.Almoxarifados.AsNoTracking().FirstAsync(a => a.Id == almoxId, ct);
            r.AlmoxarifadoCentral = almox.Tipo == TipoAlmoxarifado.Central;
            if (r.AlmoxarifadoCentral) r.EstabelecimentoId ??= almox.EstabelecimentoId;
            else if (almox.ObraId is { } ao && ao != r.ObraId) throw new RegraDeNegocioException("O almoxarifado escolhido é de outra obra: receba no da obra do pedido ou no central e transfira depois.");
        }
        var par = await ParametrosServico.CarregarAsync(db, ct);
        r.AplicarNotas(par[Parametros.PesoIdfPrazo], par[Parametros.PesoIdfQualidade], par[Parametros.PesoIdfDocumentacao], par[Parametros.IdfMinimo]);
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
        // Entrada no estoque do almoxarifado onde foi recebido (lote em quarentena entra bloqueado; devolvido não entra).
        var custos = r.PedidoId is { } pidCusto ? await db.Set<PedidoItem>().AsNoTracking().Where(i => i.PedidoId == pidCusto).ToDictionaryAsync(i => i.Id, i => i.ValorUnitario, ct) : [];
        await MotorEstoque.EntradaRecebimentoAsync(db, r, custos, relogio.GetUtcNow(), ct);

        Guid? requisicaoAtendida = null;
        if (r.PedidoId is { } pedidoId && r.Destino != DestinoRecebimento.Devolvido)
        {
            var ped = await db.Pedidos.Include(p => p.Itens).FirstAsync(p => p.Id == pedidoId, ct);
            foreach (var ri in r.Itens.Where(i => i.PedidoItemId is not null))
            {
                var pi = ped.Itens.First(i => i.Id == ri.PedidoItemId);
                pi.QuantidadeRecebidaAceita += ri.QtdRecebida;
            }
            ped.Status = ped.Itens.All(i => i.QuantidadeRecebidaAceita >= i.Quantidade) ? StatusPedido.Entregue : StatusPedido.EntregueParcial;
            requisicaoAtendida = ped.RequisicaoId;
        }
        // Material recebido entra no custo realizado da obra (preço do pedido, fase/local da requisição).
        await CustoRealizadoServico.GerarDoRecebimentoAsync(db, r, ct);
        if (RegrasAvaliacao.PlanoPorFvm(r, Hoje, await AvaliacaoFornecedoresServico.NumeroPlanoAsync(db, ct)) is { } plano) db.PlanosAcao.Add(plano);
        await Fiscal.NotasFiscaisServico.MarcarRecebidaAsync(db, r.Id, r.ChaveNFe, ct); // nota importada (SIEG/XML) sai da fila
        await db.SaveChangesAsync(ct);
        if (requisicaoAtendida is not null)
        {
            await AtendimentoServico.AtualizarAsync(db, requisicaoAtendida, ct); // requisição: parcialmente atendida / atendida
            await db.SaveChangesAsync(ct);
        }
        return r.Id;
    }
}

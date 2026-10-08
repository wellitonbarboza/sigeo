using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;

namespace QualyHub.Application.Servicos;

// ───────── Kit de tipologia: planilha com tipologias, ambientes, mix, serviços e onde cada serviço entra ─────────

public sealed record KitTipologia(
    IReadOnlyList<KitTip> Tipologias, IReadOnlyList<KitAmbiente> Ambientes, IReadOnlyList<KitMix> Mix,
    IReadOnlyList<KitServico> Servicos, IReadOnlyList<KitAplicacao> Aplicacoes);

public sealed record KitTip(int Linha, string Codigo, string Nome, TipoTipologia Tipo, int Dormitorios, int Suites, decimal AreaPrivativa,
    decimal? AreaEquivalente, int Pavimentos, bool PrimeiroTerreo, bool NumeracaoPorAndar, decimal? PeDireitoPadrao, string? Observacoes);

public sealed record KitAmbiente(int Linha, string Tipologia, int Ordem, EscopoAmbiente Escopo, string Nome, string Sigla, string? Categoria,
    decimal AreaPiso, decimal Perimetro, decimal? PeDireito, decimal AreaVaos, int? PavimentoDe, int? PavimentoAte);

public sealed record KitMix(int Linha, string Torre, int PavimentoDe, int PavimentoAte, string Apartamento, int Quantidade);

public sealed record KitServico(int Linha, string Codigo, string Descricao, string Unidade, string? Grupo, RegraQuantidade Regra, decimal Fator, string? Its, string? Observacao);

public sealed record KitAplicacao(int Linha, string Tipologia, string Servico, AlvoServico Alvo, string? Ambiente, decimal? QuantidadeFixa);

public sealed record ResumoKit(int TipologiasNovas, int TipologiasAtualizadas, int Ambientes, int ServicosNovos, int ServicosAtualizados, int Aplicacoes,
    IReadOnlyList<string> ItsNaoEncontradas, IReadOnlyList<string> Avisos);

/// <summary>
/// Importa um kit de tipologia (apartamento, torre, implantação), o catálogo de serviços (com regra de quantidade e ITS)
/// e a marcação de onde cada serviço entra. Atualiza por código: rodar de novo corrige o que mudou, sem duplicar.
/// Composições já existentes mantêm os insumos; ambientes já existentes (mesma sigla) mantêm o Id (locais e FVS continuam ligados).
/// </summary>
public sealed class KitTipologiaServico(IFabricaDb fabrica, EngenhariaServico eng, OrcamentoServico orc)
{
    public Task<ResumoKit> AnalisarAsync(Guid obraId, KitTipologia kit, CancellationToken ct = default) => ProcessarAsync(obraId, kit, false, ct);

    public Task<ResumoKit> AplicarAsync(Guid obraId, KitTipologia kit, CancellationToken ct = default) => ProcessarAsync(obraId, kit, true, ct);

    /// <summary>Código de ITS sem prefixo, pontos e espaços: "ITS 03.03 A" → "0303A".</summary>
    public static string ChaveIts(string? codigo)
    {
        var s = Regex.Replace((codigo ?? "").ToUpperInvariant(), @"^\s*(ITS|FVS)\s*", "");
        return Regex.Replace(s, "[^0-9A-Z]", "");
    }

    private static string Up(string? s) => (s ?? "").Trim().ToUpperInvariant();

    private async Task<ResumoKit> ProcessarAsync(Guid obraId, KitTipologia kit, bool aplicar, CancellationToken ct)
    {
        if (obraId == Guid.Empty) throw new RegraDeNegocioException("Escolha a obra.");
        Dictionary<string, Tipologia> existentes;
        Dictionary<string, Composicao> compsDb;
        List<(Guid Id, string Chave)> its;
        await using (var db = fabrica.CriarLeitura())
        {
            if (!await db.Obras.AnyAsync(o => o.Id == obraId, ct)) throw new RegraDeNegocioException("Obra não encontrada.");
            existentes = (await db.Tipologias.Include(t => t.Ambientes).Include(t => t.Mix).Where(t => t.ObraId == obraId).ToListAsync(ct))
                .ToDictionary(t => t.Codigo, StringComparer.OrdinalIgnoreCase);
            compsDb = (await db.Composicoes.Include(c => c.Itens).ToListAsync(ct)).ToDictionary(c => c.Codigo, StringComparer.OrdinalIgnoreCase);
            its = (await db.ServicosControlados.Where(s => s.Ativo).Select(s => new { s.Id, s.Codigo }).ToListAsync(ct)).Select(s => (s.Id, ChaveIts(s.Codigo))).ToList();
        }

        var erros = Validar(kit, existentes, compsDb);
        ValidacaoException.Se(erros);

        Guid? Its(string? cod)
        {
            if (string.IsNullOrWhiteSpace(cod)) return null;
            var k = ChaveIts(cod);
            var exato = its.FirstOrDefault(i => i.Chave == k);
            if (exato.Id != Guid.Empty) return exato.Id;
            var sem = Regex.Replace(k, "[A-Z]+$", ""); // "0303A" → "0303" quando a ITS não tem letra
            var base_ = its.FirstOrDefault(i => i.Chave == sem);
            return base_.Id == Guid.Empty ? null : base_.Id;
        }
        var itsFaltando = kit.Servicos.Where(s => !string.IsNullOrWhiteSpace(s.Its) && Its(s.Its) is null).Select(s => s.Its!.Trim()).Distinct().OrderBy(x => x).ToList();
        var avisos = new List<string>();
        foreach (var t in kit.Tipologias.Where(t => t.Tipo == TipoTipologia.Apartamento))
        {
            var soma = kit.Ambientes.Where(a => Up(a.Tipologia) == Up(t.Codigo)).Sum(a => a.AreaPiso);
            if (Math.Abs(soma - t.AreaPrivativa) > 0.05m) avisos.Add($"{t.Codigo}: soma dos cômodos {soma:N2} m² difere da área privativa {t.AreaPrivativa:N2} m².");
        }
        foreach (var g in kit.Ambientes.Where(a => a.AreaPiso == 0 && a.Perimetro == 0).GroupBy(a => a.Tipologia.Trim()))
            avisos.Add($"{g.Key}: sem medidas em {string.Join(", ", g.Select(a => a.Nome))} (serviços por área/perímetro não geram quantidade até preencher).");

        var resumo = new ResumoKit(
            kit.Tipologias.Count(t => !existentes.ContainsKey(t.Codigo.Trim())), kit.Tipologias.Count(t => existentes.ContainsKey(t.Codigo.Trim())),
            kit.Ambientes.Count, kit.Servicos.Count(s => !compsDb.ContainsKey(s.Codigo.Trim())), kit.Servicos.Count(s => compsDb.ContainsKey(s.Codigo.Trim())),
            kit.Aplicacoes.Count, itsFaltando, avisos);
        if (!aplicar) return resumo;

        // 1) Serviços (composições): atualiza os dados do serviço, mantém os insumos.
        var compId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in kit.Servicos)
        {
            var c = compsDb.GetValueOrDefault(s.Codigo.Trim()) ?? new Composicao();
            c.Codigo = Up(s.Codigo); c.Descricao = s.Descricao.Trim(); c.Unidade = s.Unidade.Trim(); c.Grupo = s.Grupo?.Trim();
            c.Regra = s.Regra; c.Fator = s.Fator; c.Observacao = s.Observacao?.Trim(); c.Ativa = true;
            c.ServicoControladoId = Its(s.Its) ?? c.ServicoControladoId;
            await orc.SalvarComposicaoAsync(c, ct);
            compId[c.Codigo] = c.Id;
        }
        foreach (var c in compsDb.Values) compId.TryAdd(c.Codigo, c.Id);

        // 2) Tipologias: apartamento e implantação antes da torre (o mix aponta para o apartamento).
        var tipId = existentes.ToDictionary(e => e.Key, e => e.Value.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var k in kit.Tipologias.OrderBy(t => t.Tipo == TipoTipologia.Torre ? 1 : 0))
        {
            var t = existentes.GetValueOrDefault(k.Codigo.Trim()) ?? new Tipologia { ObraId = obraId, Tipo = k.Tipo };
            t.Codigo = Up(k.Codigo); t.Nome = k.Nome.Trim(); t.Tipo = k.Tipo; t.Dormitorios = k.Dormitorios; t.Suites = k.Suites; t.AreaPrivativa = k.AreaPrivativa;
            t.AreaEquivalente = k.AreaEquivalente; t.Pavimentos = k.Pavimentos; t.PrimeiroTerreo = k.PrimeiroTerreo; t.NumeracaoPorAndar = k.NumeracaoPorAndar;
            t.PeDireitoPadrao = k.PeDireitoPadrao; t.Observacoes = k.Observacoes; t.Ativa = true;
            foreach (var a in kit.Ambientes.Where(a => Up(a.Tipologia) == t.Codigo))
            {
                var m = t.Ambientes.FirstOrDefault(x => string.Equals(x.Sigla, a.Sigla.Trim(), StringComparison.OrdinalIgnoreCase));
                if (m is null) { m = new TipologiaAmbiente { TipologiaId = t.Id }; t.Ambientes.Add(m); }
                m.Ordem = a.Ordem; m.Escopo = a.Escopo; m.Nome = a.Nome.Trim(); m.Sigla = Up(a.Sigla); m.Categoria = a.Categoria?.Trim();
                m.AreaPiso = a.AreaPiso; m.Perimetro = a.Perimetro; m.PeDireito = a.PeDireito; m.AreaVaos = a.AreaVaos;
                m.PavimentoDe = a.PavimentoDe; m.PavimentoAte = a.PavimentoAte;
            }
            var mix = kit.Mix.Where(m => Up(m.Torre) == t.Codigo).ToList();
            if (t.Tipo == TipoTipologia.Torre && mix.Count > 0)
            {
                t.Mix.Clear();
                var o = 0;
                foreach (var m in mix)
                    t.Mix.Add(new TipologiaMix { TipologiaId = t.Id, Ordem = ++o, PavimentoDe = m.PavimentoDe, PavimentoAte = m.PavimentoAte, ApartamentoTipologiaId = tipId[m.Apartamento.Trim()], Quantidade = m.Quantidade });
            }
            await eng.SalvarTipologiaAsync(t, ct);
            tipId[t.Codigo] = t.Id;
        }

        // 3) Onde cada serviço entra (marca; não desmarca o que já estava marcado à mão).
        Dictionary<Guid, List<TipologiaAmbiente>> ambs;
        await using (var db = fabrica.CriarLeitura())
        {
            var ids = tipId.Values.ToList();
            ambs = (await db.TipologiaAmbientes.Where(a => ids.Contains(a.TipologiaId)).ToListAsync(ct)).GroupBy(a => a.TipologiaId).ToDictionary(g => g.Key, g => g.ToList());
        }
        foreach (var ap in kit.Aplicacoes)
        {
            var tid = tipId[ap.Tipologia.Trim()];
            Guid? amb = ap.Alvo == AlvoServico.Ambiente
                ? ambs.GetValueOrDefault(tid)?.First(a => string.Equals(a.Sigla, ap.Ambiente?.Trim(), StringComparison.OrdinalIgnoreCase)).Id
                : null;
            await orc.MarcarAplicacaoAsync(tid, compId[ap.Servico.Trim()], ap.Alvo, amb, true, ap.QuantidadeFixa, ct);
        }
        return resumo;
    }

    /// <summary>Confere referências e regras do kit antes de gravar (linha da planilha em cada mensagem).</summary>
    public static List<string> Validar(KitTipologia kit, IReadOnlyDictionary<string, Tipologia> existentes, IReadOnlyDictionary<string, Composicao> composicoes)
    {
        var e = new List<string>();
        var tips = kit.Tipologias.ToDictionary(t => Up(t.Codigo), t => t.Tipo);
        foreach (var g in kit.Tipologias.GroupBy(t => Up(t.Codigo)).Where(g => g.Count() > 1)) e.Add($"Tipologias: código {g.Key} repetido.");
        foreach (var t in kit.Tipologias)
        {
            if (string.IsNullOrWhiteSpace(t.Codigo) || string.IsNullOrWhiteSpace(t.Nome)) e.Add($"Tipologias linha {t.Linha}: informe código e nome.");
            if (existentes.TryGetValue(t.Codigo.Trim(), out var x) && x.Tipo != t.Tipo) e.Add($"Tipologias linha {t.Linha}: {t.Codigo} já existe como {x.Tipo}.");
        }
        TipoTipologia? TipoDe(string c) => tips.TryGetValue(Up(c), out var t) ? t : existentes.TryGetValue(c.Trim(), out var x) ? x.Tipo : null;
        foreach (var g in kit.Ambientes.GroupBy(a => (Up(a.Tipologia), Up(a.Sigla))).Where(g => g.Count() > 1)) e.Add($"Ambientes: sigla {g.Key.Item2} repetida em {g.Key.Item1}.");
        foreach (var a in kit.Ambientes)
        {
            if (!tips.ContainsKey(Up(a.Tipologia))) e.Add($"Ambientes linha {a.Linha}: tipologia {a.Tipologia} não está na aba Tipologias.");
            if (string.IsNullOrWhiteSpace(a.Sigla) || string.IsNullOrWhiteSpace(a.Nome)) e.Add($"Ambientes linha {a.Linha}: informe nome e sigla.");
        }
        foreach (var m in kit.Mix)
        {
            if (TipoDe(m.Torre) != TipoTipologia.Torre) e.Add($"Mix linha {m.Linha}: {m.Torre} não é tipologia de torre.");
            if (TipoDe(m.Apartamento) != TipoTipologia.Apartamento) e.Add($"Mix linha {m.Linha}: {m.Apartamento} não é tipologia de apartamento.");
        }
        foreach (var g in kit.Servicos.GroupBy(s => Up(s.Codigo)).Where(g => g.Count() > 1)) e.Add($"Serviços: código {g.Key} repetido.");
        foreach (var s in kit.Servicos)
        {
            if (string.IsNullOrWhiteSpace(s.Codigo) || string.IsNullOrWhiteSpace(s.Descricao) || string.IsNullOrWhiteSpace(s.Unidade)) e.Add($"Serviços linha {s.Linha}: informe código, descrição e unidade.");
            if (s.Fator <= 0) e.Add($"Serviços linha {s.Linha}: fator deve ser maior que zero.");
        }
        var servicos = kit.Servicos.Select(s => Up(s.Codigo)).ToHashSet();
        var siglas = kit.Ambientes.Select(a => (Up(a.Tipologia), Up(a.Sigla))).ToHashSet();
        foreach (var ap in kit.Aplicacoes)
        {
            var tipo = TipoDe(ap.Tipologia);
            if (tipo is null) { e.Add($"Aplicação linha {ap.Linha}: tipologia {ap.Tipologia} não encontrada."); continue; }
            if (!servicos.Contains(Up(ap.Servico)) && !composicoes.ContainsKey(ap.Servico.Trim())) e.Add($"Aplicação linha {ap.Linha}: serviço {ap.Servico} não encontrado.");
            var ok = tipo switch
            {
                TipoTipologia.Apartamento => ap.Alvo is AlvoServico.Ambiente or AlvoServico.Apartamento,
                TipoTipologia.Torre => ap.Alvo is AlvoServico.Ambiente or AlvoServico.Pavimento or AlvoServico.Torre,
                _ => ap.Alvo is AlvoServico.Ambiente or AlvoServico.Condominio
            };
            if (!ok) e.Add($"Aplicação linha {ap.Linha}: nível {ap.Alvo} não se aplica a {ap.Tipologia}.");
            if (ap.Alvo == AlvoServico.Ambiente)
            {
                var existeNoBanco = existentes.TryGetValue(ap.Tipologia.Trim(), out var t) && t.Ambientes.Any(a => string.Equals(a.Sigla, ap.Ambiente?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (!siglas.Contains((Up(ap.Tipologia), Up(ap.Ambiente))) && !existeNoBanco) e.Add($"Aplicação linha {ap.Linha}: ambiente {ap.Ambiente} não existe em {ap.Tipologia}.");
            }
            if (ap.QuantidadeFixa is < 0) e.Add($"Aplicação linha {ap.Linha}: quantidade não pode ser negativa.");
        }
        return e;
    }
}

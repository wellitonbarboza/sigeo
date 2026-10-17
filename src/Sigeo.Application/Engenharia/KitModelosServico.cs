using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Engenharia;

/// <summary>
/// Importa modelos de FVS (ligados à ITS) e de FVM (ligados ao material controlado) com itens, critérios e ensaios.
/// Atualiza por código. ITS e materiais controlados que não existirem são criados. Modelo que já tem fichas preenchidas
/// mantém os itens (o histórico aponta para eles): só o cabeçalho é atualizado e o modelo é listado para revisão manual.
/// </summary>
public sealed class KitModelosServico(IFabricaDb fabrica, QualidadeObraServico qob)
{
    public Task<ResumoKitModelos> AnalisarAsync(KitModelos kit, CancellationToken ct = default) => ProcessarAsync(kit, false, ct);
    public Task<ResumoKitModelos> AplicarAsync(KitModelos kit, CancellationToken ct = default) => ProcessarAsync(kit, true, ct);

    private static string Up(string? s) => (s ?? "").Trim().ToUpperInvariant();

    /// <summary>
    /// Código da ITS criada: "ITS 03.02" quando a planilha usa o padrão ITS; outros padrões da empresa (PEIS.03, PES-12…) ficam como vieram.
    /// </summary>
    private static string ChaveDoc(string? c) => System.Text.RegularExpressions.Regex.Replace((c ?? "").ToUpperInvariant(), "[^0-9A-Z]", "");

    public static string CodigoIts(string? its)
    {
        var bruto = (its ?? "").Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(bruto, @"^\s*(ITS|FVS)?[\s.\-]*\d", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return System.Text.RegularExpressions.Regex.Replace(bruto.ToUpperInvariant(), @"\s+", " ");
        var ch = KitTipologiaServico.ChaveIts(bruto);
        return "ITS " + string.Join('.', ch.Length >= 4 ? [ch[..2], ch[2..4]] : [ch]);
    }

    private static string SemAcento(string? s)
    {
        var d = (s ?? "").Normalize(System.Text.NormalizationForm.FormD);
        return new string(d.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
            .Select(char.ToLowerInvariant).ToArray());
    }

    private static void Novo<T>(ISigeoDb db, ICollection<T> lista, T item) where T : class
    {
        lista.Add(item);
        db.Add(item);
    }

    public static List<string> Validar(KitModelos kit)
    {
        var e = new List<string>();
        foreach (var g in kit.Modelos.GroupBy(m => Up(m.Codigo)).Where(g => g.Count() > 1)) e.Add($"Modelos: código {g.Key} repetido.");
        var codigos = kit.Modelos.Select(m => Up(m.Codigo)).ToHashSet();
        foreach (var m in kit.Modelos)
        {
            if (string.IsNullOrWhiteSpace(m.Codigo) || string.IsNullOrWhiteSpace(m.Titulo)) e.Add($"Modelos linha {m.Linha}: informe código e título.");
            if (m.Tipo == TipoModelo.FVS && string.IsNullOrWhiteSpace(m.Its)) e.Add($"Modelos linha {m.Linha}: FVS precisa da ITS.");
            if (m.Tipo == TipoModelo.FVM && string.IsNullOrWhiteSpace(m.Material) && string.IsNullOrWhiteSpace(m.NomeMaterial)) e.Add($"Modelos linha {m.Linha}: FVM precisa do material controlado.");
            if (m.Tipo == TipoModelo.FVM && (string.IsNullOrWhiteSpace(m.Norma) || string.IsNullOrWhiteSpace(m.Amostragem))) e.Add($"Modelos linha {m.Linha}: FVM precisa de norma e amostragem.");
            if (!kit.Itens.Any(i => Up(i.Modelo) == Up(m.Codigo))) e.Add($"Modelos linha {m.Linha}: {m.Codigo} sem itens na aba Itens.");
        }
        foreach (var i in kit.Itens)
        {
            if (!codigos.Contains(Up(i.Modelo))) e.Add($"Itens linha {i.Linha}: modelo {i.Modelo} não está na aba Modelos.");
            if (string.IsNullOrWhiteSpace(i.Descricao) || string.IsNullOrWhiteSpace(i.Criterio)) e.Add($"Itens linha {i.Linha}: informe item e critério.");
        }
        foreach (var x in kit.Ensaios)
        {
            if (!codigos.Contains(Up(x.Modelo))) e.Add($"Ensaios linha {x.Linha}: modelo {x.Modelo} não está na aba Modelos.");
            if (x.Momento == MomentoEnsaio.Posterior && string.IsNullOrWhiteSpace(x.PrazoDias)) e.Add($"Ensaios linha {x.Linha}: informe o prazo em dias.");
        }
        return e;
    }

    private async Task<ResumoKitModelos> ProcessarAsync(KitModelos kit, bool aplicar, CancellationToken ct)
    {
        ValidacaoException.Se(Validar(kit));
        await using var db = fabrica.Criar();
        var existentes = await db.ModelosInspecao.Include(m => m.Itens).Include(m => m.Ensaios).ToListAsync(ct);
        var porCodigo = existentes.ToDictionary(m => Up(m.Codigo));
        var servicos = await db.ServicosControlados.ToListAsync(ct);
        var materiais = await db.MateriaisControlados.ToListAsync(ct);
        var itensIds = existentes.SelectMany(m => m.Itens.Select(i => i.Id)).ToList();
        var usados = (await db.FvsItens.Where(i => itensIds.Contains(i.ItemVerificacaoId)).Select(i => i.ItemVerificacaoId).Distinct().ToListAsync(ct))
            .Concat(await db.FvmItens.Where(i => itensIds.Contains(i.ItemVerificacaoId)).Select(i => i.ItemVerificacaoId).Distinct().ToListAsync(ct)).ToHashSet();

        ServicoControlado? Its(KitModelo k)
        {
            var c = KitTipologiaServico.ChaveIts(k.Its);
            return servicos.FirstOrDefault(s => KitTipologiaServico.ChaveIts(s.Codigo) == c);
        }
        MaterialControlado? Mat(KitModelo k)
        {
            // Só pelo nome (sem acento/caixa): o código do material vem de tabelas diferentes e pode apontar para outro material.
            var nome = SemAcento(k.NomeMaterial);
            return nome.Length == 0 ? null : materiais.FirstOrDefault(m => SemAcento(m.Nome) == nome);
        }

        var docsIts = (await db.Documentos.Select(d => new { d.Id, d.Codigo }).ToListAsync(ct))
            .GroupBy(d => ChaveDoc(d.Codigo)).ToDictionary(g => g.Key, g => (Guid?)g.First().Id);
        var vinculados = new List<string>();
        var itsCriadas = new List<string>();
        var itsRenomeadas = new List<string>();
        var matsCriados = new List<string>();
        var mantidos = new List<string>();
        var avisos = new List<string>();
        int novos = 0, atualizados = 0;
        var matsAtualizados = new List<string>();
        var codigosLiberados = new List<string>();
        static string? Corte(string? v, int max) => v is null ? null : v.Length <= max ? v : v[..max];
        foreach (var km in kit.Materiais)
        {
            // Pelo nome (como a FVM). O código da lista oficial da empresa passa a ser o do material.
            var mat = materiais.FirstOrDefault(m => SemAcento(m.Nome) == SemAcento(km.Nome));
            var codigo = km.Codigo.Trim();
            if ((mat is null || Up(mat.Codigo) != Up(codigo)) && materiais.FirstOrDefault(x => Up(x.Codigo) == Up(codigo) && x != mat) is { } dono)
            {
                // O material antigo cede o código (recebe -ANT). O vínculo é pelo Id: produtos, modelos, qualificações e recebimentos seguem intactos.
                var livre = $"{dono.Codigo}-ANT";
                for (var n = 2; materiais.Any(x => Up(x.Codigo) == Up(livre)); n++) livre = $"{dono.Codigo}-ANT{n}";
                codigosLiberados.Add($"{dono.Codigo} {dono.Nome} → {livre}");
                if (aplicar) dono.Codigo = livre;
            }
            if (mat is null)
            {
                mat = new MaterialControlado { Codigo = codigo, Nome = km.Nome.Trim() };
                materiais.Add(mat);
                if (aplicar) db.MateriaisControlados.Add(mat);
                matsCriados.Add($"{mat.Codigo} – {mat.Nome}");
            }
            else
            {
                matsAtualizados.Add(Up(mat.Codigo) == Up(codigo) ? $"{mat.Codigo} – {mat.Nome}" : $"{mat.Codigo} → {codigo} – {mat.Nome}");
                if (aplicar) mat.Codigo = codigo;
            }
            if (!aplicar) continue;
            mat.Norma = Corte(km.Norma, 500) ?? mat.Norma;
            if (string.IsNullOrWhiteSpace(mat.EspecificacaoModelo)) mat.EspecificacaoModelo = Corte(km.Norma, 400);
            mat.DocumentosExigidos = Corte(km.DocumentosExigidos, 400) ?? mat.DocumentosExigidos;
            mat.Ensaios = Corte(km.Ensaios, 400) ?? mat.Ensaios;
            mat.Armazenamento = Corte(km.Armazenamento, 1000) ?? mat.Armazenamento;
            mat.FormasQualificacao = Corte(km.FormasQualificacao, 40) ?? mat.FormasQualificacao;
            mat.CriterioQualificacao = Corte(km.CriterioQualificacao, 600) ?? mat.CriterioQualificacao;
            mat.ServicosQueUtilizam = Corte(km.ServicosQueUtilizam, 300) ?? mat.ServicosQueUtilizam;
            mat.Ativo = true;
        }
        foreach (var k in kit.Modelos)
        {
            ServicoControlado? srv = null;
            MaterialControlado? mat = null;
            if (k.Tipo == TipoModelo.FVS && (srv = Its(k)) is not null && k.NomeIts?.Trim() is { Length: > 0 } nomeIts && srv.Nome != nomeIts)
            {
                // A ITS do cadastro passa a ter o nome do documento da empresa (a carga padrão traz nomes genéricos).
                itsRenomeadas.Add($"{srv.Codigo}: {srv.Nome} → {nomeIts}");
                if (aplicar) srv.Nome = nomeIts;
            }
            if (k.Tipo == TipoModelo.FVS && srv is null)
            {
                srv = new ServicoControlado { Codigo = CodigoIts(k.Its), Nome = k.NomeIts?.Trim() is { Length: > 0 } n ? n : k.Titulo.Trim(), Sigla = null };
                var cod = srv.Codigo;
                servicos.Add(srv);
                if (aplicar) db.ServicosControlados.Add(srv);
                itsCriadas.Add($"{cod} – {srv.Nome}");
            }
            if (k.Tipo == TipoModelo.FVM && (mat = Mat(k)) is null)
            {
                var codigo = (k.Material ?? "").Trim();
                if (codigo.Length == 0 || materiais.Any(x => Up(x.Codigo) == Up(codigo))) codigo = $"{(codigo.Length == 0 ? "M" : codigo)}-{materiais.Count + 1:000}";
                mat = new MaterialControlado { Codigo = codigo, Nome = (k.NomeMaterial ?? k.Titulo).Trim() };
                materiais.Add(mat);
                if (aplicar) db.MateriaisControlados.Add(mat);
                matsCriados.Add($"{mat.Codigo} – {mat.Nome}");
            }
            if (srv is { DocumentoItsId: null } && docsIts.GetValueOrDefault(ChaveDoc(srv.Codigo)) is { } docIts)
            {
                // Documento do SGQ com o mesmo código (ex.: PEIS.03 importada em lote) vira a ITS do serviço.
                if (aplicar) srv.DocumentoItsId = docIts;
                if (!vinculados.Contains(srv.Codigo)) vinculados.Add(srv.Codigo);
            }
            var m = porCodigo.GetValueOrDefault(Up(k.Codigo));
            if (m is null) { m = new ModeloInspecao { Tipo = k.Tipo, Codigo = k.Codigo.Trim() }; novos++; if (aplicar) db.ModelosInspecao.Add(m); porCodigo[Up(k.Codigo)] = m; }
            else
            {
                atualizados++;
                if (m.Tipo != k.Tipo) { avisos.Add($"{k.Codigo}: já existe como {m.Tipo}; não alterado."); continue; }
            }
            m.Titulo = k.Titulo.Trim(); m.ServicoControladoId = srv?.Id ?? m.ServicoControladoId; m.MaterialControladoId = mat?.Id ?? m.MaterialControladoId;
            m.NormaReferencia = k.Norma; m.Amostragem = k.Amostragem; m.MomentoInspecao = k.Momento; m.NivelLocal = k.NivelLocal;
            m.LiberaMedicao = k.LiberaMedicao; m.QuarentenaAteEnsaio = k.Quarentena; m.RastrearAplicacao = k.Rastrear; m.Orientacoes = k.Orientacoes;
            var emUso = m.Itens.Any(i => usados.Contains(i.Id));
            if (emUso) { mantidos.Add(k.Codigo); continue; }
            m.Revisao = k.Revisao;
            if (aplicar)
            {
                foreach (var i in m.Itens.ToList()) db.Remove(i);
                foreach (var x in m.Ensaios.ToList()) db.Remove(x);
                m.Itens.Clear(); m.Ensaios.Clear();
                var o = 0;
                // db.Add explícito: Id já vem preenchido, e só pela navegação o EF trataria como registro existente (UPDATE).
                foreach (var i in kit.Itens.Where(i => Up(i.Modelo) == Up(k.Codigo)).OrderBy(i => i.Ordem))
                    Novo(db, m.Itens, new ItemVerificacao
                    {
                        ModeloInspecaoId = m.Id, Ordem = ++o, Descricao = i.Descricao.Trim(), Metodo = i.Metodo.Trim(), Criterio = i.Criterio.Trim(),
                        ExigeEquipamento = i.ExigeEquipamento, Foto = i.Foto, Bloqueia = i.Bloqueia, ValorMin = i.ValorMin, ValorMax = i.ValorMax
                    });
                foreach (var x in kit.Ensaios.Where(x => Up(x.Modelo) == Up(k.Codigo)))
                    Novo(db, m.Ensaios, new EnsaioModelo
                    {
                        ModeloInspecaoId = m.Id, Ensaio = x.Ensaio.Trim(), Norma = x.Norma, Momento = x.Momento, PrazoDias = x.PrazoDias, Frequencia = x.Frequencia,
                        Executante = x.Executante, Criterio = x.Criterio, AcaoReprovacao = x.Acao
                    });
                var erros = m.Validar();
                if (erros.Count > 0) avisos.AddRange(erros.Select(er => $"{k.Codigo}: {er}"));
            }
        }
        var resumo = new ResumoKitModelos(novos, atualizados, kit.Itens.Count, kit.Ensaios.Count, itsCriadas, itsRenomeadas.Distinct().ToList(), matsCriados, mantidos, avisos)
            { DocumentosVinculados = vinculados, MateriaisAtualizados = matsAtualizados, CodigosLiberados = codigosLiberados };
        if (!aplicar) return resumo;
        await db.SaveChangesAsync(ct);

        // Publica (vigente) os modelos marcados; o domínio confere itens, ITS/material, norma e amostragem.
        foreach (var k in kit.Modelos.Where(k => k.Publicar))
        {
            var m = porCodigo[Up(k.Codigo)];
            if (m.Situacao == SituacaoModelo.Vigente || m.Validar().Count > 0) continue;
            await qob.PublicarModeloAsync(m.Id, ct);
        }
        return resumo;
    }
}

using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;

namespace Groundkey.Infrastructure.Importacao;

/// <summary>Lê a planilha de modelos de FVS/FVM (abas Modelos, Itens e Ensaios; cabeçalho na linha 1).</summary>
public static class LeitorKitModelos
{
    public static KitModelos Ler(Stream arquivo)
    {
        using var wb = Planilha.Abrir(arquivo);
        var erros = new List<string>();
        var modelos = AbasPlanilha.Linhas(wb, erros, "Modelos", true).Select(l => new KitModelo(l.N,
            l.Enum("tipo", Tipos), l.Txt("codigo"), l.Txt("titulo"), l.Int("revisao"), l.TxtOpc("its"), l.TxtOpc("nomedaits"),
            l.TxtOpc("material"), l.TxtOpc("nomedomaterial"), l.TxtOpc("normaespecificacao") ?? l.TxtOpc("norma"), l.TxtOpc("amostragem"),
            l.TxtOpc("momentodainspecao") ?? l.TxtOpc("momento"), l.TxtOpc("unidadedelocal") ?? l.TxtOpc("nivel"),
            l.Bool("liberamedicao", true), l.Bool("quarentenaateensaio", false), l.Bool("rastrearaplicacao", false),
            l.TxtOpc("prerequisitosorientacoes") ?? l.TxtOpc("orientacoes"), l.Bool("publicar", true))).ToList();
        var itens = AbasPlanilha.Linhas(wb, erros, "Itens", true).Select(l => new KitItem(l.N,
            l.Txt("modelo"), l.Int("ordem"), l.Txt("itemdeverificacao") is { Length: > 0 } d ? d : l.Txt("item"), l.Txt("metodo"), l.Txt("criterio"),
            l.Bool("exigeequipamento", false), l.Enum("foto", Fotos), l.Bool("bloqueia", true), l.Dec("valorminimo"), l.Dec("valormaximo"))).ToList();
        var ensaios = AbasPlanilha.Linhas(wb, erros, "Ensaios", false).Select(l => new KitEnsaio(l.N,
            l.Txt("modelo"), l.Txt("ensaio"), l.TxtOpc("norma"), l.Enum("momento", Momentos), l.TxtOpc("prazodias"), l.Txt("frequencia"),
            l.Enum("executante", Executantes), l.Txt("criterio"), l.Enum("acaonareprovacao", Acoes))).ToList();
        var materiais = AbasPlanilha.Linhas(wb, erros, "Materiais", false).Select(l => new KitMaterialControlado(
            l.Txt("codigo"), l.Txt("nome") is { Length: > 0 } n ? n : l.Txt("material"), l.TxtOpc("norma") ?? l.TxtOpc("normaespecificacao"),
            l.TxtOpc("documentosexigidos"), l.TxtOpc("ensaios") ?? l.TxtOpc("ensaio"), l.TxtOpc("armazenamento"),
            l.TxtOpc("formasdequalificacao") ?? l.TxtOpc("formadequalificacao"), l.TxtOpc("criteriodequalificacao"), l.TxtOpc("servicosqueutilizam") ?? l.TxtOpc("servicos")))
            .Where(m => m.Codigo.Length > 0 && m.Nome.Length > 0).ToList();
        ValidacaoException.Se(erros);
        return new KitModelos(modelos, itens, ensaios) { Materiais = materiais };
    }

    /// <summary>
    /// Lê vários arquivos de uma vez: planilhas de kit (abas Modelos/Itens/Ensaios) e fichas FVS da empresa (aba BANCO DE DADOS),
    /// uma ficha por arquivo. Tudo vira um só kit para análise e importação.
    /// </summary>
    public static KitModelos LerVarios(IReadOnlyList<(string Nome, byte[] Conteudo)> arquivos)
    {
        var modelos = new List<KitModelo>(); var itens = new List<KitItem>(); var ensaios = new List<KitEnsaio>();
        var daFicha = new List<KitMaterialControlado>(); var daLista = new List<KitMaterialControlado>();
        var erros = new List<string>();
        foreach (var (nome, conteudo) in arquivos)
        {
            try
            {
                if (LeitorDocumentoSgq.LerFicha(new MemoryStream(conteudo), nome, modelos.Count + 1) is { } ficha)
                {
                    if (ficha.Itens.Count == 0) erros.Add($"{nome}: ficha {ficha.Modelo.Codigo} sem itens na tabela (Nº | Item | Método | Critério).");
                    modelos.Add(ficha.Modelo); itens.AddRange(ficha.Itens);
                    if (ficha.Material is { } mf) daFicha.Add(mf);
                    continue;
                }
                using (var wb = Planilha.Abrir(new MemoryStream(conteudo)))
                    if (!wb.Worksheets.Any(w => LeitorPlanilha.Normalizar(w.Name) == "modelos"))
                    {
                        var req = LeitorDocumentoSgq.LerRequisitosMateriais(wb);
                        if (req.Count == 0) erros.Add($"{nome}: não é kit (abas Modelos/Itens), ficha (aba BANCO DE DADOS) nem lista de requisitos de materiais.");
                        daLista.AddRange(req);
                        continue;
                    }
                var kit = Ler(new MemoryStream(conteudo));
                modelos.AddRange(kit.Modelos); itens.AddRange(kit.Itens); ensaios.AddRange(kit.Ensaios); daLista.AddRange(kit.Materiais);
            }
            catch (ValidacaoException e) { erros.AddRange(e.Erros.Select(x => $"{nome}: {x}")); }
            catch (RegraDeNegocioException e) { erros.Add($"{nome}: {e.Message}"); }
        }
        ValidacaoException.Se(erros);

        // Lista/kit dá o nome oficial (caixa e siglas); a ficha completa norma, documentos, ensaio e armazenamento.
        static string Cod(string c) => LeitorPlanilha.Normalizar(c).TrimStart('0');
        var materiais = daLista.GroupBy(m => Cod(m.Codigo)).Select(g => g.Aggregate((a, b) => a.Completar(b))).ToDictionary(m => Cod(m.Codigo));
        foreach (var f in daFicha)
            materiais[Cod(f.Codigo)] = materiais.TryGetValue(Cod(f.Codigo), out var l) ? l.Completar(f) : f;
        // FVM aponta para o material pelo nome oficial.
        for (var i = 0; i < modelos.Count; i++)
            if (modelos[i] is { Tipo: TipoModelo.FVM, Material: { } cm } fvm && materiais.TryGetValue(Cod(cm), out var mat) && fvm.NomeMaterial != mat.Nome)
                modelos[i] = fvm with
                {
                    NomeMaterial = mat.Nome,
                    Titulo = LeitorDocumentoSgq.Sem(fvm.Titulo) == LeitorDocumentoSgq.Sem(mat.Nome) ? mat.Nome : fvm.Titulo
                };
        return new KitModelos(modelos, itens, ensaios) { Materiais = materiais.Values.OrderBy(m => m.Codigo, StringComparer.Ordinal).ToList() };
    }

    private static readonly Dictionary<string, TipoModelo> Tipos = new() { ["fvs"] = TipoModelo.FVS, ["fvm"] = TipoModelo.FVM };
    private static readonly Dictionary<string, FotoItem> Fotos = new()
    { ["opcional"] = FotoItem.Opcional, ["obrigatoria"] = FotoItem.Obrigatoria, ["sempre"] = FotoItem.Obrigatoria, ["senaoconforme"] = FotoItem.SoEmNaoConformidade, ["soemnaoconformidade"] = FotoItem.SoEmNaoConformidade, [""] = FotoItem.SoEmNaoConformidade };
    private static readonly Dictionary<string, MomentoEnsaio> Momentos = new() { ["nachegada"] = MomentoEnsaio.NaChegada, ["posterior"] = MomentoEnsaio.Posterior, [""] = MomentoEnsaio.NaChegada };
    private static readonly Dictionary<string, ExecutanteEnsaio> Executantes = new() { ["obra"] = ExecutanteEnsaio.Obra, ["laboratorioexterno"] = ExecutanteEnsaio.LaboratorioExterno, ["laboratorio"] = ExecutanteEnsaio.LaboratorioExterno, [""] = ExecutanteEnsaio.Obra };
    private static readonly Dictionary<string, AcaoReprovacaoEnsaio> Acoes = new() { ["recusarcarga"] = AcaoReprovacaoEnsaio.RecusarCarga, ["abrirrnc"] = AcaoReprovacaoEnsaio.AbrirRnc, ["rnccomlocais"] = AcaoReprovacaoEnsaio.RncComLocais, [""] = AcaoReprovacaoEnsaio.AbrirRnc };
}

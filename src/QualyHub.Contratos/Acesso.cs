using QualyHub.Domain.Common;

namespace QualyHub.Contratos;

/// <summary>
/// Ações controladas por tela. Uma letra por ação; o perfil de acesso guarda, por tela, as letras liberadas (ex.: "VIX").
/// Sem <see cref="Ver"/> as outras não valem.
/// </summary>
public static class AcaoTela
{
    public const char Ver = 'V', Incluir = 'I', Excluir = 'E', Aprovar = 'A', Exportar = 'X';
    public const string Todas = "VIEAX";
    public static readonly IReadOnlyList<(char Letra, string Nome)> Lista =
        [(Ver, "Ver"), (Incluir, "Incluir/Editar"), (Excluir, "Excluir/Cancelar"), (Aprovar, "Aprovar"), (Exportar, "Exportar/Imprimir")];
    public static string Nome(char a) => Lista.FirstOrDefault(x => x.Letra == a).Nome ?? a.ToString();
    /// <summary>Verbo para a mensagem de recusa ("Seu perfil de acesso não permite excluir em Pedidos").</summary>
    public static string Verbo(char a) => a switch { Ver => "abrir", Incluir => "incluir ou alterar", Excluir => "excluir ou cancelar", Aprovar => "aprovar", Exportar => "exportar ou imprimir", _ => "fazer isso" };

    /// <summary>Normaliza: só letras válidas e da tela, na ordem padrão; sem "Ver" fica vazio.</summary>
    public static string Normalizar(string? acoes, string daTela)
    {
        if (string.IsNullOrEmpty(acoes) || !acoes.Contains(Ver)) return "";
        return new string(daTela.Where(acoes.Contains).ToArray());
    }
}

/// <summary>Tela do sistema (item do menu). <see cref="Acoes"/> = ações que existem nela.</summary>
public sealed record TelaSistema(string Modulo, string Rota, string Nome, string Icone, string Acoes);

/// <summary>Módulo (grupo do menu). Módulo fixo não é desligado pela plataforma (ex.: Sistema).</summary>
public sealed record ModuloSistema(string Id, string Nome, string Icone, bool Fixo, IReadOnlyList<TelaSistema> Telas);

public enum SituacaoTela { Livre = 0, Liberada = 1, NaoContratada = 2, SemPermissao = 3 }

/// <summary>Acesso efetivo do usuário na construtora: por tela, as ações que ele pode fazer.</summary>
public sealed record MapaAcesso(bool Administrador, Dictionary<string, string> Telas, List<string> NaoContratadas)
{
    public static readonly MapaAcesso Vazio = new(false, [], []);

    public bool Pode(string rota, char acao) => Telas.TryGetValue(rota, out var a) && a.Contains(acao);
    public bool Ve(string rota) => Telas.ContainsKey(rota);
}

/// <summary>
/// Catálogo único de módulos e telas: monta o menu, a tela de módulos da plataforma, a matriz dos perfis de acesso
/// e o bloqueio por endereço. Tela nova entra aqui (e já aparece em todos esses lugares).
/// </summary>
public static class CatalogoTelas
{
    private static TelaSistema T(string m, string r, string n, string i, string a = "VIEX") => new(m, r, n, i, a);

    public static readonly IReadOnlyList<ModuloSistema> Modulos =
    [
        new("cad", "Cadastro", "cadastro", false, [
            T("cad", "fornecedores", "Fornecedores", "predio"), T("cad", "fornecedores/qualificacao", "Qualificação Fornec.", "selo", "VIEAX"),
            T("cad", "fornecedores/documentos", "Docs. do Fornecedor", "arquivo", "VIEAX"), T("cad", "fornecedores/exigencias", "Matriz de Documentos", "tabela"),
            T("cad", "cadastros/controlados", "Materiais/Serv. Control.", "escudo-check"), T("cad", "cadastros/estabelecimentos", "Estabelecimentos (CNPJs)", "predio"),
            T("cad", "cadastros/centros-custo", "Centros de Custo", "tabela"), T("cad", "cadastros/produtos", "Produtos", "caixa"),
            T("cad", "cadastros/categorias", "Categorias de Produto", "lista")]),
        new("eng", "Engenharia", "regua", false, [
            T("eng", "engenharia/obras", "Obras e Empreendimentos", "predio"), T("eng", "engenharia/tipologias", "Tipologias", "cubo"),
            T("eng", "engenharia/composicoes", "Serviços e Composições", "lista"), T("eng", "engenharia/orcamento", "Orçamento por Local", "calculadora", "VIEAX"),
            T("eng", "engenharia/planejamento", "Planejamento", "grafico", "VX"),
            T("eng", "engenharia/cronograma", "Cronograma (edição)", "calendario", "VIEAX"), T("eng", "engenharia/last-planner", "Last Planner", "alvo"),
            T("eng", "engenharia/diario", "Diário de Obra (RDO)", "prancheta", "VIEAX"),
            T("eng", "engenharia/mao-de-obra", "Mão de Obra Própria", "pessoa")]),
        new("sup", "Suprimentos", "carrinho", false, [
            T("sup", "requisicoes", "Requisição", "prancheta", "VIEAX"), T("sup", "cotacoes", "Orçamentos", "calculadora", "VIEAX"),
            T("sup", "pedidos", "Pedidos", "sacola", "VIEAX"), T("sup", "contratos", "Contratos", "contrato", "VIEAX"),
            T("sup", "notas-fiscais", "Notas Fiscais (entrada)", "arquivo"), T("sup", "recebimentos", "Recebimento / FVM", "caixa", "VIEAX"),
            T("sup", "transferencias", "Transferências p/ Obra", "enviar"), T("sup", "medicoes", "Medições", "regua", "VIEAX"),
            T("sup", "pagamentos", "Liberação Pagamento", "dinheiro", "VIEAX"), T("sup", "contratos/parcelas", "Parcelas de Contratos", "calendario", "VIEAX"),
            T("sup", "estoque", "Estoque e Almoxarifados", "caixa", "VIEAX"), T("sup", "estoque/kits", "Kits de Material", "lista", "VIEX"),
            T("sup", "equipamentos", "Equipamentos e Manutenção", "ferramenta", "VIEAX")]),
        new("sgq", "Qualidade", "medalha", false, [
            T("sgq", "sgq/documentos", "Documentos SGQ", "pasta", "VIEAX"), T("sgq", "sgq/copias", "Cópias Controladas", "impressora"),
            T("sgq", "sgq/lista-mestra", "Lista Mestra", "lista", "VX"), T("sgq", "sgq/externos", "Documentos Externos", "livro"),
            T("sgq", "fornecedores/avaliacao", "Avaliação Fornec.", "grafico", "VIEAX"), T("sgq", "fornecedores/planos", "Planos de Ação", "alvo"),
            T("sgq", "sgq/grd", "GRD", "enviar"), T("sgq", "sgq/leituras", "Leituras / Ciência", "livro", "VX"),
            T("sgq", "sgq/registros", "Registros (Retenção)", "arquivo-morto")]),
        new("qob", "Qualidade da Obra", "escudo-check", false, [
            T("qob", "obra/mapa-torre", "Mapa da Torre (FVS)", "predio", "VX"), T("qob", "obra/mapa", "Mapa de Inspeções", "tabela", "VX"),
            T("qob", "obra/fvs", "FVS – Inspeções", "prancheta", "VIEAX"), T("qob", "obra/modelos/fvs", "Modelos de FVS", "lista", "VIEAX"),
            T("qob", "obra/modelos/fvm", "Modelos de FVM", "caixa", "VIEAX"), T("qob", "obra/rnc", "Não Conformidades", "x-circulo", "VIEAX"),
            T("qob", "obra/acoes", "Ações Corretivas", "ferramenta", "VIEAX"), T("qob", "obra/equipamentos", "Equip. de Medição", "regua"),
            T("qob", "obra/auditorias", "Auditorias Internas", "lupa")]),
        new("sst", "Segurança e Pessoas", "pessoa", false, [
            T("sst", "sst/colaboradores", "Colaboradores", "pessoa"), T("sst", "sst/exigencias", "Exigências por Função", "tabela"),
            T("sst", "sst/documentos", "Docs. do Colaborador", "arquivo", "VIEAX"), T("sst", "sst/treinamentos", "Treinamentos", "livro"),
            T("sst", "sst/epi", "Entrega de EPI", "escudo"), T("sst", "sst/inspecoes", "Inspeções de Segurança", "prancheta"),
            T("sst", "sst/pt", "Permissão de Trabalho", "chave", "VIEAX"), T("sst", "sst/ocorrencias", "Ocorrências e Acidentes", "alerta"),
            T("sst", "sst/painel", "Painel SST", "grafico", "VX")]),
        new("sis", "Sistema", "engrenagem", true, [
            T("sis", "sistema/usuarios", "Usuários e Alçadas", "pessoa"), T("sis", "sistema/perfis", "Perfis de Acesso", "cadeado", "VI"),
            T("sis", "sistema/parametros", "Parâmetros e Notificações", "ferramenta", "VI"), T("sis", "sistema/permissoes", "Permissões SGQ", "escudo", "VI"),
            T("sis", "sistema/log", "Log de Auditoria", "relogio", "VX")]),
    ];

    public static readonly IReadOnlyDictionary<string, TelaSistema> Telas =
        Modulos.SelectMany(m => m.Telas).ToDictionary(t => t.Rota, StringComparer.OrdinalIgnoreCase);

    /// <summary>Endereços que pertencem a uma tela do catálogo sem começar pela rota dela.</summary>
    private static readonly (string Prefixo, string Rota)[] Apelidos =
    [
        ("cadastros/obras", "engenharia/obras"), ("engenharia/empreendimento", "engenharia/obras"),
        ("campo/fvs", "obra/fvs"), ("campo/local", "obra/fvs"),
    ];

    private static bool Comeca(string caminho, string prefixo) =>
        caminho.Equals(prefixo, StringComparison.OrdinalIgnoreCase)
        || (caminho.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase) && caminho[prefixo.Length] == '/');

    /// <summary>Tela do catálogo a que o endereço pertence (a rota mais longa que casa); null = tela livre (pendências, mensagens, conta…).</summary>
    public static TelaSistema? TelaDe(string? caminho)
    {
        var c = (caminho ?? "").Split('?', '#')[0].Trim('/');
        if (c.Length == 0) return null;
        TelaSistema? melhor = null;
        foreach (var t in Telas.Values)
            if (Comeca(c, t.Rota) && (melhor is null || t.Rota.Length > melhor.Rota.Length)) melhor = t;
        if (melhor is not null) return melhor;
        foreach (var (p, r) in Apelidos) if (Comeca(c, p)) return Telas[r];
        return null;
    }

    /// <summary>O endereço pode ser aberto? (impressão exige "Exportar/Imprimir").</summary>
    public static SituacaoTela Situacao(MapaAcesso mapa, string? caminho, out TelaSistema? tela)
    {
        tela = TelaDe(caminho);
        if (tela is null) return SituacaoTela.Livre;
        if (mapa.NaoContratadas.Contains(tela.Rota, StringComparer.OrdinalIgnoreCase)) return SituacaoTela.NaoContratada;
        if (!mapa.Ve(tela.Rota)) return SituacaoTela.SemPermissao;
        if ((caminho ?? "").Contains("/imprimir", StringComparison.OrdinalIgnoreCase) && !mapa.Pode(tela.Rota, AcaoTela.Exportar)) return SituacaoTela.SemPermissao;
        return SituacaoTela.Liberada;
    }

    // ───── Texto "rota=ACOES;rota=ACOES" (regras do perfil) e "rota=+A-E;…" (exceções do usuário) ─────

    public static Dictionary<string, string> LerRegras(string? texto)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var par in (texto ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = par.IndexOf('=');
            if (i > 0) d[par[..i].Trim()] = par[(i + 1)..].Trim();
        }
        return d;
    }

    public static string EscreverRegras(IReadOnlyDictionary<string, string> regras) =>
        string.Join(';', regras.Where(r => r.Value.Length > 0).OrderBy(r => r.Key, StringComparer.Ordinal).Select(r => $"{r.Key}={r.Value}"));

    /// <summary>Exceções de um usuário: por tela, ação → true (liberada só para ele) / false (bloqueada só para ele).</summary>
    public static Dictionary<string, Dictionary<char, bool>> LerExcecoes(string? texto)
    {
        var d = new Dictionary<string, Dictionary<char, bool>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rota, v) in LerRegras(texto))
        {
            var e = new Dictionary<char, bool>();
            for (var i = 0; i + 1 < v.Length; i += 2) if (v[i] is '+' or '-') e[v[i + 1]] = v[i] == '+';
            if (e.Count > 0) d[rota] = e;
        }
        return d;
    }

    public static string EscreverExcecoes(IReadOnlyDictionary<string, Dictionary<char, bool>> exc) =>
        EscreverRegras(exc.ToDictionary(e => e.Key, e => string.Concat(e.Value.OrderBy(x => AcaoTela.Todas.IndexOf(x.Key)).Select(x => (x.Value ? "+" : "-") + x.Key))));

    /// <summary>Aplica exceções às ações do perfil numa tela (resultado normalizado).</summary>
    public static string Aplicar(string? doPerfil, Dictionary<char, bool>? excecoes, string daTela)
    {
        var s = new HashSet<char>(doPerfil ?? "");
        if (excecoes is not null) foreach (var (a, lib) in excecoes) { if (lib) s.Add(a); else s.Remove(a); }
        return AcaoTela.Normalizar(new string(s.ToArray()), daTela);
    }
}

/// <summary>
/// Perfis de acesso padrão (um por perfil de usuário). Espelham o uso de hoje: todo mundo enxerga as telas da operação;
/// cada área inclui/altera o que é dela; quem aprova tem "Aprovar" (que ainda depende da alçada de valor).
/// O administrador da construtora ajusta em Sistema › Perfis de Acesso.
/// </summary>
public static class PerfisPadrao
{
    private static readonly string[] Operacao = ["cad", "eng", "sup", "sgq", "qob", "sst"];

    public static Dictionary<string, string> Regras(PerfilUsuario perfil)
    {
        var r = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Dar(string rota, string acoes)
        {
            var t = CatalogoTelas.Telas[rota];
            r[rota] = AcaoTela.Normalizar((r.GetValueOrDefault(rota) ?? "") + acoes + "V", t.Acoes);
        }
        void Modulo(string m, string acoes) { foreach (var t in CatalogoTelas.Modulos.First(x => x.Id == m).Telas) Dar(t.Rota, acoes); }
        void Ver() { foreach (var m in Operacao) Modulo(m, "VX"); }

        switch (perfil)
        {
            case PerfilUsuario.Administrador:
                foreach (var m in CatalogoTelas.Modulos) Modulo(m.Id, AcaoTela.Todas);
                break;
            case PerfilUsuario.Diretoria:
                Ver(); foreach (var m in Operacao) Modulo(m, "A");
                Dar("engenharia/cronograma", "I"); Dar("sgq/documentos", "I"); Dar("sistema/log", "VX");
                break;
            case PerfilUsuario.Engenharia:
                Ver(); Modulo("eng", "IEA"); Modulo("qob", "IEA");
                foreach (var x in new[] { "requisicoes", "medicoes", "recebimentos", "transferencias" }) Dar(x, "IEA");
                foreach (var x in new[] { "cotacoes", "pedidos", "contratos", "pagamentos" }) Dar(x, "A");
                foreach (var x in new[] { "estoque", "estoque/kits", "equipamentos", "contratos/parcelas" }) Dar(x, "IEA");
                break;
            case PerfilUsuario.Qualidade:
                Ver(); Modulo("sgq", "IEA"); Modulo("qob", "IEA"); Modulo("cad", "IEA"); Dar("sistema/permissoes", "VI");
                break;
            case PerfilUsuario.Suprimentos:
                Ver(); Modulo("sup", "IE"); Modulo("cad", "IE");
                foreach (var x in new[] { "requisicoes", "cotacoes", "pedidos", "contratos", "estoque" }) Dar(x, "A");
                break;
            case PerfilUsuario.Financeiro:
                Ver(); Dar("pagamentos", "IEA"); Dar("notas-fiscais", "IE"); Dar("medicoes", "A"); Dar("contratos/parcelas", "IA");
                Dar("cadastros/centros-custo", "IE"); Dar("cadastros/estabelecimentos", "IE");
                break;
            case PerfilUsuario.Campo:
                Ver();
                foreach (var x in new[] { "requisicoes", "recebimentos", "transferencias", "obra/fvs", "obra/rnc", "obra/acoes", "engenharia/last-planner", "equipamentos" }) Dar(x, "I");
                break;
            case PerfilUsuario.Almoxarifado:
                Modulo("sup", "VX"); Modulo("cad", "VX");
                foreach (var x in new[] { "recebimentos", "transferencias", "notas-fiscais", "estoque", "estoque/kits", "equipamentos", "contratos/parcelas" }) Dar(x, "I");
                break;
            case PerfilUsuario.Seguranca:
                Ver(); Modulo("sst", "IEA");
                break;
            case PerfilUsuario.Terceiro:
                Dar("sst/colaboradores", "VI"); Dar("sst/documentos", "VI");
                break;
        }
        return r;
    }
}

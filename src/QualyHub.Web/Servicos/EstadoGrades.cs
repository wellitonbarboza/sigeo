namespace QualyHub.Web.Servicos;

/// <summary>
/// Estado das grades por tela, guardado no circuito (Scoped): quem sai de uma lista e volta
/// reencontra os mesmos filtros, ordenação, agrupamento e página. Some ao recarregar o navegador,
/// de propósito — é conveniência de navegação, não preferência gravada.
/// </summary>
public sealed class EstadoGrades
{
    private readonly Dictionary<string, EstadoGrade> _estados = new(StringComparer.OrdinalIgnoreCase);

    public EstadoGrade? Obter(string chave) => _estados.GetValueOrDefault(chave);
    public void Guardar(string chave, EstadoGrade estado) => _estados[chave] = estado;
}

/// <summary>Colunas são identificadas pelo título, que é o que se mantém estável entre visitas à tela.</summary>
public sealed record EstadoGrade(
    IReadOnlyDictionary<string, string> Filtros,
    string? Ordem,
    bool Desc,
    IReadOnlyList<string> Grupos,
    int Pagina,
    bool SomenteMeus,
    bool Compacto,
    bool MostrarFiltros);

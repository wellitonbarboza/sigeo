namespace QualyHub.Web.Components.Shared;

/// <summary>Ações da barra de ferramentas que a página repassa para a grade.</summary>
public interface IGrade
{
    void AlternarFiltros();
    void AlternarCompacto();
    void AlternarColunas();
    void AlternarSomenteMeus();
    void AlternarGrupos();
    bool Filtros { get; }
    bool Compacto { get; }
    bool SomenteMeus { get; }
    bool TemSelecionado { get; }
    Task ExportarExcelAsync();
    Task ExportarJsonAsync();
    ValueTask ImprimirAsync();
}

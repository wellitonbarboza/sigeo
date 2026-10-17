using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Engenharia;

public sealed record KitModelos(IReadOnlyList<KitModelo> Modelos, IReadOnlyList<KitItem> Itens, IReadOnlyList<KitEnsaio> Ensaios)
{
    /// <summary>Materiais controlados da lista da empresa (código, norma, documentos, ensaio, armazenamento, forma de qualificação).</summary>
    public IReadOnlyList<KitMaterialControlado> Materiais { get; init; } = [];
}

/// <summary>Material controlado com os requisitos da empresa. Campos nulos não alteram o cadastro existente.</summary>
public sealed record KitMaterialControlado(string Codigo, string Nome, string? Norma, string? DocumentosExigidos, string? Ensaios, string? Armazenamento,
    string? FormasQualificacao, string? CriterioQualificacao, string? ServicosQueUtilizam)
{
    /// <summary>Junta duas leituras do mesmo material: o que esta tem prevalece; o que falta vem da outra.</summary>
    public KitMaterialControlado Completar(KitMaterialControlado o) => new(Codigo, Nome, Norma ?? o.Norma, DocumentosExigidos ?? o.DocumentosExigidos, Ensaios ?? o.Ensaios,
        Armazenamento ?? o.Armazenamento, FormasQualificacao ?? o.FormasQualificacao, CriterioQualificacao ?? o.CriterioQualificacao, ServicosQueUtilizam ?? o.ServicosQueUtilizam);
}

public sealed record KitModelo(int Linha, TipoModelo Tipo, string Codigo, string Titulo, int Revisao, string? Its, string? NomeIts, string? Material, string? NomeMaterial,
    string? Norma, string? Amostragem, string? Momento, string? NivelLocal, bool LiberaMedicao, bool Quarentena, bool Rastrear, string? Orientacoes, bool Publicar);

public sealed record KitItem(int Linha, string Modelo, int Ordem, string Descricao, string Metodo, string Criterio, bool ExigeEquipamento, FotoItem Foto, bool Bloqueia,
    decimal? ValorMin, decimal? ValorMax);

public sealed record KitEnsaio(int Linha, string Modelo, string Ensaio, string? Norma, MomentoEnsaio Momento, string? PrazoDias, string Frequencia,
    ExecutanteEnsaio Executante, string Criterio, AcaoReprovacaoEnsaio Acao);

public sealed record ResumoKitModelos(int Novos, int Atualizados, int Itens, int Ensaios, IReadOnlyList<string> ItsCriadas, IReadOnlyList<string> ItsRenomeadas, IReadOnlyList<string> MateriaisCriados,
    IReadOnlyList<string> Mantidos, IReadOnlyList<string> Avisos)
{
    /// <summary>ITS que passam a apontar para o documento do SGQ de mesmo código.</summary>
    public IReadOnlyList<string> DocumentosVinculados { get; init; } = [];
    /// <summary>Materiais controlados já cadastrados que recebem os requisitos da lista da empresa.</summary>
    public IReadOnlyList<string> MateriaisAtualizados { get; init; } = [];
    /// <summary>Materiais antigos que cedem o código à lista oficial da empresa (recebem o sufixo -ANT; vínculos mantidos).</summary>
    public IReadOnlyList<string> CodigosLiberados { get; init; } = [];
}

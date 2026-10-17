using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>D01 · Documento do SGQ (cadastro mestre — um registro por documento, independente da revisão).</summary>
public class Documento : EntidadeAuditavel, IHistoricoTemporal
{
    /// <summary>Único. Ex.: P.SUP.01, DT.SUP.01.01, F.SGQ.03, ITS 03.02.</summary>
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    /// <summary>Lista TipoDocumentoSgq (MQ, PO, P, DT, ITS, F/FORM, FVS/FVM, PQO, DE).</summary>
    public string Tipo { get; set; } = string.Empty;
    public Guid PastaId { get; set; }
    public Pasta Pasta { get; set; } = null!;
    public string? RequisitoSiac { get; set; }
    public OrigemDocumento Origem { get; set; } = OrigemDocumento.Interno;
    public AbrangenciaDocumento Abrangencia { get; set; } = AbrangenciaDocumento.Corporativo;
    public Guid? ObraId { get; set; }
    public Guid? ResponsavelUsuarioId { get; set; }
    public string? ResponsavelNome { get; set; }
    public Guid? AprovadorUsuarioId { get; set; }
    public string? AprovadorNome { get; set; }
    public Guid? RevisaoVigenteId { get; set; }
    public DateOnly? DataVigencia { get; set; }
    public StatusDocumentoSgq Status { get; set; } = StatusDocumentoSgq.EmElaboracao;
    public int PeriodicidadeMeses { get; set; } = 12;
    public DateOnly? ProximaAnalise { get; set; }
    public NivelAcesso NivelAcesso { get; set; } = NivelAcesso.PublicoInterno;
    /// <summary>Funções/locais que recebem cópia a cada publicação (separados por ;).</summary>
    public string? DistribuicaoPadrao { get; set; }
    /// <summary>Funções que devem confirmar leitura a cada publicação (separadas por ;).</summary>
    public string? LeituraObrigatoria { get; set; }
    public bool GeraRegistro { get; set; }
    public Guid? RegraRetencaoId { get; set; }

    public ICollection<DocumentoRevisao> Revisoes { get; set; } = [];
    public ICollection<DocumentoRelacionado> Relacionados { get; set; } = [];

    public DocumentoRevisao? RevisaoVigente => Revisoes.FirstOrDefault(r => r.Id == RevisaoVigenteId);

    public bool AnaliseVencida(DateOnly hoje) => ProximaAnalise is { } p && p < hoje;

    public static string NomeArquivo(string codigo, string titulo, int revisao, string extensao) =>
        $"{codigo} - {titulo} R{revisao:00}.{extensao.TrimStart('.')}";

    /// <summary>Abre nova revisão (R00 na emissão; depois incrementa). A vigente continua valendo até publicar.</summary>
    public DocumentoRevisao NovaRevisao(MotivoRevisao motivo, string? descricao, Guid elaboradorId, string elaboradorNome, DateTimeOffset agora)
    {
        if (Revisoes.Any(r => r.Status is < StatusRevisao.Vigente and not StatusRevisao.Recusada))
            throw new RegraDeNegocioException("Já existe uma revisão em andamento para este documento.");
        var numero = Revisoes.Count == 0 ? 0 : Revisoes.Max(r => r.Revisao) + 1;
        if (numero > 0 && string.IsNullOrWhiteSpace(descricao))
            throw new RegraDeNegocioException("Informe a descrição da alteração (obrigatória a partir da R01).");
        var rev = new DocumentoRevisao
        {
            EmpresaId = EmpresaId, DocumentoId = Id, Documento = this, Revisao = numero,
            MotivoRevisao = numero == 0 ? MotivoRevisao.EmissaoInicial : motivo,
            DescricaoAlteracao = numero == 0 ? (descricao ?? "Emissão inicial") : descricao!,
            ElaboradoPorUsuarioId = elaboradorId, ElaboradoPor = elaboradorNome, ElaboradoEm = agora,
            Status = StatusRevisao.EmElaboracao
        };
        foreach (var etapa in Enum.GetValues<EtapaRevisaoTipo>())
            rev.Etapas.Add(new EtapaRevisao { Etapa = etapa, Revisao = rev });
        Revisoes.Add(rev);
        if (Status == StatusDocumentoSgq.Vigente) Status = StatusDocumentoSgq.EmRevisao;
        return rev;
    }
}

using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>D02 · Revisão do documento (arquivo editável + publicado, histórico e ciclo de aprovação).</summary>
public class DocumentoRevisao : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid DocumentoId { get; set; }
    public Documento Documento { get; set; } = null!;
    public int Revisao { get; set; }
    public string? ArquivoEditavelBlob { get; set; }
    public string? ArquivoEditavelNome { get; set; }
    public string? ArquivoPublicadoBlob { get; set; }
    public string? ArquivoPublicadoNome { get; set; }
    /// <summary>SHA-256 do PDF publicado — prova de que não foi alterado.</summary>
    public string? HashSha256 { get; set; }
    public string DescricaoAlteracao { get; set; } = string.Empty;
    public MotivoRevisao MotivoRevisao { get; set; }
    /// <summary>Nº RNC, ação corretiva, ata…</summary>
    public string? OrigemRevisao { get; set; }
    public Guid ElaboradoPorUsuarioId { get; set; }
    public string ElaboradoPor { get; set; } = string.Empty;
    public DateTimeOffset ElaboradoEm { get; set; }
    public string? AnalisadoPor { get; set; }
    public DateTimeOffset? AnalisadoEm { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTimeOffset? AprovadoEm { get; set; }
    public DateOnly? DataVigencia { get; set; }
    public StatusRevisao Status { get; set; } = StatusRevisao.EmElaboracao;
    public DateOnly? ObsoletoEm { get; set; }
    public DateOnly? RetidoAte { get; set; }
    public bool RecolhimentoOk { get; set; } = true;

    public ICollection<EtapaRevisao> Etapas { get; set; } = [];
    public ICollection<CopiaControlada> Copias { get; set; } = [];

    public string Rotulo => $"R{Revisao:00}";

    public EtapaRevisao? EtapaAtual => Etapas.Where(e => !e.Historico).OrderBy(e => e.Etapa).FirstOrDefault(e => e.Decisao == DecisaoEtapa.Pendente);

    /// <summary>
    /// Decide a etapa atual (ordem fixa: Elaboração → Análise crítica → Aprovação → Publicação).
    /// Segregação: quem elaborou não aprova a própria revisão. Devolvido/Recusado exige comentário.
    /// </summary>
    public void Decidir(Guid usuarioId, string usuarioNome, DecisaoEtapa decisao, string? comentario, DateTimeOffset agora, bool podeAprovar)
    {
        if (Status is StatusRevisao.Vigente or StatusRevisao.VigenteComRessalvas or StatusRevisao.Obsoleta or StatusRevisao.Recusada or StatusRevisao.Cancelada)
            throw new RegraDeNegocioException("Revisão encerrada.");
        var etapa = EtapaAtual ?? throw new RegraDeNegocioException("Não há etapa pendente.");
        if (decisao == DecisaoEtapa.Pendente) throw new RegraDeNegocioException("Decisão inválida.");
        if (etapa.Etapa is EtapaRevisaoTipo.Aprovacao && usuarioId == ElaboradoPorUsuarioId)
            throw new RegraDeNegocioException("Segregação de funções: quem elaborou não pode aprovar a própria revisão.");
        if (etapa.Etapa is EtapaRevisaoTipo.Aprovacao or EtapaRevisaoTipo.Publicacao && !podeAprovar)
            throw new RegraDeNegocioException("Usuário sem permissão de aprovação para este documento.");
        if (decisao is DecisaoEtapa.Devolvido or DecisaoEtapa.Recusado && string.IsNullOrWhiteSpace(comentario))
            throw new RegraDeNegocioException("Comentário obrigatório para devolver ou recusar.");
        if (etapa.Etapa == EtapaRevisaoTipo.Elaboracao && string.IsNullOrWhiteSpace(ArquivoEditavelBlob))
            throw new RegraDeNegocioException("Anexe o arquivo editável antes de concluir a elaboração.");

        etapa.Decisao = decisao;
        etapa.ResponsavelUsuarioId = usuarioId;
        etapa.ResponsavelNome = usuarioNome;
        etapa.Comentario = comentario;
        etapa.DataDecisao = agora;
        etapa.Assinatura = $"{usuarioNome} · {agora:dd/MM/yyyy HH:mm:ss} · {Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{Id}|{etapa.Etapa}|{usuarioId}|{agora:O}")))[..16]}";

        switch (decisao)
        {
            case DecisaoEtapa.Devolvido:
                // Guarda a devolução como registro histórico e reabre o ciclo a partir da elaboração.
                Etapas.Add(new EtapaRevisao
                {
                    Etapa = etapa.Etapa, Revisao = this, Historico = true, Decisao = DecisaoEtapa.Devolvido,
                    ResponsavelUsuarioId = usuarioId, ResponsavelNome = usuarioNome, Comentario = comentario,
                    DataDecisao = agora, Assinatura = etapa.Assinatura
                });
                foreach (var e in Etapas.Where(e => !e.Historico))
                {
                    e.Decisao = DecisaoEtapa.Pendente; e.ResponsavelUsuarioId = null; e.ResponsavelNome = null;
                    e.Comentario = null; e.DataDecisao = null; e.Assinatura = null;
                }
                Status = StatusRevisao.DevolvidaAjuste;
                return;
            case DecisaoEtapa.Recusado:
                Status = StatusRevisao.Recusada;
                if (Documento.Status == StatusDocumentoSgq.EmRevisao) Documento.Status = StatusDocumentoSgq.Vigente;
                return;
        }

        switch (etapa.Etapa)
        {
            case EtapaRevisaoTipo.Elaboracao: Status = StatusRevisao.EmAnaliseCritica; break;
            case EtapaRevisaoTipo.AnaliseCritica: AnalisadoPor = usuarioNome; AnalisadoEm = agora; Status = StatusRevisao.AguardandoAprovacao; break;
            case EtapaRevisaoTipo.Aprovacao: AprovadoPor = usuarioNome; AprovadoEm = agora; Status = StatusRevisao.AprovadaAguardandoVigencia; break;
            case EtapaRevisaoTipo.Publicacao:
                if (decisao == DecisaoEtapa.AprovadoComRessalvas) Status = StatusRevisao.VigenteComRessalvas;
                break;
        }
    }
}

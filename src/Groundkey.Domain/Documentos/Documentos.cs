using System.Security.Cryptography;
using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;

namespace Groundkey.Domain.Documentos;

/// <summary>Pasta do SGQ (Processo/Área): Diretoria, Manual/Política/Objetivos, Engenharia (ITS), Suprimentos, Qualidade, RH, Obra…</summary>
public class Pasta : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    public Guid? PastaPaiId { get; set; }
    public int Ordem { get; set; }
}

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

public class DocumentoRelacionado : Entidade
{
    public Guid DocumentoId { get; set; }
    public Guid RelacionadoId { get; set; }
}

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

/// <summary>D03 · Etapa de aprovação da revisão (com assinatura eletrônica).</summary>
public class EtapaRevisao : Entidade
{
    public Guid DocumentoRevisaoId { get; set; }
    public DocumentoRevisao Revisao { get; set; } = null!;
    public EtapaRevisaoTipo Etapa { get; set; }
    public Guid? ResponsavelUsuarioId { get; set; }
    public string? ResponsavelNome { get; set; }
    public DateOnly? Prazo { get; set; }
    public DecisaoEtapa Decisao { get; set; } = DecisaoEtapa.Pendente;
    public string? Comentario { get; set; }
    public DateTimeOffset? DataDecisao { get; set; }
    public string? Assinatura { get; set; }
    /// <summary>Registro de uma decisão anterior (devolução) — mantido para a trilha.</summary>
    public bool Historico { get; set; }
}

/// <summary>D04 · Cópia controlada numerada (física, eletrônica ou externa via GRD), com QR de vigência.</summary>
public class CopiaControlada : EntidadeAuditavel, IHistoricoTemporal
{
    /// <summary>&lt;CÓDIGO&gt;-R&lt;nn&gt;-C&lt;nnn&gt;</summary>
    public string NumeroCopia { get; set; } = string.Empty;
    public Guid DocumentoRevisaoId { get; set; }
    public DocumentoRevisao Revisao { get; set; } = null!;
    public TipoCopia TipoCopia { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public Guid? ObraId { get; set; }
    public Guid? SolicitacaoCopiaId { get; set; }
    public Guid? GrdId { get; set; }
    public DateTimeOffset? EntregueEm { get; set; }
    public string? RecebidoPor { get; set; }
    public string QrToken { get; set; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
    public StatusCopia Status { get; set; } = StatusCopia.EmUso;
    public DateOnly? PrazoRecolhimento { get; set; }
    public DateTimeOffset? RecolhidaEm { get; set; }
    public string? RecolhidaPor { get; set; }
    public DestinoObsoleta? DestinoObsoleta { get; set; }

    public static string Numerar(string codigoDocumento, int revisao, int sequencial) => $"{codigoDocumento}-R{revisao:00}-C{sequencial:000}";

    public void Recolher(string por, DestinoObsoleta destino, DateTimeOffset agora)
    {
        if (Status != StatusCopia.RecolhimentoPendente && Status != StatusCopia.EmUso)
            throw new RegraDeNegocioException("Cópia já recolhida ou extraviada.");
        Status = StatusCopia.Recolhida;
        RecolhidaPor = por;
        RecolhidaEm = agora;
        DestinoObsoleta = destino;
    }
}

/// <summary>D05 · Solicitação de cópia (liberação pela Qualidade gera as cópias numeradas).</summary>
public class SolicitacaoCopia : EntidadeAuditavel
{
    public int Numero { get; set; }
    public Guid SolicitadoPorUsuarioId { get; set; }
    public string SolicitadoPor { get; set; } = string.Empty;
    public DateTimeOffset SolicitadoEm { get; set; }
    public Guid? ObraId { get; set; }
    public string Copiadora { get; set; } = string.Empty;
    public DateOnly Programacao { get; set; }
    public string? LiberadoPor { get; set; }
    public DateTimeOffset? LiberadoEm { get; set; }
    public StatusSolicitacaoCopia Status { get; set; } = StatusSolicitacaoCopia.Solicitada;
    public string? MotivoRecusa { get; set; }
    public ICollection<SolicitacaoCopiaItem> Itens { get; set; } = [];

    public int QtdTotal => Itens.Sum(i => i.Quantidade);
}

public class SolicitacaoCopiaItem : Entidade
{
    public Guid SolicitacaoCopiaId { get; set; }
    public Guid DocumentoRevisaoId { get; set; }
    public DocumentoRevisao Revisao { get; set; } = null!;
    public int Quantidade { get; set; } = 1;
    public string Destinatario { get; set; } = string.Empty;
}

/// <summary>D06 · Leitura obrigatória / ciência — evidência de comunicação da mudança.</summary>
public class Ciencia : EntidadeAuditavel
{
    public Guid DocumentoRevisaoId { get; set; }
    public DocumentoRevisao Revisao { get; set; } = null!;
    public Guid UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public string? Funcao { get; set; }
    public DateOnly Prazo { get; set; }
    public DateTimeOffset? LidoEm { get; set; }
    public bool ExigeTreinamento { get; set; }

    public StatusCiencia Status(DateOnly hoje) => LidoEm is not null ? StatusCiencia.Lido : hoje > Prazo ? StatusCiencia.Atrasado : StatusCiencia.Pendente;
}

/// <summary>D07 · Permissão por pasta ou documento, por perfil ou usuário (documento sobrepõe pasta).</summary>
public class PermissaoDocumento : EntidadeAuditavel, IHistoricoTemporal
{
    public EscopoPermissao Escopo { get; set; }
    public Guid AlvoId { get; set; }
    public PerfilUsuario? Perfil { get; set; }
    public Guid? UsuarioId { get; set; }
    public bool PodeVer { get; set; } = true;
    public bool PodeBaixar { get; set; }
    public bool PodeImprimir { get; set; }
    public bool PodeEditar { get; set; }
    public bool PodeAprovar { get; set; }
    public bool PodeVerObsoletos { get; set; }
    public DateOnly? ValidoAte { get; set; }

    public bool Vigente(DateOnly hoje) => !Excluido && (ValidoAte is null || ValidoAte >= hoje);
}

/// <summary>D08 · Check-out / bloqueio (impede edição simultânea).</summary>
public class CheckoutDocumento : EntidadeAuditavel
{
    public Guid DocumentoRevisaoId { get; set; }
    public Guid UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public DateTimeOffset CheckoutEm { get; set; }
    public DateOnly Prazo { get; set; }
    public DateTimeOffset? CheckinEm { get; set; }
    public string? LiberadoForcadoPor { get; set; }

    public bool Ativo => CheckinEm is null && LiberadoForcadoPor is null;
}

/// <summary>D09 · Log de eventos do controle de documentos (permanente, append-only).</summary>
public class EventoDocumento : Entidade, IPertenceEmpresa
{
    public Guid EmpresaId { get; set; }
    public DateTimeOffset DataHora { get; set; }
    public Guid? UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public EventoDocumentoTipo Evento { get; set; }
    public Guid? DocumentoId { get; set; }
    public Guid? RevisaoId { get; set; }
    public Guid? CopiaId { get; set; }
    public string? Referencia { get; set; }
    public string? Detalhe { get; set; }
    public string? Origem { get; set; }
}

/// <summary>D10 · Documento externo (NBR, legislação, projeto do cliente) com verificação periódica de atualização.</summary>
public class DocumentoExterno : EntidadeAuditavel, IHistoricoTemporal
{
    public string Identificacao { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Edicao { get; set; } = string.Empty;
    /// <summary>Lista EmissorExterno (ABNT, INMETRO, PBQP-H, Prefeitura, Bombeiros, Cliente…).</summary>
    public string Emissor { get; set; } = string.Empty;
    public DateOnly DataVerificacao { get; set; }
    public SituacaoDocExterno Situacao { get; set; } = SituacaoDocExterno.Vigente;
    /// <summary>Códigos dos documentos internos que citam este (separados por ;).</summary>
    public string? Relacionados { get; set; }
    public string? ArquivoOuLink { get; set; }
    /// <summary>Edição anterior que esta substitui (ex.: NBR 15217:2018).</summary>
    public string? Substitui { get; set; }

    public DateOnly ProximaVerificacao(int meses) => DataVerificacao.AddMonths(meses);
}

/// <summary>D11 · Regra de retenção de registros (armazenamento, recuperação, proteção, retenção, descarte).</summary>
public class RegraRetencao : EntidadeAuditavel
{
    public string Identificacao { get; set; } = string.Empty;
    /// <summary>Código do formulário/tabela que gera o registro (ex.: F.SGQ.03, T07, FORM.AC-03).</summary>
    public string? Formulario { get; set; }
    public string Armazenamento { get; set; } = string.Empty;
    public string Recuperacao { get; set; } = string.Empty;
    public string Protecao { get; set; } = string.Empty;
    public string TempoRetencao { get; set; } = string.Empty;
    /// <summary>Anos numéricos quando aplicável (nulo = até o fim da obra/permanente).</summary>
    public int? TempoRetencaoAnos { get; set; }
    public string Descarte { get; set; } = string.Empty;
}

/// <summary>D12 · GRD – guia de remessa de documentos a terceiros (cria cópias externas; confirmação por link).</summary>
public class Grd : EntidadeAuditavel
{
    public string Numero { get; set; } = string.Empty;
    public Guid FornecedorId { get; set; }
    public string? Contato { get; set; }
    public Guid? ContratoId { get; set; }
    public Guid? PedidoId { get; set; }
    public DateTimeOffset? EnviadoEm { get; set; }
    public string? EnviadoPor { get; set; }
    public Guid TokenConfirmacao { get; set; } = Guid.NewGuid();
    public DateTimeOffset? ConfirmadoEm { get; set; }
    public ICollection<GrdItem> Itens { get; set; } = [];
}

public class GrdItem : Entidade
{
    public Guid GrdId { get; set; }
    public Guid DocumentoRevisaoId { get; set; }
    public DocumentoRevisao Revisao { get; set; } = null!;
}

using QualyHub.Domain.Common;

namespace QualyHub.Domain.Estrutura;

/// <summary>Empresa (tenant) — matriz, filial ou SPE. Permite multiempresa sem refatorar.</summary>
public class Empresa : Entidade
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    /// <summary>Código curto exibido nas telas (ex.: F001, SPE-01).</summary>
    public string? Codigo { get; set; }
    public string? EnderecoEntregaPadrao { get; set; }
    /// <summary>Nível pretendido no SiAC (A ou B). Controla o recorte de serviços/materiais controlados.</summary>
    public string NivelSiac { get; set; } = "B";
    public bool Ativa { get; set; } = true;
}

public class Obra : EntidadeAuditavel, IOrigemObraPrima
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Endereco { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataPrevistaTermino { get; set; }
    public bool Ativa { get; set; } = true;
    public string? CodigoObraPrima { get; set; }
    public Guid? UltimaImportacaoId { get; set; }

    public ICollection<Local> Locais { get; set; } = [];
}

/// <summary>Hierarquia física da obra: Torre > Pavimento > Unidade/Ambiente. Usada nas FVS e na medição.</summary>
public class Local : EntidadeDaObra
{
    public Guid? LocalPaiId { get; set; }
    public Local? LocalPai { get; set; }
    public string Tipo { get; set; } = "Pavimento"; // Torre, Bloco, Pavimento, Unidade, Área comum
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public ICollection<Local> Filhos { get; set; } = [];
}

/// <summary>Centro de custo (administração, estoque, obra/etapa). Pode apontar para uma obra.</summary>
public class CentroCusto : EntidadeAuditavel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public Guid? ObraId { get; set; }
    public bool Ativo { get; set; } = true;
}

/// <summary>Usuário vinculado à identidade do Microsoft Entra ID (ObjectId).</summary>
public class Usuario : EntidadeAuditavel
{
    public Guid EntraObjectId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    /// <summary>Função exibida e usada em distribuição/leitura obrigatória (ex.: Engenheiro da Obra, Comprador).</summary>
    public string? Funcao { get; set; }
    public bool AcessoTodasObras { get; set; }
    public bool Ativo { get; set; } = true;
    public ICollection<UsuarioObra> Obras { get; set; } = [];
    /// <summary>Limites de aprovação por tipo (e opcionalmente por obra) — "aprova até R$ X".</summary>
    public ICollection<Aprovacoes.LimiteAprovacao> LimitesAprovacao { get; set; } = [];
}

/// <summary>Quais obras cada usuário enxerga (base do filtro de acesso por obra).</summary>
public class UsuarioObra : Entidade
{
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public Guid ObraId { get; set; }
    public Obra Obra { get; set; } = null!;
}

/// <summary>
/// Alçada de aprovação: define quem aprova o quê, em qual ordem e em qual faixa de valor.
/// Ex.: Requisição → 1º Engenharia, 2º Diretoria. Pedido até R$ 5 mil → Engenharia; acima → + Diretoria.
/// </summary>
public class Alcada : EntidadeAuditavel
{
    public EntidadeAprovavel Entidade { get; set; }
    /// <summary>Nulo = vale para todas as obras.</summary>
    public Guid? ObraId { get; set; }
    public int Ordem { get; set; }
    /// <summary>Rótulo da etapa exibido nas telas (ex.: "Aprov. Engenheiro", "Aprov. Diretor Técnico").</summary>
    public string? NomeEtapa { get; set; }
    public PerfilUsuario PerfilAprovador { get; set; }
    /// <summary>Opcional: aprovador nominal (sobrepõe o perfil).</summary>
    public Guid? UsuarioAprovadorId { get; set; }
    public decimal? ValorMinimo { get; set; }
    public decimal? ValorMaximo { get; set; }
    public bool Ativa { get; set; } = true;

    public bool SeAplica(decimal valor, Guid? obraId) =>
        Ativa && !Excluido
        && (ObraId is null || ObraId == obraId)
        && (ValorMinimo is null || valor >= ValorMinimo)
        && (ValorMaximo is null || valor <= ValorMaximo);
}

/// <summary>Parâmetro do fluxo, editável na tela (limite de alçada, pesos da avaliação, prazos…). Ver <see cref="Parametros"/>.</summary>
public class Parametro : EntidadeAuditavel, IHistoricoTemporal
{
    public string Chave { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string? Unidade { get; set; }
    public string? Grupo { get; set; }
}

/// <summary>Item de lista configurável (Atividade, Categoria de serviço, Critério de qualificação, Processo/Área…).</summary>
public class ItemLista : EntidadeAuditavel
{
    public TipoLista Tipo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool Ativo { get; set; } = true;
}

/// <summary>Chaves dos parâmetros e valores padrão (carga inicial genérica, editável).</summary>
public static class Parametros
{
    public const string LimiteUmOrcamento = "SUP.LIMITE_1_ORCAMENTO";
    public const string OrcamentosAcimaLimite = "SUP.ORCAMENTOS_ACIMA_LIMITE";
    public const string ValidadeQualifMaterialMeses = "SUP.VALIDADE_QUALIF_MATERIAL_MESES";
    public const string ValidadeQualifServicoMeses = "SUP.VALIDADE_QUALIF_SERVICO_MESES";
    public const string RequalifLaboratorioMeses = "SUP.REQUALIF_LAB_MESES";
    public const string CargaHorariaAvaliadorLab = "SUP.CH_AVALIADOR_LAB_H";
    public const string PesoEscopo = "AVAL.PESO_ESCOPO";
    public const string PesoMaterial = "AVAL.PESO_MATERIAL";
    public const string PesoServico = "AVAL.PESO_SERVICO";
    public const string PesoLimpeza = "AVAL.PESO_LIMPEZA";
    public const string PesoDocSeg = "AVAL.PESO_DOCSEG";
    public const string NotaPlanoCompleto = "AVAL.NOTA_PLANO_COMPLETO";
    public const string NotaPlanoPontual = "AVAL.NOTA_PLANO_PONTUAL";
    public const string PeriodicidadeAvaliacaoMeses = "AVAL.PERIODICIDADE_MESES";
    public const string AlertaVencimentoDocDias = "DOC.ALERTA_VENCIMENTO_DIAS";
    public const string PeriodicidadeAnaliseCriticaMeses = "SGQ.PERIODICIDADE_ANALISE_MESES";
    public const string AlertaAnaliseCriticaDias = "SGQ.ALERTA_ANALISE_DIAS";
    public const string PrazoLeituraDias = "SGQ.PRAZO_LEITURA_DIAS";
    public const string PrazoRecolhimentoDias = "SGQ.PRAZO_RECOLHIMENTO_DIAS";
    public const string PrazoCheckoutDias = "SGQ.PRAZO_CHECKOUT_DIAS";
    public const string VerificacaoExternosMeses = "SGQ.VERIFICACAO_EXTERNOS_MESES";
    public const string RetencaoPadraoPerc = "CTR.RETENCAO_PADRAO_PERC";

    /// <summary>Padrão genérico SiAC/PBQP-H (sem dados de empresa). Chave → (valor, unidade, descrição, grupo).</summary>
    public static readonly IReadOnlyDictionary<string, (decimal Valor, string Unidade, string Descricao, string Grupo)> Padrao =
        new Dictionary<string, (decimal, string, string, string)>
        {
            [LimiteUmOrcamento] = (1000m, "R$", "Até este valor: 1 orçamento; acima: exige mais propostas", "Suprimentos"),
            [OrcamentosAcimaLimite] = (3m, "un", "Orçamentos exigidos acima do limite", "Suprimentos"),
            [ValidadeQualifMaterialMeses] = (3m, "meses", "Validade da consulta de qualificação – material", "Suprimentos"),
            [ValidadeQualifServicoMeses] = (12m, "meses", "Validade da qualificação – serviços e demais tipos", "Suprimentos"),
            [RequalifLaboratorioMeses] = (12m, "meses", "Requalificação de laboratório não acreditado (c a f)", "Suprimentos"),
            [CargaHorariaAvaliadorLab] = (16m, "h", "Carga horária mínima do avaliador de laboratório (NBR ISO/IEC 17025)", "Suprimentos"),
            [PesoEscopo] = (0.25m, "", "Peso – Cumprimento do escopo do contrato", "Avaliação do prestador"),
            [PesoMaterial] = (0.25m, "", "Peso – Qualidade do material (se aplicável)", "Avaliação do prestador"),
            [PesoServico] = (0.30m, "", "Peso – Qualidade do serviço", "Avaliação do prestador"),
            [PesoLimpeza] = (0.10m, "", "Peso – Limpeza e organização", "Avaliação do prestador"),
            [PesoDocSeg] = (0.10m, "", "Peso – Documentação e segurança do trabalho", "Avaliação do prestador"),
            [NotaPlanoCompleto] = (7m, "", "Nota abaixo disto → reunião + plano de ação completo", "Avaliação do prestador"),
            [NotaPlanoPontual] = (9m, "", "Nota abaixo disto → plano de ação pontual", "Avaliação do prestador"),
            [PeriodicidadeAvaliacaoMeses] = (6m, "meses", "Periodicidade da avaliação consolidada de fornecedores", "Avaliação do prestador"),
            [AlertaVencimentoDocDias] = (30m, "dias", "Alerta de vencimento de documento de fornecedor", "Suprimentos"),
            [RetencaoPadraoPerc] = (5m, "%", "Retenção contratual padrão", "Contratos"),
            [PeriodicidadeAnaliseCriticaMeses] = (12m, "meses", "Periodicidade padrão de análise crítica de documento", "Documentos do SGQ"),
            [AlertaAnaliseCriticaDias] = (30m, "dias", "Alerta antes do vencimento da análise crítica", "Documentos do SGQ"),
            [PrazoLeituraDias] = (7m, "dias", "Prazo para confirmação de leitura", "Documentos do SGQ"),
            [PrazoRecolhimentoDias] = (3m, "dias", "Prazo para recolher cópia obsoleta", "Documentos do SGQ"),
            [PrazoCheckoutDias] = (5m, "dias", "Prazo máximo de check-out", "Documentos do SGQ"),
            [VerificacaoExternosMeses] = (6m, "meses", "Verificação de atualização de documentos externos", "Documentos do SGQ"),
        };
}

/// <summary>Leitura dos parâmetros com fallback no padrão (o domínio não conhece o banco).</summary>
public sealed class ParametrosVigentes(IReadOnlyDictionary<string, decimal> valores)
{
    public static ParametrosVigentes Padrao { get; } = new(Parametros.Padrao.ToDictionary(k => k.Key, v => v.Value.Valor));

    public decimal this[string chave] =>
        valores.TryGetValue(chave, out var v) ? v
        : Parametros.Padrao.TryGetValue(chave, out var p) ? p.Valor
        : throw new KeyNotFoundException($"Parâmetro '{chave}' não cadastrado.");

    public int Int(string chave) => (int)this[chave];
}

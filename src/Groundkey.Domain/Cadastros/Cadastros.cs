using Groundkey.Domain.Common;
using Groundkey.Domain.Estrutura;

namespace Groundkey.Domain.Cadastros;

/// <summary>
/// Catálogo de tipos de documento (LAO, Certificado PBQP-H, laudo, FISPQ, ART, CND, contrato social, NR-35, ASO…).
/// Define validade padrão, antecedência do alerta e se o vencimento bloqueia compra/acesso à obra.
/// </summary>
public class TipoDocumentoExigido : EntidadeAuditavel
{
    public AplicaA AplicaA { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int? ValidadePadraoDias { get; set; }
    public int DiasAlertaAntesVencimento { get; set; } = 30;
    /// <summary>Exigido de todo fornecedor/funcionário (senão, só quando o tipo de fornecimento pedir).</summary>
    public bool Obrigatorio { get; set; } = true;
    /// <summary>Para quais tipos de fornecimento é exigido (Nenhum = todos).</summary>
    public TipoFornecimento ExigidoPara { get; set; }
    /// <summary>Documento que acompanha cada entrega (ex.: certificado do lote do aço) — não tem validade própria.</summary>
    public bool PorEntrega { get; set; }
    public MomentoDocumento Momento { get; set; } = MomentoDocumento.Cadastro;
    public Guid? MaterialControladoId { get; set; }
    public Guid? ServicoControladoId { get; set; }
    public int DiasLembrete { get; set; } = 30;
    public string? Fonte { get; set; }
    /// <summary>Se vencido: bloqueia compra (fornecedor) ou acesso à obra (funcionário).</summary>
    public bool BloqueiaQuandoVencido { get; set; } = true;
    /// <summary>LGPD: documento com dado sensível (ex.: ASO). Arquivo vai para container restrito.</summary>
    public bool ContemDadoSensivel { get; set; }
    public bool Ativo { get; set; } = true;
    /// <summary>Frequência: no cadastro, por validade, mensal (competência + dia limite), por contrato, por medição, por entrega, anual.</summary>
    public FrequenciaDocumento Frequencia { get; set; } = FrequenciaDocumento.Validade;
    /// <summary>Mensal: dia limite para entregar a competência anterior (padrão 20).</summary>
    public int? DiaLimite { get; set; }
    /// <summary>Categoria (lista): Empresa, Fiscal, Trabalhista, SST, RH, Técnico…</summary>
    public string? Categoria { get; set; }
    /// <summary>Documento dos funcionários do fornecedor (ASO, ficha de registro, treinamentos…): entra quando o pré-cadastro inclui funcionários.</summary>
    public bool DeFuncionario { get; set; }
    public int Ordem { get; set; }
}

/// <summary>T08 · Fornecedor (dados da Receita + tipo de fornecimento + status de qualificação).</summary>
/// <summary>Contato do fornecedor (comercial, financeiro, técnico, SST…).</summary>
public class FornecedorContato : Entidade
{
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public string Nome { get; set; } = string.Empty;
    public string? Funcao { get; set; }
    public string? Telefone { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public bool Principal { get; set; }
}

public class Fornecedor : EntidadeAuditavel, IOrigemErp, IHistoricoTemporal
{
    public string CpfCnpj { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string? Cep { get; set; }
    public string? Endereco { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? Telefones { get; set; }
    /// <summary>Obrigatório para envio de pedido/contrato (P.SUP 3.2.1).</summary>
    public string? Emails { get; set; }
    public string? NaturezaJuridica { get; set; }
    public string? Porte { get; set; }
    public string? SituacaoCadastral { get; set; }
    public string? Cnae { get; set; }
    public decimal? CapitalSocial { get; set; }
    public DateOnly? DataFundacao { get; set; }
    public string? Telefone0800 { get; set; }
    public string? Site { get; set; }
    public bool OptanteSimples { get; set; }
    public bool Mei { get; set; }
    /// <summary>Sistema de origem do cadastro (ex.: "ExpertData", "ERP") e trilha de quem criou/alterou lá.</summary>
    public string? OrigemSistema { get; set; }
    public string? OrigemCriadoPor { get; set; }
    public DateTimeOffset? OrigemCriadoEm { get; set; }
    public string? OrigemAlteradoPor { get; set; }
    public DateTimeOffset? OrigemAlteradoEm { get; set; }
    public TipoFornecimento TipoFornecimento { get; set; }
    public StatusQualificacao StatusQualificacao { get; set; } = StatusQualificacao.EmQualificacao;
    /// <summary>Desqualificado = inativo no sistema.</summary>
    public bool Ativo { get; set; } = true;
    /// <summary>Média das notas (medições + FVM) dos últimos 6 meses — recalculada pelo sistema.</summary>
    public decimal? NotaMedia { get; set; }
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }

    /// <summary>Pré-cadastro: criado pela empresa com o mínimo; o fornecedor completa pelo link do portal.</summary>
    public bool PreCadastro { get; set; }
    /// <summary>Exigir documentos dos funcionários (prestador que coloca pessoas na obra).</summary>
    public bool IncluirDocsFuncionarios { get; set; }

    public ICollection<FornecedorDocumento> Documentos { get; set; } = [];
    public ICollection<FornecedorQualificacao> Qualificacoes { get; set; } = [];
    public ICollection<FornecedorContato> Contatos { get; set; } = [];
    /// <summary>Grupos do catálogo que o fornecedor atende (subgrupos opcionais para separar fornecedores).</summary>
    public ICollection<FornecedorGrupo> Grupos { get; set; } = [];
    /// <summary>Serviços (ITS) que o fornecedor executa.</summary>
    public ICollection<FornecedorServico> Servicos { get; set; } = [];
    /// <summary>Documentos exigidos deste fornecedor (escolhidos no pré-cadastro a partir da matriz).</summary>
    public ICollection<FornecedorExigencia> Exigencias { get; set; } = [];

    public string NomeExibicao => string.IsNullOrWhiteSpace(NomeFantasia) ? RazaoSocial : NomeFantasia!;

    /// <summary>Qualificação vigente para o escopo informado (material controlado ou serviço), na data.</summary>
    public FornecedorQualificacao? QualificacaoVigente(DateOnly data, Guid? materialId = null, Guid? servicoId = null) =>
        Qualificacoes.Where(q => !q.Excluido && q.Resultado == ResultadoQualificacao.Qualificado && q.DataQualificacao <= data)
                     .Where(q => materialId is null || q.MaterialControladoId is null || q.MaterialControladoId == materialId)
                     .Where(q => servicoId is null || q.ServicoControladoId is null || q.ServicoControladoId == servicoId)
                     .OrderByDescending(q => q.DataQualificacao)
                     .FirstOrDefault(q => q.DataValidade is null || q.DataValidade >= data);

    /// <summary>Status exibido no cadastro, derivado das qualificações e da decisão semestral.</summary>
    public StatusQualificacao CalcularStatus(DateOnly hoje)
    {
        if (StatusQualificacao == StatusQualificacao.Desqualificado) return StatusQualificacao.Desqualificado;
        var qs = Qualificacoes.Where(q => !q.Excluido).OrderByDescending(q => q.DataQualificacao).ToList();
        if (qs.Count == 0) return StatusQualificacao.EmQualificacao;
        if (QualificacaoVigente(hoje) is not null) return StatusQualificacao.Qualificado;
        var ultima = qs[0];
        return ultima.Resultado switch
        {
            ResultadoQualificacao.Qualificado => StatusQualificacao.Vencida,
            ResultadoQualificacao.NaoQualificado => StatusQualificacao.NaoQualificado,
            _ => StatusQualificacao.EmQualificacao
        };
    }
}

/// <summary>T09 · Qualificação do fornecedor (um registro por fornecedor × escopo × data). Substitui a planilha de qualificados.</summary>
public class FornecedorQualificacao : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public TipoFornecimento TipoFornecimento { get; set; }
    public Guid? MaterialControladoId { get; set; }
    public MaterialControlado? MaterialControlado { get; set; }
    public Guid? ServicoControladoId { get; set; }
    public ServicoControlado? ServicoControlado { get; set; }
    /// <summary>Item da lista CriterioQualificacao (histórico, PSQ, SBAC, documentos, visita, amostra, indicação, laboratório).</summary>
    public string CriterioQualificacao { get; set; } = string.Empty;
    public SituacaoPsq Psq { get; set; }
    public DateOnly? DataConsultaPsq { get; set; }
    public string? CertificadoSbac { get; set; }
    public EnquadramentoLaboratorio LaboratorioEnquadramento { get; set; }
    public string? AvaliadorLaboratorio { get; set; }
    public DateOnly DataQualificacao { get; set; }
    public DateOnly? DataValidade { get; set; }
    public ResultadoQualificacao Resultado { get; set; } = ResultadoQualificacao.EmQualificacao;
    public Guid? ResponsavelUsuarioId { get; set; }
    public string? ResponsavelNome { get; set; }
    /// <summary>Qualificado pela migração do histórico (planilha anterior ao sistema).</summary>
    public bool QualificadoPorHistorico { get; set; }
    public string? Observacao { get; set; }

    /// <summary>Validade automática: material +N meses (reconsultar a cada compra após o prazo); laboratório c)–f) +12 meses.</summary>
    public void CalcularValidade(ParametrosVigentes p)
    {
        if (LaboratorioEnquadramento == EnquadramentoLaboratorio.A_Acreditado)
            DataValidade = null; // acreditação CGCRE: controla pelo certificado nos documentos
        else if (LaboratorioEnquadramento >= EnquadramentoLaboratorio.C_Avaliado)
            DataValidade = DataQualificacao.AddMonths(p.Int(Parametros.RequalifLaboratorioMeses));
        else if (TipoFornecimento.HasFlag(TipoFornecimento.Material))
            DataValidade = DataQualificacao.AddMonths(p.Int(Parametros.ValidadeQualifMaterialMeses));
        else
            DataValidade = DataQualificacao.AddMonths(p.Int(Parametros.ValidadeQualifServicoMeses));
    }

    /// <summary>Validações de preenchimento (retorna lista de erros legíveis).</summary>
    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (FornecedorId == Guid.Empty) e.Add("Informe o fornecedor.");
        if (TipoFornecimento == TipoFornecimento.Nenhum) e.Add("Informe o tipo de fornecimento.");
        if (TipoFornecimento.HasFlag(TipoFornecimento.Material) && MaterialControladoId is null) e.Add("Informe o escopo – material controlado.");
        if (TipoFornecimento.HasFlag(TipoFornecimento.ServicoObra) && ServicoControladoId is null) e.Add("Informe o escopo – serviço (ITS).");
        if (string.IsNullOrWhiteSpace(CriterioQualificacao)) e.Add("Informe o critério de qualificação.");
        if (Psq == SituacaoPsq.NaoConforme && Resultado == ResultadoQualificacao.Qualificado)
            e.Add("Fornecedor 'Não conforme' no PSQ não pode ser qualificado (compra vedada).");
        if (Psq is SituacaoPsq.Qualificado or SituacaoPsq.NaoConforme && DataConsultaPsq is null) e.Add("Informe a data da consulta ao PSQ/INMETRO.");
        if (TipoFornecimento.HasFlag(TipoFornecimento.Laboratorio))
        {
            if (LaboratorioEnquadramento == EnquadramentoLaboratorio.NaoSeAplica) e.Add("Informe o enquadramento do laboratório.");
            if (LaboratorioEnquadramento >= EnquadramentoLaboratorio.C_Avaliado && string.IsNullOrWhiteSpace(AvaliadorLaboratorio))
                e.Add("Laboratório não acreditado: informe o avaliador (treinamento em NBR ISO/IEC 17025).");
        }
        if (ResponsavelUsuarioId is null && string.IsNullOrWhiteSpace(ResponsavelNome)) e.Add("Informe o responsável.");
        return e;
    }
}

/// <summary>T10 · Documento do fornecedor (vencimento controlado; vencido bloqueia compra conforme o tipo).</summary>
public class FornecedorDocumento : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public Guid TipoDocumentoExigidoId { get; set; }
    public TipoDocumentoExigido TipoDocumento { get; set; } = null!;
    /// <summary>Documento mensal: 1º dia do mês de competência.</summary>
    public DateOnly? Competencia { get; set; }
    public string? Numero { get; set; }
    public DateOnly? DataEmissao { get; set; }
    public DateOnly? DataValidade { get; set; }
    public bool Bloqueia { get; set; } = true;
    /// <summary>Caminho no Blob Storage (container "documentos"). Nunca URL pública.</summary>
    public string? ArquivoBlob { get; set; }
    public string? ArquivoNomeOriginal { get; set; }
    public string? ArquivoHashSha256 { get; set; }
    public StatusDocumento Status { get; set; } = StatusDocumento.PendenteEnvio;
    /// <summary>Enviado pelo portal do fornecedor: aguarda validação de Suprimentos.</summary>
    public bool EnviadoPeloPortal { get; set; }
    public string? ResponsavelEnvio { get; set; }
    public string? MotivoRecusa { get; set; }
    /// <summary>Itens conferidos na validação (CNPJ confere, datas conferem, legível, autenticidade consultada).</summary>
    public string? Conferencia { get; set; }
    public Guid? ValidadoPorUsuarioId { get; set; }
    public DateTimeOffset? ValidadoEm { get; set; }
    public string? Observacao { get; set; }
}

/// <summary>Link seguro do portal do fornecedor (tela 24): envio de documentos sem login, com prazo e registro de IP.</summary>
/// <summary>Grupo do catálogo atendido pelo fornecedor. Subgrupos vazios = todos.</summary>
public class FornecedorGrupo : Entidade
{
    public Guid FornecedorId { get; set; }
    public Guid CategoriaProdutoId { get; set; }
    /// <summary>Ids dos subgrupos atendidos, separados por vírgula (vazio = todos).</summary>
    public string? Subgrupos { get; set; }
    public bool Todos => string.IsNullOrWhiteSpace(Subgrupos);
}

/// <summary>Serviço controlado (ITS) executado pelo fornecedor.</summary>
public class FornecedorServico : Entidade
{
    public Guid FornecedorId { get; set; }
    public Guid ServicoControladoId { get; set; }
}

/// <summary>Documento exigido deste fornecedor (da matriz de exigências, ajustável no pré-cadastro).</summary>
public class FornecedorExigencia : Entidade
{
    public Guid FornecedorId { get; set; }
    public Guid TipoDocumentoExigidoId { get; set; }
    public bool Obrigatorio { get; set; } = true;
}

/// <summary>Matriz de exigências: quais documentos pedir para cada tipo de fornecedor (material × serviço, com ou sem funcionários).</summary>
public static class MatrizExigencias
{
    public static bool Aplica(TipoDocumentoExigido t, TipoFornecimento tipo, bool incluirFuncionarios) =>
        t.Ativo && t.AplicaA == AplicaA.Fornecedor && (t.ExigidoPara == TipoFornecimento.Nenhum || (t.ExigidoPara & tipo) != 0) && (!t.DeFuncionario || incluirFuncionarios);

    /// <summary>Competência que deve estar entregue hoje: a do mês anterior, a partir do dia limite (padrão 20).</summary>
    public static DateOnly? CompetenciaExigida(TipoDocumentoExigido t, DateOnly hoje)
    {
        if (t.Frequencia != FrequenciaDocumento.Mensal) return null;
        var limite = Math.Clamp(t.DiaLimite ?? 20, 1, 28);
        var mesAnterior = new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(-1);
        return hoje.Day > limite ? mesAnterior : mesAnterior.AddMonths(-1);
    }
}

public class LinkPortalFornecedor : EntidadeAuditavel
{
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public string Codigo { get; set; } = string.Empty;           // ex.: SOL-0231
    /// <summary>Token aleatório (256 bits, base64url). Só o hash é consultado na validação.</summary>
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ValidoAte { get; set; }
    public bool Revogado { get; set; }
    public DateTimeOffset? UltimoAcessoEm { get; set; }
    public string? UltimoAcessoIp { get; set; }
    public string? EmailDestino { get; set; }
    public string Motivo { get; set; } = "Cadastro inicial";
    public string? ItensSolicitados { get; set; }
    public string? EnviadoPor { get; set; }
    public StatusSolicitacaoDoc Status { get; set; } = StatusSolicitacaoDoc.Enviada;

    public bool Valido(DateTimeOffset agora) => !Revogado && !Excluido && agora <= ValidoAte;
}

/// <summary>
/// Funcionário próprio ou de terceiro. LGPD (minimização): guarda apenas o necessário para controle
/// de acesso à obra e treinamentos — sem dados de saúde, endereço residencial ou dados bancários.

/// <summary>Serviço controlado (lista de serviços controlados / ITS). Liga contrato, medição e FVS.</summary>
public class ServicoControlado : EntidadeAuditavel, IOrigemErp
{
    /// <summary>Ex.: ITS 03.02</summary>
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    /// <summary>Sigla curta exibida no mapa da torre (ex.: ALV, CTP, PIN).</summary>
    public string? Sigla { get; set; }
    /// <summary>Documento do SGQ (ITS) que descreve o serviço — enviado ao prestador junto com o contrato.</summary>
    public Guid? DocumentoItsId { get; set; }
    public bool ExigeFvsParaMedicao { get; set; } = true;
    public bool AvaliadoNaMedicao { get; set; } = true;
    public bool Ativo { get; set; } = true;
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }
}

/// <summary>Material controlado (lista de materiais controlados). Dispara especificação técnica, FVM e fornecedor qualificado.</summary>
public class MaterialControlado : EntidadeAuditavel, IOrigemErp
{
    /// <summary>Ex.: 01, 05.01</summary>
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    /// <summary>Grupo > Sub-grupo do catálogo de produtos que herda a marcação de controlado.</summary>
    public string? GrupoCatalogo { get; set; }
    /// <summary>Grupo do catálogo ligado (1 grupo = 1 material): produtos do grupo viram controlados automaticamente.</summary>
    public Guid? CategoriaProdutoId { get; set; }
    /// <summary>Modelo de especificação técnica (tabela de especificação e ensaio de materiais).</summary>
    public string? EspecificacaoModelo { get; set; }
    /// <summary>Documentos que o fornecedor deve apresentar (LAO, certificado do lote, laudo, FISPQ…) — sai impresso no pedido.</summary>
    public string? DocumentosExigidos { get; set; }
    public bool ExigeEspecificacao { get; set; } = true;
    public bool ExigeFvmParaAceite { get; set; } = true;
    public bool ExigeFornecedorQualificado { get; set; } = true;
    public bool ConsultarPsq { get; set; } = true;
    public string? SubGrupos { get; set; }
    public string? Ensaios { get; set; }
    public string ConstaPsq { get; set; } = "Verificar";
    /// <summary>Norma / especificação de referência do material (ex.: ABNT NBR 7480:2024).</summary>
    public string? Norma { get; set; }
    /// <summary>Formas de qualificação aceitas para o fornecedor (ex.: "A ou B": PSQ, certificação, histórico, ensaio/visita).</summary>
    public string? FormasQualificacao { get; set; }
    /// <summary>Critério / observação da qualificação do fornecedor deste material.</summary>
    public string? CriterioQualificacao { get; set; }
    /// <summary>Armazenamento e preservação na obra (procedimento de inspeção e armazenamento).</summary>
    public string? Armazenamento { get; set; }
    /// <summary>Serviços (ITS/procedimentos) que aplicam o material, separados por " · ".</summary>
    public string? ServicosQueUtilizam { get; set; }
    public bool Ativo { get; set; } = true;
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }
}

/// <summary>
/// Marca (fabricante comercial). Opcional na requisição; obrigatória do item cotado em diante (cotação, pedido, recebimento),
/// para comparar preço por especificação + marca.
/// </summary>
public class Marca : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    public string? Fabricante { get; set; }
    public string? Observacao { get; set; }
    public bool Ativa { get; set; } = true;

    /// <summary>Chave de comparação: sem acento, maiúsculas, espaços simples.</summary>
    public static string Chave(string? nome) =>
        string.Join(' ', new string((nome ?? "").Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray())
            .ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}

/// <summary>Produto / genérico do catálogo de compras (herda "controlado" do material controlado vinculado).</summary>
public class Produto : EntidadeAuditavel, IOrigemErp
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Unidade { get; set; } = "UN";
    public string? Grupo { get; set; }
    public string? SubGrupo { get; set; }
    /// <summary>Natureza do custo (material, equipamento, mão de obra, serviço, frete, outros) — curva ABC e custo por tipo.</summary>
    public TipoCusto? TipoCusto { get; set; }
    /// <summary>Descrição completa (especificação resumida). A "genérica" é o Nome — nunca com marca.</summary>
    public string? DescricaoCompleta { get; set; }
    /// <summary>NCM (8 dígitos) — casa o produto com os itens da NF-e.</summary>
    public string? Ncm { get; set; }
    public string? Marca { get; set; }
    /// <summary>Referência / código do fabricante.</summary>
    public string? Referencia { get; set; }
    public string? Ean { get; set; }
    /// <summary>Quantidade por embalagem (caixa, pacote, fardo).</summary>
    public decimal? QuantidadePorEmbalagem { get; set; }
    public string? OrigemSistema { get; set; }
    /// <summary>Código antes da última recodificação (provisório → definitivo, ou reclassificação antes do 1º uso).</summary>
    public string? CodigoAnterior { get; set; }
    /// <summary>Grupo "A Cadastrar" (ou vazio) = produto criado pela NF/importação que ainda precisa ser classificado.</summary>
    public bool AClassificar => string.IsNullOrWhiteSpace(Grupo) || Grupo.Equals("A Cadastrar", StringComparison.OrdinalIgnoreCase);
    /// <summary>Prazo de fornecimento (lead time) usado no cronograma de suprimentos.</summary>
    public int LeadTimeDias { get; set; }
    /// <summary>Preço de referência (tabela interna/orçamento da obra) — base da "economia vs. referência" na cotação.</summary>
    public decimal? PrecoReferencia { get; set; }
    public Guid? MaterialControladoId { get; set; }
    public MaterialControlado? MaterialControlado { get; set; }
    public bool Ativo { get; set; } = true;
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }
}

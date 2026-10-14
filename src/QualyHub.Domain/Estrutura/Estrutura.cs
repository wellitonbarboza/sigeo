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
    /// <summary>Falsa = construtora suspensa pela plataforma: ninguém dela entra; os dados ficam guardados.</summary>
    public bool Ativa { get; set; } = true;
    public DateTimeOffset? CriadaEm { get; set; }
    /// <summary>Modelo de estrutura de locais (ver <see cref="EstruturaLocais"/>): Condominio, Edificio, Loteamento ou Personalizado.</summary>
    public string? ModeloEstrutura { get; set; }
    /// <summary>Nomes dos níveis separados por ";": condomínio;torre;pavimento;unidade;cômodo;prefixo da unidade.</summary>
    public string? NomesNiveis { get; set; }
    /// <summary>Telas que a plataforma não liberou para a construtora (rotas separadas por ";"). Vazio = tudo liberado.</summary>
    public string? TelasBloqueadas { get; set; }
}

/// <summary>
/// Como a construtora chama os níveis de local. O gerador continua Condomínio (dono do CNPJ/SPE) › Torre › Pavimento › Unidade › Cômodo;
/// cada construtora dá o nome que usa (ex.: loteamento = Loteamento › Quadra › Nível › Casa › Cômodo, prefixo "Casa").
/// </summary>
public sealed record EstruturaLocais(string Modelo, string Condominio, string Torre, string Pavimento, string Unidade, string Comodo, string PrefixoUnidade)
{
    public static readonly EstruturaLocais Condominios = new("Condominio", "Condomínio", "Torre", "Pavimento", "Apartamento", "Cômodo", "Apto");
    public static readonly EstruturaLocais Edificio = new("Edificio", "Empreendimento", "Torre", "Pavimento", "Apartamento", "Cômodo", "Apto");
    public static readonly EstruturaLocais Loteamento = new("Loteamento", "Loteamento", "Quadra", "Nível", "Casa", "Cômodo", "Casa");
    public static readonly IReadOnlyList<(EstruturaLocais Modelo, string Titulo, string Uso)> Modelos =
    [
        (Condominios, "Condomínios com SPE (tipo Ecoparque)", "Vários condomínios, cada um com CNPJ próprio (SPE) e torres geradas por tipologia."),
        (Edificio, "Edifício", "Um empreendimento (uma SPE) com uma ou mais torres."),
        (Loteamento, "Loteamento / casas", "Quadras com casas ou lotes; a unidade é a casa."),
    ];

    public string[] Niveis => [Condominio, Torre, Pavimento, Unidade, Comodo];

    public static EstruturaLocais De(string? modelo, string? nomes)
    {
        var baseM = Modelos.Select(m => m.Modelo).FirstOrDefault(m => m.Modelo == modelo) ?? Condominios;
        var p = (nomes ?? "").Split(';');
        string N(int i, string padrao) => p.Length > i && !string.IsNullOrWhiteSpace(p[i]) ? p[i].Trim() : padrao;
        return new(modelo is { Length: > 0 } ? modelo : baseM.Modelo, N(0, baseM.Condominio), N(1, baseM.Torre), N(2, baseM.Pavimento), N(3, baseM.Unidade), N(4, baseM.Comodo), N(5, baseM.PrefixoUnidade));
    }

    public string Serializar() => string.Join(";", Condominio, Torre, Pavimento, Unidade, Comodo, PrefixoUnidade);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        foreach (var (n, nome) in new[] { (Condominio, "SPE"), (Torre, "bloco/torre"), (Pavimento, "pavimento"), (Unidade, "unidade"), (Comodo, "cômodo") })
            if (string.IsNullOrWhiteSpace(n) || n.Length > 40 || n.Contains(';')) e.Add($"Nome do nível {nome}: de 1 a 40 caracteres, sem \";\".");
        if (string.IsNullOrWhiteSpace(PrefixoUnidade) || PrefixoUnidade.Length > 12 || PrefixoUnidade.Contains(';')) e.Add("Prefixo da unidade: de 1 a 12 caracteres.");
        return e;
    }

    /// <summary>Nome exibido para o tipo interno do local (Condomínio, Torre, Pavimento, Apartamento, Cômodo).</summary>
    public string Rotulo(string? tipo) => tipo switch
    {
        "Condomínio" => Condominio, "Torre" => Torre, "Pavimento" => Pavimento, "Apartamento" => Unidade, "Cômodo" => Comodo, _ => tipo ?? ""
    };
}

/// <summary>Quem é dono do custo da obra: uma SPE por condomínio (padrão) ou a própria construtora (obra própria, sem SPE).</summary>
public enum DonoCustoObra { Spe = 1, Construtora = 2 }

public class Obra : EntidadeAuditavel, IOrigemErp
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Endereco { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataPrevistaTermino { get; set; }
    public string? Observacoes { get; set; }
    public string? EnderecoEntrega { get; set; }
    public Guid? EngenheiroId { get; set; }
    public Guid? AdministrativoId { get; set; }
    public Guid? AlmoxarifeId { get; set; }
    public Guid? PqoDocumentoId { get; set; }
    public StatusObra Status { get; set; } = StatusObra.EmExecucao;
    public bool Ativa { get; set; } = true;
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }
    /// <summary>Obra própria: o custo fica na construtora (Matriz/Filial), sem SPE. Quando a SPE for constituída, os locais e os documentos em aberto passam para ela.</summary>
    public DonoCustoObra DonoCusto { get; set; } = DonoCustoObra.Spe;
    /// <summary>Estabelecimento (Matriz/Filial) dono do custo na obra própria — vira o "Faturar para" padrão.</summary>
    public Guid? EstabelecimentoDonoId { get; set; }

    /// <summary>
    /// Data de corte do sistema: antes dela os dados vêm do ERP como histórico (só leitura, sem aprovações nem registros de qualidade);
    /// a partir dela tudo é lançado no QualyHub com os fluxos completos.
    /// </summary>
    public DateOnly? DataCorteSistema { get; set; }

    public bool ObraPropria => DonoCusto == DonoCustoObra.Construtora;

    public ICollection<Local> Locais { get; set; } = [];
}

/// <summary>
/// Hierarquia física da obra: Condomínio (SPE) › Torre › Pavimento › Apartamento › Cômodo, com áreas comuns
/// no pavimento, na torre e no condomínio. Gerada pelas tipologias; usada no orçamento, nas FVS, na requisição e na medição.
/// </summary>
public class Local : EntidadeDaObra
{
    public Guid? LocalPaiId { get; set; }
    public Local? LocalPai { get; set; }
    public string Tipo { get; set; } = "Pavimento"; // Condomínio, Torre, Pavimento, Apartamento, Cômodo, Área comum
    public string Nome { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public decimal? Quantidade { get; set; }
    public string? Unidade { get; set; }
    public string QrToken { get; set; } = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();
    /// <summary>SPE dona (prédio e tudo abaixo dele). Custo direto do local vai para ela.</summary>
    public Guid? SpeId { get; set; }
    /// <summary>Unidade: tipologia e área privativa (cópia na geração, pode ser ajustada).</summary>
    public Guid? TipologiaId { get; set; }
    public decimal? AreaPrivativa { get; set; }
    /// <summary>Fração ideal do apartamento no condomínio (0–1). Oficial = memorial de incorporação.</summary>
    public decimal? FracaoIdeal { get; set; }
    /// <summary>Cômodo/área comum: categoria da lista padrão (Sala, Dormitório, BWC…).</summary>
    public string? Categoria { get; set; }
    /// <summary>Ambiente-modelo da tipologia que gerou o local (as medidas são copiadas e podem ser sincronizadas).</summary>
    public Guid? AmbienteModeloId { get; set; }
    public decimal? AreaPiso { get; set; }
    public decimal? Perimetro { get; set; }
    public decimal? PeDireito { get; set; }
    /// <summary>Área de parede líquida (perímetro × pé-direito − vãos).</summary>
    public decimal? AreaParede { get; set; }
    public ICollection<Local> Filhos { get; set; } = [];
}

/// <summary>
/// Estabelecimento (CNPJ): matriz, filiais (construtora, pré-moldados, móveis) e SPEs. Eixo "quem fatura" de todo lançamento.
/// Quem fatura os materiais (padrão) e o almoxarifado central são marcados aqui.
/// </summary>
public class Estabelecimento : EntidadeAuditavel, IOrigemErp
{
    public string Codigo { get; set; } = string.Empty;
    public TipoEstabelecimento Tipo { get; set; } = TipoEstabelecimento.Filial;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    /// <summary>Raiz do CNPJ (8 dígitos): mesma raiz = mesma pessoa jurídica (transferência, não venda).</summary>
    public string Raiz { get; set; } = string.Empty;
    public string? InscricaoEstadual { get; set; }
    public string? Endereco { get; set; }
    public string? Atividade { get; set; }
    /// <summary>SPE correspondente (quando Tipo = SPE).</summary>
    public Guid? SpeId { get; set; }
    /// <summary>Padrão do "Faturar para" nas compras de material (ex.: fábrica de pré-moldados).</summary>
    public bool FaturaMateriais { get; set; }
    /// <summary>Recebe as compras no almoxarifado central (FVM) e transfere para as obras/SPEs.</summary>
    public bool AlmoxarifadoCentral { get; set; }
    public string? EnderecoEntrega { get; set; }
    public bool Ativo { get; set; } = true;
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }

    public string NomeExibicao => string.IsNullOrWhiteSpace(NomeFantasia) ? RazaoSocial : NomeFantasia!;
}

/// <summary>Fluxo entre CNPJs (ex.: Fábrica → SPE, venda a custo padrão). Base para o mapa do ecossistema e para a contabilidade.</summary>
public class FluxoEstabelecimento : EntidadeAuditavel
{
    public Guid OrigemId { get; set; }
    public Guid DestinoId { get; set; }
    public TipoFluxo Tipo { get; set; } = TipoFluxo.Transferencia;
    /// <summary>Base do valor: custo padrão, custo real, % sobre custo, taxa fixa…</summary>
    public string Base { get; set; } = "Custo padrão";
    public string Situacao { get; set; } = "A definir";
    public string? Descricao { get; set; }
}

/// <summary>Centro de custo (administração, estoque, obra/etapa). Pode apontar para uma obra.</summary>
public class CentroCusto : EntidadeAuditavel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    /// <summary>Centro de custo pai (plano único em árvore com níveis livres).</summary>
    public Guid? PaiId { get; set; }
    /// <summary>Estabelecimentos onde pode ser usado (ids separados por vírgula; vazio = todos).</summary>
    public string? EstabelecimentosPermitidos { get; set; }
    public string? CodigoErp { get; set; }
    public Guid? ObraId { get; set; }
    public decimal? Orcamento { get; set; }
    public bool Ativo { get; set; } = true;
}

/// <summary>Usuário do sistema (login e senha próprios do QualyHub).</summary>
public class Usuario : EntidadeAuditavel
{
    /// <summary>Login de acesso (minúsculo, único no sistema). A senha fica em <see cref="CredencialUsuario"/>.</summary>
    public string Login { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    /// <summary>Função exibida e usada em distribuição/leitura obrigatória (ex.: Engenheiro da Obra, Comprador).</summary>
    public string? Funcao { get; set; }
    public bool AcessoTodasObras { get; set; }
    /// <summary>Administrador e Diretoria veem todas as obras; os demais perfis, só as liberadas (ou todas, se marcado).</summary>
    public static bool PerfilVeTodas(PerfilUsuario p) => p is PerfilUsuario.Administrador or PerfilUsuario.Diretoria;
    public bool VeTodasObras => AcessoTodasObras || PerfilVeTodas(Perfil);
    public bool Ativo { get; set; } = true;
    /// <summary>
    /// Acesso a outra construtora: este cadastro (nesta empresa, com perfil e obras próprios) usa o login e a senha da conta
    /// <see cref="ContaId"/> (o usuário principal, em outra construtora). Vínculo não tem login nem senha próprios.
    /// </summary>
    public Guid? ContaId { get; set; }
    /// <summary>Administra a plataforma (construtoras, usuários entre construtoras, auditoria). Só na conta principal.</summary>
    public bool AdministradorPlataforma { get; set; }
    /// <summary>Acesso de suporte da plataforma: vale até esta data/hora (depois some da lista de construtoras).</summary>
    public DateTimeOffset? SuporteAte { get; set; }
    /// <summary>Perfil de acesso (o que vê e faz em cada tela). Vazio = o perfil padrão do <see cref="Perfil"/>.</summary>
    public Guid? PerfilAcessoId { get; set; }
    /// <summary>Exceções só deste usuário sobre o perfil de acesso: "rota=+A-E;…" (+ libera, − bloqueia).</summary>
    public string? ExcecoesAcesso { get; set; }
    public ICollection<UsuarioObra> Obras { get; set; } = [];
    /// <summary>Limites de aprovação por tipo (e opcionalmente por obra) — "aprova até R$ X".</summary>
    public ICollection<Aprovacoes.LimiteAprovacao> LimitesAprovacao { get; set; } = [];
}

/// <summary>
/// Senha do usuário (hash PBKDF2, nunca a senha em si), bloqueio por tentativas e troca obrigatória.
/// Separada do cadastro para que editar o usuário nunca toque na senha.
/// </summary>
public class CredencialUsuario : EntidadeAuditavel
{
    public Guid UsuarioId { get; set; }
    public string SenhaHash { get; set; } = string.Empty;
    public bool TrocarSenhaNoProximoAcesso { get; set; } = true;
    public int TentativasFalhas { get; set; }
    public DateTimeOffset? BloqueadoAte { get; set; }
    public DateTimeOffset? UltimoAcessoEm { get; set; }
    public DateTimeOffset? SenhaAlteradaEm { get; set; }
}

/// <summary>Regras de login e senha (sem dependência de infraestrutura).</summary>
public static class PoliticaSenha
{
    public const int TamanhoMinimo = 6;
    public const int TamanhoMaximo = 128;
    public const int MaxTentativas = 5;
    public static readonly TimeSpan TempoBloqueio = TimeSpan.FromMinutes(15);

    public static string NormalizarLogin(string? login) => (login ?? "").Trim().ToLowerInvariant();

    public static IReadOnlyList<string> ValidarLogin(string? login)
    {
        var l = NormalizarLogin(login);
        var e = new List<string>();
        if (l.Length is < 3 or > 60) e.Add("Login deve ter de 3 a 60 caracteres.");
        else if (!l.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' or '@')) e.Add("Login aceita letras sem acento, números, ponto, hífen, sublinhado e @");
        return e;
    }

    /// <summary>Regra definida pela empresa (28/09): só o tamanho mínimo; composição livre. O bloqueio por tentativas continua.</summary>
    public static IReadOnlyList<string> Validar(string? senha, string? login = null, string? nome = null)
    {
        var e = new List<string>();
        senha ??= "";
        if (senha.Length < TamanhoMinimo) e.Add($"A senha precisa ter pelo menos {TamanhoMinimo} caracteres.");
        if (senha.Length > TamanhoMaximo) e.Add($"A senha pode ter no máximo {TamanhoMaximo} caracteres.");
        return e;
    }
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
    public string? Condicao { get; set; }
    public bool EngenheiroDaObra { get; set; }
    public Guid? SubstitutoUsuarioId { get; set; }
    public int? PrazoHoras { get; set; }
    public PerfilUsuario? EscalonaPara { get; set; }
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

/// <summary>T27 · Regra de notificação por evento (quem recebe, quando, por qual canal).</summary>
public class RegraNotificacao : EntidadeAuditavel
{
    public string Evento { get; set; } = string.Empty;
    public string? Antecedencia { get; set; }
    public string? Repeticao { get; set; }
    public string Destinatarios { get; set; } = string.Empty;
    public CanalNotificacao Canal { get; set; } = CanalNotificacao.Pendencias;
    public string ModeloMensagem { get; set; } = string.Empty;
    public bool Ativa { get; set; } = true;
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
    public const string PesoIdfPrazo = "AVAL.IDF_PESO_PRAZO";
    public const string PesoIdfQualidade = "AVAL.IDF_PESO_QUALIDADE";
    public const string PesoIdfDocumentacao = "AVAL.IDF_PESO_DOCUMENTACAO";
    public const string IdfPreferencial = "AVAL.IDF_PREFERENCIAL";
    public const string IdfMinimo = "AVAL.IDF_MINIMO";
    public const string AlertaVencimentoDocDias = "DOC.ALERTA_VENCIMENTO_DIAS";
    public const string PeriodicidadeAnaliseCriticaMeses = "SGQ.PERIODICIDADE_ANALISE_MESES";
    public const string AlertaAnaliseCriticaDias = "SGQ.ALERTA_ANALISE_DIAS";
    public const string PrazoLeituraDias = "SGQ.PRAZO_LEITURA_DIAS";
    public const string PrazoRecolhimentoDias = "SGQ.PRAZO_RECOLHIMENTO_DIAS";
    public const string PrazoCheckoutDias = "SGQ.PRAZO_CHECKOUT_DIAS";
    public const string VerificacaoExternosMeses = "SGQ.VERIFICACAO_EXTERNOS_MESES";
    public const string RetencaoPadraoPerc = "CTR.RETENCAO_PADRAO_PERC";
    public const string LimiteAditivoPerc = "CTR.LIMITE_ADITIVO_PERC";
    public const string ValidadeLinkPortalDias = "SUP.VALIDADE_LINK_PORTAL_DIAS";
    public const string PrazoValidarDocDias = "SUP.PRAZO_VALIDAR_DOC_DIAS";
    public const string MetaFvs1aInspecaoPerc = "QOB.META_FVS_1A_PERC";
    public const string PrazoCorrecaoFvsDias = "QOB.PRAZO_CORRECAO_FVS_DIAS";
    public const string PrazoTratarRncDias = "QOB.PRAZO_RNC_DIAS";
    public const string PrazoEficaciaAcDias = "QOB.PRAZO_EFICACIA_AC_DIAS";
    public const string AlertaCalibracaoDias = "QOB.ALERTA_CALIBRACAO_DIAS";
    public const string PeriodicidadeCalibracaoMeses = "QOB.PERIODICIDADE_CALIBRACAO_MESES";
    public const string PeriodicidadeVerificacaoMeses = "QOB.PERIODICIDADE_VERIFICACAO_MESES";
    public const string AuditoriasPorAno = "QOB.AUDITORIAS_ANO";
    public const string ReincidenciasAc = "QOB.REINCIDENCIAS_AC";
    // Etiqueta de equipamento (folha A4 de etiquetas adesivas, impressora comum)
    public const string EtiquetaColunas = "ETQ.COLUNAS";
    public const string EtiquetaLinhas = "ETQ.LINHAS";
    public const string EtiquetaLarguraMm = "ETQ.LARGURA_MM";
    public const string EtiquetaAlturaMm = "ETQ.ALTURA_MM";
    public const string EtiquetaMargemSuperiorMm = "ETQ.MARGEM_SUPERIOR_MM";
    public const string EtiquetaMargemEsquerdaMm = "ETQ.MARGEM_ESQUERDA_MM";
    public const string EtiquetaEspacoHorizontalMm = "ETQ.ESPACO_H_MM";
    public const string EtiquetaEspacoVerticalMm = "ETQ.ESPACO_V_MM";
    // Estoque e equipamentos
    public const string ConsumoMinimoDias = "EST.CONSUMO_MINIMO_DIAS";
    public const string AlertaDevolucaoLocacaoDias = "EQP.ALERTA_DEVOLUCAO_DIAS";
    public const string AlertaVencimentoSstDias = "SST.ALERTA_VENCIMENTO_DIAS";
    public const string AlertaCaDias = "SST.ALERTA_CA_DIAS";
    public const string MetaAptosPerc = "SST.META_APTOS_PERC";
    public const string MetaConformidadeInspecaoPerc = "SST.META_CONFORMIDADE_PERC";
    public const string FrequenciaInspecaoDias = "SST.FREQUENCIA_INSPECAO_DIAS";

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
            [PeriodicidadeAvaliacaoMeses] = (6m, "meses", "Periodicidade da avaliação consolidada de fornecedores (3 = trimestral, 6 = semestral)", "Avaliação do prestador"),
            [PesoIdfPrazo] = (0.4m, "", "IDF da entrega – peso do prazo (nota 0 a 10)", "Avaliação do prestador"),
            [PesoIdfQualidade] = (0.4m, "", "IDF da entrega – peso da qualidade (nota 0 a 10)", "Avaliação do prestador"),
            [PesoIdfDocumentacao] = (0.2m, "", "IDF da entrega – peso da documentação/atendimento (nota 0 a 10)", "Avaliação do prestador"),
            [IdfPreferencial] = (8m, "", "IDF a partir do qual o fornecedor é preferencial", "Avaliação do prestador"),
            [IdfMinimo] = (6m, "", "IDF abaixo disto: critério reprovado na entrega e fornecedor desqualificado na consolidação", "Avaliação do prestador"),
            [AlertaVencimentoDocDias] = (30m, "dias", "Alerta de vencimento de documento de fornecedor", "Suprimentos"),
            [RetencaoPadraoPerc] = (5m, "%", "Retenção contratual padrão", "Contratos"),
            [PeriodicidadeAnaliseCriticaMeses] = (12m, "meses", "Periodicidade padrão de análise crítica de documento", "Documentos do SGQ"),
            [AlertaAnaliseCriticaDias] = (30m, "dias", "Alerta antes do vencimento da análise crítica", "Documentos do SGQ"),
            [PrazoLeituraDias] = (7m, "dias", "Prazo para confirmação de leitura", "Documentos do SGQ"),
            [PrazoRecolhimentoDias] = (3m, "dias", "Prazo para recolher cópia obsoleta", "Documentos do SGQ"),
            [PrazoCheckoutDias] = (5m, "dias", "Prazo máximo de check-out", "Documentos do SGQ"),
            [VerificacaoExternosMeses] = (6m, "meses", "Verificação de atualização de documentos externos", "Documentos do SGQ"),
            [LimiteAditivoPerc] = (25m, "%", "Alerta quando aditivos acumulados passam deste % do contrato", "Contratos"),
            [ValidadeLinkPortalDias] = (15m, "dias", "Validade do link do portal do fornecedor", "Suprimentos"),
            [PrazoValidarDocDias] = (2m, "dias úteis", "Prazo para validar documento enviado pelo portal", "Suprimentos"),
            [MetaFvs1aInspecaoPerc] = (85m, "%", "Meta de FVS aprovadas na 1ª inspeção", "Qualidade da obra"),
            [PrazoCorrecaoFvsDias] = (3m, "dias", "Prazo padrão para correção de FVS reprovada", "Qualidade da obra"),
            [PrazoTratarRncDias] = (7m, "dias", "Prazo padrão para tratar RNC", "Qualidade da obra"),
            [PrazoEficaciaAcDias] = (30m, "dias", "Prazo para verificar eficácia da ação corretiva", "Qualidade da obra"),
            [AlertaCalibracaoDias] = (30m, "dias", "Alerta de calibração a vencer", "Qualidade da obra"),
            [PeriodicidadeCalibracaoMeses] = (12m, "meses", "Periodicidade padrão – calibração externa", "Qualidade da obra"),
            [PeriodicidadeVerificacaoMeses] = (6m, "meses", "Periodicidade padrão – verificação interna", "Qualidade da obra"),
            [AuditoriasPorAno] = (1m, "un", "Auditorias internas por ano (mínimo)", "Qualidade da obra"),
            [ReincidenciasAc] = (2m, "RNC", "Reincidências (mesmo serviço/prestador em 90 dias) que obrigam ação corretiva", "Qualidade da obra"),
            [AlertaVencimentoSstDias] = (30m, "dias", "Alerta de vencimento de documento/ASO/treinamento", "Segurança e pessoas"),
            [AlertaCaDias] = (60m, "dias", "Alerta de validade de CA de EPI", "Segurança e pessoas"),
            [MetaAptosPerc] = (95m, "%", "Meta de colaboradores aptos", "Segurança e pessoas"),
            [MetaConformidadeInspecaoPerc] = (90m, "%", "Meta de conformidade das inspeções de segurança", "Segurança e pessoas"),
            [FrequenciaInspecaoDias] = (7m, "dias", "Frequência mínima de inspeção geral do canteiro", "Segurança e pessoas"),
            [EtiquetaColunas] = (3m, "un", "Etiquetas por linha na folha A4", "Etiquetas"),
            [EtiquetaLinhas] = (8m, "un", "Linhas de etiquetas na folha A4", "Etiquetas"),
            [EtiquetaLarguraMm] = (63.5m, "mm", "Largura da etiqueta", "Etiquetas"),
            [EtiquetaAlturaMm] = (33.9m, "mm", "Altura da etiqueta", "Etiquetas"),
            [EtiquetaMargemSuperiorMm] = (12.9m, "mm", "Margem superior da folha", "Etiquetas"),
            [EtiquetaMargemEsquerdaMm] = (7.2m, "mm", "Margem esquerda da folha", "Etiquetas"),
            [EtiquetaEspacoHorizontalMm] = (2.5m, "mm", "Espaço entre etiquetas (horizontal)", "Etiquetas"),
            [EtiquetaEspacoVerticalMm] = (0m, "mm", "Espaço entre etiquetas (vertical)", "Etiquetas"),
            [ConsumoMinimoDias] = (60m, "dias", "Período de consumo usado na sugestão de mínimos", "Estoque"),
            [AlertaDevolucaoLocacaoDias] = (5m, "dias", "Alerta antes da data de devolução do equipamento locado", "Estoque"),
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

/// <summary>Registro das ações da plataforma (criar/suspender construtora, dar acesso, suporte). Fora do filtro de empresa.</summary>
public class RegistroPlataforma : Entidade
{
    public DateTimeOffset Quando { get; set; }
    public string Quem { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public Guid? EmpresaId { get; set; }
    public string? Empresa { get; set; }
    public string? Detalhe { get; set; }
}

/// <summary>
/// Perfil de acesso da construtora: por tela, as ações liberadas ("rota=VIEX;…"). O padrão de cada perfil de usuário
/// (<see cref="Base"/>) nasce igual ao padrão do sistema e pode ser ajustado; perfis criados (cópias) não têm base.
/// </summary>
public class PerfilAcesso : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    /// <summary>Perfil de usuário de que este é o padrão (usuários sem perfil de acesso escolhido caem aqui).</summary>
    public PerfilUsuario? Base { get; set; }
    public string Regras { get; set; } = string.Empty;
}

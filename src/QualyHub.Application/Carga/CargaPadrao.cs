using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Documentos;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Sst;

namespace QualyHub.Application.Carga;

/// <summary>
/// Carga inicial GENÉRICA (padrão SiAC/PBQP-H) — sem dados de nenhuma empresa. Tudo é editável nas telas.
/// Idempotente: só insere o que ainda não existe para a empresa.
/// </summary>
public static class CargaPadrao
{
    public static readonly string[] Atividades =
    [
        "Serviços preliminares", "Movimento de terra", "Fundações", "Estrutura – Fôrmas", "Estrutura – Armação", "Estrutura – Concretagem",
        "Estrutura pré-fabricada", "Alvenaria / Vedação", "Drywall", "Impermeabilização", "Revestimento de parede", "Revestimento de piso",
        "Contrapiso", "Forro", "Cobertura", "Esquadrias", "Portas", "Pintura", "Instalações elétricas", "Instalações hidrossanitárias",
        "Instalação de gás", "Prevenção e combate a incêndio", "Louças e metais", "Paisagismo", "Limpeza final", "Administração da obra"
    ];

    public static readonly string[] CategoriasServico =
        ["Serviço de obra", "Laboratório", "Serviço especializado de engenharia", "Consultoria", "Locação de equipamento"];

    public static readonly string[] CriteriosQualificacao =
    [
        "Histórico de fornecimento", "PSQ SiMaC/PBQP-H qualificado", "Certificado SBAC/INMETRO (não escopo PSQ)", "Análise de documentos",
        "Visita técnica", "Análise de amostra", "Indicação", "Laboratório a)–f)"
    ];

    public static readonly string[] Processos =
        ["Diretoria", "Manual / Política / Objetivos", "Engenharia (ITS)", "Suprimentos", "Qualidade", "Recursos Humanos", "Planejamento", "Orçamento", "Projetos", "Obra"];

    public static readonly string[] TiposDocumentoSgq =
    [
        "MQ – Manual da Qualidade", "PO – Política/Objetivos", "P – Procedimento", "DT – Diretriz Técnica", "ITS – Instrução Técnica de Serviço",
        "F / FORM – Formulário", "FVS / FVM – Ficha de verificação", "PQO – Plano da Qualidade da Obra", "DE – Documento externo"
    ];

    public static readonly string[] Emissores =
        ["ABNT", "INMETRO", "Ministério das Cidades (PBQP-H)", "Prefeitura", "Corpo de Bombeiros", "Concessionária", "Cliente / Incorporadora", "Projetista"];

    public static readonly string[] CondicoesPagamento = ["À vista", "28 dias", "30/60 dias", "30/60/90 dias", "Contra medição"];

    public static readonly string[] TiposAgrupamento = ["Condomínio", "Quadra", "Loteamento", "Gleba", "Torre única"];

    /// <summary>Lista padrão de cômodos e áreas comuns (categoria do ambiente da tipologia).</summary>
    public static readonly string[] TiposComodo =
    [
        "Sala de estar", "Sala de jantar", "Cozinha", "Área de serviço", "Dormitório", "Suíte", "BWC", "Lavabo", "Circulação", "Sacada", "Varanda / terraço",
        "Hall / circulação comum", "Escada", "Elevador", "Shaft", "Barrilete / reservatório", "Cobertura / telhado", "Fachada", "Guarita / portaria",
        "Salão de festas", "Área de lazer", "Piscina", "Estacionamento", "Vias e calçadas", "Muro / gradil", "Redes externas", "Lixeira", "Central de gás"
    ];

    /// <summary>Etapas da EAP para agrupar os serviços do orçamento.</summary>
    public static readonly string[] GruposServico =
    [
        "Serviços preliminares", "Infraestrutura / fundações", "Estrutura / pré-moldados", "Vedações", "Instalações elétricas", "Instalações hidrossanitárias",
        "Impermeabilização", "Revestimentos de piso", "Revestimentos de parede", "Forros", "Esquadrias", "Pintura", "Louças e metais", "Cobertura",
        "Áreas comuns e urbanização", "Kit de entrega (móveis e eletros)", "Limpeza e entrega"
    ];

    public static readonly string[] CategoriasDocumento = ["Empresa", "Fiscal", "Trabalhista / previdenciário", "SST", "RH (funcionários)", "Técnico", "Ambiental"];

    public static readonly string[] TiposTarefa =
        ["Compras", "Contratos", "Qualidade", "Segurança do trabalho", "Documentos do SGQ", "Obra", "Financeiro", "Outros"];

    public static readonly string[] Funcoes =
    [
        "Diretor Técnico", "Diretoria", "Engenheiro da Obra", "Mestre de Obras", "Administrativo de Obra", "Comprador", "Almoxarife",
        "Coordenador da Qualidade", "Financeiro", "Recursos Humanos"
    ];

    /// <summary>Tipos de documento de fornecedor: (nome, bloqueia, por entrega, validade padrão em dias, exigido para).</summary>
    public static readonly (string Nome, bool Bloqueia, bool PorEntrega, int? ValidadeDias, TipoFornecimento Para)[] TiposDocumentoFornecedor =
    [
        ("CND / regularidade fiscal", true, false, 180, TipoFornecimento.Nenhum),
        ("Contrato social", false, false, null, TipoFornecimento.Nenhum),
        ("LAO – Licença Ambiental de Operação", true, false, 365, TipoFornecimento.Material),
        ("Certificado de Qualidade PBQP-H", false, false, 365, TipoFornecimento.Material),
        ("Laudo técnico / ensaio do produto", false, false, 365, TipoFornecimento.Material),
        ("FDS / FISPQ", false, false, null, TipoFornecimento.Material),
        ("Certificado do lote (aço)", false, true, null, TipoFornecimento.Material),
        ("ART", false, false, null, TipoFornecimento.ServicoObra | TipoFornecimento.ServicoEspecializadoEngenharia),
        ("Certificado de calibração", true, false, 365, TipoFornecimento.Laboratorio),
        ("Acreditação CGCRE", true, false, 365, TipoFornecimento.Laboratorio),
        ("Certificado ISO 9001", false, false, 365, TipoFornecimento.Nenhum),
        ("MTR / CDF (resíduos)", false, true, null, TipoFornecimento.Nenhum),
        ("Documentos de empresa terceirizada (RH / segurança)", true, false, 30, TipoFornecimento.ServicoObra),
    ];

    /// <summary>
    /// Matriz-modelo de documentos de empresa prestadora de serviço (legislação trabalhista, previdenciária e NRs — VALIDAR com RH/SST).
    /// Mensais vencem no dia 20 (competência anterior); documentos de funcionários só entram quando o pré-cadastro inclui funcionários.
    /// </summary>
    public static readonly (string Nome, string Categoria, FrequenciaDocumento Freq, int? Validade, TipoFornecimento Para, bool Funcionario, bool Bloqueia, bool Sensivel, string Fonte)[] DocumentosPrestador =
    [
        ("Cartão CNPJ", "Empresa", FrequenciaDocumento.NoCadastro, null, TipoFornecimento.Nenhum, false, false, false, "Receita Federal"),
        ("Contrato social e alterações", "Empresa", FrequenciaDocumento.NoCadastro, null, TipoFornecimento.ServicoObra, false, false, false, "Junta Comercial"),
        ("Documentos dos sócios (RG/CPF)", "Empresa", FrequenciaDocumento.NoCadastro, null, TipoFornecimento.ServicoObra, false, false, true, "Cadastro"),
        ("Alvará de funcionamento", "Empresa", FrequenciaDocumento.Validade, 365, TipoFornecimento.ServicoObra, false, false, false, "Prefeitura"),
        ("CND federal (Receita/PGFN)", "Fiscal", FrequenciaDocumento.Validade, 180, TipoFornecimento.ServicoObra, false, true, false, "Receita Federal"),
        ("CND estadual", "Fiscal", FrequenciaDocumento.Validade, 90, TipoFornecimento.ServicoObra, false, true, false, "SEFAZ"),
        ("CND municipal", "Fiscal", FrequenciaDocumento.Validade, 90, TipoFornecimento.ServicoObra, false, true, false, "Prefeitura"),
        ("CRF – regularidade do FGTS", "Trabalhista / previdenciário", FrequenciaDocumento.Validade, 30, TipoFornecimento.ServicoObra, false, true, false, "Caixa"),
        ("CNDT – débitos trabalhistas", "Trabalhista / previdenciário", FrequenciaDocumento.Validade, 180, TipoFornecimento.ServicoObra, false, true, false, "TST"),
        ("Apólice de seguro de vida em grupo", "Trabalhista / previdenciário", FrequenciaDocumento.Validade, 365, TipoFornecimento.ServicoObra, false, true, false, "Convenção coletiva"),
        ("Fatura do seguro de vida (mês anterior)", "Trabalhista / previdenciário", FrequenciaDocumento.Mensal, null, TipoFornecimento.ServicoObra, false, true, false, "Convenção coletiva"),
        ("eSocial / DCTFWeb (mês anterior)", "Trabalhista / previdenciário", FrequenciaDocumento.Mensal, null, TipoFornecimento.ServicoObra, false, true, false, "eSocial / Receita"),
        ("Guia e comprovante INSS (mês anterior)", "Trabalhista / previdenciário", FrequenciaDocumento.Mensal, null, TipoFornecimento.ServicoObra, false, true, false, "Receita Federal"),
        ("Guia e comprovante FGTS Digital (mês anterior)", "Trabalhista / previdenciário", FrequenciaDocumento.Mensal, null, TipoFornecimento.ServicoObra, false, true, false, "MTE / Caixa"),
        ("Folha de pagamento e recibos (mês anterior)", "Trabalhista / previdenciário", FrequenciaDocumento.Mensal, null, TipoFornecimento.ServicoObra, true, true, true, "CLT"),
        ("PGR – Programa de Gerenciamento de Riscos", "SST", FrequenciaDocumento.Validade, 730, TipoFornecimento.ServicoObra, false, true, false, "NR-01 / NR-18"),
        ("PCMSO", "SST", FrequenciaDocumento.Validade, 365, TipoFornecimento.ServicoObra, false, true, false, "NR-07"),
        ("ART / RRT do serviço", "Técnico", FrequenciaDocumento.PorContrato, null, TipoFornecimento.ServicoObra | TipoFornecimento.ServicoEspecializadoEngenharia, false, false, false, "CREA / CAU"),
        ("Ficha de registro do funcionário", "RH (funcionários)", FrequenciaDocumento.NoCadastro, null, TipoFornecimento.ServicoObra, true, true, true, "CLT art. 41"),
        ("CTPS digital / contrato de trabalho", "RH (funcionários)", FrequenciaDocumento.NoCadastro, null, TipoFornecimento.ServicoObra, true, true, true, "CLT"),
        ("ASO do funcionário", "RH (funcionários)", FrequenciaDocumento.Validade, 365, TipoFornecimento.ServicoObra, true, true, true, "NR-07"),
        ("Certificados de treinamento NR do funcionário", "RH (funcionários)", FrequenciaDocumento.Validade, 730, TipoFornecimento.ServicoObra, true, true, false, "NR-18 / NR-35 / NR-10"),
        ("Ficha de entrega de EPI", "RH (funcionários)", FrequenciaDocumento.Validade, 30, TipoFornecimento.ServicoObra, true, true, false, "NR-06"),
        ("Ordem de serviço de SST", "RH (funcionários)", FrequenciaDocumento.NoCadastro, null, TipoFornecimento.ServicoObra, true, true, false, "NR-01"),
    ];

    /// <summary>Lista-modelo de materiais controlados (editável — ajuste ao recorte da sua empresa).</summary>
    public static readonly (string Codigo, string Nome, string Grupo)[] MateriaisControlados =
    [
        ("01", "Aços e armaduras", "Aço"), ("02", "Agregados (areia, brita e pó de pedra)", "Agregados"),
        ("03", "Ensacados (cimento, graute e argamassa)", "Aglomerantes"), ("04", "Blocos e tijolos (cerâmicos e de concreto)", "Alvenaria"),
        ("05", "Pré-moldados (painéis, lajes, escadas, vigas e estacas)", "Pré-moldados"), ("06", "Esquadrias metálicas e guarda-corpo", "Esquadrias"),
        ("07", "Impermeabilizantes", "Impermeabilização"), ("08", "Selantes para junta", "Vedantes"), ("09", "Esquadrias e portas de madeira", "Esquadrias"),
        ("10", "Louças e metais sanitários", "Louças e metais"), ("11", "Madeira e chapas de compensado", "Madeira"),
        ("12", "Pedras naturais e bancadas", "Revestimentos"), ("13", "Materiais elétricos (fios, cabos, quadros, luminárias)", "Elétrica"),
        ("14", "Revestimento cerâmico / porcelanato", "Revestimentos"), ("15", "Revestimentos vinílicos", "Revestimentos"),
        ("16", "Telhas", "Cobertura"), ("17", "Tintas, texturas e seladores", "Pintura"), ("18", "Tubulações, conexões, válvulas e medidores de gás", "Gás"),
        ("19", "Tubulações e conexões hidrossanitárias", "Hidráulica"), ("20", "Drywall (perfis e placas)", "Drywall"),
        ("21", "Concreto usinado", "Concreto"), ("22", "Graute e argamassa usinados", "Concreto"), ("23", "Contrapiso autonivelante", "Contrapiso"),
    ];

    /// <summary>Lista-modelo de serviços controlados / ITS (editável).</summary>
    public static readonly (string Codigo, string Nome)[] ServicosControlados =
    [
        ("ITS 01.01", "Compactação de aterro"), ("ITS 01.02", "Locação de obra"), ("ITS 02.01", "Fundação profunda"),
        ("ITS 02.02", "Fundação superficial – blocos e vigas baldrame"), ("ITS 02.03", "Fundação superficial – radier"),
        ("ITS 03.01", "Montagem de fôrma"), ("ITS 03.02", "Montagem de armadura"), ("ITS 03.03", "Concretagem"),
        ("ITS 03.04", "Montagem de estrutura pré-fabricada"), ("ITS 04.01", "Alvenaria de vedação"), ("ITS 04.02", "Divisória em drywall"),
        ("ITS 04.03", "Revestimento de parede – área úmida"), ("ITS 04.04", "Revestimento de parede – reboco / emboço"),
        ("ITS 05.01", "Contrapiso"), ("ITS 05.02", "Revestimento de piso – área seca"), ("ITS 05.03", "Revestimento de piso – área úmida"),
        ("ITS 05.04", "Revestimento externo – piso"), ("ITS 05.05", "Forro de gesso"), ("ITS 05.06", "Impermeabilização"),
        ("ITS 05.07", "Cobertura (estrutura e telhamento)"), ("ITS 06.01", "Instalação de portas"), ("ITS 06.02", "Instalação de esquadrias"),
        ("ITS 06.03", "Guarda-corpo e corrimão"), ("ITS 07.01", "Pintura interna"), ("ITS 07.02", "Pintura externa"),
        ("ITS 08.01", "Instalações elétricas"), ("ITS 08.02", "Instalações hidrossanitárias"), ("ITS 08.03", "Instalação de gás"),
        ("ITS 08.04", "Colocação de bancada"), ("ITS 08.05", "Instalação de louças e metais"), ("ITS 08.06", "Prevenção e combate a incêndio"),
    ];

    /// <summary>Regras de retenção genéricas (D11).</summary>
    public static readonly (string Id, string Prot, string Tempo, int? Anos, string Descarte)[] Retencoes =
    [
        ("Lista de materiais controlados", "Qualidade, Suprimentos e Engenharia", "Permanente", null, "Não se aplica (permanente)"),
        ("Lista de serviços controlados", "Qualidade, Suprimentos e Engenharia", "Permanente", null, "Não se aplica (permanente)"),
        ("Pedido de compra", "Obra e Suprimentos", "Até o final da obra + 5 anos", 5, "Arquivo morto 5 anos / eliminação"),
        ("Contrato", "Suprimentos", "Até o final da obra + 5 anos", 5, "Arquivo morto 5 anos / eliminação"),
        ("Planilha de medição e avaliação do prestador", "Obra e Suprimentos", "Até o final da obra + 5 anos", 5, "Arquivo morto 5 anos / eliminação"),
        ("Registros de qualificação de fornecedor", "Obra e Suprimentos", "Até o final da obra + 5 anos", 5, "Arquivo morto 5 anos / eliminação"),
        ("Fichas de verificação (FVS / FVM)", "Obra e Qualidade", "Até o fim do prazo de garantia da obra", 5, "Eliminação após o prazo"),
        ("Análise crítica pela direção (ata)", "Diretoria e Qualidade", "3 anos (ciclo de certificação)", 3, "Eliminação após o prazo"),
    ];

    /// <summary>
    /// Versão da carga padrão. Suba quando incluir itens novos nas listas/cadastros padrão: empresas existentes recebem a carga
    /// uma vez por versão (itens que o usuário renomeou ou excluiu não voltam a cada reinício do app).
    /// </summary>
    public const int Versao = 3;
    private const string ChaveVersao = "Sistema:CargaPadrao";

    /// <summary>Aplica a carga só se a empresa ainda não recebeu esta versão (usado na inicialização do app).</summary>
    public static async Task<bool> AplicarSeNovaVersaoAsync(IQualyHubDb db, Guid empresaId, CancellationToken ct = default)
    {
        var seq = await db.Sequencias.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.EmpresaId == empresaId && s.Chave == ChaveVersao, ct);
        if (seq is not null && seq.Ultimo >= Versao) return false;
        if (seq is null) db.Sequencias.Add(seq = new Domain.Common.Sequencia { EmpresaId = empresaId, Chave = ChaveVersao });
        seq.Ultimo = Versao;
        await AplicarAsync(db, empresaId, ct); // grava a versão junto com a carga (mesmo SaveChanges)
        return true;
    }

    public static async Task AplicarAsync(IQualyHubDb db, Guid empresaId, CancellationToken ct = default)
    {
        // Ignora o filtro de empresa: a carga pode rodar antes do usuário existir.
        var existentes = await db.Parametros.IgnoreQueryFilters().Where(p => p.EmpresaId == empresaId).Select(p => p.Chave).ToListAsync(ct);
        foreach (var (chave, v) in Parametros.Padrao.Where(k => !existentes.Contains(k.Key)))
            db.Parametros.Add(new Parametro { EmpresaId = empresaId, Chave = chave, Valor = v.Valor, Unidade = v.Unidade, Descricao = v.Descricao, Grupo = v.Grupo });

        var listas = await db.Listas.IgnoreQueryFilters().Where(l => l.EmpresaId == empresaId).Select(l => new { l.Tipo, l.Nome }).ToListAsync(ct);
        void Lista(TipoLista tipo, IEnumerable<string> nomes)
        {
            var i = 0;
            foreach (var n in nomes)
            {
                i++;
                if (!listas.Any(l => l.Tipo == tipo && l.Nome == n))
                    db.Listas.Add(new ItemLista { EmpresaId = empresaId, Tipo = tipo, Nome = n, Codigo = $"{i:00}", Ordem = i });
            }
        }
        Lista(TipoLista.Atividade, Atividades);
        Lista(TipoLista.CategoriaServico, CategoriasServico);
        Lista(TipoLista.CriterioQualificacao, CriteriosQualificacao);
        Lista(TipoLista.ProcessoArea, Processos);
        Lista(TipoLista.TipoDocumentoSgq, TiposDocumentoSgq);
        Lista(TipoLista.EmissorExterno, Emissores);
        Lista(TipoLista.CondicaoPagamento, CondicoesPagamento);
        Lista(TipoLista.Funcao, Funcoes);
        Lista(TipoLista.TipoTarefa, TiposTarefa);
        Lista(TipoLista.TipoAgrupamento, TiposAgrupamento);
        Lista(TipoLista.TipoComodo, TiposComodo);
        Lista(TipoLista.GrupoServico, GruposServico);
        Lista(TipoLista.CategoriaDocumento, CategoriasDocumento);

        var tipos = await db.TiposDocumento.IgnoreQueryFilters().Where(t => t.EmpresaId == empresaId).Select(t => t.Nome).ToListAsync(ct);
        foreach (var t in TiposDocumentoFornecedor.Where(t => !tipos.Contains(t.Nome)))
            db.TiposDocumento.Add(new TipoDocumentoExigido
            {
                EmpresaId = empresaId, AplicaA = AplicaA.Fornecedor, Nome = t.Nome, BloqueiaQuandoVencido = t.Bloqueia, PorEntrega = t.PorEntrega,
                ValidadePadraoDias = t.ValidadeDias, ExigidoPara = t.Para, Obrigatorio = t.Para == TipoFornecimento.Nenhum && t.Bloqueia, DiasAlertaAntesVencimento = 30,
                Frequencia = t.PorEntrega ? FrequenciaDocumento.PorEntrega : t.ValidadeDias is > 0 ? FrequenciaDocumento.Validade : FrequenciaDocumento.NoCadastro
            });
        var ordemDoc = 100;
        foreach (var t in DocumentosPrestador.Where(t => !tipos.Contains(t.Nome)))
            db.TiposDocumento.Add(new TipoDocumentoExigido
            {
                EmpresaId = empresaId, AplicaA = AplicaA.Fornecedor, Nome = t.Nome, Categoria = t.Categoria, Frequencia = t.Freq, ValidadePadraoDias = t.Validade,
                DiaLimite = t.Freq == FrequenciaDocumento.Mensal ? 20 : null, ExigidoPara = t.Para, DeFuncionario = t.Funcionario, Obrigatorio = true,
                BloqueiaQuandoVencido = t.Bloqueia, PorEntrega = t.Freq == FrequenciaDocumento.PorEntrega, DiasAlertaAntesVencimento = 30, Fonte = t.Fonte, Ordem = ++ordemDoc,
                Momento = t.Freq == FrequenciaDocumento.PorContrato ? MomentoDocumento.PorContrato : MomentoDocumento.Cadastro, ContemDadoSensivel = t.Sensivel
            });

        var mats = await db.MateriaisControlados.IgnoreQueryFilters().Where(m => m.EmpresaId == empresaId).Select(m => m.Codigo).ToListAsync(ct);
        foreach (var m in MateriaisControlados.Where(m => !mats.Contains(m.Codigo)))
            db.MateriaisControlados.Add(new MaterialControlado
            {
                EmpresaId = empresaId, Codigo = m.Codigo, Nome = m.Nome, GrupoCatalogo = m.Grupo,
                EspecificacaoModelo = "Especificar norma, classe/resistência, dimensões e ensaios de recebimento.",
                DocumentosExigidos = "Laudo/certificado do produto; FISPQ quando aplicável"
            });

        var svs = await db.ServicosControlados.IgnoreQueryFilters().Where(s => s.EmpresaId == empresaId).Select(s => s.Codigo).ToListAsync(ct);
        foreach (var s in ServicosControlados.Where(s => !svs.Contains(s.Codigo)))
            db.ServicosControlados.Add(new ServicoControlado { EmpresaId = empresaId, Codigo = s.Codigo, Nome = s.Nome });

        var pastas = await db.Pastas.IgnoreQueryFilters().Where(p => p.EmpresaId == empresaId).Select(p => p.Nome).ToListAsync(ct);
        var ordem = 0;
        foreach (var p in Processos) { ordem++; if (!pastas.Contains(p)) db.Pastas.Add(new Pasta { EmpresaId = empresaId, Nome = p, Ordem = ordem }); }

        var rets = await db.RegrasRetencao.IgnoreQueryFilters().Where(r => r.EmpresaId == empresaId).Select(r => r.Identificacao).ToListAsync(ct);
        foreach (var r in Retencoes.Where(r => !rets.Contains(r.Id)))
            db.RegrasRetencao.Add(new RegraRetencao
            {
                EmpresaId = empresaId, Identificacao = r.Id, Armazenamento = "Eletrônico no QualyHub (Azure)", Recuperacao = "Busca por código/obra/data; backup automático",
                Protecao = r.Prot, TempoRetencao = r.Tempo, TempoRetencaoAnos = r.Anos, Descarte = r.Descarte
            });

        var comAlcada = await db.Alcadas.IgnoreQueryFilters().Where(a => a.EmpresaId == empresaId).Select(a => a.Entidade).Distinct().ToListAsync(ct);
        void Alc(EntidadeAprovavel e, int ordemEtapa, PerfilUsuario perfil, string nome, bool engObra = false)
        {
            if (!comAlcada.Contains(e))
                db.Alcadas.Add(new Alcada { EmpresaId = empresaId, Entidade = e, Ordem = ordemEtapa, PerfilAprovador = perfil, NomeEtapa = nome, EngenheiroDaObra = engObra, PrazoHoras = 24 });
        }
        Alc(EntidadeAprovavel.Requisicao, 1, PerfilUsuario.Engenharia, "Aprov. Engenheiro da obra", engObra: true);
        Alc(EntidadeAprovavel.Requisicao, 2, PerfilUsuario.Diretoria, "Aprov. Diretor Técnico");
        Alc(EntidadeAprovavel.Cotacao, 1, PerfilUsuario.Diretoria, "Aprov. Diretor Técnico");
        Alc(EntidadeAprovavel.Pedido, 1, PerfilUsuario.Diretoria, "Aprov. Diretor");
        Alc(EntidadeAprovavel.Contrato, 1, PerfilUsuario.Diretoria, "Aprov. Diretoria");
        Alc(EntidadeAprovavel.Aditivo, 1, PerfilUsuario.Diretoria, "Aprov. Diretoria");
        Alc(EntidadeAprovavel.Medicao, 1, PerfilUsuario.Diretoria, "Aprov. Diretor Técnico");
        Alc(EntidadeAprovavel.Pagamento, 1, PerfilUsuario.Diretoria, "Liberação Diretor Técnico");
        Alc(EntidadeAprovavel.Cronograma, 1, PerfilUsuario.Engenharia, "Aprov. Engenheiro responsável", engObra: true);
        Alc(EntidadeAprovavel.Cronograma, 2, PerfilUsuario.Diretoria, "Aprov. Diretor Técnico");

        await AplicarSstAsync(db, empresaId, ct);
        await AplicarNotificacoesAsync(db, empresaId, ct);
        await db.SaveChangesAsync(ct);
    }

    // ───────── SST: matriz inicial genérica (VALIDAR com SST / PGR / PCMSO da obra) ─────────

    public static readonly (string Cod, string Nome, TipoExigencia Tipo, string? Carga, int? Validade, EfeitoVencimento Efeito, AtividadeRisco Ativ, string Fonte)[] ExigenciasSst =
    [
        ("ASO", "ASO – aptidão", TipoExigencia.ExameAso, null, 12, EfeitoVencimento.BloqueiaPessoa, AtividadeRisco.Nenhuma, "NR-07 · PCMSO"),
        ("INT", "Integração na obra (NR-18)", TipoExigencia.Treinamento, "Conforme NR-18", null, EfeitoVencimento.BloqueiaPessoa, AtividadeRisco.Nenhuma, "NR-18"),
        ("OS", "Ordem de serviço de SST (NR-01)", TipoExigencia.Documento, null, null, EfeitoVencimento.BloqueiaPessoa, AtividadeRisco.Nenhuma, "NR-01"),
        ("EPI", "Ficha de EPI (NR-06)", TipoExigencia.Epi, null, null, EfeitoVencimento.BloqueiaPessoa, AtividadeRisco.Nenhuma, "NR-06"),
        ("NR35", "NR-35 – trabalho em altura", TipoExigencia.Treinamento, "8 h", 24, EfeitoVencimento.RestringeAtividade, AtividadeRisco.Altura, "NR-35"),
        ("NR10", "NR-10 – básico", TipoExigencia.Treinamento, "40 h", 24, EfeitoVencimento.RestringeAtividade, AtividadeRisco.Eletrico, "NR-10"),
        ("NR12", "NR-12 – operação de máquinas", TipoExigencia.Treinamento, "Conforme máquina", null, EfeitoVencimento.RestringeAtividade, AtividadeRisco.OperacaoEquipamento, "NR-12"),
        ("NR11", "NR-11 – movimentação de cargas", TipoExigencia.Treinamento, "Conforme equipamento", null, EfeitoVencimento.RestringeAtividade, AtividadeRisco.Icamento, "NR-11 / NR-18"),
        ("NR33", "NR-33 – trabalhador autorizado", TipoExigencia.Treinamento, "16 h", 12, EfeitoVencimento.RestringeAtividade, AtividadeRisco.EspacoConfinado, "NR-33"),
        ("AND", "Montagem de andaimes", TipoExigencia.Treinamento, "Conforme NR-18", null, EfeitoVencimento.RestringeAtividade, AtividadeRisco.Andaime, "NR-18"),
    ];

    /// <summary>Linha = função; colunas na ordem de ExigenciasSst. O = obrigatório, C = conforme a atividade, - = não se aplica.</summary>
    public static readonly (string Funcao, string Matriz)[] MatrizSst =
    [
        ("Servente", "OOOOC-C-C-"), ("Pedreiro", "OOOOC-----"), ("Carpinteiro de fôrma", "OOOOO-O---"), ("Armador", "OOOOO-O---"),
        ("Eletricista", "OOOOCO--C-"), ("Encanador / bombeiro hidráulico", "OOOOC---C-"), ("Pintor (interno)", "OOOOC-----"),
        ("Pintor (fachada)", "OOOOO-----"), ("Gesseiro / montador de drywall", "OOOOC-C---"), ("Impermeabilizador", "OOOOC---C-"),
        ("Operador de grua / guindaste", "OOOO--OO--"), ("Operador de elevador de obra", "OOOO--OO--"), ("Sinaleiro / amarrador de cargas", "OOOOC--O--"),
        ("Montador de andaime", "OOOOO----O"), ("Soldador / serralheiro", "OOOOC-O-C-"), ("Operador de betoneira / serra circular", "OOOO--O---"),
        ("Encarregado / mestre de obras", "OOOOC---C-"), ("Almoxarife", "OOOO---C--"), ("Técnico de segurança", "OOOOO---C-"), ("Engenheiro / estagiário", "OOOOC-----"),
    ];

    public static readonly (string Ordem, string Item, string Ref)[] ChecklistCanteiro =
    [
        ("1", "Proteção de periferia (guarda-corpo e rodapé) nos pavimentos", "NR-18"), ("2", "Aberturas no piso fechadas ou sinalizadas", "NR-18"),
        ("3", "Escadas e rampas com corrimão", "NR-18"), ("4", "Quadros elétricos fechados e identificados", "NR-10 / NR-18"),
        ("5", "Extintores sinalizados e dentro da validade", "NR-23"), ("6", "Organização e limpeza das frentes", "NR-18"),
        ("7", "Uso de EPI pelos trabalhadores", "NR-06"), ("8", "Máquinas com proteção de partes móveis", "NR-12"),
    ];

    private static async Task AplicarSstAsync(IQualyHubDb db, Guid empresaId, CancellationToken ct)
    {
        if (await db.Exigencias.IgnoreQueryFilters().AnyAsync(x => x.EmpresaId == empresaId, ct)) return;
        var treinamentos = new Dictionary<string, Treinamento>();
        var exig = new List<Exigencia>();
        var ordem = 0;
        foreach (var e in ExigenciasSst)
        {
            Guid? tid = null;
            if (e.Tipo == TipoExigencia.Treinamento)
            {
                var t = new Treinamento
                {
                    EmpresaId = empresaId, Descricao = e.Nome, Tipo = e.Cod == "INT" ? TipoTreinamento.Integracao : TipoTreinamento.NR, CargaHoraria = e.Carga ?? "",
                    ValidadeMeses = e.Validade, MetodoEficacia = MetodoEficaciaTreinamento.Prova
                };
                db.Treinamentos.Add(t); tid = t.Id; treinamentos[e.Cod] = t;
            }
            var x = new Exigencia
            {
                EmpresaId = empresaId, Codigo = e.Cod, Descricao = e.Nome, Tipo = e.Tipo, TreinamentoId = tid, CargaInicial = e.Carga, ValidadeMeses = e.Validade,
                EfeitoVencimento = e.Efeito, Atividades = e.Ativ, Fonte = e.Fonte, Ordem = ++ordem
            };
            db.Exigencias.Add(x); exig.Add(x);
        }
        db.Treinamentos.Add(new Treinamento { EmpresaId = empresaId, Descricao = "Treinamento em procedimento/ITS do SGQ", Tipo = TipoTreinamento.Sgq, CargaHoraria = "2 h", MetodoEficacia = MetodoEficaciaTreinamento.ObservacaoEmCampo });
        foreach (var (funcao, matriz) in MatrizSst)
        {
            var f = new FuncaoObra { EmpresaId = empresaId, Descricao = funcao, Riscos = "Preencher com o PGR da obra" };
            for (var i = 0; i < matriz.Length && i < exig.Count; i++)
                if (matriz[i] is 'O' or 'C')
                    f.Exigencias.Add(new FuncaoExigencia { FuncaoObraId = f.Id, ExigenciaId = exig[i].Id, Obrigatoriedade = matriz[i] == 'O' ? Obrigatoriedade.Obrigatorio : Obrigatoriedade.ConformeAtividade });
            db.Funcoes.Add(f);
        }
        var m = new ModeloInspecao { EmpresaId = empresaId, Tipo = TipoModelo.SST, Codigo = "IS-CANTEIRO", Titulo = "Canteiro geral – semanal", Situacao = SituacaoModelo.Vigente, LiberaMedicao = false };
        foreach (var c in ChecklistCanteiro)
            m.Itens.Add(new ItemVerificacao { ModeloInspecaoId = m.Id, Ordem = int.Parse(c.Ordem), Descricao = c.Item, Referencia = c.Ref, Metodo = "Visual", Criterio = "Conforme NR", Foto = FotoItem.SoEmNaoConformidade });
        db.ModelosInspecao.Add(m);
    }

    private static async Task AplicarNotificacoesAsync(IQualyHubDb db, Guid empresaId, CancellationToken ct)
    {
        if (await db.RegrasNotificacao.IgnoreQueryFilters().AnyAsync(x => x.EmpresaId == empresaId, ct)) return;
        void N(string evento, string? antec, string? rep, string dest, CanalNotificacao canal, string modelo) =>
            db.RegrasNotificacao.Add(new RegraNotificacao { EmpresaId = empresaId, Evento = evento, Antecedencia = antec, Repeticao = rep, Destinatarios = dest, Canal = canal, ModeloMensagem = modelo });
        N("Aprovação pendente", null, "a cada 24 h", "Aprovador da etapa", CanalNotificacao.Pendencias | CanalNotificacao.Email, "{documento} aguarda sua aprovação: {link}");
        N("Prazo de aprovação estourado", null, null, "Aprovador + escalonamento", CanalNotificacao.Pendencias | CanalNotificacao.Email, "{documento} está atrasado na sua alçada.");
        N("Documento do fornecedor vencendo", "30 dias", "a cada 7 dias até renovar", "Fornecedor (link do portal) · Comprador", CanalNotificacao.Email, "Seu documento {documento} vence em {data}. Envie a renovação: {link}");
        N("Documento recusado", null, null, "Fornecedor", CanalNotificacao.Email, "O documento {documento} foi recusado: {motivo}. Reenvie pelo portal: {link}");
        N("Nova revisão publicada", null, null, "Leitura obrigatória", CanalNotificacao.Pendencias | CanalNotificacao.Email, "{documento} {revisao} publicado. Confirme a leitura: {link}");
        N("GRD sem confirmação", "1 dia", "diária", "Fornecedor · Qualidade", CanalNotificacao.Email, "Confirme o recebimento da {grd}: {link}");
        N("RNC / ação corretiva vencida", null, "diária", "Responsável", CanalNotificacao.Pendencias, "{rnc} venceu em {data}.");
        N("Calibração a vencer", "30 dias", null, "Responsável do equipamento", CanalNotificacao.Pendencias, "{equipamento} vence em {data}.");
        N("ASO / treinamento vencendo", "30 dias", "semanal", "Empresa terceira · SST", CanalNotificacao.Pendencias | CanalNotificacao.Email, "{colaborador}: {exigencia} vence em {data}.");
        N("PT sem encerramento", null, "fim do turno", "Emitente da PT", CanalNotificacao.Pendencias, "{pt} ainda não foi encerrada.");
    }
}

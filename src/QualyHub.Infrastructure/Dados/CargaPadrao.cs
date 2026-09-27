using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Documentos;
using QualyHub.Domain.Estrutura;

namespace QualyHub.Infrastructure.Dados;

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

    public static async Task AplicarAsync(QualyHubDbContext db, Guid empresaId, CancellationToken ct = default)
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

        var tipos = await db.TiposDocumento.IgnoreQueryFilters().Where(t => t.EmpresaId == empresaId).Select(t => t.Nome).ToListAsync(ct);
        foreach (var t in TiposDocumentoFornecedor.Where(t => !tipos.Contains(t.Nome)))
            db.TiposDocumento.Add(new TipoDocumentoExigido
            {
                EmpresaId = empresaId, AplicaA = AplicaA.Fornecedor, Nome = t.Nome, BloqueiaQuandoVencido = t.Bloqueia, PorEntrega = t.PorEntrega,
                ValidadePadraoDias = t.ValidadeDias, ExigidoPara = t.Para, Obrigatorio = t.Para == TipoFornecimento.Nenhum && t.Bloqueia, DiasAlertaAntesVencimento = 30
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

        if (!await db.Alcadas.IgnoreQueryFilters().AnyAsync(a => a.EmpresaId == empresaId, ct))
        {
            void Alc(EntidadeAprovavel e, int ordemEtapa, PerfilUsuario perfil, string nome) =>
                db.Alcadas.Add(new Alcada { EmpresaId = empresaId, Entidade = e, Ordem = ordemEtapa, PerfilAprovador = perfil, NomeEtapa = nome });
            Alc(EntidadeAprovavel.Requisicao, 1, PerfilUsuario.Engenharia, "Aprov. Engenheiro");
            Alc(EntidadeAprovavel.Requisicao, 2, PerfilUsuario.Diretoria, "Aprov. Diretor Técnico");
            Alc(EntidadeAprovavel.Cotacao, 1, PerfilUsuario.Diretoria, "Aprov. Diretor Técnico");
            Alc(EntidadeAprovavel.Pedido, 1, PerfilUsuario.Diretoria, "Aprov. Diretor");
            Alc(EntidadeAprovavel.Contrato, 1, PerfilUsuario.Diretoria, "Aprov. Diretoria");
            Alc(EntidadeAprovavel.Medicao, 1, PerfilUsuario.Diretoria, "Aprov. Diretor Técnico");
            Alc(EntidadeAprovavel.Pagamento, 1, PerfilUsuario.Diretoria, "Liberação Diretor Técnico");
        }

        await db.SaveChangesAsync(ct);
    }
}

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Colaboracao;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Importacao;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Documentos;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Sst;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Infrastructure.Dados;

public class QualyHubDbContext(DbContextOptions<QualyHubDbContext> options, IUsuarioAtual usuario) : DbContext(options), IQualyHubDb
{
    // Lidas pelos filtros globais a cada consulta (parametrizadas pelo EF por instância do contexto).
    public Guid EmpresaAtualId => usuario.EmpresaId;
    public bool AcessoTodasObras => usuario.AcessoTodasObras;
    public IReadOnlyCollection<Guid> ObrasPermitidas => usuario.ObrasPermitidas;

    // Estrutura
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<RegistroPlataforma> RegistrosPlataforma => Set<RegistroPlataforma>();
    public DbSet<PerfilAcesso> PerfisAcesso => Set<PerfilAcesso>();
    public DbSet<Obra> Obras => Set<Obra>();
    public DbSet<Local> Locais => Set<Local>();
    public DbSet<CentroCusto> CentrosCusto => Set<CentroCusto>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<CredencialUsuario> Credenciais => Set<CredencialUsuario>();
    public DbSet<UsuarioObra> UsuariosObras => Set<UsuarioObra>();
    public DbSet<Alcada> Alcadas => Set<Alcada>();
    public DbSet<Parametro> Parametros => Set<Parametro>();
    public DbSet<ItemLista> Listas => Set<ItemLista>();
    public DbSet<Anexo> Anexos => Set<Anexo>();
    public DbSet<Sequencia> Sequencias => Set<Sequencia>();
    // Cadastros
    public DbSet<TipoDocumentoExigido> TiposDocumento => Set<TipoDocumentoExigido>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<FornecedorQualificacao> Qualificacoes => Set<FornecedorQualificacao>();
    public DbSet<FornecedorDocumento> FornecedorDocumentos => Set<FornecedorDocumento>();
    public DbSet<LinkPortalFornecedor> LinksPortal => Set<LinkPortalFornecedor>();
    public DbSet<ServicoControlado> ServicosControlados => Set<ServicoControlado>();
    public DbSet<MaterialControlado> MateriaisControlados => Set<MaterialControlado>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Marca> Marcas => Set<Marca>();
    // Suprimentos (T01–T18)
    public DbSet<Requisicao> Requisicoes => Set<Requisicao>();
    public DbSet<RequisicaoItem> RequisicaoItens => Set<RequisicaoItem>();
    public DbSet<Cotacao> Cotacoes => Set<Cotacao>();
    public DbSet<CotacaoFornecedor> CotacaoFornecedores => Set<CotacaoFornecedor>();
    public DbSet<CotacaoItem> CotacaoItens => Set<CotacaoItem>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<ContratoItem> ContratoItens => Set<ContratoItem>();
    public DbSet<ContratoServico> ContratoServicos => Set<ContratoServico>();
    public DbSet<Recebimento> Recebimentos => Set<Recebimento>();
    public DbSet<RecebimentoItem> RecebimentoItens => Set<RecebimentoItem>();
    public DbSet<Medicao> Medicoes => Set<Medicao>();
    public DbSet<MedicaoItem> MedicaoItens => Set<MedicaoItem>();
    public DbSet<PlanoAcaoFornecedor> PlanosAcao => Set<PlanoAcaoFornecedor>();
    public DbSet<AvaliacaoSemestral> AvaliacoesSemestrais => Set<AvaliacaoSemestral>();
    public DbSet<LiberacaoPagamento> Pagamentos => Set<LiberacaoPagamento>();
    // Qualidade
    // Documentos do SGQ (D01–D13)
    public DbSet<Pasta> Pastas => Set<Pasta>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<DocumentoRelacionado> DocumentosRelacionados => Set<DocumentoRelacionado>();
    public DbSet<DocumentoRevisao> Revisoes => Set<DocumentoRevisao>();
    public DbSet<EtapaRevisao> EtapasRevisao => Set<EtapaRevisao>();
    public DbSet<CopiaControlada> Copias => Set<CopiaControlada>();
    public DbSet<SolicitacaoCopia> SolicitacoesCopia => Set<SolicitacaoCopia>();
    public DbSet<SolicitacaoCopiaItem> SolicitacaoCopiaItens => Set<SolicitacaoCopiaItem>();
    public DbSet<Ciencia> Ciencias => Set<Ciencia>();
    public DbSet<PermissaoDocumento> Permissoes => Set<PermissaoDocumento>();
    public DbSet<CheckoutDocumento> Checkouts => Set<CheckoutDocumento>();
    public DbSet<EventoDocumento> EventosDocumento => Set<EventoDocumento>();
    public DbSet<DocumentoExterno> DocumentosExternos => Set<DocumentoExterno>();
    public DbSet<RegraRetencao> RegrasRetencao => Set<RegraRetencao>();
    public DbSet<Grd> Grds => Set<Grd>();
    public DbSet<GrdItem> GrdItens => Set<GrdItem>();
    // Qualidade da obra (Q01–Q15)
    public DbSet<ModeloInspecao> ModelosInspecao => Set<ModeloInspecao>();
    public DbSet<ItemVerificacao> ItensVerificacao => Set<ItemVerificacao>();
    public DbSet<EnsaioModelo> EnsaiosModelo => Set<EnsaioModelo>();
    public DbSet<Fvs> Fvss => Set<Fvs>();
    public DbSet<FvsItem> FvsItens => Set<FvsItem>();
    public DbSet<Rnc> Rncs => Set<Rnc>();
    public DbSet<AcaoCorretiva> AcoesCorretivas => Set<AcaoCorretiva>();
    public DbSet<AcaoCorretivaItem> AcaoCorretivaItens => Set<AcaoCorretivaItem>();
    public DbSet<EquipamentoMedicao> Equipamentos => Set<EquipamentoMedicao>();
    public DbSet<Calibracao> Calibracoes => Set<Calibracao>();
    public DbSet<AuditoriaInterna> AuditoriasInternas => Set<AuditoriaInterna>();
    public DbSet<Constatacao> Constatacoes => Set<Constatacao>();
    public DbSet<FvmItem> FvmItens => Set<FvmItem>();
    public DbSet<EnsaioRecebimento> EnsaiosRecebimento => Set<EnsaioRecebimento>();
    public DbSet<AplicacaoLote> AplicacoesLote => Set<AplicacaoLote>();
    // Segurança e pessoas (S01–S13)
    public DbSet<FuncaoObra> Funcoes => Set<FuncaoObra>();
    public DbSet<Exigencia> Exigencias => Set<Exigencia>();
    public DbSet<FuncaoExigencia> FuncaoExigencias => Set<FuncaoExigencia>();
    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();
    public DbSet<DocumentoColaborador> DocumentosColaborador => Set<DocumentoColaborador>();
    public DbSet<Treinamento> Treinamentos => Set<Treinamento>();
    public DbSet<Turma> Turmas => Set<Turma>();
    public DbSet<Participante> Participantes => Set<Participante>();
    public DbSet<Epi> Epis => Set<Epi>();
    public DbSet<EntregaEpi> EntregasEpi => Set<EntregaEpi>();
    public DbSet<InspecaoSeguranca> InspecoesSeguranca => Set<InspecaoSeguranca>();
    public DbSet<InspecaoSegurancaItem> InspecaoSegurancaItens => Set<InspecaoSegurancaItem>();
    public DbSet<PermissaoTrabalho> PermissoesTrabalho => Set<PermissaoTrabalho>();
    public DbSet<ExecutantePt> ExecutantesPt => Set<ExecutantePt>();
    public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();
    public DbSet<Nota> Notas => Set<Nota>();
    public DbSet<FornecedorContato> FornecedorContatos => Set<FornecedorContato>();
    public DbSet<CategoriaProduto> CategoriasProduto => Set<CategoriaProduto>();
    public DbSet<Spe> Spes => Set<Spe>();
    public DbSet<Domain.Fiscal.NotaFiscalEntrada> NotasFiscais => Set<Domain.Fiscal.NotaFiscalEntrada>();
    public DbSet<Domain.Fiscal.NotaFiscalItem> NotaFiscalItens => Set<Domain.Fiscal.NotaFiscalItem>();
    public DbSet<Domain.Fiscal.ImportacaoNota> ImportacoesNota => Set<Domain.Fiscal.ImportacaoNota>();
    public DbSet<Domain.Fiscal.UsoIa> UsosIa => Set<Domain.Fiscal.UsoIa>();
    public DbSet<Domain.Planejamento.VersaoCronograma> VersoesCronograma => Set<Domain.Planejamento.VersaoCronograma>();
    public DbSet<Domain.Planejamento.AtividadeCronograma> AtividadesCronograma => Set<Domain.Planejamento.AtividadeCronograma>();
    public DbSet<Domain.Planejamento.VinculoAtividade> VinculosAtividade => Set<Domain.Planejamento.VinculoAtividade>();
    public DbSet<Domain.Planejamento.DesembolsoVersao> DesembolsosVersao => Set<Domain.Planejamento.DesembolsoVersao>();
    public DbSet<Domain.Planejamento.Feriado> Feriados => Set<Domain.Planejamento.Feriado>();
    public DbSet<Domain.Planejamento.RedePlanejamento> RedesPlanejamento => Set<Domain.Planejamento.RedePlanejamento>();
    public DbSet<Domain.Planejamento.RedeLocal> RedeLocais => Set<Domain.Planejamento.RedeLocal>();
    public DbSet<Domain.Planejamento.RedePacote> RedePacotes => Set<Domain.Planejamento.RedePacote>();
    public DbSet<Domain.Planejamento.RedePacoteLocal> RedePacoteLocais => Set<Domain.Planejamento.RedePacoteLocal>();
    public DbSet<Domain.Planejamento.VinculoRede> VinculosRede => Set<Domain.Planejamento.VinculoRede>();
    public DbSet<Domain.Planejamento.Restricao> Restricoes => Set<Domain.Planejamento.Restricao>();
    public DbSet<Domain.Planejamento.PlanoSemanal> PlanosSemanais => Set<Domain.Planejamento.PlanoSemanal>();
    public DbSet<Domain.Planejamento.MedicaoSemanal> MedicoesSemanais => Set<Domain.Planejamento.MedicaoSemanal>();
    public DbSet<Domain.Planejamento.DiarioObra> DiariosObra => Set<Domain.Planejamento.DiarioObra>();
    public DbSet<Domain.Planejamento.DiarioAtividade> DiariosObraAtividades => Set<Domain.Planejamento.DiarioAtividade>();
    public DbSet<Domain.Planejamento.CompromissoSemanal> CompromissosSemanais => Set<Domain.Planejamento.CompromissoSemanal>();
    public DbSet<Domain.Colaboracao.Conversa> Conversas => Set<Domain.Colaboracao.Conversa>();
    public DbSet<Domain.Colaboracao.ParticipanteConversa> ParticipantesConversa => Set<Domain.Colaboracao.ParticipanteConversa>();
    public DbSet<Domain.Colaboracao.MensagemConversa> MensagensConversa => Set<Domain.Colaboracao.MensagemConversa>();
    public DbSet<Domain.Colaboracao.LeituraMensagem> LeiturasMensagem => Set<Domain.Colaboracao.LeituraMensagem>();
    public DbSet<Domain.Colaboracao.AvisoEmail> AvisosEmail => Set<Domain.Colaboracao.AvisoEmail>();
    public DbSet<Quadra> Quadras => Set<Quadra>();
    public DbSet<Tipologia> Tipologias => Set<Tipologia>();
    public DbSet<TipologiaAmbiente> TipologiaAmbientes => Set<TipologiaAmbiente>();
    public DbSet<TipologiaMix> TipologiaMix => Set<TipologiaMix>();
    public DbSet<Composicao> Composicoes => Set<Composicao>();
    public DbSet<ComposicaoItem> ComposicaoItens => Set<ComposicaoItem>();
    public DbSet<ServicoAplicacao> ServicoAplicacoes => Set<ServicoAplicacao>();
    public DbSet<ServicoLocal> ServicosLocais => Set<ServicoLocal>();
    public DbSet<Equipe> Equipes => Set<Equipe>();
    public DbSet<EquipeMembro> EquipeMembros => Set<EquipeMembro>();
    public DbSet<CustoMaoObraMes> CustosMaoObra => Set<CustoMaoObraMes>();
    public DbSet<Apropriacao> Apropriacoes => Set<Apropriacao>();
    public DbSet<CustoRealizado> CustosRealizados => Set<CustoRealizado>();
    public DbSet<Estabelecimento> Estabelecimentos => Set<Estabelecimento>();
    public DbSet<FluxoEstabelecimento> FluxosEstabelecimento => Set<FluxoEstabelecimento>();
    public DbSet<FornecedorGrupo> FornecedorGrupos => Set<FornecedorGrupo>();
    public DbSet<FornecedorServico> FornecedorServicos => Set<FornecedorServico>();
    public DbSet<FornecedorExigencia> FornecedorExigencias => Set<FornecedorExigencia>();
    public DbSet<RequisicaoItemLocal> RequisicaoItemLocais => Set<RequisicaoItemLocal>();
    public DbSet<ContratoItemLocal> ContratoItemLocais => Set<ContratoItemLocal>();
    public DbSet<TransferenciaMaterial> Transferencias => Set<TransferenciaMaterial>();
    public DbSet<TransferenciaItem> TransferenciaItens => Set<TransferenciaItem>();
    public DbSet<Almoxarifado> Almoxarifados => Set<Almoxarifado>();
    public DbSet<SaldoEstoque> SaldosEstoque => Set<SaldoEstoque>();
    public DbSet<MovimentoEstoque> MovimentosEstoque => Set<MovimentoEstoque>();
    public DbSet<InventarioEstoque> Inventarios => Set<InventarioEstoque>();
    public DbSet<InventarioItem> InventarioItens => Set<InventarioItem>();
    public DbSet<KitMaterial> KitsMaterial => Set<KitMaterial>();
    public DbSet<KitMaterialItem> KitMaterialItens => Set<KitMaterialItem>();
    public DbSet<FamiliaEquipamento> FamiliasEquipamento => Set<FamiliaEquipamento>();
    public DbSet<Domain.Suprimentos.Equipamento> Ativos => Set<Domain.Suprimentos.Equipamento>();
    public DbSet<EquipamentoDocumento> AtivoDocumentos => Set<EquipamentoDocumento>();
    public DbSet<MovimentoEquipamento> MovimentosEquipamento => Set<MovimentoEquipamento>();
    public DbSet<PlanoManutencao> PlanosManutencao => Set<PlanoManutencao>();
    public DbSet<OrdemServico> OrdensServico => Set<OrdemServico>();
    public DbSet<ParcelaContrato> ParcelasContrato => Set<ParcelaContrato>();
    public DbSet<UsoContrato> UsosContrato => Set<UsoContrato>();
    public DbSet<TipoEquipamentoMedicao> TiposEquipamentoMedicao => Set<TipoEquipamentoMedicao>();
    public DbSet<CautelaFerramenta> CautelasFerramenta => Set<CautelaFerramenta>();
    public DbSet<Domain.Planejamento.DiarioEquipamento> DiariosObraEquipamentos => Set<Domain.Planejamento.DiarioEquipamento>();
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
    public DbSet<TarefaProrrogacao> TarefaProrrogacoes => Set<TarefaProrrogacao>();
    public DbSet<TarefaLeitura> TarefaLeituras => Set<TarefaLeitura>();
    // Contratos e sistema (T27, T28)
    public DbSet<Aditivo> Aditivos => Set<Aditivo>();
    public DbSet<AditivoItem> AditivoItens => Set<AditivoItem>();
    public DbSet<RegraNotificacao> RegrasNotificacao => Set<RegraNotificacao>();
    // Aprovações, importação, auditoria
    public DbSet<SolicitacaoAprovacao> SolicitacoesAprovacao => Set<SolicitacaoAprovacao>();
    public DbSet<EtapaAprovacao> EtapasAprovacao => Set<EtapaAprovacao>();
    public DbSet<LimiteAprovacao> LimitesAprovacao => Set<LimiteAprovacao>();
    public DbSet<DelegacaoAprovacao> DelegacoesAprovacao => Set<DelegacaoAprovacao>();
    public DbSet<LayoutImportacao> LayoutsImportacao => Set<LayoutImportacao>();
    public DbSet<LoteImportacao> LotesImportacao => Set<LoteImportacao>();
    public DbSet<VinculoOrigem> VinculosOrigem => Set<VinculoOrigem>();
    public DbSet<Domain.Financeiro.ContaGerencial> ContasGerenciais => Set<Domain.Financeiro.ContaGerencial>();
    public DbSet<Domain.Financeiro.ContaBancaria> ContasBancarias => Set<Domain.Financeiro.ContaBancaria>();
    public DbSet<Domain.Financeiro.CondicaoPagamento> CondicoesPagamento => Set<Domain.Financeiro.CondicaoPagamento>();
    public DbSet<Domain.Financeiro.Titulo> Titulos => Set<Domain.Financeiro.Titulo>();
    public DbSet<Domain.Financeiro.TituloParcela> TituloParcelas => Set<Domain.Financeiro.TituloParcela>();
    public DbSet<Domain.Financeiro.MovimentoBancario> MovimentosBancarios => Set<Domain.Financeiro.MovimentoBancario>();
    public DbSet<Domain.Incorporacao.IncorporacaoObra> Incorporacoes => Set<Domain.Incorporacao.IncorporacaoObra>();
    public DbSet<Domain.Incorporacao.AliquotaRegime> AliquotasRegime => Set<Domain.Incorporacao.AliquotaRegime>();
    public DbSet<Domain.Incorporacao.IndiceEconomico> IndicesEconomicos => Set<Domain.Incorporacao.IndiceEconomico>();
    public DbSet<Domain.Incorporacao.UnidadeComercial> UnidadesComerciais => Set<Domain.Incorporacao.UnidadeComercial>();
    public DbSet<Domain.Incorporacao.Comprador> Compradores => Set<Domain.Incorporacao.Comprador>();
    public DbSet<Domain.Incorporacao.ContratoVenda> ContratosVenda => Set<Domain.Incorporacao.ContratoVenda>();
    public DbSet<Domain.Incorporacao.BlocoCondicaoVenda> BlocosCondicaoVenda => Set<Domain.Incorporacao.BlocoCondicaoVenda>();
    public DbSet<Domain.Incorporacao.ComissaoVenda> ComissoesVenda => Set<Domain.Incorporacao.ComissaoVenda>();
    public DbSet<Domain.Incorporacao.ApuracaoRet> ApuracoesRet => Set<Domain.Incorporacao.ApuracaoRet>();
    public DbSet<LinhaImportacao> LinhasImportacao => Set<LinhaImportacao>();
    public DbSet<RegistroAuditoria> Auditoria => Set<RegistroAuditoria>();
    public DbSet<CodigoAcessoFornecedor> CodigosAcessoFornecedor => Set<CodigoAcessoFornecedor>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        b.Properties<decimal>().HavePrecision(18, 4);
        b.Properties<string>().HaveMaxLength(400);
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("qh");
        mb.ApplyConfigurationsFromAssembly(typeof(QualyHubDbContext).Assembly);

        foreach (var et in mb.Model.GetEntityTypes().ToList())
        {
            var clr = et.ClrType;

            // Enums gravados como texto: o banco fica legível para quem audita.
            foreach (var p in et.GetProperties().Where(p => (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType).IsEnum))
            {
                var enumType = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
                var converterType = typeof(Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToStringConverter<>).MakeGenericType(enumType);
                p.SetValueConverter((Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter)Activator.CreateInstance(converterType, [null])!);
                p.SetMaxLength(enumType.IsDefined(typeof(FlagsAttribute), false) ? 200 : 40);
            }

            // Textos longos por nome (o domínio não depende do EF): observação, objeto, especificação, justificativas…
            // (campo marcado com TamanhoFixo na configuração mantém o tamanho — ex.: entra em índice único).
            foreach (var p in et.GetProperties().Where(p => p.ClrType == typeof(string) && TextoLongo(p.Name) && p.FindAnnotation(TamanhoFixo) is null))
                p.SetMaxLength(null);

            // Sem exclusão em cascata: registro de qualidade nunca some por efeito colateral.
            foreach (var fk in et.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;

            if (typeof(EntidadeAuditavel).IsAssignableFrom(clr))
            {
                mb.Entity(clr).Property(nameof(EntidadeAuditavel.RowVersion)).IsRowVersion();
                mb.Entity(clr).Property(nameof(EntidadeAuditavel.CriadoPor)).HasMaxLength(150);
                mb.Entity(clr).Property(nameof(EntidadeAuditavel.AlteradoPor)).HasMaxLength(150);
                mb.Entity(clr).Property(nameof(EntidadeAuditavel.ExcluidoPor)).HasMaxLength(150);
                mb.Entity(clr).HasIndex(nameof(EntidadeAuditavel.EmpresaId));

                Invocar(nameof(FiltroEmpresa), clr, mb);
                if (typeof(EntidadeDaObra).IsAssignableFrom(clr))
                {
                    mb.Entity(clr).HasIndex(nameof(EntidadeDaObra.EmpresaId), nameof(EntidadeDaObra.ObraId));
                    Invocar(nameof(FiltroObra), clr, mb);
                }
                else if (typeof(IPertenceObra).IsAssignableFrom(clr))
                    Invocar(nameof(FiltroObraCadastro), clr, mb); // SPE, condomínio, tipologia, aplicação: também respeitam o acesso por obra
            }

            // Histórico completo (SQL Server temporal tables) para entidades críticas de evidência.
            if (typeof(IHistoricoTemporal).IsAssignableFrom(clr))
                mb.Entity(clr).ToTable(et.GetTableName() ?? clr.Name, t => t.IsTemporal(h => h.UseHistoryTable($"{clr.Name}_Historico", "qh_hist")));

            if (typeof(IOrigemErp).IsAssignableFrom(clr))
                mb.Entity(clr).Property(nameof(IOrigemErp.CodigoErp)).HasMaxLength(60);
        }
    }

    private static readonly string[] PrefixosLongos =
        ["Observacao", "Orientacoes", "Objeto", "Especificacao", "Justificativa", "Descricao", "Comentario", "Problema", "Acao", "Requisitos",
         "Ocorrencia", "Bloqueios", "Participantes", "DocumentosExigidos", "Motivo", "Detalhe", "Emails", "Relacionados",
         "DistribuicaoPadrao", "LeituraObrigatoria", "EficaciaComentario", "Protecao", "Armazenamento", "Recuperacao", "Descarte", "Assinatura",
         "Evidencia", "AnaliseCausa", "CausaRaiz", "Conteudo", "Medidas", "Paralisacao", "Riscos", "Resumo", "ModeloMensagem", "Pergunta", "Escopo",
         "AvaliacaoImpacto", "LocaisAfetados", "RncsOrigem", "Pendencias", "Concessao", "ItensSolicitados", "Destinatarios", "SubGrupos", "Ensaios", "Texto", "Resolucao"];

    /// <summary>Anotação: o campo mantém o tamanho configurado mesmo tendo nome de texto longo.</summary>
    public const string TamanhoFixo = "QualyHub:TamanhoFixo";

    private static bool TextoLongo(string nome) => PrefixosLongos.Any(p => nome.StartsWith(p, StringComparison.Ordinal));

    private void Invocar(string metodo, Type clr, ModelBuilder mb) =>
        GetType().GetMethod(metodo, BindingFlags.NonPublic | BindingFlags.Instance)!
                 .MakeGenericMethod(clr).Invoke(this, [mb]);

    private void FiltroEmpresa<T>(ModelBuilder mb) where T : EntidadeAuditavel =>
        mb.Entity<T>().HasQueryFilter(FiltrosNomes.Empresa, e => !e.Excluido && e.EmpresaId == EmpresaAtualId);

    private void FiltroObra<T>(ModelBuilder mb) where T : EntidadeDaObra =>
        mb.Entity<T>().HasQueryFilter(FiltrosNomes.Obra, e => AcessoTodasObras || ObrasPermitidas.Contains(e.ObraId));

    private void FiltroObraCadastro<T>(ModelBuilder mb) where T : EntidadeAuditavel, IPertenceObra =>
        mb.Entity<T>().HasQueryFilter(FiltrosNomes.Obra, e => AcessoTodasObras || ObrasPermitidas.Contains(e.ObraId));
}


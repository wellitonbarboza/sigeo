using System.Reflection;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Importacao;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Documentos;
using QualyHub.Domain.Suprimentos;
using QualyHub.Infrastructure.Contexto;

namespace QualyHub.Infrastructure.Dados;

public class QualyHubDbContext(DbContextOptions<QualyHubDbContext> options, IUsuarioAtual usuario) : DbContext(options)
{
    // Lidas pelos filtros globais a cada consulta (parametrizadas pelo EF por instância do contexto).
    public Guid EmpresaAtualId => usuario.EmpresaId;
    public bool AcessoTodasObras => usuario.AcessoTodasObras;
    public IReadOnlyCollection<Guid> ObrasPermitidas => usuario.ObrasPermitidas;

    // Estrutura
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Obra> Obras => Set<Obra>();
    public DbSet<Local> Locais => Set<Local>();
    public DbSet<CentroCusto> CentrosCusto => Set<CentroCusto>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
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
    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();
    public DbSet<FuncionarioObra> FuncionariosObras => Set<FuncionarioObra>();
    public DbSet<FuncionarioDocumento> FuncionarioDocumentos => Set<FuncionarioDocumento>();
    public DbSet<ServicoControlado> ServicosControlados => Set<ServicoControlado>();
    public DbSet<MaterialControlado> MateriaisControlados => Set<MaterialControlado>();
    public DbSet<Produto> Produtos => Set<Produto>();
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
    public DbSet<FormModelo> FormModelos => Set<FormModelo>();
    public DbSet<FormItem> FormItens => Set<FormItem>();
    public DbSet<Ficha> Fichas => Set<Ficha>();
    public DbSet<FichaResposta> FichaRespostas => Set<FichaResposta>();
    public DbSet<FichaEvidencia> FichaEvidencias => Set<FichaEvidencia>();
    public DbSet<Rnc> Rncs => Set<Rnc>();
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
    // Aprovações, importação, auditoria
    public DbSet<SolicitacaoAprovacao> SolicitacoesAprovacao => Set<SolicitacaoAprovacao>();
    public DbSet<EtapaAprovacao> EtapasAprovacao => Set<EtapaAprovacao>();
    public DbSet<LimiteAprovacao> LimitesAprovacao => Set<LimiteAprovacao>();
    public DbSet<DelegacaoAprovacao> DelegacoesAprovacao => Set<DelegacaoAprovacao>();
    public DbSet<LayoutImportacao> LayoutsImportacao => Set<LayoutImportacao>();
    public DbSet<LoteImportacao> LotesImportacao => Set<LoteImportacao>();
    public DbSet<LinhaImportacao> LinhasImportacao => Set<LinhaImportacao>();
    public DbSet<RegistroAuditoria> Auditoria => Set<RegistroAuditoria>();

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
            foreach (var p in et.GetProperties().Where(p => p.ClrType == typeof(string) && TextoLongo(p.Name)))
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
            }

            // Histórico completo (SQL Server temporal tables) para entidades críticas de evidência.
            if (typeof(IHistoricoTemporal).IsAssignableFrom(clr))
                mb.Entity(clr).ToTable(et.GetTableName() ?? clr.Name, t => t.IsTemporal(h => h.UseHistoryTable($"{clr.Name}_Historico", "qh_hist")));

            if (typeof(IOrigemObraPrima).IsAssignableFrom(clr))
                mb.Entity(clr).Property(nameof(IOrigemObraPrima.CodigoObraPrima)).HasMaxLength(60);
        }
    }

    private static readonly string[] PrefixosLongos =
        ["Observacao", "Objeto", "Especificacao", "Justificativa", "Descricao", "Comentario", "Problema", "Acao", "Requisitos",
         "Ocorrencia", "Bloqueios", "Participantes", "DocumentosExigidos", "Motivo", "Detalhe", "Emails", "Relacionados",
         "DistribuicaoPadrao", "LeituraObrigatoria", "EficaciaComentario", "Protecao", "Armazenamento", "Recuperacao", "Descarte", "Assinatura"];

    private static bool TextoLongo(string nome) => PrefixosLongos.Any(p => nome.StartsWith(p, StringComparison.Ordinal));

    private void Invocar(string metodo, Type clr, ModelBuilder mb) =>
        GetType().GetMethod(metodo, BindingFlags.NonPublic | BindingFlags.Instance)!
                 .MakeGenericMethod(clr).Invoke(this, [mb]);

    private void FiltroEmpresa<T>(ModelBuilder mb) where T : EntidadeAuditavel =>
        mb.Entity<T>().HasQueryFilter(FiltrosNomes.Empresa, e => !e.Excluido && e.EmpresaId == EmpresaAtualId);

    private void FiltroObra<T>(ModelBuilder mb) where T : EntidadeDaObra =>
        mb.Entity<T>().HasQueryFilter(FiltrosNomes.Obra, e => AcessoTodasObras || ObrasPermitidas.Contains(e.ObraId));
}

/// <summary>Nomes dos filtros globais (EF Core 10: filtros nomeados podem ser desligados individualmente).</summary>
public static class FiltrosNomes
{
    public const string Empresa = "empresa";
    public const string Obra = "obra";
}

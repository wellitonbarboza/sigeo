IF OBJECT_ID(N'[qh].[__Migracoes]') IS NULL
BEGIN
    IF SCHEMA_ID(N'qh') IS NULL EXEC(N'CREATE SCHEMA [qh];');
    CREATE TABLE [qh].[__Migracoes] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___Migracoes] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    IF SCHEMA_ID(N'qh') IS NULL EXEC(N'CREATE SCHEMA [qh];');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    IF SCHEMA_ID(N'qh_hist') IS NULL EXEC(N'CREATE SCHEMA [qh_hist];');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[AcoesCorretivas] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] nvarchar(400) NOT NULL,
        [RncsOrigem] nvarchar(max) NULL,
        [OcorrenciaId] uniqueidentifier NULL,
        [Problema] nvarchar(max) NOT NULL,
        [MetodoAnalise] nvarchar(40) NOT NULL,
        [AnaliseCausa] nvarchar(max) NULL,
        [CausaRaiz] nvarchar(max) NULL,
        [ResponsavelId] uniqueidentifier NULL,
        [ResponsavelNome] nvarchar(400) NULL,
        [Prazo] date NOT NULL,
        [DataEficacia] date NULL,
        [CriterioEficacia] nvarchar(400) NULL,
        [ResultadoEficacia] nvarchar(40) NOT NULL,
        [AtualizaSgq] bit NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AcoesCorretivas] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[AcaoCorretiva_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Alcadas] (
        [Id] uniqueidentifier NOT NULL,
        [Entidade] nvarchar(40) NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [Ordem] int NOT NULL,
        [NomeEtapa] nvarchar(400) NULL,
        [PerfilAprovador] nvarchar(40) NOT NULL,
        [UsuarioAprovadorId] uniqueidentifier NULL,
        [ValorMinimo] decimal(18,4) NULL,
        [ValorMaximo] decimal(18,4) NULL,
        [Condicao] nvarchar(400) NULL,
        [EngenheiroDaObra] bit NOT NULL,
        [SubstitutoUsuarioId] uniqueidentifier NULL,
        [PrazoHoras] int NULL,
        [EscalonaPara] nvarchar(40) NULL,
        [Ativa] bit NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Alcadas] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Anexos] (
        [Id] uniqueidentifier NOT NULL,
        [Entidade] nvarchar(60) NOT NULL,
        [EntidadeId] uniqueidentifier NOT NULL,
        [Categoria] nvarchar(60) NOT NULL,
        [NomeOriginal] nvarchar(400) NOT NULL,
        [ArquivoBlob] nvarchar(400) NOT NULL,
        [ArquivoHashSha256] nvarchar(64) NOT NULL,
        [ContentType] nvarchar(400) NULL,
        [TamanhoBytes] bigint NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Anexos] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Auditoria] (
        [Id] bigint NOT NULL IDENTITY,
        [EmpresaId] uniqueidentifier NULL,
        [Quando] datetimeoffset NOT NULL,
        [Usuario] nvarchar(400) NOT NULL,
        [UsuarioId] nvarchar(400) NULL,
        [Entidade] nvarchar(100) NOT NULL,
        [EntidadeId] nvarchar(50) NOT NULL,
        [Operacao] nvarchar(20) NOT NULL,
        [ValoresAnterioresJson] nvarchar(max) NULL,
        [ValoresNovosJson] nvarchar(max) NULL,
        [CamposAlterados] nvarchar(max) NULL,
        [Origem] nvarchar(400) NULL,
        [CorrelationId] nvarchar(400) NULL,
        CONSTRAINT [PK_Auditoria] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[AuditoriasInternas] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Escopo] nvarchar(max) NOT NULL,
        [Criterio] nvarchar(400) NOT NULL,
        [Inicio] date NOT NULL,
        [Fim] date NOT NULL,
        [Auditores] nvarchar(400) NOT NULL,
        [Independencia] nvarchar(400) NULL,
        [ResumoAnaliseCritica] nvarchar(max) NULL,
        [Status] nvarchar(40) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AuditoriasInternas] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[AuditoriaInterna_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Colaboradores] (
        [Id] uniqueidentifier NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [CpfHash] nvarchar(64) NOT NULL,
        [CpfMascarado] nvarchar(20) NOT NULL,
        [Vinculo] nvarchar(40) NOT NULL,
        [FornecedorId] uniqueidentifier NULL,
        [ContratoId] uniqueidentifier NULL,
        [FuncaoObraId] uniqueidentifier NOT NULL,
        [DataInicio] date NOT NULL,
        [DataSaida] date NULL,
        [Telefone] nvarchar(400) NULL,
        [ConsentimentoTelefone] bit NOT NULL,
        [Situacao] nvarchar(40) NOT NULL,
        [Restricoes] nvarchar(200) NOT NULL,
        [Pendencias] nvarchar(max) NULL,
        [QrToken] nvarchar(400) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Colaboradores] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Colaborador_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[DocumentosColaborador] (
        [Id] uniqueidentifier NOT NULL,
        [ColaboradorId] uniqueidentifier NOT NULL,
        [ExigenciaId] uniqueidentifier NOT NULL,
        [DataEmissao] date NOT NULL,
        [Validade] date NULL,
        [ConclusaoAso] nvarchar(40) NULL,
        [AptoAtividades] nvarchar(200) NOT NULL,
        [ArquivoBlob] nvarchar(400) NULL,
        [ArquivoNome] nvarchar(400) NULL,
        [ArquivoRestrito] bit NOT NULL,
        [Origem] nvarchar(40) NOT NULL,
        [Validacao] nvarchar(40) NOT NULL,
        [MotivoRecusa] nvarchar(max) NULL,
        [ValidadoPor] nvarchar(400) NULL,
        [ValidadoEm] datetimeoffset NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_DocumentosColaborador] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[DocumentoColaborador_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[DocumentosExternos] (
        [Id] uniqueidentifier NOT NULL,
        [Identificacao] nvarchar(400) NOT NULL,
        [Titulo] nvarchar(400) NOT NULL,
        [Edicao] nvarchar(400) NOT NULL,
        [Emissor] nvarchar(400) NOT NULL,
        [DataVerificacao] date NOT NULL,
        [Situacao] nvarchar(40) NOT NULL,
        [Relacionados] nvarchar(max) NULL,
        [ArquivoOuLink] nvarchar(400) NULL,
        [Substitui] nvarchar(400) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_DocumentosExternos] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[DocumentoExterno_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Empresas] (
        [Id] uniqueidentifier NOT NULL,
        [RazaoSocial] nvarchar(400) NOT NULL,
        [NomeFantasia] nvarchar(400) NOT NULL,
        [Cnpj] nvarchar(18) NOT NULL,
        [Codigo] nvarchar(400) NULL,
        [EnderecoEntregaPadrao] nvarchar(400) NULL,
        [NivelSiac] nvarchar(2) NOT NULL,
        [Ativa] bit NOT NULL,
        CONSTRAINT [PK_Empresas] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[EntregasEpi] (
        [Id] uniqueidentifier NOT NULL,
        [ColaboradorId] uniqueidentifier NOT NULL,
        [EpiId] uniqueidentifier NOT NULL,
        [Quantidade] decimal(18,4) NOT NULL,
        [DataEntrega] date NOT NULL,
        [Motivo] nvarchar(40) NOT NULL,
        [TrocaPrevista] date NULL,
        [Assinatura] nvarchar(max) NOT NULL,
        [EntreguePor] nvarchar(400) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EntregasEpi] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[EntregaEpi_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Epis] (
        [Id] uniqueidentifier NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Ca] nvarchar(400) NOT NULL,
        [ValidadeCa] date NOT NULL,
        [VidaUtilDias] int NULL,
        [ProdutoId] uniqueidentifier NULL,
        [Ativo] bit NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Epis] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Equipamentos] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [TipoControle] nvarchar(40) NOT NULL,
        [PadraoId] uniqueidentifier NULL,
        [PeriodicidadeMeses] int NOT NULL,
        [Criterio] nvarchar(400) NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [Responsavel] nvarchar(400) NULL,
        [UltimaData] date NULL,
        [UltimoResultado] nvarchar(40) NULL,
        [UltimoCertificado] nvarchar(400) NULL,
        [Baixado] bit NOT NULL,
        [EmCalibracao] bit NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Equipamentos] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[EquipamentoMedicao_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[EventosDocumento] (
        [Id] uniqueidentifier NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [DataHora] datetimeoffset NOT NULL,
        [UsuarioId] uniqueidentifier NULL,
        [UsuarioNome] nvarchar(400) NOT NULL,
        [Evento] nvarchar(40) NOT NULL,
        [DocumentoId] uniqueidentifier NULL,
        [RevisaoId] uniqueidentifier NULL,
        [CopiaId] uniqueidentifier NULL,
        [Referencia] nvarchar(400) NULL,
        [Detalhe] nvarchar(max) NULL,
        [Origem] nvarchar(400) NULL,
        CONSTRAINT [PK_EventosDocumento] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Exigencias] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [TreinamentoId] uniqueidentifier NULL,
        [CargaInicial] nvarchar(400) NULL,
        [ValidadeMeses] int NULL,
        [EfeitoVencimento] nvarchar(40) NOT NULL,
        [Atividades] nvarchar(200) NOT NULL,
        [Fonte] nvarchar(400) NOT NULL,
        [Ordem] int NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Exigencias] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Fornecedores] (
        [Id] uniqueidentifier NOT NULL,
        [CpfCnpj] nvarchar(18) NOT NULL,
        [RazaoSocial] nvarchar(400) NOT NULL,
        [NomeFantasia] nvarchar(400) NULL,
        [InscricaoEstadual] nvarchar(400) NULL,
        [InscricaoMunicipal] nvarchar(400) NULL,
        [Cep] nvarchar(9) NULL,
        [Endereco] nvarchar(400) NULL,
        [Numero] nvarchar(400) NULL,
        [Complemento] nvarchar(400) NULL,
        [Bairro] nvarchar(400) NULL,
        [Cidade] nvarchar(400) NULL,
        [Uf] nvarchar(2) NULL,
        [Telefones] nvarchar(400) NULL,
        [Emails] nvarchar(max) NULL,
        [NaturezaJuridica] nvarchar(400) NULL,
        [Porte] nvarchar(400) NULL,
        [SituacaoCadastral] nvarchar(400) NULL,
        [Cnae] nvarchar(400) NULL,
        [TipoFornecimento] nvarchar(200) NOT NULL,
        [StatusQualificacao] nvarchar(40) NOT NULL,
        [Ativo] bit NOT NULL,
        [NotaMedia] decimal(18,4) NULL,
        [CodigoObraPrima] nvarchar(60) NULL,
        [UltimaImportacaoId] uniqueidentifier NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Fornecedores] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Fornecedor_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Funcoes] (
        [Id] uniqueidentifier NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Cbo] nvarchar(400) NULL,
        [Riscos] nvarchar(max) NULL,
        [Ativa] bit NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Funcoes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[InspecoesSeguranca] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] nvarchar(400) NOT NULL,
        [ModeloInspecaoId] uniqueidentifier NOT NULL,
        [Modelo] nvarchar(400) NOT NULL,
        [LocalId] uniqueidentifier NOT NULL,
        [InspetorNome] nvarchar(400) NOT NULL,
        [Data] datetimeoffset NOT NULL,
        [Conformidade] decimal(18,4) NULL,
        [Paralisacao] nvarchar(max) NULL,
        [Status] nvarchar(40) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_InspecoesSeguranca] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[InspecaoSeguranca_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[LayoutsImportacao] (
        [Id] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [LinhaCabecalho] int NOT NULL,
        [NomePlanilha] nvarchar(400) NULL,
        [Delimitador] nvarchar(3) NOT NULL,
        [MapeamentoJson] nvarchar(max) NOT NULL,
        [Padrao] bit NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LayoutsImportacao] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Listas] (
        [Id] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Ordem] int NOT NULL,
        [Ativo] bit NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Listas] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[LotesImportacao] (
        [Id] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [LayoutImportacaoId] uniqueidentifier NULL,
        [ArquivoNomeOriginal] nvarchar(400) NOT NULL,
        [ArquivoBlob] nvarchar(400) NOT NULL,
        [ArquivoHashSha256] nvarchar(64) NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [TotalLinhas] int NOT NULL,
        [LinhasInserir] int NOT NULL,
        [LinhasAtualizar] int NOT NULL,
        [LinhasSemAlteracao] int NOT NULL,
        [LinhasComErro] int NOT NULL,
        [ConfirmadoEm] datetimeoffset NULL,
        [ConfirmadoPor] nvarchar(400) NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LotesImportacao] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[MateriaisControlados] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Unidade] nvarchar(400) NULL,
        [GrupoCatalogo] nvarchar(400) NULL,
        [EspecificacaoModelo] nvarchar(max) NULL,
        [DocumentosExigidos] nvarchar(max) NULL,
        [ExigeEspecificacao] bit NOT NULL,
        [ExigeFvmParaAceite] bit NOT NULL,
        [ExigeFornecedorQualificado] bit NOT NULL,
        [ConsultarPsq] bit NOT NULL,
        [SubGrupos] nvarchar(max) NULL,
        [Ensaios] nvarchar(max) NULL,
        [ConstaPsq] nvarchar(400) NOT NULL,
        [Ativo] bit NOT NULL,
        [CodigoObraPrima] nvarchar(60) NULL,
        [UltimaImportacaoId] uniqueidentifier NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_MateriaisControlados] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[ModelosInspecao] (
        [Id] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Titulo] nvarchar(400) NOT NULL,
        [ServicoControladoId] uniqueidentifier NULL,
        [MaterialControladoId] uniqueidentifier NULL,
        [NormaReferencia] nvarchar(400) NULL,
        [Amostragem] nvarchar(400) NULL,
        [QuarentenaAteEnsaio] bit NOT NULL,
        [RastrearAplicacao] bit NOT NULL,
        [DocumentoId] uniqueidentifier NULL,
        [RevisaoDocumentoId] uniqueidentifier NULL,
        [Revisao] int NOT NULL,
        [MomentoInspecao] nvarchar(400) NULL,
        [NivelLocal] nvarchar(400) NULL,
        [LiberaMedicao] bit NOT NULL,
        [ServicoSeguinteId] uniqueidentifier NULL,
        [Situacao] nvarchar(40) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ModelosInspecao] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[ModeloInspecao_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Obras] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(30) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Endereco] nvarchar(400) NULL,
        [Cidade] nvarchar(400) NULL,
        [Uf] nvarchar(2) NULL,
        [DataInicio] date NULL,
        [DataPrevistaTermino] date NULL,
        [EmpresaSpe] nvarchar(400) NULL,
        [EnderecoEntrega] nvarchar(400) NULL,
        [EngenheiroId] uniqueidentifier NULL,
        [AdministrativoId] uniqueidentifier NULL,
        [AlmoxarifeId] uniqueidentifier NULL,
        [PqoDocumentoId] uniqueidentifier NULL,
        [Status] nvarchar(40) NOT NULL,
        [Ativa] bit NOT NULL,
        [CodigoObraPrima] nvarchar(60) NULL,
        [UltimaImportacaoId] uniqueidentifier NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Obras] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Ocorrencias] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] nvarchar(400) NOT NULL,
        [DataHora] datetimeoffset NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [LocalId] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NULL,
        [ColaboradorId] uniqueidentifier NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [DiasPerdidos] int NOT NULL,
        [Cat] nvarchar(400) NULL,
        [ArquivoRestritoBlob] nvarchar(400) NULL,
        [PermissaoTrabalhoId] uniqueidentifier NULL,
        [InspecaoSegurancaId] uniqueidentifier NULL,
        [AcaoCorretivaId] uniqueidentifier NULL,
        [Status] nvarchar(40) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Ocorrencias] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Ocorrencia_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Parametros] (
        [Id] uniqueidentifier NOT NULL,
        [Chave] nvarchar(60) NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Valor] decimal(18,4) NOT NULL,
        [Unidade] nvarchar(400) NULL,
        [Grupo] nvarchar(400) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Parametros] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Parametro_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Pastas] (
        [Id] uniqueidentifier NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [PastaPaiId] uniqueidentifier NULL,
        [Ordem] int NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Pastas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pastas_Pastas_PastaPaiId] FOREIGN KEY ([PastaPaiId]) REFERENCES [qh].[Pastas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[PermissoesTrabalho] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] nvarchar(400) NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [LocalId] uniqueidentifier NOT NULL,
        [Atividade] nvarchar(400) NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [Inicio] datetimeoffset NOT NULL,
        [Fim] datetimeoffset NOT NULL,
        [Medidas] nvarchar(max) NOT NULL,
        [MedidasMarcadas] nvarchar(max) NOT NULL,
        [Emitente] nvarchar(400) NOT NULL,
        [ResponsavelArea] nvarchar(400) NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [EncerradaEm] datetimeoffset NULL,
        [EncerradaPor] nvarchar(400) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_PermissoesTrabalho] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[PermissaoTrabalho_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[RegrasNotificacao] (
        [Id] uniqueidentifier NOT NULL,
        [Evento] nvarchar(400) NOT NULL,
        [Antecedencia] nvarchar(400) NULL,
        [Repeticao] nvarchar(400) NULL,
        [Destinatarios] nvarchar(max) NOT NULL,
        [Canal] nvarchar(200) NOT NULL,
        [ModeloMensagem] nvarchar(max) NOT NULL,
        [Ativa] bit NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RegrasNotificacao] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[RegrasRetencao] (
        [Id] uniqueidentifier NOT NULL,
        [Identificacao] nvarchar(400) NOT NULL,
        [Formulario] nvarchar(400) NULL,
        [Armazenamento] nvarchar(max) NOT NULL,
        [Recuperacao] nvarchar(max) NOT NULL,
        [Protecao] nvarchar(max) NOT NULL,
        [TempoRetencao] nvarchar(400) NOT NULL,
        [TempoRetencaoAnos] int NULL,
        [Descarte] nvarchar(max) NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RegrasRetencao] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Rncs] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] nvarchar(400) NOT NULL,
        [Origem] nvarchar(40) NOT NULL,
        [Referencia] nvarchar(400) NULL,
        [ReferenciaId] uniqueidentifier NULL,
        [ObraId] uniqueidentifier NULL,
        [LocalId] uniqueidentifier NULL,
        [LocaisAfetados] nvarchar(max) NULL,
        [FornecedorId] uniqueidentifier NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Classificacao] nvarchar(40) NOT NULL,
        [Gravidade] nvarchar(40) NOT NULL,
        [Disposicao] nvarchar(400) NULL,
        [Concessao] nvarchar(max) NULL,
        [ResponsavelId] uniqueidentifier NULL,
        [ResponsavelNome] nvarchar(400) NULL,
        [Prazo] date NOT NULL,
        [ExigeAcao] bit NOT NULL,
        [AcaoCorretivaId] uniqueidentifier NULL,
        [Status] nvarchar(40) NOT NULL,
        [EncerradaPor] nvarchar(400) NULL,
        [EncerradaEm] datetimeoffset NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Rncs] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Rnc_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Sequencias] (
        [Id] uniqueidentifier NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [Chave] nvarchar(120) NOT NULL,
        [Ultimo] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Sequencias] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[SolicitacoesAprovacao] (
        [Id] uniqueidentifier NOT NULL,
        [Entidade] nvarchar(40) NOT NULL,
        [EntidadeId] uniqueidentifier NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [Titulo] nvarchar(400) NOT NULL,
        [Valor] decimal(18,4) NOT NULL,
        [SolicitanteUsuarioId] uniqueidentifier NOT NULL,
        [SolicitanteNome] nvarchar(150) NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [EtapaAtual] int NOT NULL,
        [EnviadoEm] datetimeoffset NOT NULL,
        [ConcluidoEm] datetimeoffset NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SolicitacoesAprovacao] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[SolicitacaoAprovacao_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[TiposDocumento] (
        [Id] uniqueidentifier NOT NULL,
        [AplicaA] nvarchar(40) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Descricao] nvarchar(max) NULL,
        [ValidadePadraoDias] int NULL,
        [DiasAlertaAntesVencimento] int NOT NULL,
        [Obrigatorio] bit NOT NULL,
        [ExigidoPara] nvarchar(200) NOT NULL,
        [PorEntrega] bit NOT NULL,
        [Momento] nvarchar(40) NOT NULL,
        [MaterialControladoId] uniqueidentifier NULL,
        [ServicoControladoId] uniqueidentifier NULL,
        [DiasLembrete] int NOT NULL,
        [Fonte] nvarchar(400) NULL,
        [BloqueiaQuandoVencido] bit NOT NULL,
        [ContemDadoSensivel] bit NOT NULL,
        [Ativo] bit NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TiposDocumento] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Treinamentos] (
        [Id] uniqueidentifier NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [DocumentoId] uniqueidentifier NULL,
        [CargaHoraria] nvarchar(400) NOT NULL,
        [Conteudo] nvarchar(max) NULL,
        [ValidadeMeses] int NULL,
        [MetodoEficacia] nvarchar(40) NOT NULL,
        [Ativo] bit NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Treinamentos] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Turmas] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [TreinamentoId] uniqueidentifier NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [Origem] nvarchar(400) NULL,
        [Data] datetimeoffset NOT NULL,
        [Instrutor] nvarchar(400) NOT NULL,
        [Local] nvarchar(400) NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Turmas] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Turma_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Usuarios] (
        [Id] uniqueidentifier NOT NULL,
        [EntraObjectId] uniqueidentifier NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [Perfil] nvarchar(40) NOT NULL,
        [Funcao] nvarchar(400) NULL,
        [AcessoTodasObras] bit NOT NULL,
        [Ativo] bit NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[AcaoCorretivaItens] (
        [Id] uniqueidentifier NOT NULL,
        [AcaoCorretivaId] uniqueidentifier NOT NULL,
        [Acao] nvarchar(max) NOT NULL,
        [Responsavel] nvarchar(400) NOT NULL,
        [Prazo] date NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [DocumentoId] uniqueidentifier NULL,
        [Resultado] nvarchar(400) NULL,
        CONSTRAINT [PK_AcaoCorretivaItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AcaoCorretivaItens_AcoesCorretivas_AcaoCorretivaId] FOREIGN KEY ([AcaoCorretivaId]) REFERENCES [qh].[AcoesCorretivas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Constatacoes] (
        [Id] uniqueidentifier NOT NULL,
        [AuditoriaInternaId] uniqueidentifier NOT NULL,
        [Ordem] int NOT NULL,
        [Requisito] nvarchar(400) NOT NULL,
        [Pergunta] nvarchar(max) NOT NULL,
        [Evidencia] nvarchar(max) NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [RncId] uniqueidentifier NULL,
        CONSTRAINT [PK_Constatacoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Constatacoes_AuditoriasInternas_AuditoriaInternaId] FOREIGN KEY ([AuditoriaInternaId]) REFERENCES [qh].[AuditoriasInternas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Calibracoes] (
        [Id] uniqueidentifier NOT NULL,
        [EquipamentoMedicaoId] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [Data] date NOT NULL,
        [Executante] nvarchar(400) NOT NULL,
        [LaboratorioId] uniqueidentifier NULL,
        [Certificado] nvarchar(400) NULL,
        [Erro] nvarchar(400) NOT NULL,
        [Resultado] nvarchar(40) NOT NULL,
        [AvaliacaoImpacto] nvarchar(max) NULL,
        [RegistradoPor] nvarchar(400) NULL,
        CONSTRAINT [PK_Calibracoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Calibracoes_Equipamentos_EquipamentoMedicaoId] FOREIGN KEY ([EquipamentoMedicaoId]) REFERENCES [qh].[Equipamentos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[AvaliacoesSemestrais] (
        [Id] uniqueidentifier NOT NULL,
        [Periodo] nvarchar(8) NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [QtdFvm] int NOT NULL,
        [PercFvmAprovadas] decimal(18,4) NULL,
        [QtdMedicoes] int NOT NULL,
        [NotaMedia] decimal(18,4) NULL,
        [PlanosAbertos] int NOT NULL,
        [QtdRnc] int NOT NULL,
        [Decisao] nvarchar(40) NULL,
        [Participantes] nvarchar(max) NULL,
        [DataReuniao] date NULL,
        [DataComunicado] date NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AvaliacoesSemestrais] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AvaliacoesSemestrais_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[AvaliacaoSemestral_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[LinksPortal] (
        [Id] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [TokenHash] nvarchar(64) NOT NULL,
        [ValidoAte] datetimeoffset NOT NULL,
        [Revogado] bit NOT NULL,
        [UltimoAcessoEm] datetimeoffset NULL,
        [UltimoAcessoIp] nvarchar(400) NULL,
        [EmailDestino] nvarchar(400) NULL,
        [Motivo] nvarchar(max) NOT NULL,
        [ItensSolicitados] nvarchar(max) NULL,
        [EnviadoPor] nvarchar(400) NULL,
        [Status] nvarchar(40) NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LinksPortal] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LinksPortal_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[FuncaoExigencias] (
        [Id] uniqueidentifier NOT NULL,
        [FuncaoObraId] uniqueidentifier NOT NULL,
        [ExigenciaId] uniqueidentifier NOT NULL,
        [Obrigatoriedade] nvarchar(40) NOT NULL,
        CONSTRAINT [PK_FuncaoExigencias] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FuncaoExigencias_Exigencias_ExigenciaId] FOREIGN KEY ([ExigenciaId]) REFERENCES [qh].[Exigencias] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FuncaoExigencias_Funcoes_FuncaoObraId] FOREIGN KEY ([FuncaoObraId]) REFERENCES [qh].[Funcoes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[InspecaoSegurancaItens] (
        [Id] uniqueidentifier NOT NULL,
        [InspecaoSegurancaId] uniqueidentifier NOT NULL,
        [ItemVerificacaoId] uniqueidentifier NOT NULL,
        [Ordem] int NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Referencia] nvarchar(400) NULL,
        [Resultado] nvarchar(40) NOT NULL,
        [Gravidade] nvarchar(40) NULL,
        [Responsavel] nvarchar(400) NULL,
        [Prazo] date NULL,
        [Observacao] nvarchar(max) NULL,
        [Corrigido] bit NOT NULL,
        [RncId] uniqueidentifier NULL,
        CONSTRAINT [PK_InspecaoSegurancaItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InspecaoSegurancaItens_InspecoesSeguranca_InspecaoSegurancaId] FOREIGN KEY ([InspecaoSegurancaId]) REFERENCES [qh].[InspecoesSeguranca] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[LinhasImportacao] (
        [Id] uniqueidentifier NOT NULL,
        [LoteImportacaoId] uniqueidentifier NOT NULL,
        [NumeroLinha] int NOT NULL,
        [ChaveNatural] nvarchar(100) NOT NULL,
        [DadosJson] nvarchar(max) NOT NULL,
        [Acao] nvarchar(40) NOT NULL,
        [Erros] nvarchar(max) NULL,
        [EntidadeAfetadaId] uniqueidentifier NULL,
        CONSTRAINT [PK_LinhasImportacao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LinhasImportacao_LotesImportacao_LoteImportacaoId] FOREIGN KEY ([LoteImportacaoId]) REFERENCES [qh].[LotesImportacao] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Produtos] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Unidade] nvarchar(400) NOT NULL,
        [Grupo] nvarchar(400) NULL,
        [SubGrupo] nvarchar(400) NULL,
        [LeadTimeDias] int NOT NULL,
        [MaterialControladoId] uniqueidentifier NULL,
        [Ativo] bit NOT NULL,
        [CodigoObraPrima] nvarchar(60) NULL,
        [UltimaImportacaoId] uniqueidentifier NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Produtos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Produtos_MateriaisControlados_MaterialControladoId] FOREIGN KEY ([MaterialControladoId]) REFERENCES [qh].[MateriaisControlados] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[EnsaiosModelo] (
        [Id] uniqueidentifier NOT NULL,
        [ModeloInspecaoId] uniqueidentifier NOT NULL,
        [Ensaio] nvarchar(400) NOT NULL,
        [Norma] nvarchar(400) NULL,
        [Momento] nvarchar(40) NOT NULL,
        [PrazoDias] nvarchar(400) NULL,
        [Frequencia] nvarchar(400) NOT NULL,
        [Executante] nvarchar(40) NOT NULL,
        [Criterio] nvarchar(400) NOT NULL,
        [AcaoReprovacao] nvarchar(40) NOT NULL,
        CONSTRAINT [PK_EnsaiosModelo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EnsaiosModelo_ModelosInspecao_ModeloInspecaoId] FOREIGN KEY ([ModeloInspecaoId]) REFERENCES [qh].[ModelosInspecao] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[ItensVerificacao] (
        [Id] uniqueidentifier NOT NULL,
        [ModeloInspecaoId] uniqueidentifier NOT NULL,
        [Ordem] int NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Metodo] nvarchar(400) NOT NULL,
        [ExigeEquipamento] bit NOT NULL,
        [Criterio] nvarchar(400) NOT NULL,
        [ValorMin] decimal(18,4) NULL,
        [ValorMax] decimal(18,4) NULL,
        [Foto] nvarchar(40) NOT NULL,
        [Bloqueia] bit NOT NULL,
        [Referencia] nvarchar(400) NULL,
        CONSTRAINT [PK_ItensVerificacao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ItensVerificacao_ModelosInspecao_ModeloInspecaoId] FOREIGN KEY ([ModeloInspecaoId]) REFERENCES [qh].[ModelosInspecao] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[CentrosCusto] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [Orcamento] decimal(18,4) NULL,
        [Ativo] bit NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_CentrosCusto] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CentrosCusto_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Locais] (
        [Id] uniqueidentifier NOT NULL,
        [LocalPaiId] uniqueidentifier NULL,
        [Tipo] nvarchar(400) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Ordem] int NOT NULL,
        [Quantidade] decimal(18,4) NULL,
        [Unidade] nvarchar(400) NULL,
        [QrToken] nvarchar(400) NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Locais] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Locais_Locais_LocalPaiId] FOREIGN KEY ([LocalPaiId]) REFERENCES [qh].[Locais] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Locais_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[PlanosAcao] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] nvarchar(20) NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [Origem] nvarchar(40) NOT NULL,
        [OrigemId] uniqueidentifier NULL,
        [TipoPlano] nvarchar(40) NOT NULL,
        [Problema] nvarchar(max) NOT NULL,
        [Acao] nvarchar(max) NOT NULL,
        [Responsavel] nvarchar(400) NOT NULL,
        [Prazo] date NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [Eficacia] nvarchar(40) NOT NULL,
        [EficaciaComentario] nvarchar(max) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PlanosAcao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlanosAcao_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlanosAcao_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[PlanoAcaoFornecedor_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[SolicitacoesCopia] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] int NOT NULL,
        [SolicitadoPorUsuarioId] uniqueidentifier NOT NULL,
        [SolicitadoPor] nvarchar(400) NOT NULL,
        [SolicitadoEm] datetimeoffset NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [Copiadora] nvarchar(400) NOT NULL,
        [Programacao] date NOT NULL,
        [LiberadoPor] nvarchar(400) NULL,
        [LiberadoEm] datetimeoffset NULL,
        [Status] nvarchar(40) NOT NULL,
        [MotivoRecusa] nvarchar(max) NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SolicitacoesCopia] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SolicitacoesCopia_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[ExecutantesPt] (
        [Id] uniqueidentifier NOT NULL,
        [PermissaoTrabalhoId] uniqueidentifier NOT NULL,
        [ColaboradorId] uniqueidentifier NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Papel] nvarchar(40) NOT NULL,
        [Liberado] bit NOT NULL,
        [Motivo] nvarchar(max) NULL,
        CONSTRAINT [PK_ExecutantesPt] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExecutantesPt_PermissoesTrabalho_PermissaoTrabalhoId] FOREIGN KEY ([PermissaoTrabalhoId]) REFERENCES [qh].[PermissoesTrabalho] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[FornecedorDocumentos] (
        [Id] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [TipoDocumentoExigidoId] uniqueidentifier NOT NULL,
        [Numero] nvarchar(400) NULL,
        [DataEmissao] date NULL,
        [DataValidade] date NULL,
        [Bloqueia] bit NOT NULL,
        [ArquivoBlob] nvarchar(400) NULL,
        [ArquivoNomeOriginal] nvarchar(400) NULL,
        [ArquivoHashSha256] nvarchar(64) NULL,
        [Status] nvarchar(40) NOT NULL,
        [EnviadoPeloPortal] bit NOT NULL,
        [ResponsavelEnvio] nvarchar(400) NULL,
        [MotivoRecusa] nvarchar(max) NULL,
        [Conferencia] nvarchar(400) NULL,
        [ValidadoPorUsuarioId] uniqueidentifier NULL,
        [ValidadoEm] datetimeoffset NULL,
        [Observacao] nvarchar(max) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_FornecedorDocumentos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FornecedorDocumentos_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FornecedorDocumentos_TiposDocumento_TipoDocumentoExigidoId] FOREIGN KEY ([TipoDocumentoExigidoId]) REFERENCES [qh].[TiposDocumento] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[FornecedorDocumento_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Participantes] (
        [Id] uniqueidentifier NOT NULL,
        [TurmaId] uniqueidentifier NOT NULL,
        [ColaboradorId] uniqueidentifier NOT NULL,
        [Presenca] bit NOT NULL,
        [Avaliacao] nvarchar(400) NULL,
        [Resultado] nvarchar(40) NULL,
        [ValidoAte] date NULL,
        CONSTRAINT [PK_Participantes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Participantes_Turmas_TurmaId] FOREIGN KEY ([TurmaId]) REFERENCES [qh].[Turmas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[DelegacoesAprovacao] (
        [Id] uniqueidentifier NOT NULL,
        [TitularUsuarioId] uniqueidentifier NOT NULL,
        [SubstitutoUsuarioId] uniqueidentifier NOT NULL,
        [Inicio] datetimeoffset NOT NULL,
        [Fim] datetimeoffset NOT NULL,
        [Entidade] nvarchar(40) NULL,
        [ObraId] uniqueidentifier NULL,
        [Motivo] nvarchar(max) NOT NULL,
        [Revogada] bit NOT NULL,
        [RevogadaEm] datetimeoffset NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_DelegacoesAprovacao] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Delegacao_Periodo] CHECK ([Fim] > [Inicio]),
        CONSTRAINT [CK_Delegacao_Pessoas] CHECK ([TitularUsuarioId] <> [SubstitutoUsuarioId]),
        CONSTRAINT [FK_DelegacoesAprovacao_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DelegacoesAprovacao_Usuarios_SubstitutoUsuarioId] FOREIGN KEY ([SubstitutoUsuarioId]) REFERENCES [qh].[Usuarios] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DelegacoesAprovacao_Usuarios_TitularUsuarioId] FOREIGN KEY ([TitularUsuarioId]) REFERENCES [qh].[Usuarios] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[DelegacaoAprovacao_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[LimitesAprovacao] (
        [Id] uniqueidentifier NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        [Entidade] nvarchar(40) NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [ValorMaximo] decimal(18,4) NOT NULL,
        [Ativo] bit NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LimitesAprovacao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LimitesAprovacao_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LimitesAprovacao_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [qh].[Usuarios] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[LimiteAprovacao_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Permissoes] (
        [Id] uniqueidentifier NOT NULL,
        [Escopo] nvarchar(40) NOT NULL,
        [AlvoId] uniqueidentifier NOT NULL,
        [Perfil] nvarchar(40) NULL,
        [UsuarioId] uniqueidentifier NULL,
        [PodeVer] bit NOT NULL,
        [PodeBaixar] bit NOT NULL,
        [PodeImprimir] bit NOT NULL,
        [PodeEditar] bit NOT NULL,
        [PodeAprovar] bit NOT NULL,
        [PodeVerObsoletos] bit NOT NULL,
        [ValidoAte] date NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Permissoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Permissoes_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [qh].[Usuarios] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[PermissaoDocumento_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[UsuariosObras] (
        [Id] uniqueidentifier NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UsuariosObras] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UsuariosObras_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UsuariosObras_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [qh].[Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Requisicoes] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] int NOT NULL,
        [EnderecoEntrega] nvarchar(400) NULL,
        [CentroCustoId] uniqueidentifier NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [CategoriaServico] nvarchar(400) NULL,
        [Atividade] nvarchar(400) NOT NULL,
        [DataEntrega] date NOT NULL,
        [PrazoCronogramaDias] int NOT NULL,
        [Observacao] nvarchar(max) NULL,
        [Status] nvarchar(40) NOT NULL,
        [SolicitacaoAprovacaoId] uniqueidentifier NULL,
        [AprovadoEngenheiroPor] nvarchar(400) NULL,
        [AprovadoEngenheiroEm] datetimeoffset NULL,
        [AprovadoDiretorPor] nvarchar(400) NULL,
        [AprovadoDiretorEm] datetimeoffset NULL,
        [MotivoReprovacao] nvarchar(max) NULL,
        [SemOrcamento] bit NOT NULL,
        [JustificativaSemOrcamento] nvarchar(max) NULL,
        [ChaveNFe] nvarchar(44) NULL,
        [SolicitanteUsuarioId] uniqueidentifier NOT NULL,
        [SolicitanteNome] nvarchar(400) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Requisicoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Requisicoes_CentrosCusto_CentroCustoId] FOREIGN KEY ([CentroCustoId]) REFERENCES [qh].[CentrosCusto] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Requisicoes_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Requisicao_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Fvs] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] nvarchar(20) NOT NULL,
        [LocalId] uniqueidentifier NOT NULL,
        [ModeloInspecaoId] uniqueidentifier NOT NULL,
        [RevisaoModelo] int NOT NULL,
        [ServicoControladoId] uniqueidentifier NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [ContratoId] uniqueidentifier NULL,
        [InspetorId] uniqueidentifier NOT NULL,
        [InspetorNome] nvarchar(400) NOT NULL,
        [DataInspecao] datetimeoffset NOT NULL,
        [EquipamentoId] uniqueidentifier NULL,
        [Disposicao] nvarchar(400) NULL,
        [PrazoCorrecao] date NULL,
        [RncId] uniqueidentifier NULL,
        [FvsOrigemId] uniqueidentifier NULL,
        [DataReinspecao] date NULL,
        [Status] nvarchar(40) NOT NULL,
        [LiberadoPorId] uniqueidentifier NULL,
        [LiberadoPorNome] nvarchar(400) NULL,
        [DataLiberacao] datetimeoffset NULL,
        [Origem] nvarchar(40) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Fvs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Fvs_Locais_LocalId] FOREIGN KEY ([LocalId]) REFERENCES [qh].[Locais] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Fvs_ModelosInspecao_ModeloInspecaoId] FOREIGN KEY ([ModeloInspecaoId]) REFERENCES [qh].[ModelosInspecao] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Fvs_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[EtapasAprovacao] (
        [Id] uniqueidentifier NOT NULL,
        [SolicitacaoAprovacaoId] uniqueidentifier NOT NULL,
        [Ordem] int NOT NULL,
        [PerfilAprovador] nvarchar(40) NOT NULL,
        [UsuarioAprovadorId] uniqueidentifier NULL,
        [Decisao] nvarchar(40) NOT NULL,
        [DecididoPorUsuarioId] uniqueidentifier NULL,
        [DecididoPorNome] nvarchar(400) NULL,
        [DecididoEm] datetimeoffset NULL,
        [Justificativa] nvarchar(max) NULL,
        [EmNomeDeUsuarioId] uniqueidentifier NULL,
        [EmNomeDeNome] nvarchar(150) NULL,
        [DelegacaoAprovacaoId] uniqueidentifier NULL,
        CONSTRAINT [PK_EtapasAprovacao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EtapasAprovacao_DelegacoesAprovacao_DelegacaoAprovacaoId] FOREIGN KEY ([DelegacaoAprovacaoId]) REFERENCES [qh].[DelegacoesAprovacao] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EtapasAprovacao_SolicitacoesAprovacao_SolicitacaoAprovacaoId] FOREIGN KEY ([SolicitacaoAprovacaoId]) REFERENCES [qh].[SolicitacoesAprovacao] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Cotacoes] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] int NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [RequisicaoId] uniqueidentifier NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [Frete] nvarchar(40) NOT NULL,
        [EnderecoEntrega] nvarchar(400) NULL,
        [ValorEstimado] decimal(18,4) NOT NULL,
        [PropostasExigidas] int NOT NULL,
        [PropostasRecebidas] int NOT NULL,
        [JustificativaPropostas] nvarchar(max) NULL,
        [FornecedorVencedorId] uniqueidentifier NULL,
        [CriterioSelecao] nvarchar(40) NULL,
        [SolicitacaoAprovacaoId] uniqueidentifier NULL,
        [AprovadoPor] nvarchar(400) NULL,
        [AprovadoEm] datetimeoffset NULL,
        [Concluido] bit NOT NULL,
        [SemOrcamento] bit NOT NULL,
        [ChaveNFe] nvarchar(44) NULL,
        [Status] nvarchar(40) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Cotacoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cotacoes_Fornecedores_FornecedorVencedorId] FOREIGN KEY ([FornecedorVencedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cotacoes_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cotacoes_Requisicoes_RequisicaoId] FOREIGN KEY ([RequisicaoId]) REFERENCES [qh].[Requisicoes] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Cotacao_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[FvsItens] (
        [Id] uniqueidentifier NOT NULL,
        [FvsId] uniqueidentifier NOT NULL,
        [ItemVerificacaoId] uniqueidentifier NOT NULL,
        [Ordem] int NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Metodo] nvarchar(400) NOT NULL,
        [Criterio] nvarchar(400) NOT NULL,
        [ValorMin] decimal(18,4) NULL,
        [ValorMax] decimal(18,4) NULL,
        [Foto] nvarchar(40) NOT NULL,
        [Bloqueia] bit NOT NULL,
        [ExigeEquipamento] bit NOT NULL,
        [Resultado] nvarchar(40) NOT NULL,
        [ValorMedido] decimal(18,4) NULL,
        [Observacao] nvarchar(max) NULL,
        [ResultadoReinspecao] nvarchar(40) NULL,
        CONSTRAINT [PK_FvsItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FvsItens_Fvs_FvsId] FOREIGN KEY ([FvsId]) REFERENCES [qh].[Fvs] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Contratos] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] nvarchar(30) NOT NULL,
        [CentroCustoId] uniqueidentifier NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [RequisicaoId] uniqueidentifier NULL,
        [CotacaoId] uniqueidentifier NULL,
        [Categoria] nvarchar(400) NOT NULL,
        [Objeto] nvarchar(max) NOT NULL,
        [RequisitosLaboratorio] nvarchar(max) NULL,
        [RequisitosDesempenho] nvarchar(max) NULL,
        [RequisitosEquipamento] nvarchar(max) NULL,
        [RetencaoPerc] decimal(18,4) NOT NULL,
        [VigenciaInicio] date NOT NULL,
        [VigenciaFim] date NOT NULL,
        [Formalizacao] nvarchar(40) NOT NULL,
        [SolicitacaoAprovacaoId] uniqueidentifier NULL,
        [AprovadoDiretoriaPor] nvarchar(400) NULL,
        [AprovadoDiretoriaEm] datetimeoffset NULL,
        [AssinaturaEmpresa] date NULL,
        [AssinaturaFornecedor] date NULL,
        [EnviadoFornecedorEm] datetimeoffset NULL,
        [Status] nvarchar(40) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Contratos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Contratos_CentrosCusto_CentroCustoId] FOREIGN KEY ([CentroCustoId]) REFERENCES [qh].[CentrosCusto] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Contratos_Cotacoes_CotacaoId] FOREIGN KEY ([CotacaoId]) REFERENCES [qh].[Cotacoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Contratos_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Contratos_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Contratos_Requisicoes_RequisicaoId] FOREIGN KEY ([RequisicaoId]) REFERENCES [qh].[Requisicoes] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Contrato_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[CotacaoFornecedores] (
        [Id] uniqueidentifier NOT NULL,
        [CotacaoId] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [LinkResposta] uniqueidentifier NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [DataResposta] date NULL,
        [ValorFrete] decimal(18,4) NULL,
        [QualificacaoNaData] nvarchar(40) NOT NULL,
        [PrazoEntregaOfertado] date NULL,
        [CondicaoPagamento] nvarchar(400) NULL,
        [ValidadeProposta] date NULL,
        [TemAnexoProposta] bit NOT NULL,
        CONSTRAINT [PK_CotacaoFornecedores] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CotacaoFornecedores_Cotacoes_CotacaoId] FOREIGN KEY ([CotacaoId]) REFERENCES [qh].[Cotacoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CotacaoFornecedores_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Aditivos] (
        [Id] uniqueidentifier NOT NULL,
        [ContratoId] uniqueidentifier NOT NULL,
        [Numero] nvarchar(400) NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [Origem] nvarchar(400) NULL,
        [Justificativa] nvarchar(max) NOT NULL,
        [NovaVigencia] date NULL,
        [Valor] decimal(18,4) NOT NULL,
        [PercAcumulado] decimal(18,4) NOT NULL,
        [SolicitacaoAprovacaoId] uniqueidentifier NULL,
        [AprovadoPor] nvarchar(400) NULL,
        [AprovadoEm] datetimeoffset NULL,
        [AssinaturaEmpresa] date NULL,
        [AssinaturaFornecedor] date NULL,
        [Status] nvarchar(40) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Aditivos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Aditivos_Contratos_ContratoId] FOREIGN KEY ([ContratoId]) REFERENCES [qh].[Contratos] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Aditivo_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Medicoes] (
        [Id] uniqueidentifier NOT NULL,
        [ContratoId] uniqueidentifier NOT NULL,
        [NumeroMedicao] int NOT NULL,
        [PeriodoInicio] date NOT NULL,
        [PeriodoFim] date NOT NULL,
        [ValorPeriodo] decimal(18,4) NOT NULL,
        [ValorAcumulado] decimal(18,4) NOT NULL,
        [Saldo] decimal(18,4) NOT NULL,
        [ValorRetencao] decimal(18,4) NOT NULL,
        [NotaEscopo] decimal(5,2) NULL,
        [NotaMaterial] decimal(5,2) NULL,
        [NotaServico] decimal(5,2) NULL,
        [NotaLimpeza] decimal(5,2) NULL,
        [NotaDocSeg] decimal(5,2) NULL,
        [NotaFinal] decimal(5,2) NULL,
        [Faixa] nvarchar(40) NOT NULL,
        [DataReuniao] date NULL,
        [AvaliadorUsuarioId] uniqueidentifier NULL,
        [AvaliadorNome] nvarchar(400) NULL,
        [AvaliadoEm] datetimeoffset NULL,
        [SolicitacaoAprovacaoId] uniqueidentifier NULL,
        [AprovadoDiretorPor] nvarchar(400) NULL,
        [AprovadoDiretorEm] datetimeoffset NULL,
        [MotivoReprovacao] nvarchar(max) NULL,
        [ChaveNFe] nvarchar(60) NULL,
        [Status] nvarchar(40) NOT NULL,
        [Bloqueios] nvarchar(max) NULL,
        [PlanoAcaoId] uniqueidentifier NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Medicoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Medicoes_Contratos_ContratoId] FOREIGN KEY ([ContratoId]) REFERENCES [qh].[Contratos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Medicoes_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Medicoes_PlanosAcao_PlanoAcaoId] FOREIGN KEY ([PlanoAcaoId]) REFERENCES [qh].[PlanosAcao] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Medicao_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Pedidos] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] int NOT NULL,
        [CotacaoId] uniqueidentifier NULL,
        [RequisicaoId] uniqueidentifier NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [CondicaoPagamento] nvarchar(400) NULL,
        [Frete] nvarchar(40) NOT NULL,
        [Transportadora] nvarchar(400) NULL,
        [ValorFrete] decimal(18,4) NULL,
        [Entrega] date NOT NULL,
        [EnderecoEntrega] nvarchar(400) NULL,
        [TipoDocumento] nvarchar(40) NOT NULL,
        [ContratoId] uniqueidentifier NULL,
        [EspecificacoesIncluidas] bit NOT NULL,
        [SolicitacaoAprovacaoId] uniqueidentifier NULL,
        [AprovadoDiretorPor] nvarchar(400) NULL,
        [AprovadoDiretorEm] datetimeoffset NULL,
        [EnviadoFornecedorEm] datetimeoffset NULL,
        [EmailsCopia] nvarchar(max) NULL,
        [Status] nvarchar(40) NOT NULL,
        [SemOrcamento] bit NOT NULL,
        [ChaveNFe] nvarchar(44) NULL,
        [Bloqueios] nvarchar(max) NULL,
        [CodigoObraPrima] nvarchar(60) NULL,
        [UltimaImportacaoId] uniqueidentifier NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Pedidos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pedidos_Contratos_ContratoId] FOREIGN KEY ([ContratoId]) REFERENCES [qh].[Contratos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pedidos_Cotacoes_CotacaoId] FOREIGN KEY ([CotacaoId]) REFERENCES [qh].[Cotacoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pedidos_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pedidos_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pedidos_Requisicoes_RequisicaoId] FOREIGN KEY ([RequisicaoId]) REFERENCES [qh].[Requisicoes] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Pedido_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[AditivoItens] (
        [Id] uniqueidentifier NOT NULL,
        [AditivoId] uniqueidentifier NOT NULL,
        [ContratoItemId] uniqueidentifier NULL,
        [Item] nvarchar(400) NULL,
        [Descricao] nvarchar(max) NULL,
        [Unidade] nvarchar(400) NULL,
        [ServicoControladoId] uniqueidentifier NULL,
        [QtdOriginal] decimal(18,4) NOT NULL,
        [QtdDelta] decimal(18,4) NOT NULL,
        [PrecoUnitario] decimal(18,4) NOT NULL,
        [PrecoOriginal] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_AditivoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AditivoItens_Aditivos_AditivoId] FOREIGN KEY ([AditivoId]) REFERENCES [qh].[Aditivos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Grds] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] nvarchar(30) NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [Contato] nvarchar(400) NULL,
        [ContratoId] uniqueidentifier NULL,
        [PedidoId] uniqueidentifier NULL,
        [EnviadoEm] datetimeoffset NULL,
        [EnviadoPor] nvarchar(400) NULL,
        [TokenConfirmacao] uniqueidentifier NOT NULL,
        [ConfirmadoEm] datetimeoffset NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Grds] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Grds_Contratos_ContratoId] FOREIGN KEY ([ContratoId]) REFERENCES [qh].[Contratos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Grds_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Grds_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [qh].[Pedidos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Pagamentos] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] int NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [Origem] nvarchar(40) NOT NULL,
        [ChaveNFe] nvarchar(44) NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [PedidoId] uniqueidentifier NULL,
        [ContratoId] uniqueidentifier NULL,
        [MedicaoId] uniqueidentifier NULL,
        [ValorNF] decimal(18,4) NOT NULL,
        [ValorLiberado] decimal(18,4) NOT NULL,
        [JustificativaDiferenca] nvarchar(max) NULL,
        [RecebimentoOk] bit NOT NULL,
        [EncaminhadoPor] nvarchar(400) NULL,
        [EncaminhadoEm] datetimeoffset NULL,
        [SolicitacaoAprovacaoId] uniqueidentifier NULL,
        [LiberadoPor] nvarchar(400) NULL,
        [LiberadoEm] datetimeoffset NULL,
        [Vencimento] date NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [Bloqueios] nvarchar(max) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Pagamentos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pagamentos_Contratos_ContratoId] FOREIGN KEY ([ContratoId]) REFERENCES [qh].[Contratos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pagamentos_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pagamentos_Medicoes_MedicaoId] FOREIGN KEY ([MedicaoId]) REFERENCES [qh].[Medicoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pagamentos_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pagamentos_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [qh].[Pedidos] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[LiberacaoPagamento_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Recebimentos] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] int NOT NULL,
        [PedidoId] uniqueidentifier NULL,
        [ContratoId] uniqueidentifier NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [ChaveNFe] nvarchar(44) NULL,
        [NumeroNotaFiscal] nvarchar(400) NULL,
        [DataRecebimento] datetimeoffset NOT NULL,
        [RecebidoPorUsuarioId] uniqueidentifier NULL,
        [RecebidoPorNome] nvarchar(400) NOT NULL,
        [FvmPrazo] nvarchar(40) NOT NULL,
        [FvmQualidade] nvarchar(40) NOT NULL,
        [FvmAtendimento] nvarchar(40) NOT NULL,
        [Ocorrencia] nvarchar(max) NULL,
        [Destino] nvarchar(40) NOT NULL,
        [ComunicadoA] nvarchar(400) NULL,
        [ComunicadoEm] datetimeoffset NULL,
        [ModeloFvmId] uniqueidentifier NULL,
        [StatusLote] nvarchar(40) NOT NULL,
        [QuarentenaAteEnsaio] bit NOT NULL,
        [RastrearAplicacao] bit NOT NULL,
        [RncId] uniqueidentifier NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Recebimentos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Recebimentos_Contratos_ContratoId] FOREIGN KEY ([ContratoId]) REFERENCES [qh].[Contratos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Recebimentos_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Recebimentos_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Recebimentos_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [qh].[Pedidos] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Recebimento_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[AplicacoesLote] (
        [Id] uniqueidentifier NOT NULL,
        [RecebimentoId] uniqueidentifier NOT NULL,
        [LocalId] uniqueidentifier NOT NULL,
        [Quantidade] decimal(18,4) NOT NULL,
        [Data] date NOT NULL,
        [FvsId] uniqueidentifier NULL,
        [RegistradoPor] nvarchar(400) NOT NULL,
        [RegistradoEm] datetimeoffset NOT NULL,
        CONSTRAINT [PK_AplicacoesLote] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AplicacoesLote_Recebimentos_RecebimentoId] FOREIGN KEY ([RecebimentoId]) REFERENCES [qh].[Recebimentos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[EnsaiosRecebimento] (
        [Id] uniqueidentifier NOT NULL,
        [RecebimentoId] uniqueidentifier NOT NULL,
        [EnsaioModeloId] uniqueidentifier NOT NULL,
        [Ensaio] nvarchar(400) NOT NULL,
        [Criterio] nvarchar(400) NOT NULL,
        [AcaoReprovacao] nvarchar(40) NOT NULL,
        [Amostra] nvarchar(400) NOT NULL,
        [DataColeta] date NOT NULL,
        [DataPrevista] date NULL,
        [LaboratorioId] uniqueidentifier NULL,
        [Resultado] nvarchar(400) NULL,
        [Situacao] nvarchar(40) NOT NULL,
        [RncId] uniqueidentifier NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EnsaiosRecebimento] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EnsaiosRecebimento_Recebimentos_RecebimentoId] FOREIGN KEY ([RecebimentoId]) REFERENCES [qh].[Recebimentos] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[EnsaioRecebimento_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[FvmItens] (
        [Id] uniqueidentifier NOT NULL,
        [RecebimentoId] uniqueidentifier NOT NULL,
        [RecebimentoItemId] uniqueidentifier NULL,
        [ItemVerificacaoId] uniqueidentifier NOT NULL,
        [Ordem] int NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Criterio] nvarchar(400) NOT NULL,
        [ValorMin] decimal(18,4) NULL,
        [ValorMax] decimal(18,4) NULL,
        [Bloqueia] bit NOT NULL,
        [ValorMedido] nvarchar(400) NULL,
        [Resultado] nvarchar(40) NOT NULL,
        [Observacao] nvarchar(max) NULL,
        CONSTRAINT [PK_FvmItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FvmItens_Recebimentos_RecebimentoId] FOREIGN KEY ([RecebimentoId]) REFERENCES [qh].[Recebimentos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Checkouts] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentoRevisaoId] uniqueidentifier NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        [UsuarioNome] nvarchar(400) NOT NULL,
        [CheckoutEm] datetimeoffset NOT NULL,
        [Prazo] date NOT NULL,
        [CheckinEm] datetimeoffset NULL,
        [LiberadoForcadoPor] nvarchar(400) NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Checkouts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Ciencias] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentoRevisaoId] uniqueidentifier NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        [UsuarioNome] nvarchar(400) NOT NULL,
        [Funcao] nvarchar(400) NULL,
        [Prazo] date NOT NULL,
        [LidoEm] datetimeoffset NULL,
        [ExigeTreinamento] bit NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Ciencias] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Ciencias_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [qh].[Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[ContratoItens] (
        [Id] uniqueidentifier NOT NULL,
        [ContratoId] uniqueidentifier NOT NULL,
        [Item] nvarchar(400) NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [ServicoControladoId] uniqueidentifier NULL,
        [Unidade] nvarchar(400) NOT NULL,
        [Quantidade] decimal(18,3) NOT NULL,
        [PrecoUnitario] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_ContratoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContratoItens_Contratos_ContratoId] FOREIGN KEY ([ContratoId]) REFERENCES [qh].[Contratos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[MedicaoItens] (
        [Id] uniqueidentifier NOT NULL,
        [MedicaoId] uniqueidentifier NOT NULL,
        [ContratoItemId] uniqueidentifier NOT NULL,
        [LocalId] uniqueidentifier NULL,
        [QtdAnterior] decimal(18,3) NOT NULL,
        [QtdPeriodo] decimal(18,3) NOT NULL,
        [QtdAcumulada] decimal(18,3) NOT NULL,
        [PercExecutado] decimal(18,4) NOT NULL,
        [ValorPeriodo] decimal(18,4) NOT NULL,
        [FvsRef] nvarchar(400) NULL,
        [FvsId] uniqueidentifier NULL,
        CONSTRAINT [PK_MedicaoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MedicaoItens_ContratoItens_ContratoItemId] FOREIGN KEY ([ContratoItemId]) REFERENCES [qh].[ContratoItens] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MedicaoItens_Locais_LocalId] FOREIGN KEY ([LocalId]) REFERENCES [qh].[Locais] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MedicaoItens_Medicoes_MedicaoId] FOREIGN KEY ([MedicaoId]) REFERENCES [qh].[Medicoes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[ContratoServicos] (
        [Id] uniqueidentifier NOT NULL,
        [ContratoId] uniqueidentifier NOT NULL,
        [ServicoControladoId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ContratoServicos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContratoServicos_Contratos_ContratoId] FOREIGN KEY ([ContratoId]) REFERENCES [qh].[Contratos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Copias] (
        [Id] uniqueidentifier NOT NULL,
        [NumeroCopia] nvarchar(60) NOT NULL,
        [DocumentoRevisaoId] uniqueidentifier NOT NULL,
        [TipoCopia] nvarchar(40) NOT NULL,
        [Destinatario] nvarchar(400) NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [SolicitacaoCopiaId] uniqueidentifier NULL,
        [GrdId] uniqueidentifier NULL,
        [EntregueEm] datetimeoffset NULL,
        [RecebidoPor] nvarchar(400) NULL,
        [QrToken] nvarchar(32) NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [PrazoRecolhimento] date NULL,
        [RecolhidaEm] datetimeoffset NULL,
        [RecolhidaPor] nvarchar(400) NULL,
        [DestinoObsoleta] nvarchar(40) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Copias] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Copias_Grds_GrdId] FOREIGN KEY ([GrdId]) REFERENCES [qh].[Grds] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Copias_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Copias_SolicitacoesCopia_SolicitacaoCopiaId] FOREIGN KEY ([SolicitacaoCopiaId]) REFERENCES [qh].[SolicitacoesCopia] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[CopiaControlada_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[CotacaoItens] (
        [Id] uniqueidentifier NOT NULL,
        [CotacaoId] uniqueidentifier NOT NULL,
        [RequisicaoItemId] uniqueidentifier NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Unidade] nvarchar(400) NOT NULL,
        [Quantidade] decimal(18,3) NOT NULL,
        [ValorOfertado] decimal(18,4) NOT NULL,
        [ValorNegociado] decimal(18,4) NULL,
        [Entrega] date NULL,
        [MaterialControlado] bit NOT NULL,
        [AtendeEspecificacao] bit NULL,
        CONSTRAINT [PK_CotacaoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CotacaoItens_Cotacoes_CotacaoId] FOREIGN KEY ([CotacaoId]) REFERENCES [qh].[Cotacoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CotacaoItens_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Documentos] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(40) NOT NULL,
        [Titulo] nvarchar(400) NOT NULL,
        [Tipo] nvarchar(400) NOT NULL,
        [PastaId] uniqueidentifier NOT NULL,
        [RequisitoSiac] nvarchar(400) NULL,
        [Origem] nvarchar(40) NOT NULL,
        [Abrangencia] nvarchar(40) NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [ResponsavelUsuarioId] uniqueidentifier NULL,
        [ResponsavelNome] nvarchar(400) NULL,
        [AprovadorUsuarioId] uniqueidentifier NULL,
        [AprovadorNome] nvarchar(400) NULL,
        [RevisaoVigenteId] uniqueidentifier NULL,
        [DataVigencia] date NULL,
        [Status] nvarchar(40) NOT NULL,
        [PeriodicidadeMeses] int NOT NULL,
        [ProximaAnalise] date NULL,
        [NivelAcesso] nvarchar(40) NOT NULL,
        [DistribuicaoPadrao] nvarchar(max) NULL,
        [LeituraObrigatoria] nvarchar(max) NULL,
        [GeraRegistro] bit NOT NULL,
        [RegraRetencaoId] uniqueidentifier NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Documentos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Documentos_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Documentos_Pastas_PastaId] FOREIGN KEY ([PastaId]) REFERENCES [qh].[Pastas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Documentos_RegrasRetencao_RegraRetencaoId] FOREIGN KEY ([RegraRetencaoId]) REFERENCES [qh].[RegrasRetencao] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Documento_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[DocumentosRelacionados] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentoId] uniqueidentifier NOT NULL,
        [RelacionadoId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_DocumentosRelacionados] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentosRelacionados_Documentos_DocumentoId] FOREIGN KEY ([DocumentoId]) REFERENCES [qh].[Documentos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentosRelacionados_Documentos_RelacionadoId] FOREIGN KEY ([RelacionadoId]) REFERENCES [qh].[Documentos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Revisoes] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentoId] uniqueidentifier NOT NULL,
        [Revisao] int NOT NULL,
        [ArquivoEditavelBlob] nvarchar(400) NULL,
        [ArquivoEditavelNome] nvarchar(400) NULL,
        [ArquivoPublicadoBlob] nvarchar(400) NULL,
        [ArquivoPublicadoNome] nvarchar(400) NULL,
        [HashSha256] nvarchar(64) NULL,
        [DescricaoAlteracao] nvarchar(max) NOT NULL,
        [MotivoRevisao] nvarchar(40) NOT NULL,
        [OrigemRevisao] nvarchar(400) NULL,
        [ElaboradoPorUsuarioId] uniqueidentifier NOT NULL,
        [ElaboradoPor] nvarchar(400) NOT NULL,
        [ElaboradoEm] datetimeoffset NOT NULL,
        [AnalisadoPor] nvarchar(400) NULL,
        [AnalisadoEm] datetimeoffset NULL,
        [AprovadoPor] nvarchar(400) NULL,
        [AprovadoEm] datetimeoffset NULL,
        [DataVigencia] date NULL,
        [Status] nvarchar(40) NOT NULL,
        [ObsoletoEm] date NULL,
        [RetidoAte] date NULL,
        [RecolhimentoOk] bit NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Revisoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Revisoes_Documentos_DocumentoId] FOREIGN KEY ([DocumentoId]) REFERENCES [qh].[Documentos] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[DocumentoRevisao_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[ServicosControlados] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [DocumentoItsId] uniqueidentifier NULL,
        [ExigeFvsParaMedicao] bit NOT NULL,
        [AvaliadoNaMedicao] bit NOT NULL,
        [Ativo] bit NOT NULL,
        [CodigoObraPrima] nvarchar(60) NULL,
        [UltimaImportacaoId] uniqueidentifier NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ServicosControlados] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServicosControlados_Documentos_DocumentoItsId] FOREIGN KEY ([DocumentoItsId]) REFERENCES [qh].[Documentos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[EtapasRevisao] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentoRevisaoId] uniqueidentifier NOT NULL,
        [Etapa] nvarchar(40) NOT NULL,
        [ResponsavelUsuarioId] uniqueidentifier NULL,
        [ResponsavelNome] nvarchar(400) NULL,
        [Prazo] date NULL,
        [Decisao] nvarchar(40) NOT NULL,
        [Comentario] nvarchar(max) NULL,
        [DataDecisao] datetimeoffset NULL,
        [Assinatura] nvarchar(max) NULL,
        [Historico] bit NOT NULL,
        CONSTRAINT [PK_EtapasRevisao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EtapasRevisao_Revisoes_DocumentoRevisaoId] FOREIGN KEY ([DocumentoRevisaoId]) REFERENCES [qh].[Revisoes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[GrdItens] (
        [Id] uniqueidentifier NOT NULL,
        [GrdId] uniqueidentifier NOT NULL,
        [DocumentoRevisaoId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_GrdItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GrdItens_Grds_GrdId] FOREIGN KEY ([GrdId]) REFERENCES [qh].[Grds] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GrdItens_Revisoes_DocumentoRevisaoId] FOREIGN KEY ([DocumentoRevisaoId]) REFERENCES [qh].[Revisoes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[SolicitacaoCopiaItens] (
        [Id] uniqueidentifier NOT NULL,
        [SolicitacaoCopiaId] uniqueidentifier NOT NULL,
        [DocumentoRevisaoId] uniqueidentifier NOT NULL,
        [Quantidade] int NOT NULL,
        [Destinatario] nvarchar(400) NOT NULL,
        CONSTRAINT [PK_SolicitacaoCopiaItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SolicitacaoCopiaItens_Revisoes_DocumentoRevisaoId] FOREIGN KEY ([DocumentoRevisaoId]) REFERENCES [qh].[Revisoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SolicitacaoCopiaItens_SolicitacoesCopia_SolicitacaoCopiaId] FOREIGN KEY ([SolicitacaoCopiaId]) REFERENCES [qh].[SolicitacoesCopia] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Qualificacoes] (
        [Id] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [TipoFornecimento] nvarchar(200) NOT NULL,
        [MaterialControladoId] uniqueidentifier NULL,
        [ServicoControladoId] uniqueidentifier NULL,
        [CriterioQualificacao] nvarchar(400) NOT NULL,
        [Psq] nvarchar(40) NOT NULL,
        [DataConsultaPsq] date NULL,
        [CertificadoSbac] nvarchar(400) NULL,
        [LaboratorioEnquadramento] nvarchar(40) NOT NULL,
        [AvaliadorLaboratorio] nvarchar(400) NULL,
        [DataQualificacao] date NOT NULL,
        [DataValidade] date NULL,
        [Resultado] nvarchar(40) NOT NULL,
        [ResponsavelUsuarioId] uniqueidentifier NULL,
        [ResponsavelNome] nvarchar(400) NULL,
        [QualificadoPorHistorico] bit NOT NULL,
        [Observacao] nvarchar(max) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Qualificacoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Qualificacoes_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Qualificacoes_MateriaisControlados_MaterialControladoId] FOREIGN KEY ([MaterialControladoId]) REFERENCES [qh].[MateriaisControlados] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Qualificacoes_ServicosControlados_ServicoControladoId] FOREIGN KEY ([ServicoControladoId]) REFERENCES [qh].[ServicosControlados] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[FornecedorQualificacao_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[RequisicaoItens] (
        [Id] uniqueidentifier NOT NULL,
        [RequisicaoId] uniqueidentifier NOT NULL,
        [ProdutoId] uniqueidentifier NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Marca] nvarchar(400) NULL,
        [MesmaMarca] bit NOT NULL,
        [Unidade] nvarchar(400) NOT NULL,
        [Quantidade] decimal(18,3) NOT NULL,
        [ValorReferencia] decimal(18,4) NULL,
        [LeadTimeDias] int NOT NULL,
        [ServicoControladoId] uniqueidentifier NULL,
        [MaterialControlado] bit NOT NULL,
        [MaterialControladoId] uniqueidentifier NULL,
        [EspecificacaoTecnica] nvarchar(max) NULL,
        [DocumentosExigidos] nvarchar(max) NULL,
        [DataNecessidade] date NULL,
        [Status] nvarchar(40) NOT NULL,
        [Observacao] nvarchar(max) NULL,
        CONSTRAINT [PK_RequisicaoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RequisicaoItens_MateriaisControlados_MaterialControladoId] FOREIGN KEY ([MaterialControladoId]) REFERENCES [qh].[MateriaisControlados] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequisicaoItens_Produtos_ProdutoId] FOREIGN KEY ([ProdutoId]) REFERENCES [qh].[Produtos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequisicaoItens_Requisicoes_RequisicaoId] FOREIGN KEY ([RequisicaoId]) REFERENCES [qh].[Requisicoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequisicaoItens_ServicosControlados_ServicoControladoId] FOREIGN KEY ([ServicoControladoId]) REFERENCES [qh].[ServicosControlados] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[PedidoItens] (
        [Id] uniqueidentifier NOT NULL,
        [PedidoId] uniqueidentifier NOT NULL,
        [RequisicaoItemId] uniqueidentifier NULL,
        [ProdutoId] uniqueidentifier NULL,
        [CodigoInsumo] nvarchar(400) NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Unidade] nvarchar(400) NULL,
        [Quantidade] decimal(18,3) NOT NULL,
        [ValorUnitario] decimal(18,4) NOT NULL,
        [ValorTotal] decimal(18,4) NOT NULL,
        [MaterialControladoId] uniqueidentifier NULL,
        [EspecificacaoTecnica] nvarchar(max) NULL,
        [DocumentosExigidos] nvarchar(max) NULL,
        [QuantidadeRecebidaAceita] decimal(18,3) NOT NULL,
        CONSTRAINT [PK_PedidoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PedidoItens_MateriaisControlados_MaterialControladoId] FOREIGN KEY ([MaterialControladoId]) REFERENCES [qh].[MateriaisControlados] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PedidoItens_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [qh].[Pedidos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PedidoItens_Produtos_ProdutoId] FOREIGN KEY ([ProdutoId]) REFERENCES [qh].[Produtos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PedidoItens_RequisicaoItens_RequisicaoItemId] FOREIGN KEY ([RequisicaoItemId]) REFERENCES [qh].[RequisicaoItens] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE TABLE [qh].[RecebimentoItens] (
        [Id] uniqueidentifier NOT NULL,
        [RecebimentoId] uniqueidentifier NOT NULL,
        [PedidoItemId] uniqueidentifier NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Unidade] nvarchar(400) NULL,
        [QtdPedida] decimal(18,3) NOT NULL,
        [QtdRecebida] decimal(18,3) NOT NULL,
        [MaterialControlado] bit NOT NULL,
        [MaterialControladoId] uniqueidentifier NULL,
        [Lote] nvarchar(400) NULL,
        [DocumentosRecebidos] bit NOT NULL,
        CONSTRAINT [PK_RecebimentoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RecebimentoItens_PedidoItens_PedidoItemId] FOREIGN KEY ([PedidoItemId]) REFERENCES [qh].[PedidoItens] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RecebimentoItens_Recebimentos_RecebimentoId] FOREIGN KEY ([RecebimentoId]) REFERENCES [qh].[Recebimentos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_AcaoCorretivaItens_AcaoCorretivaId] ON [qh].[AcaoCorretivaItens] ([AcaoCorretivaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_AcoesCorretivas_EmpresaId] ON [qh].[AcoesCorretivas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_AditivoItens_AditivoId] ON [qh].[AditivoItens] ([AditivoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Aditivos_ContratoId_Numero] ON [qh].[Aditivos] ([ContratoId], [Numero]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Aditivos_EmpresaId] ON [qh].[Aditivos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Aditivos_EmpresaId_ObraId] ON [qh].[Aditivos] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Alcadas_EmpresaId] ON [qh].[Alcadas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Anexos_EmpresaId] ON [qh].[Anexos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Anexos_Entidade_EntidadeId] ON [qh].[Anexos] ([Entidade], [EntidadeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_AplicacoesLote_RecebimentoId] ON [qh].[AplicacoesLote] ([RecebimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Auditoria_Entidade_EntidadeId] ON [qh].[Auditoria] ([Entidade], [EntidadeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Auditoria_Quando] ON [qh].[Auditoria] ([Quando]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_AuditoriasInternas_EmpresaId] ON [qh].[AuditoriasInternas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_AvaliacoesSemestrais_EmpresaId] ON [qh].[AvaliacoesSemestrais] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AvaliacoesSemestrais_EmpresaId_Periodo_FornecedorId] ON [qh].[AvaliacoesSemestrais] ([EmpresaId], [Periodo], [FornecedorId]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_AvaliacoesSemestrais_FornecedorId] ON [qh].[AvaliacoesSemestrais] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Calibracoes_EquipamentoMedicaoId] ON [qh].[Calibracoes] ([EquipamentoMedicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_CentrosCusto_EmpresaId] ON [qh].[CentrosCusto] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CentrosCusto_EmpresaId_Codigo] ON [qh].[CentrosCusto] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_CentrosCusto_ObraId] ON [qh].[CentrosCusto] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Checkouts_DocumentoRevisaoId] ON [qh].[Checkouts] ([DocumentoRevisaoId]) WHERE [CheckinEm] IS NULL AND [LiberadoForcadoPor] IS NULL AND [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Checkouts_EmpresaId] ON [qh].[Checkouts] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Ciencias_DocumentoRevisaoId_UsuarioId] ON [qh].[Ciencias] ([DocumentoRevisaoId], [UsuarioId]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Ciencias_EmpresaId] ON [qh].[Ciencias] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Ciencias_UsuarioId] ON [qh].[Ciencias] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Colaboradores_EmpresaId] ON [qh].[Colaboradores] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Colaboradores_EmpresaId_ObraId] ON [qh].[Colaboradores] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Colaboradores_ObraId_CpfHash] ON [qh].[Colaboradores] ([ObraId], [CpfHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Constatacoes_AuditoriaInternaId] ON [qh].[Constatacoes] ([AuditoriaInternaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_ContratoItens_ContratoId] ON [qh].[ContratoItens] ([ContratoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_ContratoItens_ServicoControladoId] ON [qh].[ContratoItens] ([ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_CentroCustoId] ON [qh].[Contratos] ([CentroCustoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_CotacaoId] ON [qh].[Contratos] ([CotacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_EmpresaId] ON [qh].[Contratos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Contratos_EmpresaId_Numero] ON [qh].[Contratos] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_EmpresaId_ObraId] ON [qh].[Contratos] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_FornecedorId] ON [qh].[Contratos] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_ObraId] ON [qh].[Contratos] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_RequisicaoId] ON [qh].[Contratos] ([RequisicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContratoServicos_ContratoId_ServicoControladoId] ON [qh].[ContratoServicos] ([ContratoId], [ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_ContratoServicos_ServicoControladoId] ON [qh].[ContratoServicos] ([ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Copias_DocumentoRevisaoId] ON [qh].[Copias] ([DocumentoRevisaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Copias_EmpresaId] ON [qh].[Copias] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Copias_EmpresaId_NumeroCopia] ON [qh].[Copias] ([EmpresaId], [NumeroCopia]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Copias_GrdId] ON [qh].[Copias] ([GrdId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Copias_ObraId] ON [qh].[Copias] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Copias_QrToken] ON [qh].[Copias] ([QrToken]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Copias_SolicitacaoCopiaId] ON [qh].[Copias] ([SolicitacaoCopiaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CotacaoFornecedores_CotacaoId_FornecedorId] ON [qh].[CotacaoFornecedores] ([CotacaoId], [FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_CotacaoFornecedores_FornecedorId] ON [qh].[CotacaoFornecedores] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CotacaoFornecedores_LinkResposta] ON [qh].[CotacaoFornecedores] ([LinkResposta]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_CotacaoItens_CotacaoId] ON [qh].[CotacaoItens] ([CotacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_CotacaoItens_FornecedorId] ON [qh].[CotacaoItens] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_CotacaoItens_RequisicaoItemId] ON [qh].[CotacaoItens] ([RequisicaoItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Cotacoes_EmpresaId] ON [qh].[Cotacoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Cotacoes_EmpresaId_Numero] ON [qh].[Cotacoes] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Cotacoes_EmpresaId_ObraId] ON [qh].[Cotacoes] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Cotacoes_FornecedorVencedorId] ON [qh].[Cotacoes] ([FornecedorVencedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Cotacoes_ObraId] ON [qh].[Cotacoes] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Cotacoes_RequisicaoId] ON [qh].[Cotacoes] ([RequisicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_DelegacoesAprovacao_EmpresaId] ON [qh].[DelegacoesAprovacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_DelegacoesAprovacao_ObraId] ON [qh].[DelegacoesAprovacao] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_DelegacoesAprovacao_SubstitutoUsuarioId_Inicio_Fim] ON [qh].[DelegacoesAprovacao] ([SubstitutoUsuarioId], [Inicio], [Fim]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_DelegacoesAprovacao_TitularUsuarioId] ON [qh].[DelegacoesAprovacao] ([TitularUsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Documentos_EmpresaId] ON [qh].[Documentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Documentos_EmpresaId_Codigo] ON [qh].[Documentos] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Documentos_ObraId] ON [qh].[Documentos] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Documentos_PastaId] ON [qh].[Documentos] ([PastaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Documentos_ProximaAnalise] ON [qh].[Documentos] ([ProximaAnalise]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Documentos_RegraRetencaoId] ON [qh].[Documentos] ([RegraRetencaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Documentos_RevisaoVigenteId] ON [qh].[Documentos] ([RevisaoVigenteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_DocumentosColaborador_EmpresaId] ON [qh].[DocumentosColaborador] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_DocumentosExternos_EmpresaId] ON [qh].[DocumentosExternos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DocumentosExternos_EmpresaId_Identificacao_Edicao] ON [qh].[DocumentosExternos] ([EmpresaId], [Identificacao], [Edicao]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DocumentosRelacionados_DocumentoId_RelacionadoId] ON [qh].[DocumentosRelacionados] ([DocumentoId], [RelacionadoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_DocumentosRelacionados_RelacionadoId] ON [qh].[DocumentosRelacionados] ([RelacionadoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Empresas_Cnpj] ON [qh].[Empresas] ([Cnpj]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_EnsaiosModelo_ModeloInspecaoId] ON [qh].[EnsaiosModelo] ([ModeloInspecaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_EnsaiosRecebimento_EmpresaId] ON [qh].[EnsaiosRecebimento] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_EnsaiosRecebimento_RecebimentoId] ON [qh].[EnsaiosRecebimento] ([RecebimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_EntregasEpi_EmpresaId] ON [qh].[EntregasEpi] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Epis_EmpresaId] ON [qh].[Epis] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Equipamentos_EmpresaId] ON [qh].[Equipamentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Equipamentos_EmpresaId_Codigo] ON [qh].[Equipamentos] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_EtapasAprovacao_DelegacaoAprovacaoId] ON [qh].[EtapasAprovacao] ([DelegacaoAprovacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_EtapasAprovacao_PerfilAprovador_Decisao] ON [qh].[EtapasAprovacao] ([PerfilAprovador], [Decisao]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_EtapasAprovacao_SolicitacaoAprovacaoId] ON [qh].[EtapasAprovacao] ([SolicitacaoAprovacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_EtapasAprovacao_UsuarioAprovadorId] ON [qh].[EtapasAprovacao] ([UsuarioAprovadorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_EtapasRevisao_DocumentoRevisaoId] ON [qh].[EtapasRevisao] ([DocumentoRevisaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_EventosDocumento_DocumentoId] ON [qh].[EventosDocumento] ([DocumentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_EventosDocumento_EmpresaId_DataHora] ON [qh].[EventosDocumento] ([EmpresaId], [DataHora]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_ExecutantesPt_PermissaoTrabalhoId] ON [qh].[ExecutantesPt] ([PermissaoTrabalhoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Exigencias_EmpresaId] ON [qh].[Exigencias] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorDocumentos_DataValidade] ON [qh].[FornecedorDocumentos] ([DataValidade]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorDocumentos_EmpresaId] ON [qh].[FornecedorDocumentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorDocumentos_FornecedorId] ON [qh].[FornecedorDocumentos] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorDocumentos_TipoDocumentoExigidoId] ON [qh].[FornecedorDocumentos] ([TipoDocumentoExigidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fornecedores_EmpresaId] ON [qh].[Fornecedores] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Fornecedores_EmpresaId_CpfCnpj] ON [qh].[Fornecedores] ([EmpresaId], [CpfCnpj]) WHERE [Excluido] = 0 AND [CpfCnpj] <> ''''');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fornecedores_EmpresaId_RazaoSocial] ON [qh].[Fornecedores] ([EmpresaId], [RazaoSocial]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_FuncaoExigencias_ExigenciaId] ON [qh].[FuncaoExigencias] ([ExigenciaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_FuncaoExigencias_FuncaoObraId] ON [qh].[FuncaoExigencias] ([FuncaoObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Funcoes_EmpresaId] ON [qh].[Funcoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_FvmItens_RecebimentoId] ON [qh].[FvmItens] ([RecebimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fvs_EmpresaId] ON [qh].[Fvs] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Fvs_EmpresaId_Numero] ON [qh].[Fvs] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fvs_EmpresaId_ObraId] ON [qh].[Fvs] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fvs_LocalId_ServicoControladoId_Status] ON [qh].[Fvs] ([LocalId], [ServicoControladoId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fvs_ModeloInspecaoId] ON [qh].[Fvs] ([ModeloInspecaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_FvsItens_FvsId] ON [qh].[FvsItens] ([FvsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_GrdItens_DocumentoRevisaoId] ON [qh].[GrdItens] ([DocumentoRevisaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_GrdItens_GrdId] ON [qh].[GrdItens] ([GrdId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Grds_ContratoId] ON [qh].[Grds] ([ContratoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Grds_EmpresaId] ON [qh].[Grds] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Grds_EmpresaId_Numero] ON [qh].[Grds] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Grds_FornecedorId] ON [qh].[Grds] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Grds_PedidoId] ON [qh].[Grds] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Grds_TokenConfirmacao] ON [qh].[Grds] ([TokenConfirmacao]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_InspecaoSegurancaItens_InspecaoSegurancaId] ON [qh].[InspecaoSegurancaItens] ([InspecaoSegurancaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_InspecoesSeguranca_EmpresaId] ON [qh].[InspecoesSeguranca] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_InspecoesSeguranca_EmpresaId_ObraId] ON [qh].[InspecoesSeguranca] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_ItensVerificacao_ModeloInspecaoId] ON [qh].[ItensVerificacao] ([ModeloInspecaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_LayoutsImportacao_EmpresaId] ON [qh].[LayoutsImportacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_LimitesAprovacao_EmpresaId] ON [qh].[LimitesAprovacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_LimitesAprovacao_ObraId] ON [qh].[LimitesAprovacao] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LimitesAprovacao_UsuarioId_Entidade_ObraId] ON [qh].[LimitesAprovacao] ([UsuarioId], [Entidade], [ObraId]) WHERE [Excluido] = 0 AND [Ativo] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_LinhasImportacao_LoteImportacaoId] ON [qh].[LinhasImportacao] ([LoteImportacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_LinksPortal_EmpresaId] ON [qh].[LinksPortal] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_LinksPortal_FornecedorId] ON [qh].[LinksPortal] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LinksPortal_TokenHash] ON [qh].[LinksPortal] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Listas_EmpresaId] ON [qh].[Listas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Listas_EmpresaId_Tipo_Nome] ON [qh].[Listas] ([EmpresaId], [Tipo], [Nome]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Locais_EmpresaId] ON [qh].[Locais] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Locais_EmpresaId_ObraId] ON [qh].[Locais] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Locais_LocalPaiId] ON [qh].[Locais] ([LocalPaiId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Locais_ObraId] ON [qh].[Locais] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_LotesImportacao_EmpresaId] ON [qh].[LotesImportacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_LotesImportacao_EmpresaId_ArquivoHashSha256] ON [qh].[LotesImportacao] ([EmpresaId], [ArquivoHashSha256]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_MateriaisControlados_EmpresaId] ON [qh].[MateriaisControlados] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MateriaisControlados_EmpresaId_Codigo] ON [qh].[MateriaisControlados] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_MedicaoItens_ContratoItemId] ON [qh].[MedicaoItens] ([ContratoItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_MedicaoItens_LocalId] ON [qh].[MedicaoItens] ([LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_MedicaoItens_MedicaoId] ON [qh].[MedicaoItens] ([MedicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Medicoes_ContratoId_NumeroMedicao] ON [qh].[Medicoes] ([ContratoId], [NumeroMedicao]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Medicoes_EmpresaId] ON [qh].[Medicoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Medicoes_EmpresaId_ObraId] ON [qh].[Medicoes] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Medicoes_ObraId] ON [qh].[Medicoes] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Medicoes_PlanoAcaoId] ON [qh].[Medicoes] ([PlanoAcaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_ModelosInspecao_EmpresaId] ON [qh].[ModelosInspecao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ModelosInspecao_EmpresaId_Codigo] ON [qh].[ModelosInspecao] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Obras_EmpresaId] ON [qh].[Obras] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Obras_EmpresaId_Codigo] ON [qh].[Obras] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Ocorrencias_EmpresaId] ON [qh].[Ocorrencias] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Ocorrencias_EmpresaId_ObraId] ON [qh].[Ocorrencias] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_ChaveNFe] ON [qh].[Pagamentos] ([ChaveNFe]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_ContratoId] ON [qh].[Pagamentos] ([ContratoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_EmpresaId] ON [qh].[Pagamentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Pagamentos_EmpresaId_Numero] ON [qh].[Pagamentos] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_FornecedorId] ON [qh].[Pagamentos] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_MedicaoId] ON [qh].[Pagamentos] ([MedicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_ObraId] ON [qh].[Pagamentos] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_PedidoId] ON [qh].[Pagamentos] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Parametros_EmpresaId] ON [qh].[Parametros] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Parametros_EmpresaId_Chave] ON [qh].[Parametros] ([EmpresaId], [Chave]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Participantes_TurmaId] ON [qh].[Participantes] ([TurmaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pastas_EmpresaId] ON [qh].[Pastas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pastas_PastaPaiId] ON [qh].[Pastas] ([PastaPaiId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_CodigoInsumo] ON [qh].[PedidoItens] ([CodigoInsumo]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_MaterialControladoId] ON [qh].[PedidoItens] ([MaterialControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_PedidoId] ON [qh].[PedidoItens] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_ProdutoId] ON [qh].[PedidoItens] ([ProdutoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_RequisicaoItemId] ON [qh].[PedidoItens] ([RequisicaoItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_ContratoId] ON [qh].[Pedidos] ([ContratoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_CotacaoId] ON [qh].[Pedidos] ([CotacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_EmpresaId] ON [qh].[Pedidos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Pedidos_EmpresaId_Numero] ON [qh].[Pedidos] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_EmpresaId_ObraId] ON [qh].[Pedidos] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_FornecedorId] ON [qh].[Pedidos] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_ObraId] ON [qh].[Pedidos] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_RequisicaoId] ON [qh].[Pedidos] ([RequisicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Permissoes_EmpresaId] ON [qh].[Permissoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Permissoes_Escopo_AlvoId] ON [qh].[Permissoes] ([Escopo], [AlvoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Permissoes_UsuarioId] ON [qh].[Permissoes] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_PermissoesTrabalho_EmpresaId] ON [qh].[PermissoesTrabalho] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_PermissoesTrabalho_EmpresaId_ObraId] ON [qh].[PermissoesTrabalho] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_PlanosAcao_EmpresaId] ON [qh].[PlanosAcao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PlanosAcao_EmpresaId_Numero] ON [qh].[PlanosAcao] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_PlanosAcao_FornecedorId_Status] ON [qh].[PlanosAcao] ([FornecedorId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_PlanosAcao_ObraId] ON [qh].[PlanosAcao] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Produtos_EmpresaId] ON [qh].[Produtos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Produtos_EmpresaId_Codigo] ON [qh].[Produtos] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Produtos_MaterialControladoId] ON [qh].[Produtos] ([MaterialControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Qualificacoes_DataValidade] ON [qh].[Qualificacoes] ([DataValidade]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Qualificacoes_EmpresaId] ON [qh].[Qualificacoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Qualificacoes_FornecedorId_DataQualificacao] ON [qh].[Qualificacoes] ([FornecedorId], [DataQualificacao]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Qualificacoes_MaterialControladoId] ON [qh].[Qualificacoes] ([MaterialControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Qualificacoes_ServicoControladoId] ON [qh].[Qualificacoes] ([ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_RecebimentoItens_PedidoItemId] ON [qh].[RecebimentoItens] ([PedidoItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_RecebimentoItens_RecebimentoId] ON [qh].[RecebimentoItens] ([RecebimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_ContratoId] ON [qh].[Recebimentos] ([ContratoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_EmpresaId] ON [qh].[Recebimentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Recebimentos_EmpresaId_Numero] ON [qh].[Recebimentos] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_EmpresaId_ObraId] ON [qh].[Recebimentos] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_FornecedorId_DataRecebimento] ON [qh].[Recebimentos] ([FornecedorId], [DataRecebimento]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_ObraId] ON [qh].[Recebimentos] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_PedidoId] ON [qh].[Recebimentos] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_RegrasNotificacao_EmpresaId] ON [qh].[RegrasNotificacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_RegrasRetencao_EmpresaId] ON [qh].[RegrasRetencao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_RequisicaoItens_MaterialControladoId] ON [qh].[RequisicaoItens] ([MaterialControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_RequisicaoItens_ProdutoId] ON [qh].[RequisicaoItens] ([ProdutoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_RequisicaoItens_RequisicaoId] ON [qh].[RequisicaoItens] ([RequisicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_RequisicaoItens_ServicoControladoId] ON [qh].[RequisicaoItens] ([ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Requisicoes_CentroCustoId] ON [qh].[Requisicoes] ([CentroCustoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Requisicoes_EmpresaId] ON [qh].[Requisicoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Requisicoes_EmpresaId_Numero] ON [qh].[Requisicoes] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Requisicoes_EmpresaId_ObraId] ON [qh].[Requisicoes] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Requisicoes_EmpresaId_Status] ON [qh].[Requisicoes] ([EmpresaId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Requisicoes_ObraId] ON [qh].[Requisicoes] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Revisoes_DocumentoId_Revisao] ON [qh].[Revisoes] ([DocumentoId], [Revisao]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Revisoes_EmpresaId] ON [qh].[Revisoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Rncs_EmpresaId] ON [qh].[Rncs] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Rncs_EmpresaId_Numero] ON [qh].[Rncs] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Rncs_FornecedorId_Status] ON [qh].[Rncs] ([FornecedorId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sequencias_EmpresaId_Chave] ON [qh].[Sequencias] ([EmpresaId], [Chave]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_ServicosControlados_DocumentoItsId] ON [qh].[ServicosControlados] ([DocumentoItsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_ServicosControlados_EmpresaId] ON [qh].[ServicosControlados] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ServicosControlados_EmpresaId_Codigo] ON [qh].[ServicosControlados] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacaoCopiaItens_DocumentoRevisaoId] ON [qh].[SolicitacaoCopiaItens] ([DocumentoRevisaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacaoCopiaItens_SolicitacaoCopiaId] ON [qh].[SolicitacaoCopiaItens] ([SolicitacaoCopiaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesAprovacao_EmpresaId] ON [qh].[SolicitacoesAprovacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesAprovacao_EmpresaId_Status] ON [qh].[SolicitacoesAprovacao] ([EmpresaId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesAprovacao_Entidade_EntidadeId] ON [qh].[SolicitacoesAprovacao] ([Entidade], [EntidadeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesAprovacao_SolicitanteUsuarioId] ON [qh].[SolicitacoesAprovacao] ([SolicitanteUsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesCopia_EmpresaId] ON [qh].[SolicitacoesCopia] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SolicitacoesCopia_EmpresaId_Numero] ON [qh].[SolicitacoesCopia] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesCopia_ObraId] ON [qh].[SolicitacoesCopia] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_TiposDocumento_EmpresaId] ON [qh].[TiposDocumento] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Treinamentos_EmpresaId] ON [qh].[Treinamentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Turmas_EmpresaId] ON [qh].[Turmas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_Usuarios_EmpresaId] ON [qh].[Usuarios] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_EntraObjectId] ON [qh].[Usuarios] ([EntraObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE INDEX [IX_UsuariosObras_ObraId] ON [qh].[UsuariosObras] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UsuariosObras_UsuarioId_ObraId] ON [qh].[UsuariosObras] ([UsuarioId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    ALTER TABLE [qh].[Checkouts] ADD CONSTRAINT [FK_Checkouts_Revisoes_DocumentoRevisaoId] FOREIGN KEY ([DocumentoRevisaoId]) REFERENCES [qh].[Revisoes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    ALTER TABLE [qh].[Ciencias] ADD CONSTRAINT [FK_Ciencias_Revisoes_DocumentoRevisaoId] FOREIGN KEY ([DocumentoRevisaoId]) REFERENCES [qh].[Revisoes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    ALTER TABLE [qh].[ContratoItens] ADD CONSTRAINT [FK_ContratoItens_ServicosControlados_ServicoControladoId] FOREIGN KEY ([ServicoControladoId]) REFERENCES [qh].[ServicosControlados] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    ALTER TABLE [qh].[ContratoServicos] ADD CONSTRAINT [FK_ContratoServicos_ServicosControlados_ServicoControladoId] FOREIGN KEY ([ServicoControladoId]) REFERENCES [qh].[ServicosControlados] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    ALTER TABLE [qh].[Copias] ADD CONSTRAINT [FK_Copias_Revisoes_DocumentoRevisaoId] FOREIGN KEY ([DocumentoRevisaoId]) REFERENCES [qh].[Revisoes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    ALTER TABLE [qh].[CotacaoItens] ADD CONSTRAINT [FK_CotacaoItens_RequisicaoItens_RequisicaoItemId] FOREIGN KEY ([RequisicaoItemId]) REFERENCES [qh].[RequisicaoItens] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    ALTER TABLE [qh].[Documentos] ADD CONSTRAINT [FK_Documentos_Revisoes_RevisaoVigenteId] FOREIGN KEY ([RevisaoVigenteId]) REFERENCES [qh].[Revisoes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927110343_Inicial'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20260927110343_Inicial', N'10.0.12');
END;

COMMIT;
GO


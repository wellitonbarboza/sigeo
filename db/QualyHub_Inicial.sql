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
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    IF SCHEMA_ID(N'qh') IS NULL EXEC(N'CREATE SCHEMA [qh];');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    IF SCHEMA_ID(N'qh_hist') IS NULL EXEC(N'CREATE SCHEMA [qh_hist];');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
        [CodigoErp] nvarchar(60) NULL,
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
        [CodigoErp] nvarchar(60) NULL,
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Notas] (
        [Id] uniqueidentifier NOT NULL,
        [Entidade] nvarchar(60) NOT NULL,
        [EntidadeId] uniqueidentifier NOT NULL,
        [Texto] nvarchar(max) NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Notas] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
        [CodigoErp] nvarchar(60) NULL,
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Tarefas] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] int NOT NULL,
        [Titulo] nvarchar(200) NOT NULL,
        [Descricao] nvarchar(max) NULL,
        [Tipo] nvarchar(60) NULL,
        [Prioridade] nvarchar(40) NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [ResponsavelId] uniqueidentifier NOT NULL,
        [ResponsavelNome] nvarchar(400) NOT NULL,
        [PrazoOriginal] date NOT NULL,
        [Prazo] date NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [Resolucao] nvarchar(max) NULL,
        [ConcluidaEm] datetimeoffset NULL,
        [ConcluidaPor] nvarchar(400) NULL,
        [OrigemEntidade] nvarchar(60) NULL,
        [OrigemId] uniqueidentifier NULL,
        [OrigemDescricao] nvarchar(400) NULL,
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
        CONSTRAINT [PK_Tarefas] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Tarefa_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
        [PrecoReferencia] decimal(18,4) NULL,
        [MaterialControladoId] uniqueidentifier NULL,
        [Ativo] bit NOT NULL,
        [CodigoErp] nvarchar(60) NULL,
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE TABLE [qh].[TarefaLeituras] (
        [Id] uniqueidentifier NOT NULL,
        [TarefaId] uniqueidentifier NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [LidaEm] datetimeoffset NOT NULL,
        CONSTRAINT [PK_TarefaLeituras] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TarefaLeituras_Tarefas_TarefaId] FOREIGN KEY ([TarefaId]) REFERENCES [qh].[Tarefas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE TABLE [qh].[TarefaProrrogacoes] (
        [Id] uniqueidentifier NOT NULL,
        [TarefaId] uniqueidentifier NOT NULL,
        [PrazoAnterior] date NOT NULL,
        [NovoPrazo] date NOT NULL,
        [Dias] int NOT NULL,
        [Justificativa] nvarchar(max) NOT NULL,
        [Usuario] nvarchar(400) NOT NULL,
        [Em] datetimeoffset NOT NULL,
        CONSTRAINT [PK_TarefaProrrogacoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TarefaProrrogacoes_Tarefas_TarefaId] FOREIGN KEY ([TarefaId]) REFERENCES [qh].[Tarefas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
        [CodigoErp] nvarchar(60) NULL,
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
        [PrecoUltimo] decimal(18,4) NULL,
        [PrecoReferencia] decimal(18,4) NULL,
        CONSTRAINT [PK_CotacaoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CotacaoItens_Cotacoes_CotacaoId] FOREIGN KEY ([CotacaoId]) REFERENCES [qh].[Cotacoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CotacaoItens_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
        [CodigoErp] nvarchar(60) NULL,
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
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
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_AcaoCorretivaItens_AcaoCorretivaId] ON [qh].[AcaoCorretivaItens] ([AcaoCorretivaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_AcoesCorretivas_EmpresaId] ON [qh].[AcoesCorretivas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_AditivoItens_AditivoId] ON [qh].[AditivoItens] ([AditivoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Aditivos_ContratoId_Numero] ON [qh].[Aditivos] ([ContratoId], [Numero]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Aditivos_EmpresaId] ON [qh].[Aditivos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Aditivos_EmpresaId_ObraId] ON [qh].[Aditivos] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Alcadas_EmpresaId] ON [qh].[Alcadas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Anexos_EmpresaId] ON [qh].[Anexos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Anexos_Entidade_EntidadeId] ON [qh].[Anexos] ([Entidade], [EntidadeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_AplicacoesLote_RecebimentoId] ON [qh].[AplicacoesLote] ([RecebimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Auditoria_Entidade_EntidadeId] ON [qh].[Auditoria] ([Entidade], [EntidadeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Auditoria_Quando] ON [qh].[Auditoria] ([Quando]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_AuditoriasInternas_EmpresaId] ON [qh].[AuditoriasInternas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_AvaliacoesSemestrais_EmpresaId] ON [qh].[AvaliacoesSemestrais] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AvaliacoesSemestrais_EmpresaId_Periodo_FornecedorId] ON [qh].[AvaliacoesSemestrais] ([EmpresaId], [Periodo], [FornecedorId]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_AvaliacoesSemestrais_FornecedorId] ON [qh].[AvaliacoesSemestrais] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Calibracoes_EquipamentoMedicaoId] ON [qh].[Calibracoes] ([EquipamentoMedicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_CentrosCusto_EmpresaId] ON [qh].[CentrosCusto] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CentrosCusto_EmpresaId_Codigo] ON [qh].[CentrosCusto] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_CentrosCusto_ObraId] ON [qh].[CentrosCusto] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Checkouts_DocumentoRevisaoId] ON [qh].[Checkouts] ([DocumentoRevisaoId]) WHERE [CheckinEm] IS NULL AND [LiberadoForcadoPor] IS NULL AND [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Checkouts_EmpresaId] ON [qh].[Checkouts] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Ciencias_DocumentoRevisaoId_UsuarioId] ON [qh].[Ciencias] ([DocumentoRevisaoId], [UsuarioId]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Ciencias_EmpresaId] ON [qh].[Ciencias] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Ciencias_UsuarioId] ON [qh].[Ciencias] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Colaboradores_EmpresaId] ON [qh].[Colaboradores] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Colaboradores_EmpresaId_ObraId] ON [qh].[Colaboradores] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Colaboradores_ObraId_CpfHash] ON [qh].[Colaboradores] ([ObraId], [CpfHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Constatacoes_AuditoriaInternaId] ON [qh].[Constatacoes] ([AuditoriaInternaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_ContratoItens_ContratoId] ON [qh].[ContratoItens] ([ContratoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_ContratoItens_ServicoControladoId] ON [qh].[ContratoItens] ([ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_CentroCustoId] ON [qh].[Contratos] ([CentroCustoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_CotacaoId] ON [qh].[Contratos] ([CotacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_EmpresaId] ON [qh].[Contratos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Contratos_EmpresaId_Numero] ON [qh].[Contratos] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_EmpresaId_ObraId] ON [qh].[Contratos] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_FornecedorId] ON [qh].[Contratos] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_ObraId] ON [qh].[Contratos] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Contratos_RequisicaoId] ON [qh].[Contratos] ([RequisicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContratoServicos_ContratoId_ServicoControladoId] ON [qh].[ContratoServicos] ([ContratoId], [ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_ContratoServicos_ServicoControladoId] ON [qh].[ContratoServicos] ([ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Copias_DocumentoRevisaoId] ON [qh].[Copias] ([DocumentoRevisaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Copias_EmpresaId] ON [qh].[Copias] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Copias_EmpresaId_NumeroCopia] ON [qh].[Copias] ([EmpresaId], [NumeroCopia]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Copias_GrdId] ON [qh].[Copias] ([GrdId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Copias_ObraId] ON [qh].[Copias] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Copias_QrToken] ON [qh].[Copias] ([QrToken]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Copias_SolicitacaoCopiaId] ON [qh].[Copias] ([SolicitacaoCopiaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CotacaoFornecedores_CotacaoId_FornecedorId] ON [qh].[CotacaoFornecedores] ([CotacaoId], [FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_CotacaoFornecedores_FornecedorId] ON [qh].[CotacaoFornecedores] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CotacaoFornecedores_LinkResposta] ON [qh].[CotacaoFornecedores] ([LinkResposta]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_CotacaoItens_CotacaoId] ON [qh].[CotacaoItens] ([CotacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_CotacaoItens_FornecedorId] ON [qh].[CotacaoItens] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_CotacaoItens_RequisicaoItemId] ON [qh].[CotacaoItens] ([RequisicaoItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Cotacoes_EmpresaId] ON [qh].[Cotacoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Cotacoes_EmpresaId_Numero] ON [qh].[Cotacoes] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Cotacoes_EmpresaId_ObraId] ON [qh].[Cotacoes] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Cotacoes_FornecedorVencedorId] ON [qh].[Cotacoes] ([FornecedorVencedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Cotacoes_ObraId] ON [qh].[Cotacoes] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Cotacoes_RequisicaoId] ON [qh].[Cotacoes] ([RequisicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_DelegacoesAprovacao_EmpresaId] ON [qh].[DelegacoesAprovacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_DelegacoesAprovacao_ObraId] ON [qh].[DelegacoesAprovacao] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_DelegacoesAprovacao_SubstitutoUsuarioId_Inicio_Fim] ON [qh].[DelegacoesAprovacao] ([SubstitutoUsuarioId], [Inicio], [Fim]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_DelegacoesAprovacao_TitularUsuarioId] ON [qh].[DelegacoesAprovacao] ([TitularUsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Documentos_EmpresaId] ON [qh].[Documentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Documentos_EmpresaId_Codigo] ON [qh].[Documentos] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Documentos_ObraId] ON [qh].[Documentos] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Documentos_PastaId] ON [qh].[Documentos] ([PastaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Documentos_ProximaAnalise] ON [qh].[Documentos] ([ProximaAnalise]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Documentos_RegraRetencaoId] ON [qh].[Documentos] ([RegraRetencaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Documentos_RevisaoVigenteId] ON [qh].[Documentos] ([RevisaoVigenteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_DocumentosColaborador_EmpresaId] ON [qh].[DocumentosColaborador] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_DocumentosExternos_EmpresaId] ON [qh].[DocumentosExternos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DocumentosExternos_EmpresaId_Identificacao_Edicao] ON [qh].[DocumentosExternos] ([EmpresaId], [Identificacao], [Edicao]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DocumentosRelacionados_DocumentoId_RelacionadoId] ON [qh].[DocumentosRelacionados] ([DocumentoId], [RelacionadoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_DocumentosRelacionados_RelacionadoId] ON [qh].[DocumentosRelacionados] ([RelacionadoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Empresas_Cnpj] ON [qh].[Empresas] ([Cnpj]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_EnsaiosModelo_ModeloInspecaoId] ON [qh].[EnsaiosModelo] ([ModeloInspecaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_EnsaiosRecebimento_EmpresaId] ON [qh].[EnsaiosRecebimento] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_EnsaiosRecebimento_RecebimentoId] ON [qh].[EnsaiosRecebimento] ([RecebimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_EntregasEpi_EmpresaId] ON [qh].[EntregasEpi] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Epis_EmpresaId] ON [qh].[Epis] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Equipamentos_EmpresaId] ON [qh].[Equipamentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Equipamentos_EmpresaId_Codigo] ON [qh].[Equipamentos] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_EtapasAprovacao_DelegacaoAprovacaoId] ON [qh].[EtapasAprovacao] ([DelegacaoAprovacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_EtapasAprovacao_PerfilAprovador_Decisao] ON [qh].[EtapasAprovacao] ([PerfilAprovador], [Decisao]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_EtapasAprovacao_SolicitacaoAprovacaoId] ON [qh].[EtapasAprovacao] ([SolicitacaoAprovacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_EtapasAprovacao_UsuarioAprovadorId] ON [qh].[EtapasAprovacao] ([UsuarioAprovadorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_EtapasRevisao_DocumentoRevisaoId] ON [qh].[EtapasRevisao] ([DocumentoRevisaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_EventosDocumento_DocumentoId] ON [qh].[EventosDocumento] ([DocumentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_EventosDocumento_EmpresaId_DataHora] ON [qh].[EventosDocumento] ([EmpresaId], [DataHora]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_ExecutantesPt_PermissaoTrabalhoId] ON [qh].[ExecutantesPt] ([PermissaoTrabalhoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Exigencias_EmpresaId] ON [qh].[Exigencias] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorDocumentos_DataValidade] ON [qh].[FornecedorDocumentos] ([DataValidade]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorDocumentos_EmpresaId] ON [qh].[FornecedorDocumentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorDocumentos_FornecedorId] ON [qh].[FornecedorDocumentos] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorDocumentos_TipoDocumentoExigidoId] ON [qh].[FornecedorDocumentos] ([TipoDocumentoExigidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fornecedores_EmpresaId] ON [qh].[Fornecedores] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Fornecedores_EmpresaId_CpfCnpj] ON [qh].[Fornecedores] ([EmpresaId], [CpfCnpj]) WHERE [Excluido] = 0 AND [CpfCnpj] <> ''''');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fornecedores_EmpresaId_RazaoSocial] ON [qh].[Fornecedores] ([EmpresaId], [RazaoSocial]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_FuncaoExigencias_ExigenciaId] ON [qh].[FuncaoExigencias] ([ExigenciaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_FuncaoExigencias_FuncaoObraId] ON [qh].[FuncaoExigencias] ([FuncaoObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Funcoes_EmpresaId] ON [qh].[Funcoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_FvmItens_RecebimentoId] ON [qh].[FvmItens] ([RecebimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fvs_EmpresaId] ON [qh].[Fvs] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Fvs_EmpresaId_Numero] ON [qh].[Fvs] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fvs_EmpresaId_ObraId] ON [qh].[Fvs] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fvs_LocalId_ServicoControladoId_Status] ON [qh].[Fvs] ([LocalId], [ServicoControladoId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fvs_ModeloInspecaoId] ON [qh].[Fvs] ([ModeloInspecaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_FvsItens_FvsId] ON [qh].[FvsItens] ([FvsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_GrdItens_DocumentoRevisaoId] ON [qh].[GrdItens] ([DocumentoRevisaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_GrdItens_GrdId] ON [qh].[GrdItens] ([GrdId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Grds_ContratoId] ON [qh].[Grds] ([ContratoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Grds_EmpresaId] ON [qh].[Grds] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Grds_EmpresaId_Numero] ON [qh].[Grds] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Grds_FornecedorId] ON [qh].[Grds] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Grds_PedidoId] ON [qh].[Grds] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Grds_TokenConfirmacao] ON [qh].[Grds] ([TokenConfirmacao]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_InspecaoSegurancaItens_InspecaoSegurancaId] ON [qh].[InspecaoSegurancaItens] ([InspecaoSegurancaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_InspecoesSeguranca_EmpresaId] ON [qh].[InspecoesSeguranca] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_InspecoesSeguranca_EmpresaId_ObraId] ON [qh].[InspecoesSeguranca] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_ItensVerificacao_ModeloInspecaoId] ON [qh].[ItensVerificacao] ([ModeloInspecaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_LayoutsImportacao_EmpresaId] ON [qh].[LayoutsImportacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_LimitesAprovacao_EmpresaId] ON [qh].[LimitesAprovacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_LimitesAprovacao_ObraId] ON [qh].[LimitesAprovacao] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LimitesAprovacao_UsuarioId_Entidade_ObraId] ON [qh].[LimitesAprovacao] ([UsuarioId], [Entidade], [ObraId]) WHERE [Excluido] = 0 AND [Ativo] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_LinhasImportacao_LoteImportacaoId] ON [qh].[LinhasImportacao] ([LoteImportacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_LinksPortal_EmpresaId] ON [qh].[LinksPortal] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_LinksPortal_FornecedorId] ON [qh].[LinksPortal] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LinksPortal_TokenHash] ON [qh].[LinksPortal] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Listas_EmpresaId] ON [qh].[Listas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Listas_EmpresaId_Tipo_Nome] ON [qh].[Listas] ([EmpresaId], [Tipo], [Nome]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Locais_EmpresaId] ON [qh].[Locais] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Locais_EmpresaId_ObraId] ON [qh].[Locais] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Locais_LocalPaiId] ON [qh].[Locais] ([LocalPaiId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Locais_ObraId] ON [qh].[Locais] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_LotesImportacao_EmpresaId] ON [qh].[LotesImportacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_LotesImportacao_EmpresaId_ArquivoHashSha256] ON [qh].[LotesImportacao] ([EmpresaId], [ArquivoHashSha256]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_MateriaisControlados_EmpresaId] ON [qh].[MateriaisControlados] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MateriaisControlados_EmpresaId_Codigo] ON [qh].[MateriaisControlados] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_MedicaoItens_ContratoItemId] ON [qh].[MedicaoItens] ([ContratoItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_MedicaoItens_LocalId] ON [qh].[MedicaoItens] ([LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_MedicaoItens_MedicaoId] ON [qh].[MedicaoItens] ([MedicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Medicoes_ContratoId_NumeroMedicao] ON [qh].[Medicoes] ([ContratoId], [NumeroMedicao]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Medicoes_EmpresaId] ON [qh].[Medicoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Medicoes_EmpresaId_ObraId] ON [qh].[Medicoes] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Medicoes_ObraId] ON [qh].[Medicoes] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Medicoes_PlanoAcaoId] ON [qh].[Medicoes] ([PlanoAcaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_ModelosInspecao_EmpresaId] ON [qh].[ModelosInspecao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ModelosInspecao_EmpresaId_Codigo] ON [qh].[ModelosInspecao] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Notas_EmpresaId] ON [qh].[Notas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Notas_Entidade_EntidadeId] ON [qh].[Notas] ([Entidade], [EntidadeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Obras_EmpresaId] ON [qh].[Obras] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Obras_EmpresaId_Codigo] ON [qh].[Obras] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Ocorrencias_EmpresaId] ON [qh].[Ocorrencias] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Ocorrencias_EmpresaId_ObraId] ON [qh].[Ocorrencias] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_ChaveNFe] ON [qh].[Pagamentos] ([ChaveNFe]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_ContratoId] ON [qh].[Pagamentos] ([ContratoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_EmpresaId] ON [qh].[Pagamentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Pagamentos_EmpresaId_Numero] ON [qh].[Pagamentos] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_FornecedorId] ON [qh].[Pagamentos] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_MedicaoId] ON [qh].[Pagamentos] ([MedicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_ObraId] ON [qh].[Pagamentos] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagamentos_PedidoId] ON [qh].[Pagamentos] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Parametros_EmpresaId] ON [qh].[Parametros] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Parametros_EmpresaId_Chave] ON [qh].[Parametros] ([EmpresaId], [Chave]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Participantes_TurmaId] ON [qh].[Participantes] ([TurmaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pastas_EmpresaId] ON [qh].[Pastas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pastas_PastaPaiId] ON [qh].[Pastas] ([PastaPaiId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_CodigoInsumo] ON [qh].[PedidoItens] ([CodigoInsumo]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_MaterialControladoId] ON [qh].[PedidoItens] ([MaterialControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_PedidoId] ON [qh].[PedidoItens] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_ProdutoId] ON [qh].[PedidoItens] ([ProdutoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_RequisicaoItemId] ON [qh].[PedidoItens] ([RequisicaoItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_ContratoId] ON [qh].[Pedidos] ([ContratoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_CotacaoId] ON [qh].[Pedidos] ([CotacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_EmpresaId] ON [qh].[Pedidos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Pedidos_EmpresaId_Numero] ON [qh].[Pedidos] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_EmpresaId_ObraId] ON [qh].[Pedidos] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_FornecedorId] ON [qh].[Pedidos] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_ObraId] ON [qh].[Pedidos] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_RequisicaoId] ON [qh].[Pedidos] ([RequisicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Permissoes_EmpresaId] ON [qh].[Permissoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Permissoes_Escopo_AlvoId] ON [qh].[Permissoes] ([Escopo], [AlvoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Permissoes_UsuarioId] ON [qh].[Permissoes] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_PermissoesTrabalho_EmpresaId] ON [qh].[PermissoesTrabalho] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_PermissoesTrabalho_EmpresaId_ObraId] ON [qh].[PermissoesTrabalho] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_PlanosAcao_EmpresaId] ON [qh].[PlanosAcao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PlanosAcao_EmpresaId_Numero] ON [qh].[PlanosAcao] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_PlanosAcao_FornecedorId_Status] ON [qh].[PlanosAcao] ([FornecedorId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_PlanosAcao_ObraId] ON [qh].[PlanosAcao] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Produtos_EmpresaId] ON [qh].[Produtos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Produtos_EmpresaId_Codigo] ON [qh].[Produtos] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Produtos_MaterialControladoId] ON [qh].[Produtos] ([MaterialControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Qualificacoes_DataValidade] ON [qh].[Qualificacoes] ([DataValidade]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Qualificacoes_EmpresaId] ON [qh].[Qualificacoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Qualificacoes_FornecedorId_DataQualificacao] ON [qh].[Qualificacoes] ([FornecedorId], [DataQualificacao]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Qualificacoes_MaterialControladoId] ON [qh].[Qualificacoes] ([MaterialControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Qualificacoes_ServicoControladoId] ON [qh].[Qualificacoes] ([ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_RecebimentoItens_PedidoItemId] ON [qh].[RecebimentoItens] ([PedidoItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_RecebimentoItens_RecebimentoId] ON [qh].[RecebimentoItens] ([RecebimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_ContratoId] ON [qh].[Recebimentos] ([ContratoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_EmpresaId] ON [qh].[Recebimentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Recebimentos_EmpresaId_Numero] ON [qh].[Recebimentos] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_EmpresaId_ObraId] ON [qh].[Recebimentos] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_FornecedorId_DataRecebimento] ON [qh].[Recebimentos] ([FornecedorId], [DataRecebimento]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_ObraId] ON [qh].[Recebimentos] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_PedidoId] ON [qh].[Recebimentos] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_RegrasNotificacao_EmpresaId] ON [qh].[RegrasNotificacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_RegrasRetencao_EmpresaId] ON [qh].[RegrasRetencao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_RequisicaoItens_MaterialControladoId] ON [qh].[RequisicaoItens] ([MaterialControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_RequisicaoItens_ProdutoId] ON [qh].[RequisicaoItens] ([ProdutoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_RequisicaoItens_RequisicaoId] ON [qh].[RequisicaoItens] ([RequisicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_RequisicaoItens_ServicoControladoId] ON [qh].[RequisicaoItens] ([ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Requisicoes_CentroCustoId] ON [qh].[Requisicoes] ([CentroCustoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Requisicoes_EmpresaId] ON [qh].[Requisicoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Requisicoes_EmpresaId_Numero] ON [qh].[Requisicoes] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Requisicoes_EmpresaId_ObraId] ON [qh].[Requisicoes] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Requisicoes_EmpresaId_Status] ON [qh].[Requisicoes] ([EmpresaId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Requisicoes_ObraId] ON [qh].[Requisicoes] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Revisoes_DocumentoId_Revisao] ON [qh].[Revisoes] ([DocumentoId], [Revisao]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Revisoes_EmpresaId] ON [qh].[Revisoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Rncs_EmpresaId] ON [qh].[Rncs] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Rncs_EmpresaId_Numero] ON [qh].[Rncs] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Rncs_FornecedorId_Status] ON [qh].[Rncs] ([FornecedorId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sequencias_EmpresaId_Chave] ON [qh].[Sequencias] ([EmpresaId], [Chave]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_ServicosControlados_DocumentoItsId] ON [qh].[ServicosControlados] ([DocumentoItsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_ServicosControlados_EmpresaId] ON [qh].[ServicosControlados] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ServicosControlados_EmpresaId_Codigo] ON [qh].[ServicosControlados] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacaoCopiaItens_DocumentoRevisaoId] ON [qh].[SolicitacaoCopiaItens] ([DocumentoRevisaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacaoCopiaItens_SolicitacaoCopiaId] ON [qh].[SolicitacaoCopiaItens] ([SolicitacaoCopiaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesAprovacao_EmpresaId] ON [qh].[SolicitacoesAprovacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesAprovacao_EmpresaId_Status] ON [qh].[SolicitacoesAprovacao] ([EmpresaId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesAprovacao_Entidade_EntidadeId] ON [qh].[SolicitacoesAprovacao] ([Entidade], [EntidadeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesAprovacao_SolicitanteUsuarioId] ON [qh].[SolicitacoesAprovacao] ([SolicitanteUsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesCopia_EmpresaId] ON [qh].[SolicitacoesCopia] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SolicitacoesCopia_EmpresaId_Numero] ON [qh].[SolicitacoesCopia] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesCopia_ObraId] ON [qh].[SolicitacoesCopia] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TarefaLeituras_TarefaId_UsuarioId] ON [qh].[TarefaLeituras] ([TarefaId], [UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_TarefaProrrogacoes_TarefaId] ON [qh].[TarefaProrrogacoes] ([TarefaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Tarefas_EmpresaId] ON [qh].[Tarefas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Tarefas_EmpresaId_Numero] ON [qh].[Tarefas] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Tarefas_OrigemEntidade_OrigemId] ON [qh].[Tarefas] ([OrigemEntidade], [OrigemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Tarefas_ResponsavelId_Status] ON [qh].[Tarefas] ([ResponsavelId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_TiposDocumento_EmpresaId] ON [qh].[TiposDocumento] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Treinamentos_EmpresaId] ON [qh].[Treinamentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Turmas_EmpresaId] ON [qh].[Turmas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_Usuarios_EmpresaId] ON [qh].[Usuarios] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_EntraObjectId] ON [qh].[Usuarios] ([EntraObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE INDEX [IX_UsuariosObras_ObraId] ON [qh].[UsuariosObras] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UsuariosObras_UsuarioId_ObraId] ON [qh].[UsuariosObras] ([UsuarioId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    ALTER TABLE [qh].[Checkouts] ADD CONSTRAINT [FK_Checkouts_Revisoes_DocumentoRevisaoId] FOREIGN KEY ([DocumentoRevisaoId]) REFERENCES [qh].[Revisoes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    ALTER TABLE [qh].[Ciencias] ADD CONSTRAINT [FK_Ciencias_Revisoes_DocumentoRevisaoId] FOREIGN KEY ([DocumentoRevisaoId]) REFERENCES [qh].[Revisoes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    ALTER TABLE [qh].[ContratoItens] ADD CONSTRAINT [FK_ContratoItens_ServicosControlados_ServicoControladoId] FOREIGN KEY ([ServicoControladoId]) REFERENCES [qh].[ServicosControlados] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    ALTER TABLE [qh].[ContratoServicos] ADD CONSTRAINT [FK_ContratoServicos_ServicosControlados_ServicoControladoId] FOREIGN KEY ([ServicoControladoId]) REFERENCES [qh].[ServicosControlados] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    ALTER TABLE [qh].[Copias] ADD CONSTRAINT [FK_Copias_Revisoes_DocumentoRevisaoId] FOREIGN KEY ([DocumentoRevisaoId]) REFERENCES [qh].[Revisoes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    ALTER TABLE [qh].[CotacaoItens] ADD CONSTRAINT [FK_CotacaoItens_RequisicaoItens_RequisicaoItemId] FOREIGN KEY ([RequisicaoItemId]) REFERENCES [qh].[RequisicaoItens] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    ALTER TABLE [qh].[Documentos] ADD CONSTRAINT [FK_Documentos_Revisoes_RevisaoVigenteId] FOREIGN KEY ([RevisaoVigenteId]) REFERENCES [qh].[Revisoes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927125805_Inicial'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20260927125805_Inicial', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [CapitalSocial] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [DataFundacao] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [Mei] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [OptanteSimples] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [OrigemAlteradoEm] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [OrigemAlteradoPor] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [OrigemCriadoEm] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [OrigemCriadoPor] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [OrigemSistema] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [Site] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [Telefone0800] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    CREATE TABLE [qh].[FornecedorContatos] (
        [Id] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Funcao] nvarchar(400) NULL,
        [Telefone] nvarchar(400) NULL,
        [Celular] nvarchar(400) NULL,
        [Email] nvarchar(400) NULL,
        [Principal] bit NOT NULL,
        CONSTRAINT [PK_FornecedorContatos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FornecedorContatos_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    CREATE INDEX [IX_FornecedorContatos_FornecedorId] ON [qh].[FornecedorContatos] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927170352_FornecedorCadastroCompleto'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20260927170352_FornecedorCadastroCompleto', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927180428_ProdutoCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Produtos] ADD [DescricaoCompleta] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927180428_ProdutoCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Produtos] ADD [Ean] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927180428_ProdutoCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Produtos] ADD [Marca] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927180428_ProdutoCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Produtos] ADD [Ncm] nvarchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927180428_ProdutoCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Produtos] ADD [OrigemSistema] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927180428_ProdutoCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Produtos] ADD [QuantidadePorEmbalagem] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927180428_ProdutoCadastroCompleto'
)
BEGIN
    ALTER TABLE [qh].[Produtos] ADD [Referencia] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927180428_ProdutoCadastroCompleto'
)
BEGIN
    CREATE INDEX [IX_Produtos_Ncm] ON [qh].[Produtos] ([Ncm]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927180428_ProdutoCadastroCompleto'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20260927180428_ProdutoCadastroCompleto', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927182223_CodigoProdutoPadronizado'
)
BEGIN
    ALTER TABLE [qh].[Produtos] ADD [CodigoAnterior] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927182223_CodigoProdutoPadronizado'
)
BEGIN
    CREATE TABLE [qh].[CategoriasProduto] (
        [Id] uniqueidentifier NOT NULL,
        [PaiId] uniqueidentifier NULL,
        [Nome] nvarchar(120) NOT NULL,
        [Sigla] nvarchar(3) NOT NULL,
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
        CONSTRAINT [PK_CategoriasProduto] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CategoriasProduto_CategoriasProduto_PaiId] FOREIGN KEY ([PaiId]) REFERENCES [qh].[CategoriasProduto] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927182223_CodigoProdutoPadronizado'
)
BEGIN
    CREATE INDEX [IX_CategoriasProduto_EmpresaId] ON [qh].[CategoriasProduto] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927182223_CodigoProdutoPadronizado'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CategoriasProduto_EmpresaId_PaiId_Nome] ON [qh].[CategoriasProduto] ([EmpresaId], [PaiId], [Nome]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927182223_CodigoProdutoPadronizado'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CategoriasProduto_EmpresaId_PaiId_Sigla] ON [qh].[CategoriasProduto] ([EmpresaId], [PaiId], [Sigla]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927182223_CodigoProdutoPadronizado'
)
BEGIN
    CREATE INDEX [IX_CategoriasProduto_PaiId] ON [qh].[CategoriasProduto] ([PaiId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260927182223_CodigoProdutoPadronizado'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20260927182223_CodigoProdutoPadronizado', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    DROP INDEX [IX_Locais_ObraId] ON [qh].[Locais];
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    ALTER TABLE [qh].[Requisicoes] ADD [SpeId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    ALTER TABLE [qh].[Pedidos] ADD [SpeId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD [AreaPrivativa] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD [FracaoIdeal] decimal(18,8) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD [SpeId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD [TipologiaId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    ALTER TABLE [qh].[Contratos] ADD [SpeId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    IF SCHEMA_ID(N'qh_hist') IS NULL EXEC(N'CREATE SCHEMA [qh_hist];');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    CREATE TABLE [qh].[Spes] (
        [Id] uniqueidentifier NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(10) NOT NULL,
        [RazaoSocial] nvarchar(400) NOT NULL,
        [NomeFantasia] nvarchar(400) NULL,
        [Cnpj] nvarchar(20) NOT NULL,
        [InscricaoEstadual] nvarchar(400) NULL,
        [EnderecoFaturamento] nvarchar(400) NULL,
        [EmailFiscal] nvarchar(400) NULL,
        [PatrimonioAfetacao] nvarchar(20) NOT NULL,
        [Ativa] bit NOT NULL,
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
        CONSTRAINT [PK_Spes] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Spe_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    CREATE TABLE [qh].[Tipologias] (
        [Id] uniqueidentifier NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(20) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Dormitorios] int NOT NULL,
        [Suites] int NOT NULL,
        [AreaPrivativa] decimal(18,4) NOT NULL,
        [AreaEquivalente] decimal(18,4) NULL,
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
        CONSTRAINT [PK_Tipologias] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    CREATE INDEX [IX_Locais_ObraId_SpeId_Tipo] ON [qh].[Locais] ([ObraId], [SpeId], [Tipo]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    CREATE INDEX [IX_Locais_SpeId] ON [qh].[Locais] ([SpeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    CREATE INDEX [IX_Locais_TipologiaId] ON [qh].[Locais] ([TipologiaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    CREATE INDEX [IX_Spes_EmpresaId] ON [qh].[Spes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Spes_EmpresaId_Cnpj] ON [qh].[Spes] ([EmpresaId], [Cnpj]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Spes_ObraId_Codigo] ON [qh].[Spes] ([ObraId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    CREATE INDEX [IX_Tipologias_EmpresaId] ON [qh].[Tipologias] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Tipologias_ObraId_Codigo] ON [qh].[Tipologias] ([ObraId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD CONSTRAINT [FK_Locais_Spes_SpeId] FOREIGN KEY ([SpeId]) REFERENCES [qh].[Spes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD CONSTRAINT [FK_Locais_Tipologias_TipologiaId] FOREIGN KEY ([TipologiaId]) REFERENCES [qh].[Tipologias] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928015338_EngenhariaFase1'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20260928015338_EngenhariaFase1', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928111116_QuadrasECodigoErp'
)
BEGIN

    DECLARE @t sysname, @c sysname, @sql nvarchar(400);
    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT t.name, c.name FROM sys.columns c
        JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = 'qh' AND t.temporal_type IN (0, 2) AND c.name LIKE 'CodigoObra[_A-Z]%' AND c.name <> 'CodigoObra'
          AND NOT EXISTS (SELECT 1 FROM sys.columns x WHERE x.object_id = t.object_id AND x.name = 'CodigoErp');
    OPEN cur; FETCH NEXT FROM cur INTO @t, @c;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'qh.' + QUOTENAME(@t) + N'.' + QUOTENAME(@c);
        EXEC sp_rename @sql, N'CodigoErp', N'COLUMN';
        FETCH NEXT FROM cur INTO @t, @c;
    END
    CLOSE cur; DEALLOCATE cur;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928111116_QuadrasECodigoErp'
)
BEGIN
    ALTER TABLE [qh].[Spes] ADD [QuadraId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928111116_QuadrasECodigoErp'
)
BEGIN
    IF SCHEMA_ID(N'qh_hist') IS NULL EXEC(N'CREATE SCHEMA [qh_hist];');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928111116_QuadrasECodigoErp'
)
BEGIN
    CREATE TABLE [qh].[Quadras] (
        [Id] uniqueidentifier NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(6) NOT NULL,
        [Nome] nvarchar(120) NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [NomeCondominio] nvarchar(400) NULL,
        [CnpjCondominio] nvarchar(20) NULL,
        [Endereco] nvarchar(400) NULL,
        [Matricula] nvarchar(400) NULL,
        [AreaTerreno] decimal(18,2) NULL,
        [Observacoes] nvarchar(400) NULL,
        [Ordem] int NOT NULL,
        [Ativa] bit NOT NULL,
        [LocalId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_Quadras] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Quadras_Locais_LocalId] FOREIGN KEY ([LocalId]) REFERENCES [qh].[Locais] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Quadra_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928111116_QuadrasECodigoErp'
)
BEGIN
    CREATE INDEX [IX_Spes_QuadraId] ON [qh].[Spes] ([QuadraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928111116_QuadrasECodigoErp'
)
BEGIN
    CREATE INDEX [IX_Quadras_EmpresaId] ON [qh].[Quadras] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928111116_QuadrasECodigoErp'
)
BEGIN
    CREATE INDEX [IX_Quadras_LocalId] ON [qh].[Quadras] ([LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928111116_QuadrasECodigoErp'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Quadras_ObraId_Codigo] ON [qh].[Quadras] ([ObraId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928111116_QuadrasECodigoErp'
)
BEGIN
    ALTER TABLE [qh].[Spes] ADD CONSTRAINT [FK_Spes_Quadras_QuadraId] FOREIGN KEY ([QuadraId]) REFERENCES [qh].[Quadras] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928111116_QuadrasECodigoErp'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20260928111116_QuadrasECodigoErp', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928120141_ObraSemEmpresaSpe'
)
BEGIN
    EXEC sp_rename N'[qh].[Obras].[EmpresaSpe]', N'Observacoes', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928120141_ObraSemEmpresaSpe'
)
BEGIN
    EXEC(N'UPDATE qh.Obras SET Observacoes = N''Empresa/SPE (cadastro antigo): '' + Observacoes WHERE Observacoes IS NOT NULL AND LEN(Observacoes) > 0')
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928120141_ObraSemEmpresaSpe'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20260928120141_ObraSemEmpresaSpe', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928121406_LoginPorSenha'
)
BEGIN
    DROP INDEX [IX_Usuarios_EntraObjectId] ON [qh].[Usuarios];
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928121406_LoginPorSenha'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[qh].[Usuarios]') AND [c].[name] = N'EntraObjectId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [qh].[Usuarios] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [qh].[Usuarios] DROP COLUMN [EntraObjectId];
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928121406_LoginPorSenha'
)
BEGIN
    ALTER TABLE [qh].[Usuarios] ADD [Login] nvarchar(60) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928121406_LoginPorSenha'
)
BEGIN
    EXEC(N'
    WITH x AS (SELECT Id, LOWER(LEFT(LTRIM(RTRIM(Email)), 60)) AS L, ROW_NUMBER() OVER (PARTITION BY LOWER(LTRIM(RTRIM(Email))) ORDER BY CriadoEm) AS N
               FROM qh.Usuarios WHERE Excluido = 0 AND LEN(Email) > 0)
    UPDATE u SET Login = x.L FROM qh.Usuarios u JOIN x ON x.Id = u.Id WHERE x.N = 1 AND LEN(u.Login) = 0;')
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928121406_LoginPorSenha'
)
BEGIN
    CREATE TABLE [qh].[CredenciaisUsuario] (
        [Id] uniqueidentifier NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        [SenhaHash] nvarchar(300) NOT NULL,
        [TrocarSenhaNoProximoAcesso] bit NOT NULL,
        [TentativasFalhas] int NOT NULL,
        [BloqueadoAte] datetimeoffset NULL,
        [UltimoAcessoEm] datetimeoffset NULL,
        [SenhaAlteradaEm] datetimeoffset NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_CredenciaisUsuario] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CredenciaisUsuario_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [qh].[Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928121406_LoginPorSenha'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Usuarios_Login] ON [qh].[Usuarios] ([Login]) WHERE [Excluido] = 0 AND [Login] <> ''''');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928121406_LoginPorSenha'
)
BEGIN
    CREATE INDEX [IX_CredenciaisUsuario_EmpresaId] ON [qh].[CredenciaisUsuario] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928121406_LoginPorSenha'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CredenciaisUsuario_UsuarioId] ON [qh].[CredenciaisUsuario] ([UsuarioId]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260928121406_LoginPorSenha'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20260928121406_LoginPorSenha', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    DROP INDEX [IX_Spes_QuadraId] ON [qh].[Spes];
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    WITH x AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY QuadraId ORDER BY CriadoEm) AS N FROM qh.Spes WHERE QuadraId IS NOT NULL AND Excluido = 0)
    UPDATE s SET QuadraId = NULL FROM qh.Spes s JOIN x ON x.Id = s.Id WHERE x.N > 1;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[TiposDocumento] ADD [Categoria] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[TiposDocumento] ADD [DeFuncionario] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[TiposDocumento] ADD [DiaLimite] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[TiposDocumento] ADD [Frequencia] nvarchar(40) NOT NULL DEFAULT N'Validade';
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[TiposDocumento] ADD [Ordem] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Tipologias] ADD [Observacoes] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Tipologias] ADD [Pavimentos] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Tipologias] ADD [PeDireitoPadrao] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Tipologias] ADD [PrimeiroTerreo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Tipologias] ADD [Tipo] nvarchar(40) NOT NULL DEFAULT N'Apartamento';
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[ServicosControlados] ADD [Sigla] nvarchar(8) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Requisicoes] ADD [EstabelecimentoFaturamentoId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Requisicoes] ADD [JustificativaAcimaOrcado] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[RequisicaoItens] ADD [ComposicaoId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Recebimentos] ADD [AlmoxarifadoCentral] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Recebimentos] ADD [EstabelecimentoId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Quadras] ADD [ImplantacaoId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Pedidos] ADD [EntregaAlmoxarifadoCentral] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Pedidos] ADD [EstabelecimentoFaturamentoId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[MateriaisControlados] ADD [CategoriaProdutoId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD [AmbienteModeloId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD [AreaParede] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD [AreaPiso] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD [Categoria] nvarchar(60) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD [PeDireito] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD [Perimetro] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [IncluirDocsFuncionarios] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Fornecedores] ADD [PreCadastro] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[FornecedorDocumentos] ADD [Competencia] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Contratos] ADD [EstabelecimentoFaturamentoId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[ContratoItens] ADD [ComposicaoId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[CentrosCusto] ADD [CodigoErp] nvarchar(60) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[CentrosCusto] ADD [EstabelecimentosPermitidos] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[CentrosCusto] ADD [PaiId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    IF SCHEMA_ID(N'qh_hist') IS NULL EXEC(N'CREATE SCHEMA [qh_hist];');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[Composicoes] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(30) NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Unidade] nvarchar(10) NOT NULL,
        [Grupo] nvarchar(80) NULL,
        [Regra] nvarchar(40) NOT NULL,
        [Fator] decimal(18,6) NOT NULL,
        [ServicoControladoId] uniqueidentifier NULL,
        [Ativa] bit NOT NULL,
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
        CONSTRAINT [PK_Composicoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Composicoes_ServicosControlados_ServicoControladoId] FOREIGN KEY ([ServicoControladoId]) REFERENCES [qh].[ServicosControlados] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Composicao_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[ContratoItemLocais] (
        [Id] uniqueidentifier NOT NULL,
        [ContratoItemId] uniqueidentifier NOT NULL,
        [LocalId] uniqueidentifier NOT NULL,
        [Quantidade] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_ContratoItemLocais] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContratoItemLocais_ContratoItens_ContratoItemId] FOREIGN KEY ([ContratoItemId]) REFERENCES [qh].[ContratoItens] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ContratoItemLocais_Locais_LocalId] FOREIGN KEY ([LocalId]) REFERENCES [qh].[Locais] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[Equipes] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(20) NOT NULL,
        [Nome] nvarchar(120) NOT NULL,
        [Encarregado] nvarchar(400) NULL,
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
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Equipes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[Estabelecimentos] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(20) NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [RazaoSocial] nvarchar(200) NOT NULL,
        [NomeFantasia] nvarchar(400) NULL,
        [Cnpj] nvarchar(20) NOT NULL,
        [Raiz] nvarchar(8) NOT NULL,
        [InscricaoEstadual] nvarchar(400) NULL,
        [Endereco] nvarchar(400) NULL,
        [Atividade] nvarchar(400) NULL,
        [SpeId] uniqueidentifier NULL,
        [FaturaMateriais] bit NOT NULL,
        [AlmoxarifadoCentral] bit NOT NULL,
        [EnderecoEntrega] nvarchar(400) NULL,
        [Ativo] bit NOT NULL,
        [CodigoErp] nvarchar(60) NULL,
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
        CONSTRAINT [PK_Estabelecimentos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Estabelecimentos_Spes_SpeId] FOREIGN KEY ([SpeId]) REFERENCES [qh].[Spes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[FornecedorExigencias] (
        [Id] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [TipoDocumentoExigidoId] uniqueidentifier NOT NULL,
        [Obrigatorio] bit NOT NULL,
        CONSTRAINT [PK_FornecedorExigencias] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FornecedorExigencias_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FornecedorExigencias_TiposDocumento_TipoDocumentoExigidoId] FOREIGN KEY ([TipoDocumentoExigidoId]) REFERENCES [qh].[TiposDocumento] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[FornecedorGrupos] (
        [Id] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [CategoriaProdutoId] uniqueidentifier NOT NULL,
        [Subgrupos] nvarchar(2000) NULL,
        CONSTRAINT [PK_FornecedorGrupos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FornecedorGrupos_CategoriasProduto_CategoriaProdutoId] FOREIGN KEY ([CategoriaProdutoId]) REFERENCES [qh].[CategoriasProduto] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FornecedorGrupos_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[FornecedorServicos] (
        [Id] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [ServicoControladoId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_FornecedorServicos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FornecedorServicos_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FornecedorServicos_ServicosControlados_ServicoControladoId] FOREIGN KEY ([ServicoControladoId]) REFERENCES [qh].[ServicosControlados] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[RequisicaoItemLocais] (
        [Id] uniqueidentifier NOT NULL,
        [RequisicaoItemId] uniqueidentifier NOT NULL,
        [LocalId] uniqueidentifier NOT NULL,
        [ComposicaoId] uniqueidentifier NULL,
        [Quantidade] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_RequisicaoItemLocais] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RequisicaoItemLocais_Locais_LocalId] FOREIGN KEY ([LocalId]) REFERENCES [qh].[Locais] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequisicaoItemLocais_RequisicaoItens_RequisicaoItemId] FOREIGN KEY ([RequisicaoItemId]) REFERENCES [qh].[RequisicaoItens] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[TipologiaAmbientes] (
        [Id] uniqueidentifier NOT NULL,
        [TipologiaId] uniqueidentifier NOT NULL,
        [Ordem] int NOT NULL,
        [Escopo] nvarchar(40) NOT NULL,
        [Nome] nvarchar(80) NOT NULL,
        [Sigla] nvarchar(8) NULL,
        [Categoria] nvarchar(60) NULL,
        [AreaPiso] decimal(18,4) NOT NULL,
        [Perimetro] decimal(18,4) NOT NULL,
        [PeDireito] decimal(18,4) NULL,
        [AreaVaos] decimal(18,4) NOT NULL,
        [PavimentoDe] int NULL,
        [PavimentoAte] int NULL,
        CONSTRAINT [PK_TipologiaAmbientes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TipologiaAmbientes_Tipologias_TipologiaId] FOREIGN KEY ([TipologiaId]) REFERENCES [qh].[Tipologias] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[TipologiaMix] (
        [Id] uniqueidentifier NOT NULL,
        [TipologiaId] uniqueidentifier NOT NULL,
        [Ordem] int NOT NULL,
        [PavimentoDe] int NOT NULL,
        [PavimentoAte] int NOT NULL,
        [ApartamentoTipologiaId] uniqueidentifier NOT NULL,
        [Quantidade] int NOT NULL,
        CONSTRAINT [PK_TipologiaMix] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TipologiaMix_Tipologias_ApartamentoTipologiaId] FOREIGN KEY ([ApartamentoTipologiaId]) REFERENCES [qh].[Tipologias] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TipologiaMix_Tipologias_TipologiaId] FOREIGN KEY ([TipologiaId]) REFERENCES [qh].[Tipologias] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[Transferencias] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] int NOT NULL,
        [RecebimentoId] uniqueidentifier NULL,
        [PedidoId] uniqueidentifier NULL,
        [OrigemEstabelecimentoId] uniqueidentifier NULL,
        [SpeId] uniqueidentifier NULL,
        [Data] date NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [ConferidoPor] nvarchar(400) NULL,
        [ConferidoEm] datetimeoffset NULL,
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
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Transferencias] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Transferencias_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Transferencias_Recebimentos_RecebimentoId] FOREIGN KEY ([RecebimentoId]) REFERENCES [qh].[Recebimentos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Transferencias_Spes_SpeId] FOREIGN KEY ([SpeId]) REFERENCES [qh].[Spes] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[TransferenciaMaterial_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[ComposicaoItens] (
        [Id] uniqueidentifier NOT NULL,
        [ComposicaoId] uniqueidentifier NOT NULL,
        [Ordem] int NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [ProdutoId] uniqueidentifier NULL,
        [FuncaoObraId] uniqueidentifier NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Unidade] nvarchar(10) NOT NULL,
        [Coeficiente] decimal(18,6) NOT NULL,
        [PerdaPerc] decimal(9,4) NOT NULL,
        [CustoUnitario] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_ComposicaoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ComposicaoItens_Composicoes_ComposicaoId] FOREIGN KEY ([ComposicaoId]) REFERENCES [qh].[Composicoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ComposicaoItens_Funcoes_FuncaoObraId] FOREIGN KEY ([FuncaoObraId]) REFERENCES [qh].[Funcoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ComposicaoItens_Produtos_ProdutoId] FOREIGN KEY ([ProdutoId]) REFERENCES [qh].[Produtos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[ServicoAplicacoes] (
        [Id] uniqueidentifier NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        [TipologiaId] uniqueidentifier NOT NULL,
        [ComposicaoId] uniqueidentifier NOT NULL,
        [Alvo] nvarchar(40) NOT NULL,
        [AmbienteModeloId] uniqueidentifier NULL,
        [QuantidadeFixa] decimal(18,4) NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ServicoAplicacoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServicoAplicacoes_Composicoes_ComposicaoId] FOREIGN KEY ([ComposicaoId]) REFERENCES [qh].[Composicoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServicoAplicacoes_Tipologias_TipologiaId] FOREIGN KEY ([TipologiaId]) REFERENCES [qh].[Tipologias] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[ServicosLocais] (
        [Id] uniqueidentifier NOT NULL,
        [ComposicaoId] uniqueidentifier NOT NULL,
        [LocalId] uniqueidentifier NOT NULL,
        [SpeId] uniqueidentifier NULL,
        [Quantidade] decimal(18,4) NOT NULL,
        [CustoUnitario] decimal(18,4) NOT NULL,
        [Custo] decimal(18,2) NOT NULL,
        [Origem] nvarchar(20) NOT NULL,
        [Travado] bit NOT NULL,
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
        CONSTRAINT [PK_ServicosLocais] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServicosLocais_Composicoes_ComposicaoId] FOREIGN KEY ([ComposicaoId]) REFERENCES [qh].[Composicoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServicosLocais_Locais_LocalId] FOREIGN KEY ([LocalId]) REFERENCES [qh].[Locais] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[Apropriacoes] (
        [Id] uniqueidentifier NOT NULL,
        [Data] date NOT NULL,
        [EquipeId] uniqueidentifier NULL,
        [ColaboradorId] uniqueidentifier NULL,
        [ComposicaoId] uniqueidentifier NOT NULL,
        [LocalId] uniqueidentifier NOT NULL,
        [Horas] decimal(18,2) NOT NULL,
        [QuantidadeExecutada] decimal(18,4) NULL,
        [Observacao] nvarchar(max) NULL,
        [Lancamento] uniqueidentifier NOT NULL,
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
        CONSTRAINT [PK_Apropriacoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Apropriacoes_Colaboradores_ColaboradorId] FOREIGN KEY ([ColaboradorId]) REFERENCES [qh].[Colaboradores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Apropriacoes_Composicoes_ComposicaoId] FOREIGN KEY ([ComposicaoId]) REFERENCES [qh].[Composicoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Apropriacoes_Equipes_EquipeId] FOREIGN KEY ([EquipeId]) REFERENCES [qh].[Equipes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Apropriacoes_Locais_LocalId] FOREIGN KEY ([LocalId]) REFERENCES [qh].[Locais] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[CustosMaoObra] (
        [Id] uniqueidentifier NOT NULL,
        [Competencia] date NOT NULL,
        [EquipeId] uniqueidentifier NULL,
        [ColaboradorId] uniqueidentifier NULL,
        [FuncaoObraId] uniqueidentifier NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Pessoas] int NOT NULL,
        [Salario] decimal(18,2) NOT NULL,
        [EncargosPerc] decimal(9,4) NOT NULL,
        [Beneficios] decimal(18,2) NOT NULL,
        [Outros] decimal(18,2) NOT NULL,
        [HorasMes] decimal(9,2) NOT NULL,
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
        CONSTRAINT [PK_CustosMaoObra] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustosMaoObra_Colaboradores_ColaboradorId] FOREIGN KEY ([ColaboradorId]) REFERENCES [qh].[Colaboradores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustosMaoObra_Equipes_EquipeId] FOREIGN KEY ([EquipeId]) REFERENCES [qh].[Equipes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustosMaoObra_Funcoes_FuncaoObraId] FOREIGN KEY ([FuncaoObraId]) REFERENCES [qh].[Funcoes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[EquipeMembros] (
        [Id] uniqueidentifier NOT NULL,
        [EquipeId] uniqueidentifier NOT NULL,
        [ColaboradorId] uniqueidentifier NOT NULL,
        [Desde] date NOT NULL,
        [Ate] date NULL,
        CONSTRAINT [PK_EquipeMembros] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EquipeMembros_Colaboradores_ColaboradorId] FOREIGN KEY ([ColaboradorId]) REFERENCES [qh].[Colaboradores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EquipeMembros_Equipes_EquipeId] FOREIGN KEY ([EquipeId]) REFERENCES [qh].[Equipes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[FluxosEstabelecimento] (
        [Id] uniqueidentifier NOT NULL,
        [OrigemId] uniqueidentifier NOT NULL,
        [DestinoId] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [Base] nvarchar(80) NOT NULL,
        [Situacao] nvarchar(30) NOT NULL,
        [Descricao] nvarchar(max) NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_FluxosEstabelecimento] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FluxosEstabelecimento_Estabelecimentos_DestinoId] FOREIGN KEY ([DestinoId]) REFERENCES [qh].[Estabelecimentos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FluxosEstabelecimento_Estabelecimentos_OrigemId] FOREIGN KEY ([OrigemId]) REFERENCES [qh].[Estabelecimentos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE TABLE [qh].[TransferenciaItens] (
        [Id] uniqueidentifier NOT NULL,
        [TransferenciaId] uniqueidentifier NOT NULL,
        [ProdutoId] uniqueidentifier NULL,
        [RecebimentoItemId] uniqueidentifier NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Unidade] nvarchar(400) NULL,
        [Quantidade] decimal(18,4) NOT NULL,
        [QuantidadeRecebida] decimal(18,4) NULL,
        [CustoUnitario] decimal(18,4) NOT NULL,
        [Lote] nvarchar(60) NULL,
        [LocalId] uniqueidentifier NULL,
        CONSTRAINT [PK_TransferenciaItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TransferenciaItens_Transferencias_TransferenciaId] FOREIGN KEY ([TransferenciaId]) REFERENCES [qh].[Transferencias] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Spes_QuadraId] ON [qh].[Spes] ([QuadraId]) WHERE [Excluido] = 0 AND [QuadraId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Requisicoes_EstabelecimentoFaturamentoId] ON [qh].[Requisicoes] ([EstabelecimentoFaturamentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_RequisicaoItens_ComposicaoId] ON [qh].[RequisicaoItens] ([ComposicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Recebimentos_EstabelecimentoId] ON [qh].[Recebimentos] ([EstabelecimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Quadras_ImplantacaoId] ON [qh].[Quadras] ([ImplantacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Pedidos_EstabelecimentoFaturamentoId] ON [qh].[Pedidos] ([EstabelecimentoFaturamentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_MateriaisControlados_CategoriaProdutoId] ON [qh].[MateriaisControlados] ([CategoriaProdutoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Locais_AmbienteModeloId] ON [qh].[Locais] ([AmbienteModeloId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Contratos_EstabelecimentoFaturamentoId] ON [qh].[Contratos] ([EstabelecimentoFaturamentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_ContratoItens_ComposicaoId] ON [qh].[ContratoItens] ([ComposicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_CentrosCusto_PaiId] ON [qh].[CentrosCusto] ([PaiId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Apropriacoes_ColaboradorId] ON [qh].[Apropriacoes] ([ColaboradorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Apropriacoes_ComposicaoId] ON [qh].[Apropriacoes] ([ComposicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Apropriacoes_EmpresaId] ON [qh].[Apropriacoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Apropriacoes_EmpresaId_ObraId] ON [qh].[Apropriacoes] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Apropriacoes_EquipeId] ON [qh].[Apropriacoes] ([EquipeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Apropriacoes_LocalId] ON [qh].[Apropriacoes] ([LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Apropriacoes_ObraId_Data] ON [qh].[Apropriacoes] ([ObraId], [Data]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_ComposicaoItens_ComposicaoId] ON [qh].[ComposicaoItens] ([ComposicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_ComposicaoItens_FuncaoObraId] ON [qh].[ComposicaoItens] ([FuncaoObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_ComposicaoItens_ProdutoId] ON [qh].[ComposicaoItens] ([ProdutoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Composicoes_EmpresaId] ON [qh].[Composicoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Composicoes_EmpresaId_Codigo] ON [qh].[Composicoes] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Composicoes_ServicoControladoId] ON [qh].[Composicoes] ([ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_ContratoItemLocais_ContratoItemId] ON [qh].[ContratoItemLocais] ([ContratoItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_ContratoItemLocais_LocalId] ON [qh].[ContratoItemLocais] ([LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_CustosMaoObra_ColaboradorId] ON [qh].[CustosMaoObra] ([ColaboradorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_CustosMaoObra_EmpresaId] ON [qh].[CustosMaoObra] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_CustosMaoObra_EmpresaId_ObraId] ON [qh].[CustosMaoObra] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_CustosMaoObra_EquipeId] ON [qh].[CustosMaoObra] ([EquipeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_CustosMaoObra_FuncaoObraId] ON [qh].[CustosMaoObra] ([FuncaoObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_CustosMaoObra_ObraId_Competencia] ON [qh].[CustosMaoObra] ([ObraId], [Competencia]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_EquipeMembros_ColaboradorId] ON [qh].[EquipeMembros] ([ColaboradorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_EquipeMembros_EquipeId] ON [qh].[EquipeMembros] ([EquipeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Equipes_EmpresaId] ON [qh].[Equipes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Equipes_EmpresaId_ObraId] ON [qh].[Equipes] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Equipes_ObraId_Codigo] ON [qh].[Equipes] ([ObraId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Estabelecimentos_EmpresaId] ON [qh].[Estabelecimentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Estabelecimentos_EmpresaId_Cnpj] ON [qh].[Estabelecimentos] ([EmpresaId], [Cnpj]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Estabelecimentos_EmpresaId_Codigo] ON [qh].[Estabelecimentos] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Estabelecimentos_SpeId] ON [qh].[Estabelecimentos] ([SpeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_FluxosEstabelecimento_DestinoId] ON [qh].[FluxosEstabelecimento] ([DestinoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_FluxosEstabelecimento_EmpresaId] ON [qh].[FluxosEstabelecimento] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_FluxosEstabelecimento_OrigemId] ON [qh].[FluxosEstabelecimento] ([OrigemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FornecedorExigencias_FornecedorId_TipoDocumentoExigidoId] ON [qh].[FornecedorExigencias] ([FornecedorId], [TipoDocumentoExigidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_FornecedorExigencias_TipoDocumentoExigidoId] ON [qh].[FornecedorExigencias] ([TipoDocumentoExigidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_FornecedorGrupos_CategoriaProdutoId] ON [qh].[FornecedorGrupos] ([CategoriaProdutoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FornecedorGrupos_FornecedorId_CategoriaProdutoId] ON [qh].[FornecedorGrupos] ([FornecedorId], [CategoriaProdutoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FornecedorServicos_FornecedorId_ServicoControladoId] ON [qh].[FornecedorServicos] ([FornecedorId], [ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_FornecedorServicos_ServicoControladoId] ON [qh].[FornecedorServicos] ([ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_RequisicaoItemLocais_LocalId] ON [qh].[RequisicaoItemLocais] ([LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_RequisicaoItemLocais_RequisicaoItemId] ON [qh].[RequisicaoItemLocais] ([RequisicaoItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_ServicoAplicacoes_ComposicaoId] ON [qh].[ServicoAplicacoes] ([ComposicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_ServicoAplicacoes_EmpresaId] ON [qh].[ServicoAplicacoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ServicoAplicacoes_TipologiaId_ComposicaoId_Alvo_AmbienteModeloId] ON [qh].[ServicoAplicacoes] ([TipologiaId], [ComposicaoId], [Alvo], [AmbienteModeloId]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ServicosLocais_ComposicaoId_LocalId] ON [qh].[ServicosLocais] ([ComposicaoId], [LocalId]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_ServicosLocais_EmpresaId] ON [qh].[ServicosLocais] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_ServicosLocais_EmpresaId_ObraId] ON [qh].[ServicosLocais] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_ServicosLocais_LocalId] ON [qh].[ServicosLocais] ([LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_ServicosLocais_ObraId_LocalId] ON [qh].[ServicosLocais] ([ObraId], [LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_TipologiaAmbientes_TipologiaId] ON [qh].[TipologiaAmbientes] ([TipologiaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_TipologiaMix_ApartamentoTipologiaId] ON [qh].[TipologiaMix] ([ApartamentoTipologiaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_TipologiaMix_TipologiaId] ON [qh].[TipologiaMix] ([TipologiaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_TransferenciaItens_TransferenciaId] ON [qh].[TransferenciaItens] ([TransferenciaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Transferencias_EmpresaId] ON [qh].[Transferencias] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Transferencias_EmpresaId_Numero] ON [qh].[Transferencias] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Transferencias_EmpresaId_ObraId] ON [qh].[Transferencias] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Transferencias_ObraId] ON [qh].[Transferencias] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Transferencias_RecebimentoId] ON [qh].[Transferencias] ([RecebimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    CREATE INDEX [IX_Transferencias_SpeId] ON [qh].[Transferencias] ([SpeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[CentrosCusto] ADD CONSTRAINT [FK_CentrosCusto_CentrosCusto_PaiId] FOREIGN KEY ([PaiId]) REFERENCES [qh].[CentrosCusto] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[ContratoItens] ADD CONSTRAINT [FK_ContratoItens_Composicoes_ComposicaoId] FOREIGN KEY ([ComposicaoId]) REFERENCES [qh].[Composicoes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Contratos] ADD CONSTRAINT [FK_Contratos_Estabelecimentos_EstabelecimentoFaturamentoId] FOREIGN KEY ([EstabelecimentoFaturamentoId]) REFERENCES [qh].[Estabelecimentos] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[MateriaisControlados] ADD CONSTRAINT [FK_MateriaisControlados_CategoriasProduto_CategoriaProdutoId] FOREIGN KEY ([CategoriaProdutoId]) REFERENCES [qh].[CategoriasProduto] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Pedidos] ADD CONSTRAINT [FK_Pedidos_Estabelecimentos_EstabelecimentoFaturamentoId] FOREIGN KEY ([EstabelecimentoFaturamentoId]) REFERENCES [qh].[Estabelecimentos] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Quadras] ADD CONSTRAINT [FK_Quadras_Tipologias_ImplantacaoId] FOREIGN KEY ([ImplantacaoId]) REFERENCES [qh].[Tipologias] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Recebimentos] ADD CONSTRAINT [FK_Recebimentos_Estabelecimentos_EstabelecimentoId] FOREIGN KEY ([EstabelecimentoId]) REFERENCES [qh].[Estabelecimentos] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[RequisicaoItens] ADD CONSTRAINT [FK_RequisicaoItens_Composicoes_ComposicaoId] FOREIGN KEY ([ComposicaoId]) REFERENCES [qh].[Composicoes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    ALTER TABLE [qh].[Requisicoes] ADD CONSTRAINT [FK_Requisicoes_Estabelecimentos_EstabelecimentoFaturamentoId] FOREIGN KEY ([EstabelecimentoFaturamentoId]) REFERENCES [qh].[Estabelecimentos] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    UPDATE qh.Locais SET Tipo = N'Condomínio' WHERE Tipo = N'Quadra';
    UPDATE qh.Locais SET Tipo = N'Torre' WHERE Tipo IN (N'Prédio', N'Predio', N'Bloco');
    UPDATE qh.Locais SET Tipo = N'Apartamento' WHERE Tipo = N'Unidade';
    UPDATE qh.Locais SET Tipo = N'Cômodo' WHERE Tipo = N'Ambiente';
    UPDATE qh.ModelosInspecao SET NivelLocal = N'Torre' WHERE NivelLocal IN (N'Prédio', N'Predio', N'Bloco');
    UPDATE qh.ModelosInspecao SET NivelLocal = N'Apartamento' WHERE NivelLocal = N'Unidade';
    UPDATE qh.ModelosInspecao SET NivelLocal = N'Cômodo' WHERE NivelLocal = N'Ambiente';
    UPDATE qh.Quadras SET Tipo = N'Condomínio' WHERE Tipo = N'Quadra';
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    EXEC(N'UPDATE qh.TiposDocumento SET Frequencia = CASE WHEN PorEntrega = 1 THEN N''PorEntrega'' WHEN ValidadePadraoDias > 0 THEN N''Validade'' ELSE N''NoCadastro'' END');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004120634_CondominioOrcamentoFornecedores'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20261004120634_CondominioOrcamentoFornecedores', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'
    WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY ObraId, Competencia, EquipeId, ColaboradorId, FuncaoObraId, LEFT(Descricao, 120) ORDER BY COALESCE(AlteradoEm, CriadoEm) DESC) n
               FROM qh.CustosMaoObra WHERE Excluido = 0)
    UPDATE c SET Excluido = 1, ExcluidoEm = SYSDATETIMEOFFSET(), ExcluidoPor = N''migracao'' FROM qh.CustosMaoObra c JOIN d ON d.Id = c.Id WHERE d.n > 1;
    UPDATE qh.CustosMaoObra SET Descricao = LEFT(Descricao, 120) WHERE LEN(Descricao) > 120;')
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'
    WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY EmpresaId ORDER BY COALESCE(AlteradoEm, CriadoEm) DESC) n FROM qh.Estabelecimentos WHERE Excluido = 0 AND FaturaMateriais = 1)
    UPDATE e SET FaturaMateriais = 0 FROM qh.Estabelecimentos e JOIN d ON d.Id = e.Id WHERE d.n > 1;
    WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY EmpresaId ORDER BY COALESCE(AlteradoEm, CriadoEm) DESC) n FROM qh.Estabelecimentos WHERE Excluido = 0 AND AlmoxarifadoCentral = 1)
    UPDATE e SET AlmoxarifadoCentral = 0 FROM qh.Estabelecimentos e JOIN d ON d.Id = e.Id WHERE d.n > 1;
    WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY SpeId ORDER BY COALESCE(AlteradoEm, CriadoEm) DESC) n FROM qh.Estabelecimentos WHERE Excluido = 0 AND SpeId IS NOT NULL)
    UPDATE e SET SpeId = NULL FROM qh.Estabelecimentos e JOIN d ON d.Id = e.Id WHERE d.n > 1;')
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'
    UPDATE qh.FornecedorDocumentos SET Competencia = DATEFROMPARTS(YEAR(Competencia), MONTH(Competencia), 1) WHERE Competencia IS NOT NULL AND DAY(Competencia) <> 1;
    WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY FornecedorId, TipoDocumentoExigidoId, Competencia ORDER BY CriadoEm DESC) n FROM qh.FornecedorDocumentos WHERE Excluido = 0 AND Competencia IS NOT NULL)
    UPDATE f SET Excluido = 1, ExcluidoEm = SYSDATETIMEOFFSET(), ExcluidoPor = N''migracao'' FROM qh.FornecedorDocumentos f JOIN d ON d.Id = f.Id WHERE d.n > 1;')
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'
    WITH g AS (SELECT RequisicaoItemId, LocalId, ComposicaoId, SUM(Quantidade) q, MIN(CAST(Id AS nvarchar(36))) manter FROM qh.RequisicaoItemLocais WHERE ComposicaoId IS NOT NULL GROUP BY RequisicaoItemId, LocalId, ComposicaoId HAVING COUNT(*) > 1)
    UPDATE r SET Quantidade = g.q FROM qh.RequisicaoItemLocais r JOIN g ON CAST(r.Id AS nvarchar(36)) = g.manter;
    DELETE r FROM qh.RequisicaoItemLocais r WHERE r.ComposicaoId IS NOT NULL AND EXISTS (SELECT 1 FROM qh.RequisicaoItemLocais o WHERE o.RequisicaoItemId = r.RequisicaoItemId AND o.LocalId = r.LocalId AND o.ComposicaoId = r.ComposicaoId AND CAST(o.Id AS nvarchar(36)) < CAST(r.Id AS nvarchar(36)));
    WITH g AS (SELECT ContratoItemId, LocalId, SUM(Quantidade) q, MIN(CAST(Id AS nvarchar(36))) manter FROM qh.ContratoItemLocais GROUP BY ContratoItemId, LocalId HAVING COUNT(*) > 1)
    UPDATE c SET Quantidade = g.q FROM qh.ContratoItemLocais c JOIN g ON CAST(c.Id AS nvarchar(36)) = g.manter;
    DELETE c FROM qh.ContratoItemLocais c WHERE EXISTS (SELECT 1 FROM qh.ContratoItemLocais o WHERE o.ContratoItemId = c.ContratoItemId AND o.LocalId = c.LocalId AND CAST(o.Id AS nvarchar(36)) < CAST(c.Id AS nvarchar(36)));
    DELETE m FROM qh.EquipeMembros m WHERE EXISTS (SELECT 1 FROM qh.EquipeMembros o WHERE o.EquipeId = m.EquipeId AND o.ColaboradorId = m.ColaboradorId AND CAST(o.Id AS nvarchar(36)) < CAST(m.Id AS nvarchar(36)));')
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'
    WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY TipologiaId, Sigla ORDER BY Ordem, Id) n FROM qh.TipologiaAmbientes WHERE Sigla IS NOT NULL)
    UPDATE a SET Sigla = LEFT(a.Sigla, 6) + CAST(d.n AS nvarchar(2)) FROM qh.TipologiaAmbientes a JOIN d ON d.Id = a.Id WHERE d.n > 1;')
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'
    WITH d AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY RecebimentoId ORDER BY CASE Status WHEN N''Recebida'' THEN 0 WHEN N''Enviada'' THEN 1 ELSE 2 END, CriadoEm) n
               FROM qh.Transferencias WHERE Excluido = 0 AND RecebimentoId IS NOT NULL AND Status <> N''Cancelada'')
    UPDATE t SET Status = N''Cancelada'', Observacao = CONCAT(Observacao, N'' [cancelada na migração: transferência repetida]'') FROM qh.Transferencias t JOIN d ON d.Id = t.Id WHERE d.n > 1;')
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'
    WITH arv AS (
        SELECT l.Id, s.Id AS SpeId FROM qh.Quadras q JOIN qh.Spes s ON s.QuadraId = q.Id AND s.Excluido = 0 JOIN qh.Locais l ON l.Id = q.LocalId WHERE q.Excluido = 0
        UNION ALL
        SELECT f.Id, a.SpeId FROM qh.Locais f JOIN arv a ON f.LocalPaiId = a.Id)
    UPDATE l SET SpeId = a.SpeId FROM qh.Locais l JOIN arv a ON a.Id = l.Id WHERE l.SpeId IS NULL OR l.SpeId <> a.SpeId
    OPTION (MAXRECURSION 50);
    UPDATE c SET SpeId = l.SpeId FROM qh.ServicosLocais c JOIN qh.Locais l ON l.Id = c.LocalId WHERE (c.SpeId IS NULL AND l.SpeId IS NOT NULL) OR c.SpeId <> l.SpeId;')
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'
    WITH arv AS (
        SELECT l.Id, q.Id AS QuadraId FROM qh.Quadras q JOIN qh.Locais l ON l.Id = q.LocalId WHERE q.Excluido = 0
        UNION ALL
        SELECT f.Id, a.QuadraId FROM qh.Locais f JOIN arv a ON f.LocalPaiId = a.Id),
    ap AS (SELECT l.Id, a.QuadraId, COALESCE(l.AreaPrivativa, 0) Area, l.FracaoIdeal, l.Codigo FROM qh.Locais l JOIN arv a ON a.Id = l.Id WHERE l.Tipo = N''Apartamento'' AND l.Excluido = 0),
    tot AS (SELECT QuadraId, SUM(Area) Area, SUM(COALESCE(FracaoIdeal, 0)) Soma FROM ap GROUP BY QuadraId),
    rec AS (SELECT ap.Id, ap.QuadraId, ROUND(ap.Area / tot.Area, 6) f, ROW_NUMBER() OVER (PARTITION BY ap.QuadraId ORDER BY ap.Codigo DESC) ult
            FROM ap JOIN tot ON tot.QuadraId = ap.QuadraId WHERE tot.Area > 0 AND ABS(tot.Soma - 1) > 0.0001)
    UPDATE l SET FracaoIdeal = rec.f FROM qh.Locais l JOIN rec ON rec.Id = l.Id
    OPTION (MAXRECURSION 50);
    WITH arv AS (
        SELECT l.Id, q.Id AS QuadraId FROM qh.Quadras q JOIN qh.Locais l ON l.Id = q.LocalId WHERE q.Excluido = 0
        UNION ALL
        SELECT f.Id, a.QuadraId FROM qh.Locais f JOIN arv a ON f.LocalPaiId = a.Id),
    ap AS (SELECT l.Id, a.QuadraId, l.FracaoIdeal, ROW_NUMBER() OVER (PARTITION BY a.QuadraId ORDER BY l.Codigo DESC) ult FROM qh.Locais l JOIN arv a ON a.Id = l.Id WHERE l.Tipo = N''Apartamento'' AND l.Excluido = 0),
    tot AS (SELECT QuadraId, SUM(COALESCE(FracaoIdeal, 0)) Soma FROM ap GROUP BY QuadraId)
    UPDATE l SET FracaoIdeal = l.FracaoIdeal + (1 - tot.Soma) FROM qh.Locais l JOIN ap ON ap.Id = l.Id AND ap.ult = 1 JOIN tot ON tot.QuadraId = ap.QuadraId
    WHERE ABS(tot.Soma - 1) > 0 AND ABS(tot.Soma - 1) < 0.001
    OPTION (MAXRECURSION 50);')
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    DROP INDEX [IX_Transferencias_RecebimentoId] ON [qh].[Transferencias];
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    DROP INDEX [IX_RequisicaoItemLocais_RequisicaoItemId] ON [qh].[RequisicaoItemLocais];
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    DROP INDEX [IX_FornecedorDocumentos_FornecedorId] ON [qh].[FornecedorDocumentos];
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    DROP INDEX [IX_Estabelecimentos_SpeId] ON [qh].[Estabelecimentos];
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    DROP INDEX [IX_EquipeMembros_EquipeId] ON [qh].[EquipeMembros];
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    DROP INDEX [IX_ContratoItemLocais_ContratoItemId] ON [qh].[ContratoItemLocais];
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[qh].[CustosMaoObra]') AND [c].[name] = N'Descricao');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [qh].[CustosMaoObra] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [qh].[CustosMaoObra] ALTER COLUMN [Descricao] nvarchar(120) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    CREATE INDEX [IX_Transferencias_OrigemEstabelecimentoId] ON [qh].[Transferencias] ([OrigemEstabelecimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    CREATE INDEX [IX_Transferencias_PedidoId] ON [qh].[Transferencias] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Transferencias_Recebimento] ON [qh].[Transferencias] ([RecebimentoId]) WHERE [Excluido] = 0 AND [RecebimentoId] IS NOT NULL AND [Status] <> N''Cancelada''');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    CREATE INDEX [IX_TransferenciaItens_LocalId] ON [qh].[TransferenciaItens] ([LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    CREATE INDEX [IX_TransferenciaItens_ProdutoId] ON [qh].[TransferenciaItens] ([ProdutoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    CREATE INDEX [IX_TransferenciaItens_RecebimentoItemId] ON [qh].[TransferenciaItens] ([RecebimentoItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TipologiaAmbientes_TipologiaId_Sigla] ON [qh].[TipologiaAmbientes] ([TipologiaId], [Sigla]) WHERE [Sigla] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    CREATE INDEX [IX_ServicosLocais_SpeId] ON [qh].[ServicosLocais] ([SpeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    CREATE INDEX [IX_ServicoAplicacoes_AmbienteModeloId] ON [qh].[ServicoAplicacoes] ([AmbienteModeloId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    CREATE INDEX [IX_ServicoAplicacoes_ObraId] ON [qh].[ServicoAplicacoes] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    CREATE INDEX [IX_RequisicaoItemLocais_ComposicaoId] ON [qh].[RequisicaoItemLocais] ([ComposicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RequisicaoItemLocais_RequisicaoItemId_LocalId_ComposicaoId] ON [qh].[RequisicaoItemLocais] ([RequisicaoItemId], [LocalId], [ComposicaoId]) WHERE [ComposicaoId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_FornecedorDocumentos_Competencia] ON [qh].[FornecedorDocumentos] ([FornecedorId], [TipoDocumentoExigidoId], [Competencia]) WHERE [Excluido] = 0 AND [Competencia] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Estabelecimentos_Almoxarifado] ON [qh].[Estabelecimentos] ([EmpresaId], [AlmoxarifadoCentral]) WHERE [Excluido] = 0 AND [AlmoxarifadoCentral] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Estabelecimentos_FaturaMateriais] ON [qh].[Estabelecimentos] ([EmpresaId], [FaturaMateriais]) WHERE [Excluido] = 0 AND [FaturaMateriais] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Estabelecimentos_Spe] ON [qh].[Estabelecimentos] ([SpeId]) WHERE [Excluido] = 0 AND [SpeId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EquipeMembros_EquipeId_ColaboradorId] ON [qh].[EquipeMembros] ([EquipeId], [ColaboradorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CustosMaoObra_Linha] ON [qh].[CustosMaoObra] ([ObraId], [Competencia], [EquipeId], [ColaboradorId], [FuncaoObraId], [Descricao]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContratoItemLocais_ContratoItemId_LocalId] ON [qh].[ContratoItemLocais] ([ContratoItemId], [LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    CREATE INDEX [IX_Apropriacoes_Lancamento] ON [qh].[Apropriacoes] ([Lancamento]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    ALTER TABLE [qh].[Locais] ADD CONSTRAINT [FK_Locais_TipologiaAmbientes_AmbienteModeloId] FOREIGN KEY ([AmbienteModeloId]) REFERENCES [qh].[TipologiaAmbientes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    ALTER TABLE [qh].[RequisicaoItemLocais] ADD CONSTRAINT [FK_RequisicaoItemLocais_Composicoes_ComposicaoId] FOREIGN KEY ([ComposicaoId]) REFERENCES [qh].[Composicoes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    ALTER TABLE [qh].[ServicoAplicacoes] ADD CONSTRAINT [FK_ServicoAplicacoes_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    ALTER TABLE [qh].[ServicoAplicacoes] ADD CONSTRAINT [FK_ServicoAplicacoes_TipologiaAmbientes_AmbienteModeloId] FOREIGN KEY ([AmbienteModeloId]) REFERENCES [qh].[TipologiaAmbientes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    ALTER TABLE [qh].[ServicosLocais] ADD CONSTRAINT [FK_ServicosLocais_Spes_SpeId] FOREIGN KEY ([SpeId]) REFERENCES [qh].[Spes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    ALTER TABLE [qh].[TransferenciaItens] ADD CONSTRAINT [FK_TransferenciaItens_Locais_LocalId] FOREIGN KEY ([LocalId]) REFERENCES [qh].[Locais] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    ALTER TABLE [qh].[TransferenciaItens] ADD CONSTRAINT [FK_TransferenciaItens_Produtos_ProdutoId] FOREIGN KEY ([ProdutoId]) REFERENCES [qh].[Produtos] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    ALTER TABLE [qh].[TransferenciaItens] ADD CONSTRAINT [FK_TransferenciaItens_RecebimentoItens_RecebimentoItemId] FOREIGN KEY ([RecebimentoItemId]) REFERENCES [qh].[RecebimentoItens] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    ALTER TABLE [qh].[Transferencias] ADD CONSTRAINT [FK_Transferencias_Estabelecimentos_OrigemEstabelecimentoId] FOREIGN KEY ([OrigemEstabelecimentoId]) REFERENCES [qh].[Estabelecimentos] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    ALTER TABLE [qh].[Transferencias] ADD CONSTRAINT [FK_Transferencias_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [qh].[Pedidos] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004152719_RevisaoIntegridade'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20261004152719_RevisaoIntegridade', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004182735_AreaFornecedorCodigo'
)
BEGIN
    CREATE TABLE [qh].[CodigosAcessoFornecedor] (
        [Id] uniqueidentifier NOT NULL,
        [Documento] nvarchar(14) NOT NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [ContatoId] uniqueidentifier NOT NULL,
        [Canal] nvarchar(40) NOT NULL,
        [Destino] nvarchar(200) NOT NULL,
        [CodigoHash] nvarchar(64) NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [ExpiraEm] datetimeoffset NOT NULL,
        [Tentativas] int NOT NULL,
        [UsadoEm] datetimeoffset NULL,
        [Ip] nvarchar(64) NULL,
        CONSTRAINT [PK_CodigosAcessoFornecedor] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CodigosAcessoFornecedor_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004182735_AreaFornecedorCodigo'
)
BEGIN
    CREATE INDEX [IX_CodigosAcessoFornecedor_Documento_CriadoEm] ON [qh].[CodigosAcessoFornecedor] ([Documento], [CriadoEm]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004182735_AreaFornecedorCodigo'
)
BEGIN
    CREATE INDEX [IX_CodigosAcessoFornecedor_FornecedorId] ON [qh].[CodigosAcessoFornecedor] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004182735_AreaFornecedorCodigo'
)
BEGIN
    CREATE INDEX [IX_CodigosAcessoFornecedor_Ip_CriadoEm] ON [qh].[CodigosAcessoFornecedor] ([Ip], [CriadoEm]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261004182735_AreaFornecedorCodigo'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20261004182735_AreaFornecedorCodigo', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261006040044_TipologiaNumeracaoPorAndar'
)
BEGIN
    ALTER TABLE [qh].[Tipologias] ADD [NumeracaoPorAndar] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261006040044_TipologiaNumeracaoPorAndar'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20261006040044_TipologiaNumeracaoPorAndar', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261006044525_ModeloOrientacoes'
)
BEGIN
    ALTER TABLE [qh].[ModelosInspecao] ADD [Orientacoes] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261006044525_ModeloOrientacoes'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20261006044525_ModeloOrientacoes', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261006114857_SpeEngenheiro'
)
BEGIN
    ALTER TABLE [qh].[Spes] ADD [EngenheiroId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261006114857_SpeEngenheiro'
)
BEGIN
    EXEC(N'UPDATE s SET s.EngenheiroId = o.EngenheiroId FROM qh.Spes s JOIN qh.Obras o ON o.Id = s.ObraId WHERE s.EngenheiroId IS NULL AND o.EngenheiroId IS NOT NULL;');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20261006114857_SpeEngenheiro'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20261006114857_SpeEngenheiro', N'10.0.12');
END;

COMMIT;
GO


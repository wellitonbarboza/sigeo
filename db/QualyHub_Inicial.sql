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
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    IF SCHEMA_ID(N'qh') IS NULL EXEC(N'CREATE SCHEMA [qh];');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Alcadas] (
        [Id] uniqueidentifier NOT NULL,
        [Entidade] nvarchar(40) NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [Ordem] int NOT NULL,
        [PerfilAprovador] nvarchar(40) NOT NULL,
        [UsuarioAprovadorId] uniqueidentifier NULL,
        [ValorMinimo] decimal(18,4) NULL,
        [ValorMaximo] decimal(18,4) NULL,
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
    WHERE [MigrationId] = N'20260925053437_Inicial'
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
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    IF SCHEMA_ID(N'qh_hist') IS NULL EXEC(N'CREATE SCHEMA [qh_hist];');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[DocumentosSgq] (
        [Id] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [Codigo] nvarchar(30) NOT NULL,
        [Titulo] nvarchar(400) NOT NULL,
        [ProcessoResponsavel] nvarchar(400) NULL,
        [ObraId] uniqueidentifier NULL,
        [RevisaoVigente] int NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [PrazoRetencaoAnos] int NULL,
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
        CONSTRAINT [PK_DocumentosSgq] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[DocumentoSgq_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Empresas] (
        [Id] uniqueidentifier NOT NULL,
        [RazaoSocial] nvarchar(400) NOT NULL,
        [NomeFantasia] nvarchar(400) NOT NULL,
        [Cnpj] nvarchar(18) NOT NULL,
        [NivelSiac] nvarchar(2) NOT NULL,
        [Ativa] bit NOT NULL,
        CONSTRAINT [PK_Empresas] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Fornecedores] (
        [Id] uniqueidentifier NOT NULL,
        [RazaoSocial] nvarchar(400) NOT NULL,
        [NomeFantasia] nvarchar(400) NULL,
        [CpfCnpj] nvarchar(18) NOT NULL,
        [Email] nvarchar(400) NULL,
        [Telefone] nvarchar(400) NULL,
        [Cidade] nvarchar(400) NULL,
        [Uf] nvarchar(2) NULL,
        [FornecedorMaterialControlado] bit NOT NULL,
        [PrestadorServicoControlado] bit NOT NULL,
        [Situacao] nvarchar(40) NOT NULL,
        [DataQualificacao] date NULL,
        [ProximaReavaliacao] date NULL,
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
    WHERE [MigrationId] = N'20260925053437_Inicial'
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
    WHERE [MigrationId] = N'20260925053437_Inicial'
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
    WHERE [MigrationId] = N'20260925053437_Inicial'
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
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[SolicitacoesAprovacao] (
        [Id] uniqueidentifier NOT NULL,
        [Entidade] nvarchar(40) NOT NULL,
        [EntidadeId] uniqueidentifier NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [Titulo] nvarchar(400) NOT NULL,
        [Valor] decimal(18,4) NOT NULL,
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
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[TiposDocumento] (
        [Id] uniqueidentifier NOT NULL,
        [AplicaA] nvarchar(40) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Descricao] nvarchar(400) NULL,
        [ValidadePadraoDias] int NULL,
        [DiasAlertaAntesVencimento] int NOT NULL,
        [Obrigatorio] bit NOT NULL,
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
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Usuarios] (
        [Id] uniqueidentifier NOT NULL,
        [EntraObjectId] uniqueidentifier NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [Perfil] nvarchar(40) NOT NULL,
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
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[FormModelos] (
        [Id] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Titulo] nvarchar(400) NOT NULL,
        [Versao] int NOT NULL,
        [DocumentoSgqOrigemId] uniqueidentifier NULL,
        [Vigente] bit NOT NULL,
        [ExigeFotoGeral] bit NOT NULL,
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
        CONSTRAINT [PK_FormModelos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FormModelos_DocumentosSgq_DocumentoSgqOrigemId] FOREIGN KEY ([DocumentoSgqOrigemId]) REFERENCES [qh].[DocumentosSgq] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[FormModelo_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[VersoesDocumentoSgq] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentoSgqId] uniqueidentifier NOT NULL,
        [Revisao] int NOT NULL,
        [DescricaoAlteracao] nvarchar(max) NOT NULL,
        [ArquivoBlob] nvarchar(400) NOT NULL,
        [ArquivoHashSha256] nvarchar(64) NOT NULL,
        [ElaboradoPor] nvarchar(400) NULL,
        [AnalisadoPor] nvarchar(400) NULL,
        [AprovadoPor] nvarchar(400) NULL,
        [AprovadoEm] datetimeoffset NULL,
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
        CONSTRAINT [PK_VersoesDocumentoSgq] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VersoesDocumentoSgq_DocumentosSgq_DocumentoSgqId] FOREIGN KEY ([DocumentoSgqId]) REFERENCES [qh].[DocumentosSgq] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[VersaoDocumentoSgq_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[FornecedorAvaliacoes] (
        [Id] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [DataAvaliacao] date NOT NULL,
        [Tipo] nvarchar(400) NOT NULL,
        [NotaPrazo] decimal(18,4) NOT NULL,
        [NotaQualidade] decimal(18,4) NOT NULL,
        [NotaAtendimento] decimal(18,4) NOT NULL,
        [NotaDocumentacao] decimal(18,4) NOT NULL,
        [NotaFinal] decimal(18,4) NOT NULL,
        [Resultado] nvarchar(40) NOT NULL,
        [StatusAprovacao] nvarchar(40) NOT NULL,
        [Observacao] nvarchar(400) NULL,
        [EmpresaId] uniqueidentifier NOT NULL,
        [CriadoEm] datetimeoffset NOT NULL,
        [CriadoPor] nvarchar(150) NOT NULL,
        [AlteradoEm] datetimeoffset NULL,
        [AlteradoPor] nvarchar(150) NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetimeoffset NULL,
        [ExcluidoPor] nvarchar(150) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_FornecedorAvaliacoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FornecedorAvaliacoes_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Funcionarios] (
        [Id] uniqueidentifier NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [CpfHash] nvarchar(64) NOT NULL,
        [CpfMascarado] nvarchar(14) NOT NULL,
        [Funcao] nvarchar(400) NULL,
        [Terceiro] bit NOT NULL,
        [FornecedorId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_Funcionarios] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Funcionarios_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
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
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Locais] (
        [Id] uniqueidentifier NOT NULL,
        [LocalPaiId] uniqueidentifier NULL,
        [Tipo] nvarchar(400) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
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
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Locais] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Locais_Locais_LocalPaiId] FOREIGN KEY ([LocalPaiId]) REFERENCES [qh].[Locais] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Locais_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Medicoes] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] nvarchar(400) NOT NULL,
        [NumeroContrato] nvarchar(400) NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [PeriodoInicio] date NOT NULL,
        [PeriodoFim] date NOT NULL,
        [ValorMedido] decimal(18,4) NOT NULL,
        [ValorRetencao] decimal(18,4) NOT NULL,
        [Status] nvarchar(40) NOT NULL,
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
        CONSTRAINT [PK_Medicoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Medicoes_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Medicoes_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Medicao_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Pedidos] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] nvarchar(400) NOT NULL,
        [DataPedido] date NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [ValorTotal] decimal(18,4) NOT NULL,
        [PrevisaoEntrega] date NULL,
        [Solicitante] nvarchar(400) NULL,
        [Status] nvarchar(40) NOT NULL,
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
        CONSTRAINT [FK_Pedidos_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pedidos_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Pedido_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
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
        [Justificativa] nvarchar(2000) NULL,
        CONSTRAINT [PK_EtapasAprovacao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EtapasAprovacao_SolicitacoesAprovacao_SolicitacaoAprovacaoId] FOREIGN KEY ([SolicitacaoAprovacaoId]) REFERENCES [qh].[SolicitacoesAprovacao] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[FornecedorDocumentos] (
        [Id] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NOT NULL,
        [TipoDocumentoExigidoId] uniqueidentifier NOT NULL,
        [DataEmissao] date NULL,
        [DataValidade] date NULL,
        [ArquivoBlob] nvarchar(400) NULL,
        [ArquivoNomeOriginal] nvarchar(400) NULL,
        [ArquivoHashSha256] nvarchar(64) NULL,
        [Status] nvarchar(40) NOT NULL,
        [Observacao] nvarchar(400) NULL,
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
    WHERE [MigrationId] = N'20260925053437_Inicial'
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
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[FormItens] (
        [Id] uniqueidentifier NOT NULL,
        [FormModeloId] uniqueidentifier NOT NULL,
        [Ordem] int NOT NULL,
        [Grupo] nvarchar(400) NOT NULL,
        [ItemVerificacao] nvarchar(1000) NOT NULL,
        [CriterioAceitacao] nvarchar(2000) NOT NULL,
        [MetodoVerificacao] nvarchar(1000) NULL,
        [TipoResposta] nvarchar(40) NOT NULL,
        [ToleranciaMin] decimal(18,4) NULL,
        [ToleranciaMax] decimal(18,4) NULL,
        [Obrigatorio] bit NOT NULL,
        [ExigeFotoSeNaoConforme] bit NOT NULL,
        CONSTRAINT [PK_FormItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FormItens_FormModelos_FormModeloId] FOREIGN KEY ([FormModeloId]) REFERENCES [qh].[FormModelos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[MateriaisControlados] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [Unidade] nvarchar(400) NULL,
        [CodigosInsumo] nvarchar(max) NULL,
        [FormModeloFvmId] uniqueidentifier NULL,
        [ExigeFvmParaAceite] bit NOT NULL,
        [ExigeFornecedorQualificado] bit NOT NULL,
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
        CONSTRAINT [PK_MateriaisControlados] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MateriaisControlados_FormModelos_FormModeloFvmId] FOREIGN KEY ([FormModeloFvmId]) REFERENCES [qh].[FormModelos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[ServicosControlados] (
        [Id] uniqueidentifier NOT NULL,
        [Codigo] nvarchar(400) NOT NULL,
        [Nome] nvarchar(400) NOT NULL,
        [CodigosItemOrcamento] nvarchar(400) NULL,
        [DocumentoPeisId] uniqueidentifier NULL,
        [FormModeloFvsId] uniqueidentifier NULL,
        [ExigeFvsParaMedicao] bit NOT NULL,
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
        CONSTRAINT [FK_ServicosControlados_DocumentosSgq_DocumentoPeisId] FOREIGN KEY ([DocumentoPeisId]) REFERENCES [qh].[DocumentosSgq] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServicosControlados_FormModelos_FormModeloFvsId] FOREIGN KEY ([FormModeloFvsId]) REFERENCES [qh].[FormModelos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Distribuicoes] (
        [Id] uniqueidentifier NOT NULL,
        [VersaoDocumentoSgqId] uniqueidentifier NOT NULL,
        [ObraId] uniqueidentifier NULL,
        [Destinatario] nvarchar(400) NOT NULL,
        [CopiaImpressa] bit NOT NULL,
        [DistribuidoEm] datetimeoffset NOT NULL,
        [RecolhidoEm] datetimeoffset NULL,
        CONSTRAINT [PK_Distribuicoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Distribuicoes_VersoesDocumentoSgq_VersaoDocumentoSgqId] FOREIGN KEY ([VersaoDocumentoSgqId]) REFERENCES [qh].[VersoesDocumentoSgq] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[FuncionarioDocumentos] (
        [Id] uniqueidentifier NOT NULL,
        [FuncionarioId] uniqueidentifier NOT NULL,
        [TipoDocumentoExigidoId] uniqueidentifier NOT NULL,
        [DataRealizacao] date NULL,
        [DataValidade] date NULL,
        [Apto] bit NULL,
        [CargaHoraria] nvarchar(400) NULL,
        [ArquivoBlob] nvarchar(400) NULL,
        [ArquivoHashSha256] nvarchar(64) NULL,
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
        CONSTRAINT [PK_FuncionarioDocumentos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FuncionarioDocumentos_Funcionarios_FuncionarioId] FOREIGN KEY ([FuncionarioId]) REFERENCES [qh].[Funcionarios] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FuncionarioDocumentos_TiposDocumento_TipoDocumentoExigidoId] FOREIGN KEY ([TipoDocumentoExigidoId]) REFERENCES [qh].[TiposDocumento] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[FuncionarioDocumento_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[FuncionariosObras] (
        [Id] uniqueidentifier NOT NULL,
        [FuncionarioId] uniqueidentifier NOT NULL,
        [ObraId] uniqueidentifier NOT NULL,
        [DataIntegracao] date NULL,
        [AcessoLiberado] bit NOT NULL,
        CONSTRAINT [PK_FuncionariosObras] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FuncionariosObras_Funcionarios_FuncionarioId] FOREIGN KEY ([FuncionarioId]) REFERENCES [qh].[Funcionarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[PedidoItens] (
        [Id] uniqueidentifier NOT NULL,
        [PedidoId] uniqueidentifier NOT NULL,
        [CodigoInsumo] nvarchar(400) NOT NULL,
        [Descricao] nvarchar(400) NOT NULL,
        [Unidade] nvarchar(400) NULL,
        [Quantidade] decimal(18,4) NOT NULL,
        [ValorUnitario] decimal(18,4) NOT NULL,
        [ValorTotal] decimal(18,4) NOT NULL,
        [MaterialControladoId] uniqueidentifier NULL,
        [QuantidadeRecebidaAceita] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_PedidoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PedidoItens_MateriaisControlados_MaterialControladoId] FOREIGN KEY ([MaterialControladoId]) REFERENCES [qh].[MateriaisControlados] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PedidoItens_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [qh].[Pedidos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[MedicaoItens] (
        [Id] uniqueidentifier NOT NULL,
        [MedicaoId] uniqueidentifier NOT NULL,
        [CodigoItem] nvarchar(400) NOT NULL,
        [Descricao] nvarchar(400) NOT NULL,
        [Unidade] nvarchar(400) NULL,
        [QuantidadeMedida] decimal(18,4) NOT NULL,
        [ValorUnitario] decimal(18,4) NOT NULL,
        [ValorTotal] decimal(18,4) NOT NULL,
        [LocalId] uniqueidentifier NULL,
        [LocalTexto] nvarchar(400) NULL,
        [ServicoControladoId] uniqueidentifier NULL,
        CONSTRAINT [PK_MedicaoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MedicaoItens_Locais_LocalId] FOREIGN KEY ([LocalId]) REFERENCES [qh].[Locais] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MedicaoItens_Medicoes_MedicaoId] FOREIGN KEY ([MedicaoId]) REFERENCES [qh].[Medicoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MedicaoItens_ServicosControlados_ServicoControladoId] FOREIGN KEY ([ServicoControladoId]) REFERENCES [qh].[ServicosControlados] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Fichas] (
        [Id] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [Numero] nvarchar(400) NOT NULL,
        [FormModeloId] uniqueidentifier NOT NULL,
        [VersaoModelo] int NOT NULL,
        [ServicoControladoId] uniqueidentifier NULL,
        [LocalId] uniqueidentifier NULL,
        [PrestadorFornecedorId] uniqueidentifier NULL,
        [MaterialControladoId] uniqueidentifier NULL,
        [PedidoId] uniqueidentifier NULL,
        [PedidoItemId] uniqueidentifier NULL,
        [NotaFiscal] nvarchar(400) NULL,
        [Lote] nvarchar(400) NULL,
        [QuantidadeRecebida] decimal(18,4) NULL,
        [QuantidadeAceita] decimal(18,4) NULL,
        [Status] nvarchar(40) NOT NULL,
        [FichaOrigemReinspecaoId] uniqueidentifier NULL,
        [InspetorNome] nvarchar(400) NOT NULL,
        [InspetorUsuarioId] uniqueidentifier NOT NULL,
        [CapturadoEm] datetimeoffset NOT NULL,
        [SincronizadoEm] datetimeoffset NULL,
        [DispositivoId] nvarchar(400) NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [Observacao] nvarchar(max) NULL,
        [AprovadoPorUsuarioId] uniqueidentifier NULL,
        [AprovadoEm] datetimeoffset NULL,
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
        CONSTRAINT [PK_Fichas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Fichas_Fichas_FichaOrigemReinspecaoId] FOREIGN KEY ([FichaOrigemReinspecaoId]) REFERENCES [qh].[Fichas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Fichas_FormModelos_FormModeloId] FOREIGN KEY ([FormModeloId]) REFERENCES [qh].[FormModelos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Fichas_Fornecedores_PrestadorFornecedorId] FOREIGN KEY ([PrestadorFornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Fichas_Locais_LocalId] FOREIGN KEY ([LocalId]) REFERENCES [qh].[Locais] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Fichas_MateriaisControlados_MaterialControladoId] FOREIGN KEY ([MaterialControladoId]) REFERENCES [qh].[MateriaisControlados] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Fichas_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Fichas_PedidoItens_PedidoItemId] FOREIGN KEY ([PedidoItemId]) REFERENCES [qh].[PedidoItens] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Fichas_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [qh].[Pedidos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Fichas_ServicosControlados_ServicoControladoId] FOREIGN KEY ([ServicoControladoId]) REFERENCES [qh].[ServicosControlados] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Ficha_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[FichaEvidencias] (
        [Id] uniqueidentifier NOT NULL,
        [FichaId] uniqueidentifier NOT NULL,
        [FormItemId] uniqueidentifier NULL,
        [ArquivoBlob] nvarchar(400) NOT NULL,
        [ArquivoHashSha256] nvarchar(64) NOT NULL,
        [TamanhoBytes] bigint NOT NULL,
        [CapturadoEm] datetimeoffset NOT NULL,
        [UploadConcluido] bit NOT NULL,
        CONSTRAINT [PK_FichaEvidencias] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FichaEvidencias_Fichas_FichaId] FOREIGN KEY ([FichaId]) REFERENCES [qh].[Fichas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FichaEvidencias_FormItens_FormItemId] FOREIGN KEY ([FormItemId]) REFERENCES [qh].[FormItens] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[FichaRespostas] (
        [Id] uniqueidentifier NOT NULL,
        [FichaId] uniqueidentifier NOT NULL,
        [FormItemId] uniqueidentifier NOT NULL,
        [Resultado] nvarchar(40) NOT NULL,
        [ValorNumerico] decimal(18,4) NULL,
        [ValorTexto] nvarchar(2000) NULL,
        [Observacao] nvarchar(2000) NULL,
        CONSTRAINT [PK_FichaRespostas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FichaRespostas_Fichas_FichaId] FOREIGN KEY ([FichaId]) REFERENCES [qh].[Fichas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FichaRespostas_FormItens_FormItemId] FOREIGN KEY ([FormItemId]) REFERENCES [qh].[FormItens] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE TABLE [qh].[Rncs] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] nvarchar(400) NOT NULL,
        [Origem] nvarchar(40) NOT NULL,
        [FichaId] uniqueidentifier NULL,
        [FornecedorId] uniqueidentifier NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [AcaoImediata] nvarchar(400) NULL,
        [AnaliseCausa] nvarchar(max) NULL,
        [AcaoCorretiva] nvarchar(max) NULL,
        [Responsavel] nvarchar(400) NULL,
        [PrazoTratativa] date NULL,
        [VerificacaoEficacia] nvarchar(max) NULL,
        [Eficaz] bit NULL,
        [Status] nvarchar(40) NOT NULL,
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
        [ObraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Rncs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Rncs_Fichas_FichaId] FOREIGN KEY ([FichaId]) REFERENCES [qh].[Fichas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Rncs_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [qh].[Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Rncs_Obras_ObraId] FOREIGN KEY ([ObraId]) REFERENCES [qh].[Obras] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [qh_hist].[Rnc_Historico]));
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Alcadas_EmpresaId] ON [qh].[Alcadas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Auditoria_Entidade_EntidadeId] ON [qh].[Auditoria] ([Entidade], [EntidadeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Auditoria_Quando] ON [qh].[Auditoria] ([Quando]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Distribuicoes_VersaoDocumentoSgqId] ON [qh].[Distribuicoes] ([VersaoDocumentoSgqId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_DocumentosSgq_EmpresaId] ON [qh].[DocumentosSgq] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DocumentosSgq_EmpresaId_Codigo] ON [qh].[DocumentosSgq] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Empresas_Cnpj] ON [qh].[Empresas] ([Cnpj]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_EtapasAprovacao_PerfilAprovador_Decisao] ON [qh].[EtapasAprovacao] ([PerfilAprovador], [Decisao]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_EtapasAprovacao_SolicitacaoAprovacaoId] ON [qh].[EtapasAprovacao] ([SolicitacaoAprovacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FichaEvidencias_FichaId] ON [qh].[FichaEvidencias] ([FichaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FichaEvidencias_FormItemId] ON [qh].[FichaEvidencias] ([FormItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FichaRespostas_FichaId_FormItemId] ON [qh].[FichaRespostas] ([FichaId], [FormItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FichaRespostas_FormItemId] ON [qh].[FichaRespostas] ([FormItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fichas_EmpresaId] ON [qh].[Fichas] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Fichas_EmpresaId_Numero] ON [qh].[Fichas] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fichas_EmpresaId_ObraId] ON [qh].[Fichas] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fichas_FichaOrigemReinspecaoId] ON [qh].[Fichas] ([FichaOrigemReinspecaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fichas_FormModeloId] ON [qh].[Fichas] ([FormModeloId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fichas_LocalId] ON [qh].[Fichas] ([LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fichas_MaterialControladoId] ON [qh].[Fichas] ([MaterialControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fichas_ObraId_Tipo_Status] ON [qh].[Fichas] ([ObraId], [Tipo], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fichas_PedidoId] ON [qh].[Fichas] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fichas_PedidoItemId] ON [qh].[Fichas] ([PedidoItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fichas_PrestadorFornecedorId] ON [qh].[Fichas] ([PrestadorFornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fichas_ServicoControladoId_LocalId] ON [qh].[Fichas] ([ServicoControladoId], [LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FormItens_FormModeloId] ON [qh].[FormItens] ([FormModeloId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FormModelos_DocumentoSgqOrigemId] ON [qh].[FormModelos] ([DocumentoSgqOrigemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FormModelos_EmpresaId] ON [qh].[FormModelos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_FormModelos_EmpresaId_Codigo_Versao] ON [qh].[FormModelos] ([EmpresaId], [Codigo], [Versao]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorAvaliacoes_EmpresaId] ON [qh].[FornecedorAvaliacoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorAvaliacoes_FornecedorId] ON [qh].[FornecedorAvaliacoes] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorDocumentos_DataValidade] ON [qh].[FornecedorDocumentos] ([DataValidade]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorDocumentos_EmpresaId] ON [qh].[FornecedorDocumentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorDocumentos_FornecedorId] ON [qh].[FornecedorDocumentos] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FornecedorDocumentos_TipoDocumentoExigidoId] ON [qh].[FornecedorDocumentos] ([TipoDocumentoExigidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Fornecedores_EmpresaId] ON [qh].[Fornecedores] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Fornecedores_EmpresaId_CpfCnpj] ON [qh].[Fornecedores] ([EmpresaId], [CpfCnpj]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FuncionarioDocumentos_DataValidade] ON [qh].[FuncionarioDocumentos] ([DataValidade]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FuncionarioDocumentos_EmpresaId] ON [qh].[FuncionarioDocumentos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FuncionarioDocumentos_FuncionarioId] ON [qh].[FuncionarioDocumentos] ([FuncionarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FuncionarioDocumentos_TipoDocumentoExigidoId] ON [qh].[FuncionarioDocumentos] ([TipoDocumentoExigidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Funcionarios_EmpresaId] ON [qh].[Funcionarios] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Funcionarios_EmpresaId_CpfHash] ON [qh].[Funcionarios] ([EmpresaId], [CpfHash]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Funcionarios_FornecedorId] ON [qh].[Funcionarios] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_FuncionariosObras_FuncionarioId] ON [qh].[FuncionariosObras] ([FuncionarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_LayoutsImportacao_EmpresaId] ON [qh].[LayoutsImportacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_LinhasImportacao_LoteImportacaoId] ON [qh].[LinhasImportacao] ([LoteImportacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Locais_EmpresaId] ON [qh].[Locais] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Locais_EmpresaId_ObraId] ON [qh].[Locais] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Locais_LocalPaiId] ON [qh].[Locais] ([LocalPaiId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Locais_ObraId] ON [qh].[Locais] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_LotesImportacao_EmpresaId] ON [qh].[LotesImportacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_LotesImportacao_EmpresaId_ArquivoHashSha256] ON [qh].[LotesImportacao] ([EmpresaId], [ArquivoHashSha256]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_MateriaisControlados_EmpresaId] ON [qh].[MateriaisControlados] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MateriaisControlados_EmpresaId_Codigo] ON [qh].[MateriaisControlados] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_MateriaisControlados_FormModeloFvmId] ON [qh].[MateriaisControlados] ([FormModeloFvmId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_MedicaoItens_LocalId] ON [qh].[MedicaoItens] ([LocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_MedicaoItens_MedicaoId] ON [qh].[MedicaoItens] ([MedicaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_MedicaoItens_ServicoControladoId] ON [qh].[MedicaoItens] ([ServicoControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Medicoes_EmpresaId] ON [qh].[Medicoes] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Medicoes_EmpresaId_Numero] ON [qh].[Medicoes] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Medicoes_EmpresaId_ObraId] ON [qh].[Medicoes] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Medicoes_FornecedorId] ON [qh].[Medicoes] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Medicoes_ObraId] ON [qh].[Medicoes] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Obras_EmpresaId] ON [qh].[Obras] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Obras_EmpresaId_Codigo] ON [qh].[Obras] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_CodigoInsumo] ON [qh].[PedidoItens] ([CodigoInsumo]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_MaterialControladoId] ON [qh].[PedidoItens] ([MaterialControladoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_PedidoId] ON [qh].[PedidoItens] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_EmpresaId] ON [qh].[Pedidos] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Pedidos_EmpresaId_Numero] ON [qh].[Pedidos] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_EmpresaId_ObraId] ON [qh].[Pedidos] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_FornecedorId] ON [qh].[Pedidos] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_ObraId] ON [qh].[Pedidos] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Rncs_EmpresaId] ON [qh].[Rncs] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Rncs_EmpresaId_Numero] ON [qh].[Rncs] ([EmpresaId], [Numero]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Rncs_EmpresaId_ObraId] ON [qh].[Rncs] ([EmpresaId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Rncs_FichaId] ON [qh].[Rncs] ([FichaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Rncs_FornecedorId] ON [qh].[Rncs] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Rncs_ObraId] ON [qh].[Rncs] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_ServicosControlados_DocumentoPeisId] ON [qh].[ServicosControlados] ([DocumentoPeisId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_ServicosControlados_EmpresaId] ON [qh].[ServicosControlados] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ServicosControlados_EmpresaId_Codigo] ON [qh].[ServicosControlados] ([EmpresaId], [Codigo]) WHERE [Excluido] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_ServicosControlados_FormModeloFvsId] ON [qh].[ServicosControlados] ([FormModeloFvsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesAprovacao_EmpresaId] ON [qh].[SolicitacoesAprovacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesAprovacao_EmpresaId_Status] ON [qh].[SolicitacoesAprovacao] ([EmpresaId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesAprovacao_Entidade_EntidadeId] ON [qh].[SolicitacoesAprovacao] ([Entidade], [EntidadeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_TiposDocumento_EmpresaId] ON [qh].[TiposDocumento] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_Usuarios_EmpresaId] ON [qh].[Usuarios] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_EntraObjectId] ON [qh].[Usuarios] ([EntraObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_UsuariosObras_ObraId] ON [qh].[UsuariosObras] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UsuariosObras_UsuarioId_ObraId] ON [qh].[UsuariosObras] ([UsuarioId], [ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VersoesDocumentoSgq_DocumentoSgqId_Revisao] ON [qh].[VersoesDocumentoSgq] ([DocumentoSgqId], [Revisao]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    CREATE INDEX [IX_VersoesDocumentoSgq_EmpresaId] ON [qh].[VersoesDocumentoSgq] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925053437_Inicial'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20260925053437_Inicial', N'10.0.12');
END;

COMMIT;
GO


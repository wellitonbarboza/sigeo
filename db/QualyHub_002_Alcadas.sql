BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    ALTER TABLE [qh].[SolicitacoesAprovacao] ADD [SolicitanteNome] nvarchar(150) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    ALTER TABLE [qh].[SolicitacoesAprovacao] ADD [SolicitanteUsuarioId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    ALTER TABLE [qh].[EtapasAprovacao] ADD [DelegacaoAprovacaoId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    ALTER TABLE [qh].[EtapasAprovacao] ADD [EmNomeDeNome] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    ALTER TABLE [qh].[EtapasAprovacao] ADD [EmNomeDeUsuarioId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    IF SCHEMA_ID(N'qh_hist') IS NULL EXEC(N'CREATE SCHEMA [qh_hist];');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
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
        [Motivo] nvarchar(500) NOT NULL,
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
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
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
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesAprovacao_SolicitanteUsuarioId] ON [qh].[SolicitacoesAprovacao] ([SolicitanteUsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    CREATE INDEX [IX_EtapasAprovacao_DelegacaoAprovacaoId] ON [qh].[EtapasAprovacao] ([DelegacaoAprovacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    CREATE INDEX [IX_EtapasAprovacao_UsuarioAprovadorId] ON [qh].[EtapasAprovacao] ([UsuarioAprovadorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    CREATE INDEX [IX_DelegacoesAprovacao_EmpresaId] ON [qh].[DelegacoesAprovacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    CREATE INDEX [IX_DelegacoesAprovacao_ObraId] ON [qh].[DelegacoesAprovacao] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    CREATE INDEX [IX_DelegacoesAprovacao_SubstitutoUsuarioId_Inicio_Fim] ON [qh].[DelegacoesAprovacao] ([SubstitutoUsuarioId], [Inicio], [Fim]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    CREATE INDEX [IX_DelegacoesAprovacao_TitularUsuarioId] ON [qh].[DelegacoesAprovacao] ([TitularUsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    CREATE INDEX [IX_LimitesAprovacao_EmpresaId] ON [qh].[LimitesAprovacao] ([EmpresaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    CREATE INDEX [IX_LimitesAprovacao_ObraId] ON [qh].[LimitesAprovacao] ([ObraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LimitesAprovacao_UsuarioId_Entidade_ObraId] ON [qh].[LimitesAprovacao] ([UsuarioId], [Entidade], [ObraId]) WHERE [Excluido] = 0 AND [Ativo] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    ALTER TABLE [qh].[EtapasAprovacao] ADD CONSTRAINT [FK_EtapasAprovacao_DelegacoesAprovacao_DelegacaoAprovacaoId] FOREIGN KEY ([DelegacaoAprovacaoId]) REFERENCES [qh].[DelegacoesAprovacao] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [qh].[__Migracoes]
    WHERE [MigrationId] = N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao'
)
BEGIN
    INSERT INTO [qh].[__Migracoes] ([MigrationId], [ProductVersion])
    VALUES (N'20260925155444_AlcadasPorUsuarioDelegacaoSegregacao', N'10.0.12');
END;

COMMIT;
GO


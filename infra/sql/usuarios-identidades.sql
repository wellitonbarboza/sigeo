-- =====================================================================================
-- QualyHub — usuário da Managed Identity do App Service no Azure SQL (menor privilégio)
-- {{WEB}} é substituído pelo deploy.sh
-- =====================================================================================
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '{{WEB}}')
    CREATE USER [{{WEB}}] FROM EXTERNAL PROVIDER;
GO

IF DATABASE_PRINCIPAL_ID('qualyhub_app') IS NULL CREATE ROLE qualyhub_app;
GO
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::qh TO qualyhub_app;
-- Exclusão de registros de negócio é sempre lógica (o AuditoriaInterceptor converte DELETE em Excluido=1).
-- DELETE físico só ocorre em itens/vínculos sincronizados pelo formulário (ex.: itens de pedido/medição).
-- Log de auditoria é append-only também no banco, não só na aplicação.
DENY UPDATE, DELETE ON qh.Auditoria TO qualyhub_app;
-- Histórico temporal: somente leitura.
GRANT SELECT ON SCHEMA::qh_hist TO qualyhub_app;
GO

ALTER ROLE qualyhub_app ADD MEMBER [{{WEB}}];
GO

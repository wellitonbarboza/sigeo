-- =====================================================================================
-- QualyHub — usuários das Managed Identities no Azure SQL (menor privilégio)
-- {{WEB}} e {{FUNC}} são substituídos pelo deploy.sh
-- =====================================================================================
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '{{WEB}}')
    CREATE USER [{{WEB}}] FROM EXTERNAL PROVIDER;
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '{{FUNC}}')
    CREATE USER [{{FUNC}}] FROM EXTERNAL PROVIDER;
GO

IF DATABASE_PRINCIPAL_ID('qualyhub_app') IS NULL CREATE ROLE qualyhub_app;
GO
GRANT SELECT, INSERT, UPDATE ON SCHEMA::qh TO qualyhub_app;
-- Sem DELETE no schema: exclusão é sempre lógica (o interceptor converte).
-- DELETE físico só em tabelas de vínculo/itens reprocessáveis (registrado no log de auditoria).
GRANT DELETE ON qh.UsuariosObras     TO qualyhub_app;
GRANT DELETE ON qh.FuncionariosObras TO qualyhub_app;
GRANT DELETE ON qh.PedidoItens       TO qualyhub_app;
GRANT DELETE ON qh.MedicaoItens      TO qualyhub_app;
GRANT DELETE ON qh.LinhasImportacao  TO qualyhub_app;
-- Log de auditoria é append-only também no banco, não só na aplicação.
DENY UPDATE ON qh.Auditoria TO qualyhub_app;
-- Histórico temporal: somente leitura.
GRANT SELECT ON SCHEMA::qh_hist TO qualyhub_app;
GO

ALTER ROLE qualyhub_app ADD MEMBER [{{WEB}}];
ALTER ROLE qualyhub_app ADD MEMBER [{{FUNC}}];
GO

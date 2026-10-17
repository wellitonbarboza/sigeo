# Trabalhar no SIGEO pelo VS Code

Guia para abrir, rodar, testar e mexer no código. Tudo o que o VS Code precisa já vem no repositório (pasta `.vscode`).

## 1. Instalar uma vez

| O quê | Para quê |
|---|---|
| [.NET SDK 10](https://dotnet.microsoft.com/download) | compilar e rodar (`global.json` fixa a versão 10.0) |
| [VS Code](https://code.visualstudio.com/) | editor |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | SQL Server local (opcional se você já tem um SQL Server ou LocalDB) |
| Python 3 | só para regenerar o mapa do banco (`docs/BANCO.md`) |

Ao abrir a pasta, o VS Code pergunta se quer instalar as **extensões recomendadas** (`.vscode/extensions.json`). Aceite:

- **C# Dev Kit**: navegação, "Ir para definição" (F12), testes no painel lateral.
- **SQL Server (mssql)**: ver tabelas e dados do banco.
- **Markdown Preview Mermaid Support**: diagramas do `docs/BANCO.md`.
- **EditorConfig**, **Docker**, **Python**.

## 2. Abrir o projeto

1. Descompacte o código (ou clone do GitHub) numa pasta sem espaços estranhos, por exemplo `C:\dev\sigeo`.
2. VS Code → **Arquivo › Abrir Pasta…** → escolha a pasta (a que tem `Sigeo.slnx`).
3. Espere o C# Dev Kit carregar a solução (barra inferior). O painel **Solution Explorer** mostra os projetos.

O explorador já vem limpo: `bin/` e `obj/` escondidos, `Tela.razor` agrupado com `Tela.razor.cs` e `.razor.css`, cada migração agrupada com o seu `.Designer.cs`.

## 3. Banco local

**Opção A: Docker (Windows, Mac ou Linux)**

```powershell
# PowerShell (Windows). A senha precisa de maiúscula, minúscula, número e símbolo, 8+ caracteres.
$env:SIGEO_SQL_SENHA="<sua-senha-local>"
docker compose up -d sql
```

Ou pelo VS Code: **Terminal › Executar Tarefa… › SQL local: subir (Docker)** (pede a senha).

**Opção B: LocalDB (só Windows, já vem com o Visual Studio)**: não precisa fazer nada; é o padrão de `appsettings.Development.json`.

**Apontar o site para o banco (Opção A)**: crie `src/Sigeo.Web/appsettings.Development.local.json` (fica fora do Git, a senha não vai para o repositório):

```json
{
  "ConnectionStrings": {
    "Sigeo": "Server=localhost,1433;Database=Sigeo;User Id=sa;Password=<sua-senha-local>;TrustServerCertificate=True"
  }
}
```

Em desenvolvimento o site cria/atualiza o banco sozinho ao iniciar (aplica as migrações). Banco vazio abre a **configuração inicial**: cadastre uma empresa fictícia e o administrador.

## 4. Rodar, depurar e testar

| Ação | Como |
|---|---|
| Rodar com depuração | **F5** (configuração "SIGEO (site local)") → abre http://localhost:5144 |
| Rodar recarregando ao salvar | Tarefa **rodar o site (watch)** |
| Compilar | **Ctrl+Shift+B** |
| Testes das regras | Tarefa **testes (unitários)** ou painel de Testes |
| Testes no banco real | Tarefa **testes (integração, precisa do SQL local)** (pede a senha do SQL) |
| Nova migração | Tarefa **nova migração** (pede o nome) |
| Atualizar o mapa do banco | Tarefa **mapa do banco (docs/BANCO.md)** |

Pontos de parada (F9) funcionam no `.razor.cs`, nos serviços e nas consultas.

## 5. Onde fica cada coisa

O mesmo nome de módulo aparece em todas as camadas. Exemplo, a tela de **Documentação mensal** de Suprimentos:

| Camada | Arquivo | Papel |
|---|---|---|
| Tela | `src/Sigeo.Web/Components/Pages/Suprimentos/DocumentacaoMensal.razor` | marcação (o que aparece) |
| Código da tela | bloco `@code` do `.razor`; telas maiores usam `…/Tela.razor.cs` | eventos e estado da tela |
| Leitura | `src/Sigeo.Application/Suprimentos/CompetenciasServico.cs` (`FilaAsync`, `DetalheAsync`); nas outras telas, `<Tela>Consultas.cs` | busca os dados que a tela mostra |
| Gravação e regra | `src/Sigeo.Application/Suprimentos/CompetenciasServico.cs` | valida, aplica a regra, grava |
| Entidade (tabela) | `src/Sigeo.Domain/Suprimentos/CompetenciaDocumental.cs` | colunas e regras da própria entidade |
| Configuração da tabela | `src/Sigeo.Infrastructure/Dados/Configuracoes/Suprimentos.cs` | tamanhos, índices, chaves |
| Migração | `src/Sigeo.Infrastructure/Dados/Migracoes/` | cria/altera a tabela no banco |
| Menu e permissão | `src/Sigeo.Contratos/Acesso.cs` (`CatalogoTelas`) | rota, nome no menu, ações por perfil |
| Testes | `tests/Sigeo.Tests/Suprimentos/` e `tests/Sigeo.Tests.Integracao/Suprimentos/` | regras e banco real |

Pastas de tela que têm nome de menu diferente do módulo: `Pages/Documentos` = menu **Qualidade (SGQ)**; `Pages/Qualidade` = **Qualidade da obra**; `Pages/Sst` = **Segurança e pessoas**; telas de planejamento ficam em `Pages/Engenharia` e as consultas em `Application/Planejamento`.

## 6. Regras da casa (o que os testes conferem)

- **A tela não abre o banco.** Ela lê por uma classe `*Consultas` e grava por um `*Servico` da Application. O teste `TelasSemBancoTests` falha se uma tela usar o banco direto.
- **Classe terminada em `Consultas` entra no DI sozinha**; `*Servico` é registrado em `src/Sigeo.Infrastructure/DependencyInjection.cs`.
- **Toda rota do menu tem tela** (`CatalogoTemPaginaTests`) e **toda consulta de tela roda no SQL** (`ConsultasTelasTests`, integração).
- **Um tipo por arquivo**; DTOs de um serviço em `<Servico>.Dtos.cs`; enums do módulo em `<Módulo>/Enums.cs` (os compartilhados em `Domain/Common/Enums/`).
- **Namespace = pasta** (`Sigeo.Application.Suprimentos`, `Sigeo.Domain.Suprimentos`…).
- **Migração só acrescenta** (coluna nova anulável ou com padrão; tabela nova). Nunca editar migração já publicada.
- Senhas e chaves: só em `appsettings.*.local.json` (fora do Git) ou no Key Vault. Nunca no código.
- Dados de teste: sempre fictícios.

## 7. Checklist: tela nova

1. Entidade em `Domain/<Módulo>/<Entidade>.cs` (+ `DbSet` em `ISigeoDb` e `SigeoDbContext`).
2. Configuração em `Infrastructure/Dados/Configuracoes/<Módulo>.cs`.
3. Tarefa **nova migração** → conferir o arquivo gerado (só `AddColumn`/`CreateTable`).
4. Regra e gravação em `Application/<Módulo>/<Processo>Servico.cs` (registrar no DI).
5. Leitura em `Application/<Módulo>/<Tela>Consultas.cs`.
6. Tela em `Web/Components/Pages/<Módulo>/<Tela>.razor` (+ `.razor.cs` se o código passar de ~100 linhas).
7. Rota e permissão em `Contratos/Acesso.cs` (`CatalogoTelas` e `PerfisPadrao`).
8. Testes em `tests/…/<Módulo>/`.
9. Tarefa **mapa do banco** e commit junto.

## 8. Na VM do cérebro (VS Code no navegador)

Na VM da equipe (pacote `cerebro-sigeo-azure`), o código fica em `/srv/cerebro/projetos/sigeo/repo` e já vem pronto:
.NET 10, `dotnet-ef`, SQL Server de desenvolvimento em `127.0.0.1:1433` (variáveis `QH_TEST_SQL`, `QH_SQL` e
`ConnectionStrings__Sigeo` carregadas no terminal para quem está no grupo `sigeo-dev`) e o comando `sigeo-consulta`
para ler o banco de produção pelo endpoint privado, com a conta Entra de cada pessoa (grupo SIGEO-Leitura, só SELECT).
As extensões vêm do Open VSX: use **C#** (muhammad-sammy.csharp) no lugar do C# Dev Kit e **SQLTools** no lugar do mssql.

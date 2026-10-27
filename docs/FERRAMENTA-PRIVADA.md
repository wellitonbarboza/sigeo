# Ferramenta privada: Planning Lab no endereço do SIGEO

O Planning Lab é um produto independente: tem repositório e build próprios, e é um PWA em Blazor WebAssembly. O SIGEO só **empresta o endereço e o login** para ele.

| | |
|---|---|
| Menu | Nenhum. O SIGEO não tem link para a ferramenta. |
| Banco / perfis | Nenhum. Não é módulo. |
| Endereço | `https://<site-sigeo>/<PlanningLab:Caminho>/` (ex.: `/x/pl-3f9a1c7e20b4`) |
| Quem abre | Quem estiver logado no SIGEO com e-mail em `PlanningLab:Emails` |
| Outros usuários | Recebem a mesma página "não encontrada" de qualquer endereço inexistente |
| Sem login | O SIGEO pede login e depois volta para a ferramenta |
| Arquivos | Contêiner privado `ferramentas`, prefixo `planning-lab/`, lido pela Managed Identity do site |
| Atualizar | `bash infra/publicar-planning-lab.sh PlanningLab_publish_wwwroot.zip`. Não republica o SIGEO. |
| Desligar | Apagar o app setting `PlanningLab__Caminho` |

**Código**
- `src/Sigeo.Web/Seguranca/FerramentaPrivada.cs`: serve os arquivos depois do login.
- `src/Sigeo.Application/Comum/AcessoPrivado.cs`: regras puras, testadas em `tests/Sigeo.Tests/Comum/AcessoPrivadoTests.cs`.

**Configuração (App Settings)**
- `PlanningLab__Caminho`: obrigatório para ligar a ferramenta.
- `PlanningLab__Emails`: separados por vírgula.
- `PlanningLab__Conteiner`: opcional, padrão `ferramentas`.
- `PlanningLab__Prefixo`: opcional, padrão `planning-lab`.

**Desenvolvimento.** Sem `Sigeo:BlobUri`, os arquivos são lidos de `App_Data/planning-lab` (ou de `PlanningLab:PastaLocal`).

## Histórico (obras, revisões, cenários, simulações e versões)

O histórico usa o mesmo caminho escondido e o mesmo bloqueio: só abre logado e com e-mail em `PlanningLab:Emails`. Não cria tabela no banco do SIGEO.

| | |
|---|---|
| API | `GET {caminho}/api/estado` · `GET {caminho}/api/dados?prefixo=` · `GET`/`PUT`/`DELETE {caminho}/api/dados/{nome}` |
| Armazenamento | Contêiner privado `ferramentas`, prefixo `planning-lab-dados/`, lido e gravado pela Managed Identity do site |
| Estrutura | `obras/{obra}/obra.json` · `rev/{rev}/cronograma.xml` e `cenarios.json` · `simulacoes/*.json` · `versoes/vNNN/` |
| Escrita | Exige o cabeçalho `X-PlanningLab: 1`, o que bloqueia envio a partir de outro site. Limite de 40 MB por arquivo |
| Versões | Pastas `versoes` são imutáveis: regravar devolve 409 e apagar devolve 403 |
| Configuração opcional | `PlanningLab__PrefixoDados` (padrão `planning-lab-dados`); em desenvolvimento, `PlanningLab__PastaDados` (padrão `App_Data/planning-lab-dados`) |

**Código.** `src/Sigeo.Web/Seguranca/ApiDadosPrivados.cs`, com as regras de nome em `AcessoPrivado` (testadas).

## Simulador de Sequência (27/10/2026)

O mesmo caminho escondido pode servir o **Simulador de Sequência** (página única: `index.html`, `manifest.webmanifest`, `icone.svg`) no lugar do Planning Lab. Publicar com:

```bash
CAMINHO=/x/simulador bash publicar-planning-lab.sh simulador_publish.zip seu-email
```

Os estudos ficam no histórico privado (`planning-lab-dados/simulador/*.json`), pela mesma API (`api/dados`), visíveis só para o e-mail liberado. Sem servidor (arquivo aberto localmente), ficam no navegador.

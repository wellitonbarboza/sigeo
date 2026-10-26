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

# QualyHub — Arquitetura (outubro/2026)

## Camadas

```
QualyHub.Web.Client (WebAssembly) ──HTTP /api/telas──┐
        ▲ (layout e componentes compartilhados)      ▼
QualyHub.Web (Blazor Server + API)  ──►  QualyHub.Application (casos de uso)  ──►  QualyHub.Domain (regras puras)
        │                                   ▲      │
        └──────►  QualyHub.Infrastructure ──┘      └──► QualyHub.Contratos (DTOs da API)
```

| Projeto | O que mora aqui | O que NÃO mora aqui |
|---|---|---|
| **Domain** | entidades, value objects, regras puras (travas, rateio, motor do orçamento, gerador de locais) | EF, SQL, Azure |
| **Application** | serviços de caso de uso (`Servicos/`), abstrações (`Abstracoes/`: `IQualyHubDb`, `IFabricaDb`, `IFabricaSistema`, `IUsuarioAtual`, `IArmazenamento`), utilitários (`Comum/`), carga padrão (`Carga/`) | provedor de banco, migrations, Blob, planilhas |
| **Infrastructure** | `QualyHubDbContext` (implementa `IQualyHubDb`), configurações EF, migrations, interceptor de auditoria, `FabricaDb`/`FabricaSistema`, armazenamento (Blob/local), importadores de planilha | regra de negócio |
| **Web** | telas `.razor` do servidor, autenticação, endpoints de arquivo, API das telas do navegador (`Servicos/ApiTelas.cs`) | gravação no banco (toda gravação passa por serviço) |
| **Web.Client** | telas que rodam no navegador (WebAssembly), layout (`MainLayout`) e componentes de apresentação usados pelos dois lados (`Icone`, `Selo`, `Kpi`, `Pagina`, `Modal`, `Abas`, `SeletorLocais`, `SeletorDemanda`), `Rotulos` | acesso a banco (só pela API) |
| **Contratos** | DTOs trocados entre API e navegador (`Paineis.cs`, `Orcamento.cs`, `Opcao`) | regra, EF |

## Regras de convivência
- **Tela não grava no banco.** Lê com `Fabrica.CriarLeitura()` (sem rastreamento) e grava chamando um serviço da Application dentro de `Executar(...)`.
- **Serviço confere perfil** com `Permissoes.ExigirAsync` quando a ação é restrita (a tela só esconde o botão).
- **Transação** com `IFabricaDb.EmTransacaoAsync(...)` — compatível com a nova tentativa do Azure SQL (cada tentativa usa um contexto novo).
- **Sem MediatR e sem repositório genérico**: `DbSet` já é repositório e `SaveChanges` a unidade de trabalho.
- **Consultas**: `SplitQuery` é o padrão (sem produto cartesiano); listas grandes paginam no banco (`Skip/Take`).
- **Cancelamento**: as telas passam `Cancelamento` (da `PaginaBase`) para serviços e consultas.
- **Erros**: regra de negócio = `RegraDeNegocioException`/`ValidacaoException` (mensagem na tela; 409 nos endpoints, em Problem Details); erro inesperado é registrado e não derruba a tela (`ErrorBoundary` + `Executar`).

## Suprimentos (um serviço por processo)
`RequisicoesServico`, `CotacoesServico` (inclui gerar pedido/contrato), `PedidosServico`, `ContratosServico` (inclui aditivo), `RecebimentosServico`, `TransferenciasServico`, `MedicoesServico`, `PagamentosServico`, `AvaliacaoFornecedoresServico`.

## Frontend
- Blazor Server interativo **sem pré-renderização** (cada tela consulta o banco uma vez).
- **Telas no navegador (WebAssembly)**: painel de indicadores (`/painel`), painel SST (`/sst/painel`), mapa da torre (`/obra/mapa-torre`) e orçamento por local (`/engenharia/orcamento`). O `App.razor` escolhe o modo pela rota (`RotasNavegador.Atende`); ir de uma tela do servidor para uma do navegador recarrega a página (cada roteador só conhece as suas rotas; `RecarregarNoServidor` e `NotFound` tratam a navegação por código).
- **API das telas do navegador** (`/api/telas/...`): mesmo cookie do login, exige usuário interno, confere a obra, responde Problem Details; sessão vencida = 401 (a tela volta ao login). Painéis com cache de 5 min por empresa (`CacheCompartilhado`). A tela manda só escolhas; quantidades e valores são recalculados no servidor (ex.: requisição pelo orçamento).
- Primeiro acesso a uma tela do navegador baixa ~3 MB (comprimido) uma vez; depois fica no cache do navegador.
- Componentes base: `Pagina`, `Grade` (teclado, filtros guardados por tela), `Campo` (rótulo ligado ao campo), `Erros` (anunciado), `Modal`, `Confirmacao`, `Selo` (ícone + cor), `SeletorObra`, `SeletorLocais`, `SeletorDemanda`, `QuebraPorLocal`, `Paginacao`.
- CSS: `app.css` (tema, componentes, utilitárias, celular) + `css/telas-*.css` por área.

## Testes
- `tests/QualyHub.Tests`: unitários (domínio e validações dos serviços).
- `tests/QualyHub.Tests.Integracao`: cenários com SQL Server real; rodam quando a variável `QH_TEST_SQL` existe (veja o LEIAME da pasta).

## Área do Fornecedor (`/fornecedor`)
- Entrada pública por **CNPJ/CPF + código de 6 dígitos** enviado a um **contato cadastrado** escolhido pela pessoa (e-mail ou WhatsApp). Código: 10 min, uso único, 5 tentativas, guardado só como hash (`CodigosAcessoFornecedor`); limites por CNPJ (3/h), por IP (10/h) e por requisição (60 por 5 min).
- Sessão em cookie próprio (`qualyhub-fornecedor`, 8 h, sem renovação) — separado do login interno; telas internas exigem a claim de empresa. O circuito do Blazor junta as duas identidades e cada tela confere a sua.
- Depois de entrar: escolhe o CNPJ/construtora (o do documento e os outros em que o mesmo e-mail/celular verificado é contato), vê as cotações com situação (não enviada / enviada / encerrada) e preenche ou edita a proposta; aba "Cadastro e documentos".
- Envio por `IEnvioMensagens` (Application) → `EnvioAcs` (Azure Communication Services, identidade gerenciada). Desenvolvimento: `EnvioLocalLog` (código no log). Produção sem configuração: canais indisponíveis. Configuração: `infra/configurar-comunicacao.sh`.
- Links antigos (`/portal/{token}` e `/cotacao/{chave}`) continuam valendo até vencer; a equipe agora usa "Enviar convite"/"Convidar" (contato + canal).

## Notas fiscais de entrada (SIEG / XML)
- `Domain/Fiscal`: `NotaFiscalEntrada` (+ itens), `ImportacaoNota` (registro de cada busca), `ConferenciaNota` (pareamento nota × pedido sem IA).
- `Application/Fiscal`: `LeitorNfe` (XML 4.00 e ZIP; DTD desligado; evento 110111 = cancelamento), `NotasFiscaisServico` (importa sem repetir chave, só para CNPJs da construtora, guarda o XML no armazenamento, liga fornecedor pelo CNPJ e pedido pelo número citado ou único aberto).
- `Infrastructure/Servicos/FonteSieg` (`IFonteNotas`): API nova do SIEG (token com Client ID + Secret, `X-Api-Key`, ZIP de XMLs, 2 meses por chamada, ~2 chamadas/min). Credenciais por construtora no Key Vault: `Sieg--Empresas--{empresaId}--ApiKey|ClientId|SecretKey` (`infra/configurar-sieg.sh`). Endereços configuráveis (`Sieg:UrlBase`, `Sieg:CaminhoToken`, `Sieg:CaminhoBaixar`, `Sieg:TipoXmlNfe`).
- Rotina das 6h30 (`RotinaMatinal`): busca os últimos 5 dias de cada construtora configurada; notas que a regra não ligou passam pela IA (`IA:AnalisarNotasAutomatico`).
- Tela `Suprimentos › Notas fiscais`: importar XML/ZIP, buscar no SIEG, vincular/desvincular pedido, ajustar item, ignorar, baixar XML, gerar recebimento (`recebimentos?nota=`: chave, número e quantidades preenchidos). Registrar o recebimento com a chave tira a nota da fila.

## IA (Azure OpenAI + Document Intelligence)
- `Application/IA`: `IAssistenteIa`, `ILeitorDocumentos`, `AnalisesIaServico` (nota × pedido, mapa de cotação, RNC, classificação de produto, leitura de laudo). Tudo por botão (ou rotina de notas), resposta em JSON validado, registro em `UsosIa` e limite mensal por construtora (`IA:LimiteMensalPorEmpresa`).
- A IA sugere; não aprova, não escolhe vencedor. Única ação automática: vincular nota sem pedido quando a confiança ≥ 80% (marcado "sugestão IA"; o recebimento continua com a pessoa).
- `Infrastructure/Servicos/IaAzure.cs`: REST do Azure OpenAI (chat completions, modelos comuns ou de raciocínio) e do Document Intelligence (`prebuilt-layout`), autenticação por identidade gerenciada (sem chave). Configuração: `IA:Endpoint`, `IA:Deployment`, `IA:DocumentosEndpoint` (`infra/configurar-ia.sh` ou `main.bicep` com `criarIa`).

## Planejamento (Engenharia › Planejamento)
- **Domínio** (`Domain/Planejamento`): `VersaoCronograma` (LB0, R1…; em elaboração → em aprovação → vigente/substituída), `AtividadeCronograma` (EAP, duração em dias úteis, avanço, custo, base, folga, crítica), `VinculoAtividade` (TI/II/TT/IT + folga), `DesembolsoVersao`, `Feriado`, redes (`RedePlanejamento`, `RedeLocal`, `RedePacote`, `RedePacoteLocal`, `VinculoRede`).
- **Motor próprio** (`MotorCronograma`, função pura): calendário de dias úteis (`Calendario`: seg–sex, feriados nacionais calculados + municipais/paradas cadastrados), ida/volta do caminho crítico, folga total, críticas só entre não concluídas, soma dos agrupadores, EAP automática, predecessoras por EAP em texto (`1.2.3TI+2; 4II`). **Arrastar só move**: a folga do vínculo que segura a atividade muda (sem restrição de data); sem predecessora, muda o início mínimo.
- **Redes** (`GeradorRedes`): torre por apartamento, estrutura por pavimento, área comum por local; pacote = serviço do orçamento; fluxo das equipes (local k espera k − equipes), sequência no local + pulmão, vínculos entre redes (pavimento N+deslocamento ou após o fim da rede). Custo de cada atividade = orçamento (serviço × local, somando a subárvore). Regerar mantém avanço, base e folgas ajustadas pela chave estável. "Sugerir redes" monta tudo a partir do orçamento por local.
- **Aprovação** (`EntidadeAprovavel.Cronograma`): engenheiro responsável → diretor técnico (carga padrão v3). Aprovada: vira vigente, a anterior fica substituída, a 1ª aprovação congela a linha de base e o desembolso mensal (atual × base) é gravado. Devolvida volta para elaboração com o motivo.
- **Tela**: Gantt da Syncfusion (Blazor, modo manual — o cálculo é sempre o nosso; script e tema carregados só nesta tela, `wwwroot/lib/syncfusion`), textos em português (`LocalizadorSyncfusion`), telemetria do fornecedor desligada, licença em `Syncfusion:ChaveLicenca` (Key Vault `Syncfusion--ChaveLicenca`). Abas: cronograma (edição lateral), redes, linha de balanço (SVG), desembolso e curva S (SVG), versões e aprovação (`Autorizacoes`), calendário.
- **Exportações**: Excel (`/arquivos/cronograma/{versao}/excel`, ClosedXML, níveis agrupados + desembolso), MS Project XML (`.../msproject`, MSPDI com EAP, vínculos, folga e custo; importação na versão em elaboração), PDF/impressão A0–A4 em `engenharia/planejamento/imprimir` (SVG no tamanho da folha, salvar como PDF pelo navegador).

### Médio e curto prazo (Last Planner)
- Abas **Médio prazo · restrições** e **Curto prazo · PPC** (`CurtoPrazoServico`). Usam a versão vigente do cronograma (sem aprovação, a mais recente, com aviso).
- Restrição (`Restricao`, temporal): tipo, atividade (chave estável `Chave` do gerador; manual = `N:<nome>`), responsável e data-limite. Aberta = a atividade não entra no plano.
- Plano semanal (`PlanoSemanal` + `CompromissoSemanal`): compromissos tirados do cronograma (só livres) ou avulsos. Depois da segunda-feira entram como "fora da meta" (não contam no PPC) e o planejado não sai do plano.
- Fechamento a partir de sexta: todo planejado apontado; não concluído com causa. PPC gravado no fechamento (série não muda). Indicadores: PPC por semana, acumulado, Pareto das causas e restrições removidas no prazo.
- Perfis: Engenharia, Diretoria e Campo.

## Mensagens internas (`/mensagens`)
- `MensagensServico`: conversa (`Conversa`, temporal) com participantes (`ParticipanteConversa`: arquivada, lida até), mensagens imutáveis (`MensagemConversa`) e leitura por mensagem (`LeituraMensagem` = "lido por quem e quando").
- Cada usuário vê só as conversas de que participa. Destinatários: pessoa (`u:`), perfil (`p:`) ou obra (`o:` = quem tem acesso); grupo vira pessoas no envio.
- Ligação a registro (`RegistroEntidade/Id/Descricao`, rotas em `RotasRegistro`): o componente `Notas` mostra "Mensagens deste registro" em todas as telas que já têm notas.
- Texto com formatação simples sem HTML (`TextoFormatado`: **negrito**, _itálico_, lista "- "), codificado antes de virar HTML.
- Tempo real no mesmo servidor: `CanalAvisos` (singleton) acende o contador do menu (`IContadorMensagens`: servidor lê direto; WebAssembly usa `/api/telas/mensagens/nao-lidas`) e acorda o envio de e-mail. Reserva: consulta a cada 90 s no expediente.
- Anexos da conversa pelo componente `Anexos` (entidade "Conversa").

## Avisos por e-mail (regras de notificação)
- `AvisosServico` + tabela `AvisosEmail` (fila com chave única por empresa: não repete). A regra (Sistema › Parâmetros e Notificações) decide: ativa + canal E-mail + texto com `{campos}`.
- `RotinaAvisos` (BackgroundService): a cada 15 min no expediente (seg–sáb, 6h–21h) gera lembretes (aprovação pendente 1×/dia, prazo de aprovação estourado, leitura obrigatória nova, tarefa atribuída e atrasada, RNC vencida, restrição vencendo) e envia a fila; mensagem nova acorda na hora. Fora do expediente só envia quando acordada (Azure SQL pausa).
- Falha de envio: nova tentativa com espera crescente (1, 4, 9, 16 min), depois "Falhou". Sem ACS configurado a fila espera.
- Link dos e-mails: `QualyHub:EnderecoSite` ou `https://$WEBSITE_HOSTNAME/` (App Service).
- Carga padrão v4: regras novas (Nova mensagem interna, Tarefa atribuída, Tarefa atrasada, Restrição do planejamento vencendo); empresas existentes recebem só os eventos que faltam.

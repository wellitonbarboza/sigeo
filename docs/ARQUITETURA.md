# SIGEO — Arquitetura (outubro/2026)

## Camadas

```
Sigeo.Web.Client (WebAssembly) ──HTTP /api/telas──┐
        ▲ (layout e componentes compartilhados)      ▼
Sigeo.Web (Blazor Server + API)  ──►  Sigeo.Application (casos de uso)  ──►  Sigeo.Domain (regras puras)
        │                                   ▲      │
        └──────►  Sigeo.Infrastructure ──┘      └──► Sigeo.Contratos (DTOs da API)
```

| Projeto | O que mora aqui | O que NÃO mora aqui |
|---|---|---|
| **Domain** | entidades, value objects, regras puras (travas, rateio, motor do orçamento, gerador de locais) | EF, SQL, Azure |
| **Application** | serviços de caso de uso (`Servicos/`), abstrações (`Abstracoes/`: `ISigeoDb`, `IFabricaDb`, `IFabricaSistema`, `IUsuarioAtual`, `IArmazenamento`), utilitários (`Comum/`), carga padrão (`Carga/`) | provedor de banco, migrations, Blob, planilhas |
| **Infrastructure** | `SigeoDbContext` (implementa `ISigeoDb`), configurações EF, migrations, interceptor de auditoria, `FabricaDb`/`FabricaSistema`, armazenamento (Blob/local), importadores de planilha | regra de negócio |
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
- `tests/Sigeo.Tests`: unitários (domínio e validações dos serviços).
- `tests/Sigeo.Tests.Integracao`: cenários com SQL Server real; rodam quando a variável `QH_TEST_SQL` existe (veja o LEIAME da pasta).

## Área do Fornecedor (`/fornecedor`)
- Entrada pública por **CNPJ/CPF + código de 6 dígitos** enviado a um **contato cadastrado** escolhido pela pessoa (e-mail ou WhatsApp). Código: 10 min, uso único, 5 tentativas, guardado só como hash (`CodigosAcessoFornecedor`); limites por CNPJ (3/h), por IP (10/h) e por requisição (60 por 5 min).
- Sessão em cookie próprio (`sigeo-fornecedor`, 8 h, sem renovação) — separado do login interno; telas internas exigem a claim de empresa. O circuito do Blazor junta as duas identidades e cada tela confere a sua.
- Depois de entrar: escolhe o CNPJ/construtora (o do documento e os outros em que o mesmo e-mail/celular verificado é contato), vê as cotações com situação (não enviada / enviada / encerrada) e preenche ou edita a proposta; aba "Cadastro e documentos".
- Envio por `IEnvioMensagens` (Application) → `EnvioAcs` (Azure Communication Services, identidade gerenciada). Desenvolvimento: `EnvioLocalLog` (código no log). Produção sem configuração: canais indisponíveis. Configuração: `infra/configurar-comunicacao.sh`.
- Links antigos (`/portal/{token}` e `/cotacao/{chave}`) continuam valendo até vencer; a equipe agora usa "Enviar convite"/"Convidar" (contato + canal).

## Notas fiscais de entrada (SIEG / XML)
- `Domain/Fiscal`: `NotaFiscalEntrada` (+ itens), `ImportacaoNota` (registro de cada busca), `ConferenciaNota` (pareamento nota × pedido sem IA).
- `Application/Fiscal`: `LeitorNfe` (XML 4.00 e ZIP; DTD desligado; evento 110111 = cancelamento), `NotasFiscaisServico` (importa sem repetir chave, só para CNPJs da construtora, guarda o XML no armazenamento, liga fornecedor pelo CNPJ e pedido pelo número citado ou único aberto).
- `Infrastructure/Servicos/FonteSieg` (`IFonteNotas`): API nova do SIEG (token com Client ID + Secret, `X-Api-Key`, ZIP de XMLs, 2 meses por chamada, ~2 chamadas/min). Credenciais por construtora no Key Vault: `Sieg--Empresas--{empresaId}--ApiKey|ClientId|SecretKey` (`infra/configurar-sieg.sh`). Endereços configuráveis (`Sieg:UrlBase`, `Sieg:CaminhoToken`, `Sieg:CaminhoBaixar`, `Sieg:TipoXmlNfe`).
- Rotina das 6h30 (`RotinaMatinal`): busca os últimos 5 dias de cada construtora configurada; notas que a regra não ligou passam pela IA só se `IA:AnalisarNotasAutomatico` = true (desligado por padrão desde 14/10: a análise é pelo botão).
- Tela `Suprimentos › Notas fiscais`: importar XML/ZIP, buscar no SIEG, vincular/desvincular pedido, ajustar item, ignorar, baixar XML, gerar recebimento (`recebimentos?nota=`: chave, número e quantidades preenchidos). Registrar o recebimento com a chave tira a nota da fila.
- Leitura da nota no recebimento (`LeituraNotaServico` + componente `LeituraNotaCamera`): foto pela câmera do tablet/celular (`<InputFile capture=environment>`, reduzida no navegador para 2400 px). 1º código de barras do DANFE no servidor (`ILeitorImagemNota` → `LeitorImagemNota`, ZXing + SkiaSharp, sem custo), chave aceita só com DV correto (`Domain/Fiscal/ChaveNfe`). Sem código, botão "Ler com IA": Document Intelligence (texto) e, só se a nota não estiver importada, o modelo lê os itens. Também resolve a chave digitada, colada ou lida por leitor USB/Bluetooth. Nota importada e ligada ao pedido → recebimento inteiro pela nota; senão chave, número, fornecedor e pedido (único aguardando entrega ou lista só dos dele). Avisa nota já recebida (em qualquer obra), cancelada ou fornecedor não cadastrado. A foto vira anexo "DANFE NF n (foto).jpg" do recebimento. Publicação passa a ser `-r linux-x64` (só a biblioteca nativa do Skia para Linux).

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

### Acompanhamento (painel, Gantt por local, medição, programação, plano de ação) e Diário de obra
- **Abas do Planejamento**: Painel · Cronograma (Gantt por local | Editar cronograma por rede | Tabela/Gantt Syncfusion) · Plano de ação · Programação semanal · médio/curto prazo · desembolso · redes · versões · calendário. Linha de balanço retirada.
- `AcompanhamentoServico`: painel por SPE e macroetapa (1º nível da EAP = `Composicao.Grupo`; previsto linear nos dias úteis da base × real), curva física semanal pelas medições; Gantt por local (rede › locais › uma barra por serviço/pacote, ligações); mapa e gravação da medição semanal; programação semanal (local › serviço › avanço previsto de→até › dias seg–sáb; usa o plano semanal quando existe); plano de ação como `Tarefa` (OrigemEntidade = "Cronograma").
- `MedicaoSemanal` (temporal): obra × chave da atividade × semana (única; medir de novo sobrepõe, histórico na tabela temporal). Só Engenharia (e Administrador). Atualiza o avanço na versão vigente e na em elaboração. 100% aceito com FVS do serviço controlado não aprovada → `FvsPendente`.
- Editor por rede: `wwwroot/js/gantt-local.js` (`window.qhGantt.montar`) desenha e devolve a intenção (mover, duração, ligar, alterar/excluir ligação) para `GanttPorLocal.razor` (JSInvokable) → `PlanejamentoServico` (`MoverAsync`, `LigarAsync`, `AlterarVinculoAsync`, `ExcluirVinculoAsync`); o motor do cronograma recalcula (ciclo recusado).
- Impressões estáticas: `/engenharia/planejamento/imprimir-programacao`, `/engenharia/diario/imprimir`.
- **Diário de obra** (Engenharia › Diário de Obra, `/engenharia/diario`): `DiarioObra` (temporal, 1 por obra/dia, numerado) + `DiarioEfetivo` (próprio por função / terceiro por nome, com quantidade) + `DiarioAtividade` (descrição, local opcional; fotos = anexos "DiarioAtividade", categoria "Foto"). `DiarioObraServico`: preenche Engenharia; assina o engenheiro da obra ou de uma SPE dela (sem cadastro, a Engenharia); assinado não muda. Alimenta o painel (dias úteis × trabalhados × chuva, efetivo médio).

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
- Link dos e-mails: `Sigeo:EnderecoSite` ou `https://$WEBSITE_HOSTNAME/` (App Service).
- Carga padrão v4: regras novas (Nova mensagem interna, Tarefa atribuída, Tarefa atrasada, Restrição do planejamento vencendo); empresas existentes recebem só os eventos que faltam.

## Plataforma (construtoras, contas entre construtoras, suporte) e estrutura de locais por construtora
- **Contas**: o cadastro principal (`Usuario` com `Login` e senha) pode ter **vínculos** em outras construtoras: `Usuario` na outra empresa com `ContaId` = principal, `Login` vazio, perfil e obras próprios. O login autentica a conta; `ContaServico.AcessosAsync` lista as construtoras (principal + vínculos ativos, construtora ativa, suporte no prazo). Com mais de uma, `/conta/construtora` escolhe e `POST /conta/construtora/trocar` renova o cookie (claims `qh:conta`, `qh:plataforma`, `qh:construtoras`, `qh:suporte`). A senha é sempre da conta (`/conta/trocar-senha` usa `qh:conta`).
- **Administrador da plataforma**: `Usuario.AdministradorPlataforma` (na conta principal). Quem faz a configuração inicial já é; os demais ativam em `/plataforma/ativar` com o segredo `CodigoPlataforma` (ou `CodigoConfiguracaoInicial`).
- `PlataformaServico` (IFabricaSistema, sem filtro de empresa): construtoras (lista com obras/usuários/último acesso; editar, suspender = `Empresa.Ativa` falsa, ninguém entra), nova construtora (carga padrão, administrador com senha provisória — ou vínculo se o login já existe —, estrutura de locais, primeira obra opcional), contas (dar/retirar acesso por construtora e perfil, bloquear, senha provisória), **suporte** (vínculo Administrador de 4 h com motivo, `SuporteAte`, faixa no topo) e `RegistroPlataforma` (auditoria da plataforma). Telas: Plataforma › Construtoras / Usuários e acessos / Auditoria.
- **Estrutura de locais**: `Empresa.ModeloEstrutura` + `NomesNiveis` → `EstruturaLocais` (Condomínios com SPE, Edifício, Loteamento/casas ou personalizado): nomes dos níveis (dono do CNPJ, bloco/torre/quadra, pavimento/nível, unidade, cômodo) e prefixo da unidade gerada ("Apto 101", "Casa 01"). O gerador e os tipos internos seguem Condomínio › Torre › Pavimento › Apartamento › Cômodo; as telas de Obras e Tipologias mostram os nomes da construtora (`Catalogos.EstruturaAsync`).

## Acesso por tela (módulos da construtora + perfis de acesso)
- **Catálogo único** (`Sigeo.Contratos/Acesso.cs` → `CatalogoTelas`): módulos e telas com as ações que existem em cada uma
  (V Ver, I Incluir/Editar, E Excluir/Cancelar, A Aprovar, X Exportar/Imprimir). Monta o menu, a aba de módulos da plataforma,
  a matriz dos perfis e o bloqueio por endereço. Tela nova entra aqui. Pendências, Mensagens, Tarefas, Painel, Conta e Plataforma ficam fora (livres).
- **Plataforma › Construtoras › editar › Módulos e telas liberados**: `Empresa.TelasBloqueadas` ("rota;rota"). Módulo Sistema é fixo.
  Bloqueio/liberação vai para a auditoria da plataforma.
- **Sistema › Perfis de Acesso** (`/sistema/perfis`, só Administrador altera): `PerfilAcesso` (Regras "rota=VIEX;…"); um padrão por
  `PerfilUsuario` (`Base`, criado na 1ª abertura com `PerfisPadrao`), cópias livres. Aba "Por usuário": `Usuario.PerfilAcessoId`
  (vazio = padrão do perfil dele) + `Usuario.ExcecoesAcesso` ("rota=+A-E"). Usuário em várias construtoras tem um cadastro (e perfil) em cada.
  Administrador = tudo o que foi liberado (fixo, ninguém se tranca para fora).
- **Mapa efetivo**: `AcessoServico.MapaAsync` (cache em memória de 5 min, invalidado a cada mudança → vale sem sair e entrar).
- **Onde é aplicado**: menu e bloqueio "Módulo não contratado"/"Sem permissão" no `MainLayout` (servidor e WebAssembly via `IAcessoTelas`,
  reavaliado a cada navegação); botões escondidos por CSS (`.conteudo[data-negar]` + `data-i` do ícone: mais/editar/upload = I, lixeira = E,
  excel/json/impressora/baixar = X; impressão `/imprimir` exige X); **gravação** pelo `AcessoInterceptor` (tela atual via `IContextoTela`:
  endereço do circuito ou `.Tela("rota")` nos endpoints da API) — gravar exige I, operação só de exclusão/cancelamento exige E;
  **aprovação** no `AprovacaoServico` (A na tela do documento + alçada). Fora de tela (rotinas, portal, login) não há barreira.
- Regras de negócio por função continuam (ex.: assinatura do RDO pelo engenheiro da obra, cronograma por Engenharia/Diretoria, medição semanal pela Engenharia).

## Planejamento em três telas
- **Planejamento** (`engenharia/planejamento`, ações V/X): só indicadores — KPIs, Painel, Gantt por local (só ver) e Desembolso/curva S.
  Abre na versão vigente. Links antigos `?aba=` de edição ou de Last Planner são redirecionados para a tela nova.
- **Cronograma (edição)** (`engenharia/cronograma`): edição do cronograma (Gantt por local, editar redes/ligações, tabela/Gantt detalhado),
  Gerar pelas redes, Importar/Exportar MS Project, Nova reprogramação, Enviar para aprovação, Redes, Versões e aprovação, Calendário.
  `?versao=` abre a obra da versão (links de aprovação/registro).
- **Last Planner** (`engenharia/last-planner`): médio prazo/restrições, curto prazo/PPC, programação semanal, plano de ação e medição semanal.
- Migração `DivisaoTelasPlanejamento`: regras de perfis, exceções por usuário e telas bloqueadas da antiga "Planejamento" copiadas para as duas telas novas.

## Kits de importação (docs/kits)
- **Kit de tipologia** (Engenharia › Tipologias › Importar kit): abas Tipologias, Ambientes, Mix, Servicos, Aplicacao e, opcional,
  **Insumos** (Serviço, Tipo, Descrição, Unidade, Quantidade por unidade do serviço, Perda %, Custo unitário, Produto (código),
  Grupo/Subgrupo do produto). Serviço com insumos na planilha tem os insumos substituídos (reimportar atualiza custos); material sem
  código de produto é procurado por nome + unidade ou criado no cadastro (código padronizado pelo grupo/subgrupo).
- `Kit_Tipologia_Nest_Cancelli_R01.xlsx`: orçamento revisado R01 do Nest Cancelli (128 serviços, 641 insumos); itens "apt × 38" no
  apartamento, demais na torre com quantidade fixa — o orçamento gerado bate com o total do R01 (teste `KitNestCancelliTests`).
- **Cronograma**: XML do MS Project (MSPDI) na versão em elaboração; custo em centavos (padrão MSPDI); feriados do calendário do
  projeto entram no calendário da obra. `Nest_Cancelli_Cronograma_R08_importacao_Sigeo.xml` = baseline R08 adequada.

## Nome do sistema: SIGEO (antes QualyHub, depois Groundkey) — 16/10/2026
- SIGEO = Sistema Integrado de Governança de Empreendimentos e Obras (governança imobiliária 360°: incorporação, projeto, obra, entrega e pós-obra). Marca: anel de 360° aberto no topo (ardósia = incorporação, projeto e obra; cobre = entrega e pós-obra) envolvendo dois volumes sobre a linha de chão; palavra "sigeo" em contorno (Outfit Bold), uma cor só; subtítulo "GOVERNANÇA IMOBILIÁRIA 360°". Paleta Sand & Slate mantida.
- Solução, projetos e namespaces: `Sigeo.*`; executável `Sigeo.Web.dll`.
- Configuração: chaves `Sigeo:*` e conexão `Sigeo`. `ConfiguracaoLegada` copia as antigas (`Groundkey:*` e `QualyHub:*`, conexões e segredos com esses nomes) quando a nova não existe; Groundkey vence QualyHub. Sites já configurados seguem funcionando.
- Endereço: `infra/criar-endereco-sigeo.sh` cria o site `sigeo` no mesmo plano (copia do site em uso — `groundkey` se já existir e não redirecionar, senão `qualyhub` —, renomeia as chaves, permissões, banco, IA, comunicação) e põe `Sigeo__RedirecionarPara` nos sites antigos: respondem 301 para o novo (mantém caminho e parâmetros) e não rodam as rotinas em segundo plano.
- Nomes de recursos do Azure existentes não mudam (grupo `rg-qualyhub-prd`, banco, Key Vault, Storage, grupo `QualyHub-DBA`, papel `qualyhub_app`, modelos de WhatsApp aprovados).
- Cookies passam a `sigeo` e `sigeo-fornecedor`: todos entram de novo uma vez.

## Banco: DTU Standard S1 (16/10/2026)
- Plano padrão do banco passa de vCore serverless (GP_S_Gen5, 0,5–1 vCore, pausa em 60 min) para **DTU Standard S1** (20 DTU, 250 GB incluídos): custo fixo e previsível, sem o atraso de ~1 min para "acordar".
- `infra/main.bicep`: parâmetro `sqlPlano` (S0 | S1 | S2 | Serverless; padrão S1). Ambiente existente: `infra/mudar-plano-banco.sh` (troca online, confirma antes; `PLANO=S0`, `S2` ou `Serverless` para outras opções).
- Nada muda no app nem nos dados: mesma conexão, Managed Identity, backups (14 dias) e colação UTF-8. Não há recursos exclusivos de vCore no esquema (sem columnstore/in-memory).
- Acompanhar "DTU percentage": média acima de ~70% com frequência → S2.

## Limpeza final do que é antigo (16/10/2026)
- `infra/limpar-antigos.sh` (rodar depois de `criar-endereco-sigeo.sh` e do teste de login): confere `/saude` do site `sigeo`, descobre pelo connection string qual banco ele usa e, só no grupo `rg-qualyhub-prd` e item por item com confirmação, apaga sites que não sejam o `sigeo`, bancos que o `sigeo` não usa (restauráveis por 7 dias), servidores SQL e planos que ficaram vazios e permissões das identidades apagadas. O banco em uso e o plano do `sigeo` nunca entram na lista.
- Apagar os sites antigos faz os endereços qualyhub/groundkey pararem de responder (sem redirecionamento).
- Ordem completa em `infra/LEIA-ME-SUBIR.txt`.

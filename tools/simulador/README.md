# Planning Lab — Simulador de Sequência

Página única (`index.html` + `manifest.webmanifest` + `icone.svg`), sem build. Publicada no caminho
escondido do SIGEO (`/x/planninglab`), visível só para o e-mail liberado. Ver `docs/FERRAMENTA-PRIVADA.md`.

## Publicar
```bash
cd tools/simulador && zip -q ../../planninglab_simulador.zip index.html manifest.webmanifest icone.svg
# no Cloud Shell, com o zip e infra/publicar-planning-lab.sh:
bash publicar-planning-lab.sh planninglab_simulador.zip      # mantém /x/planninglab
```
Depois de publicar, recarregar com Cmd+Shift+R.

## Testar localmente
Abrir `index.html` no navegador (sem servidor os estudos ficam no navegador). Com servidor, os estudos vão para
`api/dados/simulador/*.json` (contêiner privado do SIGEO, prefixo `planning-lab-dados/`).

## Modelo de dados (estudo = várias alternativas)
- `estudo = { id, nome, origem?, alts: [Simulacao] }`
- `Simulacao`:
  - `equipes[]`: `{ id, nome, cor, formacao: [{ qtd, funcao }] }`
  - `etapas[]` na ordem de execução: `{ id, nome, unidade: apto|pav|torre, dias, diasT?: {torreId: dias}, equipe, qtd,
    regra: { tipo: apos|junto|torre, pav, espera }, modo: propria|terc, terc (R$ por unidade), cor }`
  - `torres[]`: `{ id, nome, pavimentos[], aptos, inicio, ciclo, lib?: [data por pavimento] }`
  - `sabado`, `compart` (torres dividem equipes), `feriados?[]`, `aPartirDe?` (data de status),
    `fixas?: { "torreId|pav|etapaId": [ini, fim, pct] }` (já iniciadas no cronograma),
    `custo: { base: dia|mes, diasMes, encargos, ociosidade }`

## Partes do código (index.html)
- `calcular(s)`: motor (linha de balanço por pavimento/apartamento/torre, equipes, regra de entrada, fixas)
- `calcCusto(s, r)`: própria (produtivo + dias parados) × terceirizada, por etapa, função e mês
- `lerProject` / `gruposRepetitivos` / `estudoDoProject`: importar .xml do MS Project
- `xmlDaAlternativa`: exportar a alternativa para o MS Project
- `render*`: telas (Prazo e sequência, Custo, comparação)

## Histórico
- 27/10: protótipo → link separado → substitui o Planning Lab no mesmo caminho; importar/exportar .xml do Project
  (QC R34: Torre B 10/05/27 igual ao cronograma; .xml exportado 100% coerente no motor do Planning Lab)
- 28/10: excluir etapas/equipes; custo (unitário + bônus por função), aba Custo, própria × terceirizada
- 03/11: custo retirado; o foco é dimensionar equipes (formação × quantidade) e o prazo; tabela "Equipes e prazo por etapa" com pessoas e dias

## Planejamento completo (estudo tipo `plano`)
Em "Importar do Project (.xml)", o botão **"Criar planejamento completo (todas as atividades)"** cria um estudo com
todas as atividades do cronograma (EAP, durações, datas, % concluído, datas reais, linhas de base 0 e 10, vínculos).
Cada atividade recebe uma especialidade (inferida pelo nome) e um número de equipes; a composição por especialidade
(funções por equipe e equipamentos com h/dia) gera homem-hora, horas-máquina, histograma e recursos por mês.
- Abas: Cronograma (árvore da EAP com filtros, expandir até o nível N, +/− nível, expandir/recolher tudo e edição em lote), Gantt (semana/mês/trimestre, linha de base, data de status, cor por especialidade ou situação, download .svg), Equipes e composições, Recursos por mês, Histograma.
- Exportar para o Project gera tarefas, vínculos, recursos (mão de obra e equipamentos) e atribuições com o trabalho.
- "Baixar planilha (.csv)" tira todas as linhas com a quantificação.
- As datas não são recalculadas: é o cronograma como está, quantificado.
- Versões e reprogramações: a versão 0 é o cronograma do arquivo; cada versão guarda só as alterações
  (% concluído, situação, duração, início/término, datas reais). "Gerar reprogramação" liga o recálculo
  pelos vínculos (FS/SS/FF/SF com lag) a partir da data de status; o que já começou mantém o início real,
  o resto vai para depois da data de status, e "não antecipar o planejado" segura o início do arquivo.
  A LB0 fica como referência. Aba Reprogramações compara as versões e lista as alterações.
- Linha de balanço e quadro pavimento × semana (mesmo gráfico do simulador), montados com os pacotes que se repetem nos pavimentos de cada torre. Edição da estrutura: nome, subir/descer, recuar/avançar nível, mover para outra EAP, incluir atividade ou subatividade, excluir, desfazer.
- Alternativas (estudos): alterações em lote por versão — duração em %, atrasar início, iniciar após,
  remover restrição, alterar/novo/remover vínculo (por EAP), paralisação, incerteza; premissas h/dia e sábado;
  capacidade por especialidade com nivelamento. Caminho crítico e folga (passagem de volta pelos vínculos).
- Reprogramações: quadro comparativo, grandes etapas por alternativa, curva S física (com LB0) e detalhe A × B.
- Riscos: Monte Carlo (triangular por grupo de incerteza; padrão −10/0/+30% por especialidade),
  P10/P50/P80/P90, chance de cumprir a LB0, tornado e índice de criticidade.

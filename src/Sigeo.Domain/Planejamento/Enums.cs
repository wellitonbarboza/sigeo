using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>Clima de um período do dia no diário de obra.</summary>
public enum ClimaDia { Bom = 0, Nublado = 1, ChuvaFraca = 2, ChuvaForte = 3 }

/// <summary>Condição do canteiro no dia.</summary>
public enum CondicaoDia { Praticavel = 0, ParcialmentePraticavel = 1, Impraticavel = 2 }

/// <summary>Se o dia foi trabalhado (base dos dias trabalhados × úteis e dos dias perdidos por chuva).</summary>
public enum DiaTrabalhado { Sim = 0, NaoChuva = 1, NaoFeriadoParalisacao = 2 }

public enum StatusDiario { Rascunho = 0, Assinado = 1 }

/// <summary>Tipo de restrição (o que impede o pacote de começar) — médio prazo / lookahead.</summary>
public enum TipoRestricao { Projeto = 1, Material = 2, MaoDeObra = 3, Equipamento = 4, Contrato = 5, Financeiro = 6, Documentacao = 7, ServicoAnterior = 8, Outro = 9 }

public enum StatusRestricao { Aberta = 0, Removida = 1, Cancelada = 2 }

/// <summary>Situação do plano semanal: aberto (montando/apontando) ou fechado (PPC apurado, não muda mais).</summary>
public enum StatusPlanoSemanal { Aberto = 0, Fechado = 1 }

public enum SituacaoCompromisso { Pendente = 0, Concluido = 1, NaoConcluido = 2 }

/// <summary>Causa de não cumprimento do compromisso semanal (Last Planner).</summary>
public enum CausaNaoCumprimento
{
    Material = 1, MaoDeObra = 2, Projeto = 3, Equipamento = 4, Clima = 5, ServicoAnterior = 6,
    Fornecedor = 7, Planejamento = 8, Qualidade = 9, Seguranca = 10, Outra = 99
}

/// <summary>Tipo do vínculo entre atividades (TI = término→início, II, TT, IT), como no MS Project (FS, SS, FF, SF).</summary>
public enum TipoVinculo { TerminoInicio = 0, InicioInicio = 1, TerminoTermino = 2, InicioTermino = 3 }

/// <summary>Situação da versão do cronograma. Só uma versão por obra fica Vigente (a aprovada mais recente).</summary>
public enum StatusVersao { EmElaboracao = 0, EmAprovacao = 1, Vigente = 2, Substituida = 3 }

/// <summary>Como a rede enxerga os locais: torre por apartamento, estrutura por pavimento, área comum por local.</summary>
public enum TipoRede { PorApartamento = 1, PorPavimento = 2, PorLocal = 3 }

/// <summary>
/// Vínculo entre pacotes de redes diferentes. PorPavimento: o destino no pavimento N espera a origem no pavimento N+deslocamento
/// (ex.: alvenaria do 3º espera a estrutura do 5º). AposFimDaRede: o destino só começa depois do último local da origem.
/// </summary>
public enum ModoVinculoRede { PorPavimento = 1, AposFimDaRede = 2 }

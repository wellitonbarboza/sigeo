using Sigeo.Domain.Common;

namespace Sigeo.Domain.Colaboracao;

public enum SituacaoAviso { Pendente = 0, Enviado = 1, Falhou = 2, Ignorado = 3 }

/// <summary>Papel de quem participa da conversa (quem abriu, para, cópia, ou entrou depois respondendo/sendo incluído).</summary>
public enum PapelParticipante { Remetente = 0, Para = 1, Copia = 2 }

public enum DonoMeta { Setor = 1, Pessoa = 2, Equipe = 3 }

/// <summary>De onde vem o valor realizado da meta (automático) ou lançado à mão com evidência.</summary>
public enum IndicadorMeta
{
    Manual = 0, EconomiaReal = 1, TempoCompra = 2, EntregaNoPrazo = 3, CotacaoCompetitiva = 4,
    PpcLastPlanner = 5, TarefasNoPrazo = 6, AtendimentoNecessidade = 7
}

public enum AceiteTarefa { NaoSeAplica = 0, Aguardando = 1, Aceita = 2, Devolvida = 3 }

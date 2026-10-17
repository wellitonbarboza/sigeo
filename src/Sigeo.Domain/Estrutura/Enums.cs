using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>Quem é dono do custo da obra: uma SPE por condomínio (padrão) ou a própria construtora (obra própria, sem SPE).</summary>
public enum DonoCustoObra { Spe = 1, Construtora = 2 }

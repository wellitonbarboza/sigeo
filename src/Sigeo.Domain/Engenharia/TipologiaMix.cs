using System.Globalization;
using System.Text;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

/// <summary>Mix de apartamentos da torre: na faixa de pavimentos, N apartamentos da tipologia (na ordem).</summary>
public class TipologiaMix : Entidade
{
    public Guid TipologiaId { get; set; }
    public int Ordem { get; set; }
    public int PavimentoDe { get; set; } = 1;
    public int PavimentoAte { get; set; } = 1;
    public Guid ApartamentoTipologiaId { get; set; }
    public int Quantidade { get; set; } = 1;
}

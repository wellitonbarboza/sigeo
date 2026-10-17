using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;

namespace Sigeo.Domain.Incorporacao;

public static class NomesVenda
{
    public static string Bloco(TipoParcelaVenda t) => t switch
    {
        TipoParcelaVenda.Entrada => "Entrada", TipoParcelaVenda.Mensal => "Mensais", TipoParcelaVenda.Intermediaria => "Balões (intermediárias)",
        TipoParcelaVenda.Chaves => "Chaves", TipoParcelaVenda.Financiamento => "Financiamento bancário", TipoParcelaVenda.Permuta => "Permuta", _ => "Avulsa"
    };

    public static string Situacao(SituacaoUnidade s) => s switch
    {
        SituacaoUnidade.Disponivel => "Disponível", SituacaoUnidade.Reservada => "Reservada", SituacaoUnidade.Proposta => "Em proposta",
        SituacaoUnidade.Vendida => "Vendida", SituacaoUnidade.Permuta => "Permuta", _ => "Bloqueada"
    };

    public static string Regime(RegimeTributario r) => r switch
    {
        RegimeTributario.RetGeral => "RET 4%", RegimeTributario.RetSocial => "RET social 1%", RegimeTributario.LucroPresumido => "Lucro presumido", _ => "Lucro real"
    };
}

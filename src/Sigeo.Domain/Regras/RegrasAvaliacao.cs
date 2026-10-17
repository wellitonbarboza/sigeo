using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Qualidade;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Domain.Regras;

/// <summary>Planos de ação automáticos (P.SUP 3.3): nota de medição &lt; 9 e FVM reprovada.</summary>
public static class RegrasAvaliacao
{
    public static PlanoAcaoFornecedor? PlanoPorMedicao(Medicao m, Guid fornecedorId, DateOnly hoje, string numero)
    {
        if (m.NotaFinal is null || m.Faixa == FaixaNota.Nenhuma) return null;
        var piores = new (string Nome, decimal? Nota)[]
        {
            ("cumprimento do escopo", m.NotaEscopo), ("qualidade do material", m.NotaMaterial), ("qualidade do serviço", m.NotaServico),
            ("limpeza e organização", m.NotaLimpeza), ("documentação e segurança do trabalho", m.NotaDocSeg)
        }.Where(c => c.Nota is not null).OrderBy(c => c.Nota).Take(2).Select(c => $"{c.Nome} (nota {c.Nota:0.0})");
        return new PlanoAcaoFornecedor
        {
            EmpresaId = m.EmpresaId, Numero = numero, FornecedorId = fornecedorId, ObraId = m.ObraId,
            Origem = OrigemPlanoAcao.Medicao, OrigemId = m.Id,
            TipoPlano = m.Faixa == FaixaNota.ReuniaoPlanoCompleto ? TipoPlanoAcao.Completo : TipoPlanoAcao.Pontual,
            Problema = $"Medição {m.NumeroMedicao:00} com nota {m.NotaFinal:0.00}: {string.Join("; ", piores)}.",
            Acao = "A definir com o fornecedor.", Responsavel = m.AvaliadorNome ?? "Engenheiro da obra",
            Prazo = hoje.AddDays(m.Faixa == FaixaNota.ReuniaoPlanoCompleto ? 15 : 30)
        };
    }

    public static PlanoAcaoFornecedor? PlanoPorFvm(Recebimento r, DateOnly hoje, string numero) =>
        r.FvmAprovada != false ? null : new PlanoAcaoFornecedor
        {
            EmpresaId = r.EmpresaId, Numero = numero, FornecedorId = r.FornecedorId, ObraId = r.ObraId,
            Origem = OrigemPlanoAcao.Fvm, OrigemId = r.Id, TipoPlano = TipoPlanoAcao.Pontual,
            Problema = $"FVM reprovada no recebimento {r.Numero}: {r.Ocorrencia}", Acao = "A definir com o fornecedor.",
            Responsavel = "Suprimentos", Prazo = hoje.AddDays(30)
        };
}

using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Qualidade;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de medições de contrato de serviço.</summary>
public sealed class MedicoesConsultas(IFabricaDb fabrica)
{
    /// <summary>Parâmetros vigentes (pesos das notas).</summary>
    public async Task<ParametrosVigentes> ParametrosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await ParametrosServico.CarregarAsync(db, ct);
    }

    /// <summary>Lista das medições, mais recentes primeiro, até o limite da grade.</summary>
    public async Task<List<MedicaoLinha>> ListaAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Medicoes.AsNoTracking().OrderByDescending(m => m.CriadoEm).Take(limite)
            .Select(m => new MedicaoLinha(m.Id, m.Contrato.Numero, m.NumeroMedicao, m.Contrato.Fornecedor.RazaoSocial, m.PeriodoInicio, m.PeriodoFim, m.ValorPeriodo, m.NotaFinal, m.Faixa, m.Status, m.CriadoPor)).ToListAsync(ct);
    }

    /// <summary>Contratos ativos para uma nova medição.</summary>
    public async Task<List<Opcao>> ContratosAtivosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Contratos.AsNoTracking().Where(k => k.Status == StatusContrato.Ativo).OrderBy(k => k.Numero)
            .Select(k => new Opcao(k.Id, k.Numero + " – " + k.Fornecedor.RazaoSocial)).ToListAsync(ct);
    }

    /// <summary>Medição aberta com FVS do prestador, nomes dos locais, RNC de FVS, competências e colaboradores bloqueados.</summary>
    public async Task<MedicaoAberta> AbrirAsync(Guid id, DateOnly hoje, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var m = await MedicoesServico.CarregarMedicaoAsync(db, id, ct);
        var fornId = m.Contrato.FornecedorId;
        var fvs = await db.Fvss.AsNoTracking().Include(f => f.Itens).Where(f => f.ObraId == m.ObraId && f.FornecedorId == fornId).ToListAsync(ct);
        var locNomes = new ArvoreLocais(await db.Locais.AsNoTracking().Where(l => l.ObraId == m.ObraId).ToListAsync(ct)).Nomes();
        var rncs = await db.Rncs.AsNoTracking().CountAsync(r => r.FornecedorId == fornId && r.Origem == OrigemRnc.Fvs, ct);
        var comps = await CompetenciasServico.DoContratoAsync(db, m.ContratoId, hoje, m.PeriodoFim, ct);
        var colabBloq = await db.Colaboradores.CountAsync(c => c.FornecedorId == fornId && c.ObraId == m.ObraId && c.Situacao == SituacaoColaborador.Bloqueado && (c.DataSaida == null || c.DataSaida >= m.PeriodoInicio), ct);
        return new(m, fvs, locNomes, rncs, comps, colabBloq);
    }
}

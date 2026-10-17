using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.PortalFornecedor;

// Funcionarios
public sealed partial class PortalServico
{
    /// <summary>
    /// Cadastro do funcionário pelo fornecedor: entra "Aguardando validação" e só vai para a obra depois que a construtora valida.
    /// CPF protegido (só o hash e a máscara ficam gravados). Desligamento tira da lista ativa e da cobrança de documentos.
    /// </summary>
    public async Task<Guid> SalvarColaboradorAsync(string token, ColaboradorForm f, CancellationToken ct = default)
    {
        var s = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Sessão expirada.");
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(f.Nome) || f.Nome.Trim().Length > 150) e.Add("Informe o nome (até 150 caracteres).");
        if (s.Funcoes.All(x => x.Id != f.FuncaoId)) e.Add("Escolha a função.");
        if (f.Admissao == default) e.Add("Informe a data de admissão.");
        if (f.Desligamento is { } dsl && dsl < f.Admissao) e.Add("Desligamento antes da admissão.");
        var contrato = f.ContratoId is { } kid ? s.Contratos.FirstOrDefault(k => k.Id == kid) : s.Contratos.Count == 1 ? s.Contratos[0] : null;
        if (contrato is null) e.Add("Escolha a obra/contrato em que o funcionário vai trabalhar.");
        await using var db = sistema.Criar(s.EmpresaId, "Portal");
        Domain.Sst.Colaborador? c = null;
        if (f.Id is { } id)
        {
            c = await db.Colaboradores.FirstOrDefaultAsync(x => x.Id == id && x.FornecedorId == s.Fornecedor.Id, ct);
            if (c is null) e.Add("Funcionário não pertence a esta empresa.");
        }
        string? hash = null, mascara = null;
        if (!string.IsNullOrWhiteSpace(f.Cpf))
        {
            var d = Conversor.SomenteDigitos(f.Cpf);
            if (!Conversor.CpfValido(d)) e.Add("CPF inválido.");
            else { hash = cpf.Hash(d); mascara = ProtecaoCpf.Mascarar(d); }
        }
        else if (c is null) e.Add("Informe o CPF.");
        if (hash is not null && contrato is not null && await db.Colaboradores.AnyAsync(x => x.CpfHash == hash && x.ObraId == contrato.ObraId && x.Id != (f.Id ?? Guid.Empty), ct))
            e.Add("Já existe funcionário com este CPF nesta obra.");
        ValidacaoException.Se(e);
        if (c is null)
        {
            c = new Domain.Sst.Colaborador
            {
                EmpresaId = s.EmpresaId, Vinculo = VinculoColaborador.Terceiro, FornecedorId = s.Fornecedor.Id, CadastradoPeloFornecedor = true,
                AguardandoValidacao = true, Situacao = SituacaoColaborador.AguardandoValidacao, Pendencias = "Aguardando validação da construtora"
            };
            db.Colaboradores.Add(c);
        }
        var mudouObra = c.ObraId != contrato!.ObraId || c.ContratoId != contrato.Id;
        c.Nome = f.Nome.Trim(); c.FuncaoObraId = f.FuncaoId; c.DataInicio = f.Admissao; c.DataSaida = f.Desligamento;
        c.ObraId = contrato.ObraId; c.ContratoId = contrato.Id;
        if (hash is not null) { mudouObra |= c.CpfHash != hash; c.CpfHash = hash; c.CpfMascarado = mascara!; }
        if (mudouObra && c.CriadoEm != default && !c.AguardandoValidacao)
        {
            c.AguardandoValidacao = true; c.Situacao = SituacaoColaborador.AguardandoValidacao; c.Pendencias = "Dados alterados pelo fornecedor: aguardando nova validação";
        }
        if (c.DataSaida is { } saida && saida <= DateOnly.FromDateTime(relogio.GetLocalNow().Date)) c.Situacao = SituacaoColaborador.DesligadoDaObra;
        await db.SaveChangesAsync(ct);
        return c.Id;
    }
}

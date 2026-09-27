using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Documentos;
using QualyHub.Domain.Estrutura;

namespace QualyHub.Domain.Regras;

/// <summary>Resultado da publicação: o que o sistema gerou/alterou (para log e avisos).</summary>
public sealed record ResultadoPublicacao(DocumentoRevisao? Anterior, IReadOnlyList<CopiaControlada> CopiasParaRecolher, IReadOnlyList<Ciencia> Ciencias);

/// <summary>Regras do controle de documentos que envolvem mais de um registro (publicação, cópias, check-out).</summary>
public static class RegrasDocumentos
{
    /// <summary>
    /// Publica a revisão aprovada: só 1 vigente por documento; a anterior vira OBSOLETA; as cópias em uso dela vão para
    /// "Recolhimento pendente"; gera a leitura obrigatória por função; recalcula a próxima análise crítica.
    /// </summary>
    public static ResultadoPublicacao Publicar(Documento doc, DocumentoRevisao rev, DateOnly vigencia, IEnumerable<Usuario> usuarios,
        ParametrosVigentes p, int? retencaoAnos, Guid usuarioId, string usuarioNome, DateTimeOffset agora, bool podeAprovar)
    {
        if (rev.Status != StatusRevisao.AprovadaAguardandoVigencia) throw new RegraDeNegocioException("Revisão ainda não aprovada.");
        if (string.IsNullOrWhiteSpace(rev.ArquivoPublicadoBlob) || string.IsNullOrWhiteSpace(rev.HashSha256))
            throw new RegraDeNegocioException("Anexe o arquivo publicado (PDF) antes de publicar.");

        rev.Decidir(usuarioId, usuarioNome, DecisaoEtapa.Aprovado, null, agora, podeAprovar);
        rev.Status = StatusRevisao.Vigente;
        rev.DataVigencia = vigencia;

        var anterior = doc.RevisaoVigente;
        var recolher = new List<CopiaControlada>();
        if (anterior is not null && anterior.Id != rev.Id)
        {
            anterior.Status = StatusRevisao.Obsoleta;
            anterior.ObsoletoEm = vigencia;
            anterior.RetidoAte = retencaoAnos is { } anos ? vigencia.AddYears(anos) : null;
            foreach (var c in anterior.Copias.Where(c => c.Status == StatusCopia.EmUso))
            {
                c.Status = StatusCopia.RecolhimentoPendente;
                c.PrazoRecolhimento = vigencia.AddDays(p.Int(Parametros.PrazoRecolhimentoDias));
                recolher.Add(c);
            }
            anterior.RecolhimentoOk = !recolher.Any(c => c.TipoCopia == TipoCopia.FisicaControlada);
        }

        doc.RevisaoVigenteId = rev.Id;
        doc.DataVigencia = vigencia;
        doc.Status = StatusDocumentoSgq.Vigente;
        doc.ProximaAnalise = vigencia.AddMonths(doc.PeriodicidadeMeses > 0 ? doc.PeriodicidadeMeses : p.Int(Parametros.PeriodicidadeAnaliseCriticaMeses));

        var funcoes = Separar(doc.LeituraObrigatoria);
        var ciencias = usuarios.Where(u => u.Ativo && !u.Excluido && u.Funcao is not null && funcoes.Contains(u.Funcao, StringComparer.OrdinalIgnoreCase))
            .Select(u => new Ciencia
            {
                EmpresaId = doc.EmpresaId, DocumentoRevisaoId = rev.Id, Revisao = rev, UsuarioId = u.Id, UsuarioNome = u.Nome, Funcao = u.Funcao,
                Prazo = vigencia.AddDays(p.Int(Parametros.PrazoLeituraDias))
            }).ToList();

        return new ResultadoPublicacao(anterior, recolher, ciencias);
    }

    /// <summary>Liberação da solicitação de cópia: só revisões vigentes; gera cópias numeradas &lt;CÓDIGO&gt;-R&lt;nn&gt;-C&lt;nnn&gt;.</summary>
    public static IReadOnlyList<CopiaControlada> LiberarSolicitacao(SolicitacaoCopia s, Func<string, int> proximoNumero, string liberadoPor, DateTimeOffset agora)
    {
        if (s.Status != StatusSolicitacaoCopia.Solicitada) throw new RegraDeNegocioException("Solicitação não está pendente.");
        var copias = new List<CopiaControlada>();
        foreach (var i in s.Itens)
        {
            if (i.Revisao.Status is not (StatusRevisao.Vigente or StatusRevisao.VigenteComRessalvas))
                throw new RegraDeNegocioException($"{i.Revisao.Documento.Codigo} {i.Revisao.Rotulo} não é a revisão vigente.");
            for (var n = 0; n < i.Quantidade; n++)
            {
                var chave = $"Copia-{i.Revisao.Documento.Codigo}-R{i.Revisao.Revisao:00}";
                copias.Add(new CopiaControlada
                {
                    EmpresaId = s.EmpresaId, DocumentoRevisaoId = i.DocumentoRevisaoId, Revisao = i.Revisao,
                    NumeroCopia = CopiaControlada.Numerar(i.Revisao.Documento.Codigo, i.Revisao.Revisao, proximoNumero(chave)),
                    TipoCopia = TipoCopia.FisicaControlada, Destinatario = i.Destinatario, ObraId = s.ObraId, SolicitacaoCopiaId = s.Id
                });
            }
        }
        s.Status = StatusSolicitacaoCopia.Liberada;
        s.LiberadoPor = liberadoPor;
        s.LiberadoEm = agora;
        return copias;
    }

    /// <summary>Check-out exclusivo: impede edição simultânea da mesma revisão.</summary>
    public static CheckoutDocumento Checkout(DocumentoRevisao rev, IEnumerable<CheckoutDocumento> existentes, Guid usuarioId, string nome, ParametrosVigentes p, DateTimeOffset agora)
    {
        if (rev.Status is not (StatusRevisao.EmElaboracao or StatusRevisao.DevolvidaAjuste))
            throw new RegraDeNegocioException("Só é possível editar revisão em elaboração ou devolvida para ajuste.");
        var ativo = existentes.FirstOrDefault(c => c.DocumentoRevisaoId == rev.Id && c.Ativo && !c.Excluido);
        if (ativo is not null && ativo.UsuarioId != usuarioId)
            throw new RegraDeNegocioException($"Documento em edição por {ativo.UsuarioNome} desde {ativo.CheckoutEm:dd/MM HH:mm}.");
        return ativo ?? new CheckoutDocumento
        {
            EmpresaId = rev.EmpresaId, DocumentoRevisaoId = rev.Id, UsuarioId = usuarioId, UsuarioNome = nome, CheckoutEm = agora,
            Prazo = DateOnly.FromDateTime(agora.Date).AddDays(p.Int(Parametros.PrazoCheckoutDias))
        };
    }

    /// <summary>Permissão efetiva: regra do documento sobrepõe a da pasta; usuário sobrepõe perfil.</summary>
    public static PermissaoDocumento? Efetiva(IEnumerable<PermissaoDocumento> regras, Guid pastaId, Guid documentoId, Guid usuarioId, PerfilUsuario perfil, DateOnly hoje)
    {
        var validas = regras.Where(r => r.Vigente(hoje)).ToList();
        PermissaoDocumento? Buscar(EscopoPermissao e, Guid alvo) =>
            validas.FirstOrDefault(r => r.Escopo == e && r.AlvoId == alvo && r.UsuarioId == usuarioId)
            ?? validas.FirstOrDefault(r => r.Escopo == e && r.AlvoId == alvo && r.UsuarioId is null && r.Perfil == perfil);
        return Buscar(EscopoPermissao.Documento, documentoId) ?? Buscar(EscopoPermissao.Pasta, pastaId);
    }

    public static IReadOnlyList<string> Separar(string? lista) =>
        (lista ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

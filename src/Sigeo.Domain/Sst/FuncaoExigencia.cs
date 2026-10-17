using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

/// <summary>S03A · Matriz função × exigência (O = obrigatório, C = conforme a atividade).</summary>
public class FuncaoExigencia : Entidade
{
    public Guid FuncaoObraId { get; set; }
    public Guid ExigenciaId { get; set; }
    public Exigencia? Exigencia { get; set; }
    public Obrigatoriedade Obrigatoriedade { get; set; } = Obrigatoriedade.Obrigatorio;
}

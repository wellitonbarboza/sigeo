using Sigeo.Domain.Common;

namespace Sigeo.Domain.Aprovacoes;

public class RegraDeNegocioException(string mensagem) : Exception(mensagem);

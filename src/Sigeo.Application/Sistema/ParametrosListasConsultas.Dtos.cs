using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Sistema;

public sealed record ParametrosListasDados(List<Parametro> Parametros, List<ItemLista> Listas, List<TipoDocumentoExigido> TiposDocumento,
    List<RegraNotificacao> RegrasNotificacao, List<AvisoEmail> Avisos);

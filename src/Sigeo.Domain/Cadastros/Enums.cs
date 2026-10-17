using System.Security.Cryptography;
using System.Text;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Cadastros;

public enum CanalCodigo { Email = 1, WhatsApp = 2 }

/// <summary>Situação do insumo frente ao catálogo padronizado (grupos e subgrupos ativos em Categorias de Produto).</summary>
public enum SituacaoCadastroProduto
{
    /// <summary>Grupo e subgrupo existem no catálogo ativo.</summary>
    Padronizado = 1,
    /// <summary>Classificado num grupo/subgrupo que não está mais no catálogo (cadastro antigo).</summary>
    ForaDoCatalogo = 2,
    /// <summary>Grupo "A Cadastrar" ou vazio.</summary>
    AClassificar = 3,
}

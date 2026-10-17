using System.Globalization;
using System.Text;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

/// <summary>Uma torre a gerar no condomínio: nome e tipologia de torre (com mix e áreas comuns carregados).</summary>
public sealed record TorreGerar(string Nome, Tipologia Tipologia);

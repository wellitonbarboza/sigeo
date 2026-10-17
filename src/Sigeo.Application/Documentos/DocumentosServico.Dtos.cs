using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;
using Sigeo.Domain.Regras;

namespace Sigeo.Application.Documentos;

/// <summary>Permissão efetiva do usuário logado sobre um documento (D07). Sem regra: Diretoria/Qualidade/Admin tudo; demais só ver vigentes.</summary>
public sealed record PermissaoEfetiva(bool Ver, bool Baixar, bool Imprimir, bool Editar, bool Aprovar, bool VerObsoletos);

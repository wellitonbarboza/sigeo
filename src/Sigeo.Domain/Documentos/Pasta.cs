using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>Pasta do SGQ (Processo/Área): Diretoria, Manual/Política/Objetivos, Engenharia (ITS), Suprimentos, Qualidade, RH, Obra…</summary>
public class Pasta : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    public Guid? PastaPaiId { get; set; }
    public int Ordem { get; set; }
}

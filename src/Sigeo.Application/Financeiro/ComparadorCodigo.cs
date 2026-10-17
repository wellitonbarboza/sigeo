using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Financeiro;

/// <summary>Ordena códigos hierárquicos numericamente ("2.10" depois de "2.9").</summary>
public sealed class ComparadorCodigo : IComparer<string>
{
    public static readonly ComparadorCodigo Instancia = new();
    public int Compare(string? x, string? y)
    {
        var a = (x ?? "").Split('.'); var b = (y ?? "").Split('.');
        for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            var c = int.TryParse(a[i], out var ia) && int.TryParse(b[i], out var ib) ? ia.CompareTo(ib) : string.CompareOrdinal(a[i], b[i]);
            if (c != 0) return c;
        }
        return a.Length.CompareTo(b.Length);
    }
}

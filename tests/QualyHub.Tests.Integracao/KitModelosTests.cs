using QualyHub.Domain.Common;
using QualyHub.Domain.Qualidade;
using QualyHub.Infrastructure.Importacao;

namespace QualyHub.Tests.Integracao;

/// <summary>Modelos de FVS/FVM (docs/kits): importa, publica, liga ITS/material e não duplica ao importar de novo.</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class KitModelosTests(BancoFixture f)
{
    private static string Arquivo()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "QualyHub.slnx"))) d = d.Parent;
        return Path.Combine(d!.FullName, "docs", "kits", "Modelos_FVS_FVM_Ecoparque.xlsx");
    }

    [FatoComBanco]
    public async Task Modelos_FVS_e_FVM_entram_vigentes_ligados_a_ITS_e_material()
    {
        KitModelos kit;
        await using (var s = File.OpenRead(Arquivo())) kit = LeitorKitModelos.Ler(s);
        Assert.Equal(58, kit.Modelos.Count(m => m.Tipo == TipoModelo.FVS));
        Assert.Equal(40, kit.Modelos.Count(m => m.Tipo == TipoModelo.FVM));

        var servico = new KitModelosServico(f.Fabrica, new QualidadeObraServico(f.Fabrica, f.Usuario, f.Relogio));
        var previa = await servico.AnalisarAsync(kit);
        Assert.Equal(98, previa.Novos + previa.Atualizados);
        await servico.AplicarAsync(kit);
        var segunda = await servico.AplicarAsync(kit);
        Assert.Equal(0, segunda.Novos);
        Assert.Empty(segunda.ItsCriadas);
        Assert.Empty(segunda.MateriaisCriados);

        await using var db = f.Fabrica.CriarLeitura();
        var ms = await db.ModelosInspecao.Include(m => m.Itens).Include(m => m.Ensaios).Where(m => m.Codigo.StartsWith("FVS ") || m.Codigo.StartsWith("FVM ")).ToListAsync();
        Assert.Equal(98, ms.Count);
        Assert.All(ms, m => Assert.Equal(SituacaoModelo.Vigente, m.Situacao));
        Assert.Equal(kit.Itens.Count, ms.Sum(m => m.Itens.Count));
        // Várias FVS vigentes para a mesma ITS (03.03: painéis, solda e vigas).
        var its0303 = await db.ServicosControlados.Where(s => s.Codigo == "ITS 03.03").Select(s => s.Id).SingleAsync();
        Assert.Equal(3, ms.Count(m => m.ServicoControladoId == its0303));
        Assert.Equal("Montagem da estrutura pré-fabricada – Painéis e vigas", await db.ServicosControlados.Where(s => s.Id == its0303).Select(s => s.Nome).SingleAsync());
        var concreto = ms.Single(m => m.Codigo == "FVM 23");
        Assert.NotNull(concreto.MaterialControladoId);
        Assert.Equal(2, concreto.Ensaios.Count);
        Assert.False(string.IsNullOrWhiteSpace(ms.Single(m => m.Codigo == "FVS 05.03").Orientacoes));
    }
}

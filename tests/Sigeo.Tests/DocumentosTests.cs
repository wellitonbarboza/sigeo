using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Regras;

namespace Sigeo.Tests;

/// <summary>Controle de documentos do SGQ (D01–D13). Dados fictícios.</summary>
public class DocumentosTests
{
    private static readonly DateTimeOffset Agora = new(2026, 9, 26, 9, 0, 0, TimeSpan.FromHours(-3));
    private static readonly Guid Elaborador = Guid.NewGuid();
    private static readonly Guid Analista = Guid.NewGuid();
    private static readonly Guid Aprovador = Guid.NewGuid();

    private static Documento Doc() => new()
    {
        Codigo = "P.SUP.01", Titulo = "Suprimentos", Tipo = "P", PeriodicidadeMeses = 12, LeituraObrigatoria = "Comprador; Engenheiro da Obra"
    };

    private static DocumentoRevisao Aprovada(Documento d)
    {
        var r = d.NovaRevisao(MotivoRevisao.EmissaoInicial, null, Elaborador, "Elaborador", Agora);
        r.ArquivoEditavelBlob = "ed.docx";
        r.Decidir(Elaborador, "Elaborador", DecisaoEtapa.Aprovado, null, Agora, false);
        r.Decidir(Analista, "Analista", DecisaoEtapa.Aprovado, null, Agora, false);
        r.Decidir(Aprovador, "Diretor", DecisaoEtapa.Aprovado, null, Agora, true);
        r.ArquivoPublicadoBlob = "pub.pdf"; r.HashSha256 = new string('a', 64);
        return r;
    }

    [Fact]
    public void Revisao_segue_as_etapas_em_ordem_e_quem_elaborou_nao_aprova()
    {
        var d = Doc();
        var r = d.NovaRevisao(MotivoRevisao.EmissaoInicial, null, Elaborador, "Elaborador", Agora);
        Assert.Equal("R00", r.Rotulo);
        Assert.Equal(EtapaRevisaoTipo.Elaboracao, r.EtapaAtual!.Etapa);

        Assert.Throws<RegraDeNegocioException>(() => r.Decidir(Elaborador, "Elaborador", DecisaoEtapa.Aprovado, null, Agora, false)); // sem arquivo editável
        r.ArquivoEditavelBlob = "ed.docx";
        r.Decidir(Elaborador, "Elaborador", DecisaoEtapa.Aprovado, null, Agora, false);
        r.Decidir(Analista, "Analista", DecisaoEtapa.Aprovado, null, Agora, false);
        Assert.Equal(StatusRevisao.AguardandoAprovacao, r.Status);

        var ex = Assert.Throws<RegraDeNegocioException>(() => r.Decidir(Elaborador, "Elaborador", DecisaoEtapa.Aprovado, null, Agora, true));
        Assert.Contains("Segregação", ex.Message);
        r.Decidir(Aprovador, "Diretor", DecisaoEtapa.Aprovado, null, Agora, true);
        Assert.Equal(StatusRevisao.AprovadaAguardandoVigencia, r.Status);
        Assert.NotNull(r.Etapas.First(e => e.Etapa == EtapaRevisaoTipo.Aprovacao).Assinatura);
    }

    [Fact]
    public void Devolucao_exige_comentario_reabre_o_ciclo_e_guarda_historico()
    {
        var d = Doc();
        var r = d.NovaRevisao(MotivoRevisao.EmissaoInicial, null, Elaborador, "Elaborador", Agora);
        r.ArquivoEditavelBlob = "ed.docx";
        r.Decidir(Elaborador, "Elaborador", DecisaoEtapa.Aprovado, null, Agora, false);
        Assert.Throws<RegraDeNegocioException>(() => r.Decidir(Analista, "Analista", DecisaoEtapa.Devolvido, "", Agora, false));
        r.Decidir(Analista, "Analista", DecisaoEtapa.Devolvido, "Ajustar item 3.2", Agora, false);

        Assert.Equal(StatusRevisao.DevolvidaAjuste, r.Status);
        Assert.Equal(EtapaRevisaoTipo.Elaboracao, r.EtapaAtual!.Etapa);
        Assert.Contains(r.Etapas, e => e.Historico && e.Comentario == "Ajustar item 3.2");
    }

    [Fact]
    public void Publicar_torna_vigente_obsoleta_a_anterior_manda_copias_para_recolhimento_e_gera_leituras()
    {
        var d = Doc();
        var r0 = Aprovada(d);
        var usuarios = new[]
        {
            new Usuario { Nome = "Comprador A", Funcao = "Comprador" },
            new Usuario { Nome = "Eng. B", Funcao = "Engenheiro da Obra" },
            new Usuario { Nome = "Financeiro C", Funcao = "Financeiro" },
        };
        var p0 = RegrasDocumentos.Publicar(d, r0, new DateOnly(2026, 6, 30), usuarios, Fake.P, 5, Aprovador, "Diretor", Agora, true);
        Assert.Equal(StatusDocumentoSgq.Vigente, d.Status);
        Assert.Equal(new DateOnly(2027, 6, 30), d.ProximaAnalise);
        Assert.Equal(2, p0.Ciencias.Count);
        r0.Copias.Add(new CopiaControlada { NumeroCopia = CopiaControlada.Numerar(d.Codigo, 0, 1), TipoCopia = TipoCopia.FisicaControlada, Revisao = r0 });

        Assert.Throws<RegraDeNegocioException>(() => d.NovaRevisao(MotivoRevisao.Melhoria, "", Elaborador, "Elaborador", Agora)); // R01 exige descrição
        var r1 = d.NovaRevisao(MotivoRevisao.Melhoria, "Inclui regra de 3 orçamentos", Elaborador, "Elaborador", Agora);
        Assert.Equal(StatusDocumentoSgq.EmRevisao, d.Status);
        Assert.Equal(r0.Id, d.RevisaoVigenteId);    // a vigente continua valendo até publicar

        r1.ArquivoEditavelBlob = "ed1.docx";
        r1.Decidir(Elaborador, "Elaborador", DecisaoEtapa.Aprovado, null, Agora, false);
        r1.Decidir(Analista, "Analista", DecisaoEtapa.Aprovado, null, Agora, false);
        r1.Decidir(Aprovador, "Diretor", DecisaoEtapa.Aprovado, null, Agora, true);
        r1.ArquivoPublicadoBlob = "p1.pdf"; r1.HashSha256 = new string('b', 64);
        var p1 = RegrasDocumentos.Publicar(d, r1, new DateOnly(2026, 9, 26), usuarios, Fake.P, 5, Aprovador, "Diretor", Agora, true);

        Assert.Equal(StatusRevisao.Obsoleta, r0.Status);
        Assert.Equal(new DateOnly(2031, 9, 26), r0.RetidoAte);
        Assert.Single(p1.CopiasParaRecolher);
        Assert.Equal(StatusCopia.RecolhimentoPendente, r0.Copias.Single().Status);
        Assert.False(r0.RecolhimentoOk);
        Assert.Equal("P.SUP.01-R00-C001", r0.Copias.Single().NumeroCopia);
        Assert.Equal(r1.Id, d.RevisaoVigenteId);
    }

    [Fact]
    public void Solicitacao_de_copia_so_libera_revisao_vigente_e_numera_as_copias()
    {
        var d = Doc();
        var r = Aprovada(d);
        var s = new SolicitacaoCopia { Copiadora = "Qualidade", Programacao = new DateOnly(2026, 9, 30) };
        s.Itens.Add(new SolicitacaoCopiaItem { Revisao = r, DocumentoRevisaoId = r.Id, Quantidade = 2, Destinatario = "Almoxarifado" });
        var seq = 0;
        Assert.Throws<RegraDeNegocioException>(() => RegrasDocumentos.LiberarSolicitacao(s, _ => ++seq, "Qualidade", Agora));

        RegrasDocumentos.Publicar(d, r, new DateOnly(2026, 9, 26), [], Fake.P, null, Aprovador, "Diretor", Agora, true);
        var copias = RegrasDocumentos.LiberarSolicitacao(s, _ => ++seq, "Qualidade", Agora);
        Assert.Equal(["P.SUP.01-R00-C001", "P.SUP.01-R00-C002"], copias.Select(c => c.NumeroCopia));
        Assert.Equal(StatusSolicitacaoCopia.Liberada, s.Status);
    }

    [Fact]
    public void Checkout_e_exclusivo_por_revisao()
    {
        var d = Doc();
        var r = d.NovaRevisao(MotivoRevisao.EmissaoInicial, null, Elaborador, "Elaborador", Agora);
        var c1 = RegrasDocumentos.Checkout(r, [], Elaborador, "Elaborador", Fake.P, Agora);
        Assert.Equal(new DateOnly(2026, 10, 1), c1.Prazo);
        var ex = Assert.Throws<RegraDeNegocioException>(() => RegrasDocumentos.Checkout(r, [c1], Analista, "Analista", Fake.P, Agora));
        Assert.Contains("em edição por Elaborador", ex.Message);
    }

    [Fact]
    public void Permissao_do_documento_sobrepoe_a_da_pasta()
    {
        var pasta = Guid.NewGuid(); var doc = Guid.NewGuid(); var u = Guid.NewGuid();
        var regras = new[]
        {
            new PermissaoDocumento { Escopo = EscopoPermissao.Pasta, AlvoId = pasta, Perfil = PerfilUsuario.Engenharia, PodeBaixar = true },
            new PermissaoDocumento { Escopo = EscopoPermissao.Documento, AlvoId = doc, Perfil = PerfilUsuario.Engenharia, PodeBaixar = false },
        };
        Assert.False(RegrasDocumentos.Efetiva(regras, pasta, doc, u, PerfilUsuario.Engenharia, Fake.Hoje)!.PodeBaixar);
        Assert.True(RegrasDocumentos.Efetiva(regras, pasta, Guid.NewGuid(), u, PerfilUsuario.Engenharia, Fake.Hoje)!.PodeBaixar);
    }

    [Fact]
    public void Nome_de_arquivo_gerado_pelo_sistema()
    {
        Assert.Equal("DT.SUP.01.01 - Tabela de Especificação R01.pdf", Documento.NomeArquivo("DT.SUP.01.01", "Tabela de Especificação", 1, "pdf"));
        var c = new Ciencia { Prazo = Fake.Hoje.AddDays(-1) };
        Assert.Equal(StatusCiencia.Atrasado, c.Status(Fake.Hoje));
    }
}

using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Engenharia;

/// <summary>Leitura da tela de mão de obra; <see cref="ComposicoesUsadas"/> são as composições com quantidade na obra.</summary>
public sealed record MaoObraDados(
    List<Equipe> Equipes, List<Opcao> Colaboradores, List<CustoMaoObraMes> Custos, List<Apropriacao> Apropriacoes,
    List<Local> Locais, List<Guid> ComposicoesUsadas, Dictionary<Guid, Composicao> Composicoes);

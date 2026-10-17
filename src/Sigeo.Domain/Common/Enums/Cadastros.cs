namespace Sigeo.Domain.Common;

public enum StatusDocumento { PendenteEnvio = 0, Valido = 1, AVencer = 2, Vencido = 3, Reprovado = 4, EmValidacao = 5 }

/// <summary>Status de qualificação exibido no cadastro (T08), derivado da última qualificação (T09).</summary>
public enum StatusQualificacao { EmQualificacao = 1, Qualificado = 2, Vencida = 3, NaoQualificado = 4, Desqualificado = 5 }

[Flags]
public enum TipoFornecimento
{
    Nenhum = 0, Material = 1, ServicoObra = 2, Laboratorio = 4, ServicoEspecializadoEngenharia = 8, Consultoria = 16, Locacao = 32
}

public enum ResultadoQualificacao { EmQualificacao = 1, Qualificado = 2, NaoQualificado = 3 }

public enum SituacaoPsq { NaoAplicavel = 0, Qualificado = 1, NaoConforme = 2, NaoEscopoPsq = 3 }

/// <summary>Enquadramento de laboratório (SiAC Anexo 7): a) acreditado CGCRE … f).</summary>
public enum EnquadramentoLaboratorio { NaoSeAplica = 0, A_Acreditado = 1, B_EmAcreditacao = 2, C_Avaliado = 3, D_InstituicaoPesquisa = 4, E_Outro = 5, F_Outro = 6 }

public enum AplicaA { Fornecedor = 1, Funcionario = 2 }

public enum FrequenciaDocumento { NoCadastro = 1, Validade = 2, Mensal = 3, PorContrato = 4, PorMedicao = 5, PorEntrega = 6, Anual = 7 }

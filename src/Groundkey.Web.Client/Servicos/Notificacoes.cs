namespace Groundkey.Web.Servicos;

/// <summary>Mensagens (toast) exibidas no canto da tela.</summary>
public sealed class Notificacoes
{
    public event Action? Mudou;
    public List<(Guid Id, string Texto, string Tipo)> Itens { get; } = [];

    public void Sucesso(string texto) => Add(texto, "ok");
    public void Erro(string texto) => Add(texto, "erro");
    public void Aviso(string texto) => Add(texto, "aviso");

    private void Add(string t, string tipo)
    {
        var id = Guid.NewGuid();
        Itens.Add((id, t, tipo));
        Mudou?.Invoke();
        _ = Task.Delay(tipo == "erro" ? 9000 : 4500).ContinueWith(_ => { Itens.RemoveAll(i => i.Id == id); Mudou?.Invoke(); });
    }
}

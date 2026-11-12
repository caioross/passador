using System.Text.Json.Serialization;

namespace Batuta.Core;

public enum TipoAcao
{
    /// <summary>Manda a tecla que o botão manda de fábrica (como se o Batuta não existisse).</summary>
    Original,
    /// <summary>Engole o botão: não acontece nada.</summary>
    Nada,
    /// <summary>Uma tecla ou atalho ("Ctrl+Shift+F5").</summary>
    Tecla,
    /// <summary>Digita um texto.</summary>
    Texto,
    /// <summary>Uma sequência de passos (macro): atalhos, textos, esperas, abrir.</summary>
    Sequencia,
    /// <summary>Abre um programa, arquivo ou endereço.</summary>
    Abrir,
    /// <summary>Clique ou rolagem do mouse.</summary>
    Mouse,
}

public enum TipoPasso { Tecla, Texto, Espera, Abrir, Mouse }

public sealed class Passo
{
    public TipoPasso Tipo { get; set; }
    /// <summary>Tecla: o atalho; Texto: o texto; Espera: milissegundos; Abrir: o caminho; Mouse: o gesto.</summary>
    public string Valor { get; set; } = "";

    public string Descrever() => Tipo switch
    {
        TipoPasso.Tecla => Atalho.Ler(Valor)?.Rotulo() ?? Valor,
        TipoPasso.Texto => $"digitar “{Curto(Valor)}”",
        TipoPasso.Espera => $"esperar {Valor} ms",
        TipoPasso.Abrir => "abrir " + (Path.GetFileName(Valor.TrimEnd('\\', '/')) is { Length: > 0 } n ? n : Valor),
        TipoPasso.Mouse => Acao.RotuloMouse(Valor),
        _ => Valor,
    };

    static string Curto(string s) => s.Length > 24 ? s[..23] + "…" : s.Replace("\n", " ");
}

public sealed class Acao
{
    public TipoAcao Tipo { get; set; }
    public string? Tecla { get; set; }
    public string? Texto { get; set; }
    public string? Caminho { get; set; }
    public string? Argumentos { get; set; }
    public string? Mouse { get; set; }
    public List<Passo>? Passos { get; set; }

    public static Acao DeTecla(string atalho) => new() { Tipo = TipoAcao.Tecla, Tecla = atalho };
    public static Acao Original() => new() { Tipo = TipoAcao.Original };

    public static readonly IReadOnlyDictionary<string, string> GestosDeMouse = new Dictionary<string, string>
    {
        ["clique-esquerdo"] = "Clique esquerdo",
        ["clique-direito"] = "Clique direito",
        ["clique-meio"] = "Clique do meio",
        ["rolar-cima"] = "Rolar para cima",
        ["rolar-baixo"] = "Rolar para baixo",
    };

    public static string RotuloMouse(string? g) => g is not null && GestosDeMouse.TryGetValue(g, out var r) ? r : g ?? "";

    [JsonIgnore]
    public string Descricao => Tipo switch
    {
        TipoAcao.Original => "Tecla de fábrica",
        TipoAcao.Nada => "Não faz nada",
        TipoAcao.Tecla => Atalho.Ler(Tecla)?.Rotulo() ?? "(tecla vazia)",
        TipoAcao.Texto => $"Digita “{(Texto ?? "").Replace("\n", " ")}”",
        TipoAcao.Sequencia => Passos is { Count: > 0 } p
            ? string.Join(", depois ", p.Take(3).Select(x => x.Descrever())) + (p.Count > 3 ? $" (+{p.Count - 3})" : "")
            : "Sequência vazia",
        TipoAcao.Abrir => "Abre " + (Path.GetFileName((Caminho ?? "").TrimEnd('\\', '/')) is { Length: > 0 } n ? n : Caminho),
        TipoAcao.Mouse => RotuloMouse(Mouse),
        _ => "",
    };
}

public enum ModoSegurar
{
    /// <summary>Faz a ação uma vez quando o tempo de segurar passa.</summary>
    UmaVez,
    /// <summary>Repete a ação enquanto o botão estiver apertado.</summary>
    Repetir,
    /// <summary>Aperta a tecla da ação e só solta quando o botão for solto (ex.: falar no microfone).</summary>
    Enquanto,
}

/// <summary>O que um botão faz num perfil: um toque, dois toques, segurar.</summary>
public sealed class MapaBotao
{
    public Acao? Toque { get; set; }
    public Acao? Duplo { get; set; }
    public Acao? Segurar { get; set; }
    public ModoSegurar ModoSegurar { get; set; } = ModoSegurar.UmaVez;
    public int RepetirMs { get; set; } = 120;

    [JsonIgnore] public bool Vazio => Toque is null && Duplo is null && Segurar is null;
    [JsonIgnore] public bool TemDuplo => Duplo is not null;
    [JsonIgnore] public bool TemSegurar => Segurar is not null;

    public MapaBotao Clonar() => System.Text.Json.JsonSerializer.Deserialize<MapaBotao>(
        System.Text.Json.JsonSerializer.Serialize(this, Catalogo.Json), Catalogo.Json)!;
}

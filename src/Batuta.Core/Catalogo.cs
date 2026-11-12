using System.Text.Json;
using System.Text.Json.Serialization;

namespace Batuta.Core;

/// <summary>Identificação USB de um aparelho. PID nulo = qualquer produto daquele fabricante (palpite mais fraco).</summary>
public sealed class IdUsb
{
    public string Vid { get; set; } = "";
    public string? Pid { get; set; }
    public string? Nota { get; set; }

    public bool Casa(string vid, string pid) =>
        string.Equals(Vid, vid, StringComparison.OrdinalIgnoreCase) &&
        (Pid is null || string.Equals(Pid, pid, StringComparison.OrdinalIgnoreCase));

    public override string ToString() => Pid is null ? $"{Vid}:*" : $"{Vid}:{Pid}";
}

/// <summary>Um botão físico do passador e o que ele manda de fábrica.</summary>
public sealed class Botao
{
    public string Id { get; set; } = "";
    public string Nome { get; set; } = "";
    /// <summary>O papel do botão (avancar, voltar, iniciar, tela-preta, laser...): os perfis prontos mapeiam por papel.</summary>
    public string Papel { get; set; } = "";
    /// <summary>
    /// O que ele manda: uma ou mais teclas ("PageDown"; "F5" e "Esc" quando o aparelho alterna a cada toque).
    /// Vazio = o botão não chega ao PC (laser, liga/desliga).
    /// </summary>
    public List<string> Envia { get; set; } = [];
    /// <summary>O que acontece ao segurar, de fábrica ("repete", "nada", ou outra tecla).</summary>
    public string? Segurar { get; set; }
    /// <summary>Botão que só existe no aparelho (laser) ou que o Windows vê como mouse: mostrado, mas não configurável.</summary>
    public string? SoHardware { get; set; }

    [JsonIgnore] public bool Configuravel => SoHardware is null && Envia.Count > 0;
    [JsonIgnore] public IEnumerable<Atalho> Atalhos => Envia.Select(Atalho.Ler).Where(a => a is not null).Select(a => a!.Value);
}

public sealed class Modelo
{
    public string Id { get; set; } = "";
    public string Marca { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Desenho { get; set; } = "generico";
    public string Conexao { get; set; } = "";
    public List<IdUsb> Ids { get; set; } = [];
    public List<Botao> Botoes { get; set; } = [];
    /// <summary>conferido (fonte técnica), comunidade (relatos), estimado (pelo formato e por clones), aprendido (deste PC).</summary>
    public string Confianca { get; set; } = "estimado";
    public string? Notas { get; set; }
    public List<string> Fontes { get; set; } = [];
    /// <summary>A que foto de referência o desenho corresponde (docs/referencias).</summary>
    public string? Referencia { get; set; }

    [JsonIgnore] public string NomeCompleto => string.IsNullOrEmpty(Marca) ? Nome : $"{Marca} {Nome}";

    public Botao? BotaoPorId(string id) => Botoes.FirstOrDefault(b => b.Id == id);

    /// <summary>Qual botão manda esta tecla (com os modificadores).</summary>
    public Botao? BotaoDoAtalho(Atalho a) => Botoes.FirstOrDefault(b => b.Atalhos.Contains(a));
}

public sealed class Catalogo
{
    public int Versao { get; set; } = 1;
    public List<Modelo> Modelos { get; set; } = [];

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static Catalogo Ler(string arquivo) =>
        JsonSerializer.Deserialize<Catalogo>(File.ReadAllText(arquivo), Json) ?? new Catalogo();

    public Modelo? PorId(string? id) => id is null ? null : Modelos.FirstOrDefault(m => m.Id == id);

    /// <summary>Os modelos que casam com este VID:PID, do mais certo para o mais fraco (PID exato antes do só-fabricante).</summary>
    public IReadOnlyList<Modelo> Candidatos(string vid, string pid) =>
        [.. Modelos.Where(m => m.Ids.Any(i => i.Pid is not null && i.Casa(vid, pid)))
            .Concat(Modelos.Where(m => m.Ids.Any(i => i.Pid is null && i.Casa(vid, pid))))
            .Distinct()];

    public IEnumerable<string> Marcas => Modelos.Select(m => m.Marca).Distinct().Order(StringComparer.CurrentCulture);
}

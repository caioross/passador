namespace Passador.Core;

/// <summary>Quando um perfil vale: o programa na frente (nome do processo) e, se preciso, um pedaço do título.</summary>
public sealed class Regra
{
    public List<string> Processos { get; set; } = [];
    public List<string> TituloContem { get; set; } = [];

    public bool Casa(string processo, string titulo)
    {
        var p = processo.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? processo[..^4] : processo;
        if (Processos.Count > 0 && !Processos.Any(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase))) return false;
        if (TituloContem.Count > 0 && !TituloContem.Any(t => titulo.Contains(t, StringComparison.OrdinalIgnoreCase))) return false;
        return Processos.Count > 0 || TituloContem.Count > 0;
    }
}

/// <summary>Dois botões apertados juntos.</summary>
public sealed class Combinacao
{
    public string BotaoA { get; set; } = "";
    public string BotaoB { get; set; } = "";
    public Acao Acao { get; set; } = new();
}

public sealed class Perfil
{
    public const string IdPadrao = "padrao";

    public string Id { get; set; } = "";
    public string Nome { get; set; } = "";
    /// <summary>Nome do ícone (Lucide) mostrado na tela.</summary>
    public string Icone { get; set; } = "layers";
    public bool Ativo { get; set; } = true;
    public string? Dica { get; set; }
    /// <summary>Vale se QUALQUER regra casar (o Padrão não tem regra e vale quando nenhum outro casa).</summary>
    public List<Regra> Regras { get; set; } = [];
    /// <summary>
    /// Chave "papel:avancar" (vale para qualquer passador) ou "botao:modelo/botao" (só aquele botão daquele modelo, que
    /// ganha do papel).
    /// </summary>
    public Dictionary<string, MapaBotao> Mapas { get; set; } = [];
    public List<Combinacao> Combinacoes { get; set; } = [];

    public static string ChavePapel(string papel) => "papel:" + papel;
    public static string ChaveBotao(string modelo, string botao) => $"botao:{modelo}/{botao}";

    public MapaBotao? Mapa(string modelo, Botao b) =>
        Mapas.TryGetValue(ChaveBotao(modelo, b.Id), out var m) ? m :
        b.Papel.Length > 0 && Mapas.TryGetValue(ChavePapel(b.Papel), out var p) ? p : null;
}

public static class Resolucao
{
    /// <summary>O perfil da janela da frente: o primeiro ativo que casa; senão o Padrão.</summary>
    public static Perfil Escolher(IReadOnlyList<Perfil> perfis, string processo, string titulo) =>
        perfis.FirstOrDefault(p => p.Ativo && p.Id != Perfil.IdPadrao && p.Regras.Any(r => r.Casa(processo, titulo)))
        ?? perfis.First(p => p.Id == Perfil.IdPadrao);

    /// <summary>
    /// O que o botão faz agora: o mapa do perfil da frente e, no que ele não disser (toque, duplo, segurar), o do Padrão.
    /// Nulo = o botão segue de fábrica.
    /// </summary>
    public static MapaBotao? MapaEfetivo(IReadOnlyList<Perfil> perfis, Perfil atual, string modelo, Botao b)
    {
        var padrao = perfis.FirstOrDefault(p => p.Id == Perfil.IdPadrao);
        var a = atual.Mapa(modelo, b);
        var d = atual == padrao ? null : padrao?.Mapa(modelo, b);
        if (a is null && d is null) return null;
        if (a is null) return d;
        if (d is null) return a;
        return new MapaBotao
        {
            Toque = a.Toque ?? d.Toque,
            Duplo = a.Duplo ?? d.Duplo,
            Segurar = a.Segurar ?? d.Segurar,
            ModoSegurar = a.Segurar is not null ? a.ModoSegurar : d.ModoSegurar,
            RepetirMs = a.Segurar is not null ? a.RepetirMs : d.RepetirMs,
        };
    }

    public static Combinacao? CombinacaoDe(IReadOnlyList<Perfil> perfis, Perfil atual, string a, string b)
    {
        bool Casa(Combinacao c) => (c.BotaoA == a && c.BotaoB == b) || (c.BotaoA == b && c.BotaoB == a);
        return atual.Combinacoes.FirstOrDefault(Casa) ?? perfis.FirstOrDefault(p => p.Id == Perfil.IdPadrao)?.Combinacoes.FirstOrDefault(Casa);
    }
}

/// <summary>Os perfis que já vêm prontos (o dono muda à vontade; "Restaurar" volta a estes).</summary>
public static class PerfisProntos
{
    static readonly string[] Navegadores = ["chrome", "msedge", "firefox", "brave", "opera", "vivaldi", "arc"];

    static MapaBotao M(string? toque = null, string? duplo = null, string? segurar = null, ModoSegurar modo = ModoSegurar.UmaVez) => new()
    {
        Toque = toque is null ? null : Acao.DeTecla(toque),
        Duplo = duplo is null ? null : Acao.DeTecla(duplo),
        Segurar = segurar is null ? null : Acao.DeTecla(segurar),
        ModoSegurar = modo,
    };

    public static List<Perfil> Todos() =>
    [
        new()
        {
            Id = Perfil.IdPadrao, Nome = "Padrão", Icone = "monitor",
            Dica = "Vale em qualquer programa que não tenha perfil próprio. Vazio = cada botão segue de fábrica.",
        },
        new()
        {
            Id = "powerpoint", Nome = "PowerPoint", Icone = "presentation",
            Dica = "Segurar Avançar vai ao último slide; dois toques em Iniciar começam do slide atual.",
            Regras = [new() { Processos = ["POWERPNT"] }],
            Mapas =
            {
                [Perfil.ChavePapel("avancar")] = M(segurar: "End"),
                [Perfil.ChavePapel("voltar")] = M(segurar: "Home"),
                [Perfil.ChavePapel("iniciar")] = M(duplo: "Shift+F5", segurar: "Esc"),
                [Perfil.ChavePapel("tela-preta")] = M(toque: "B", duplo: "W", segurar: "Ctrl+L"),
            },
        },
        new()
        {
            Id = "google-slides", Nome = "Google Slides", Icone = "app-window",
            Dica = "No navegador, quando o título da aba tem “Google Slides” ou “Apresentações Google”.",
            Regras = [new() { Processos = [.. Navegadores], TituloContem = ["Google Slides", "Apresentações Google", "Presentaciones de Google"] }],
            Mapas =
            {
                [Perfil.ChavePapel("avancar")] = M(toque: "Right", segurar: "End"),
                [Perfil.ChavePapel("voltar")] = M(toque: "Left", segurar: "Home"),
                [Perfil.ChavePapel("iniciar")] = M(toque: "Ctrl+F5", segurar: "Esc"),
                [Perfil.ChavePapel("tela-preta")] = M(toque: "B", duplo: "W"),
            },
        },
        new()
        {
            Id = "keynote-web", Nome = "Keynote no navegador", Icone = "app-window",
            Dica = "Keynote pelo iCloud, no navegador.",
            Regras = [new() { Processos = [.. Navegadores], TituloContem = ["Keynote"] }],
            Mapas =
            {
                [Perfil.ChavePapel("avancar")] = M(toque: "Right"),
                [Perfil.ChavePapel("voltar")] = M(toque: "Left"),
                [Perfil.ChavePapel("iniciar")] = M(segurar: "Esc"),
            },
        },
        new()
        {
            Id = "pdf", Nome = "PDF", Icone = "file-text",
            Dica = "Acrobat, Sumatra, Foxit ou um PDF aberto no navegador. Iniciar põe em tela cheia.",
            Regras =
            [
                new() { Processos = ["Acrobat", "AcroRd32", "SumatraPDF", "FoxitPDFReader", "FoxitPhantomPDF", "PDFXEdit"] },
                new() { Processos = [.. Navegadores], TituloContem = [".pdf"] },
            ],
            Mapas =
            {
                [Perfil.ChavePapel("avancar")] = M(toque: "PageDown", segurar: "End"),
                [Perfil.ChavePapel("voltar")] = M(toque: "PageUp", segurar: "Home"),
                [Perfil.ChavePapel("iniciar")] = M(toque: "Ctrl+L", segurar: "Esc"),
            },
        },
        new()
        {
            Id = "youtube", Nome = "YouTube", Icone = "youtube",
            Dica = "Avançar e Voltar andam 10 segundos; segurar passa ao próximo vídeo.",
            Regras = [new() { Processos = [.. Navegadores], TituloContem = ["YouTube"] }],
            Mapas =
            {
                [Perfil.ChavePapel("avancar")] = M(toque: "L", segurar: "Shift+N"),
                [Perfil.ChavePapel("voltar")] = M(toque: "J", segurar: "Shift+P"),
                [Perfil.ChavePapel("iniciar")] = M(toque: "K"),
                [Perfil.ChavePapel("tela-preta")] = M(toque: "F"),
                [Perfil.ChavePapel("cima")] = M(toque: "Up"),
                [Perfil.ChavePapel("baixo")] = M(toque: "Down"),
            },
        },
        new()
        {
            Id = "obs", Nome = "OBS Studio", Icone = "video",
            Dica = "Manda F13 a F17, teclas que nenhum teclado tem: no OBS, em Configurações > Atalhos, ligue cada uma a uma cena ou à gravação.",
            Regras = [new() { Processos = ["obs64", "obs32", "obs"] }],
            Mapas =
            {
                [Perfil.ChavePapel("avancar")] = M(toque: "F14"),
                [Perfil.ChavePapel("voltar")] = M(toque: "F13"),
                [Perfil.ChavePapel("iniciar")] = M(toque: "F15", segurar: "F17"),
                [Perfil.ChavePapel("tela-preta")] = M(toque: "F16"),
            },
        },
    ];
}

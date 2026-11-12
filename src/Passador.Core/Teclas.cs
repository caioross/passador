using System.Text;

namespace Passador.Core;

[Flags]
public enum Mod { Nenhum = 0, Ctrl = 1, Shift = 2, Alt = 4, Win = 8 }

/// <summary>
/// Nome das teclas (o que vai no catálogo e na configuração) e o código virtual do Windows (VK) de cada uma.
/// Os nomes são estáveis e sem acento ("PageDown", "Ponto", "VolumeUp"); o rótulo em português é só para a tela.
/// </summary>
public static class Teclas
{
    static readonly Dictionary<string, ushort> PorNome = new(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<ushort, string> PorVk = new();
    static readonly Dictionary<string, string> Rotulos = new(StringComparer.OrdinalIgnoreCase);

    static void D(string nome, ushort vk, string? rotulo = null)
    {
        PorNome[nome] = vk;
        PorVk.TryAdd(vk, nome);
        Rotulos[nome] = rotulo ?? nome;
    }

    static Teclas()
    {
        for (var c = 'A'; c <= 'Z'; c++) D(c.ToString(), c);
        for (var c = '0'; c <= '9'; c++) D(c.ToString(), c);
        for (var i = 1; i <= 24; i++) D("F" + i, (ushort)(0x6F + i));
        D("PageUp", 0x21, "Page Up"); D("PageDown", 0x22, "Page Down");
        D("End", 0x23); D("Home", 0x24);
        D("Left", 0x25, "Seta esquerda"); D("Up", 0x26, "Seta cima"); D("Right", 0x27, "Seta direita"); D("Down", 0x28, "Seta baixo");
        D("Esc", 0x1B); D("Enter", 0x0D); D("Tab", 0x09); D("Space", 0x20, "Espaço"); D("Backspace", 0x08);
        D("Insert", 0x2D); D("Delete", 0x2E); D("PrintScreen", 0x2C, "Print Screen"); D("Pause", 0x13);
        D("Ponto", 0xBE, "Ponto (.)"); D("Virgula", 0xBC, "Vírgula (,)"); D("Mais", 0xBB, "+"); D("Menos", 0xBD, "−");
        D("Barra", 0xBF, "/"); D("Menu", 0x5D, "Menu de contexto");
        D("VolumeMute", 0xAD, "Mudo"); D("VolumeDown", 0xAE, "Volume −"); D("VolumeUp", 0xAF, "Volume +");
        D("MediaNext", 0xB0, "Próxima faixa"); D("MediaPrev", 0xB1, "Faixa anterior"); D("MediaStop", 0xB2, "Parar mídia");
        D("MediaPlayPause", 0xB3, "Tocar/pausar");
        D("BrowserBack", 0xA6, "Voltar (navegador)"); D("BrowserForward", 0xA7, "Avançar (navegador)");
        D("NumMais", 0x6B, "+ (numérico)"); D("NumMenos", 0x6D, "− (numérico)");
        // Modificadores (os genéricos e os de cada lado caem no mesmo nome)
        D("Ctrl", 0x11); D("Shift", 0x10); D("Alt", 0x12); D("Win", 0x5B);
        PorVk[0xA2] = PorVk[0xA3] = "Ctrl"; PorVk[0xA0] = PorVk[0xA1] = "Shift"; PorVk[0xA4] = PorVk[0xA5] = "Alt"; PorVk[0x5C] = "Win";
    }

    public static IReadOnlyCollection<string> Nomes => PorNome.Keys;

    public static bool TentarVk(string nome, out ushort vk) => PorNome.TryGetValue(nome.Trim(), out vk);
    public static string Nome(ushort vk) => PorVk.TryGetValue(vk, out var n) ? n : $"VK{vk:X2}";
    public static string Rotulo(string nome) => Rotulos.TryGetValue(nome, out var r) ? r : nome;

    public static Mod ModificadorDe(ushort vk) => vk switch
    {
        0x11 or 0xA2 or 0xA3 => Mod.Ctrl,
        0x10 or 0xA0 or 0xA1 => Mod.Shift,
        0x12 or 0xA4 or 0xA5 => Mod.Alt,
        0x5B or 0x5C => Mod.Win,
        _ => Mod.Nenhum,
    };

    /// <summary>Teclas que o Windows marca como "estendidas" (setas, Page Up/Down etc.): o SendInput precisa da marca.</summary>
    public static bool Estendida(ushort vk) => vk is >= 0x21 and <= 0x28 or 0x2D or 0x2E or 0x5B or 0x5C or 0x5D or 0x6F
        or 0xA3 or 0xA5 or 0x90 or 0x2C or (>= 0xA6 and <= 0xB7);
}

/// <summary>Uma tecla com modificadores: "PageDown", "Shift+F5", "Ctrl+Alt+Delete".</summary>
public readonly record struct Atalho(Mod Mods, ushort Vk)
{
    public static Atalho? Ler(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var mods = Mod.Nenhum;
        ushort vk = 0;
        // "Ctrl++" quer dizer Ctrl e a tecla +: separar só pelos '+' que têm algo dos dois lados
        var partes = new List<string>();
        var atual = new StringBuilder();
        foreach (var ch in texto.Trim())
        {
            if (ch == '+' && atual.Length > 0) { partes.Add(atual.ToString()); atual.Clear(); }
            else atual.Append(ch);
        }
        if (atual.Length > 0) partes.Add(atual.ToString());
        foreach (var p in partes)
        {
            var t = p.Trim();
            var m = t.ToLowerInvariant() switch
            {
                "ctrl" or "control" => Mod.Ctrl,
                "shift" => Mod.Shift,
                "alt" => Mod.Alt,
                "win" => Mod.Win,
                _ => Mod.Nenhum,
            };
            if (m != Mod.Nenhum && vk == 0 && p != partes[^1]) { mods |= m; continue; }
            if (t == "+") t = "Mais";
            else if (t == "-" || t == "−") t = "Menos";
            else if (t == ".") t = "Ponto";
            else if (t == ",") t = "Virgula";
            if (!Teclas.TentarVk(t, out vk)) return null;
        }
        return vk == 0 ? null : new Atalho(mods, vk);
    }

    public string Nome => Teclas.Nome(Vk);

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (Mods.HasFlag(Mod.Ctrl)) sb.Append("Ctrl+");
        if (Mods.HasFlag(Mod.Shift)) sb.Append("Shift+");
        if (Mods.HasFlag(Mod.Alt)) sb.Append("Alt+");
        if (Mods.HasFlag(Mod.Win)) sb.Append("Win+");
        sb.Append(Nome);
        return sb.ToString();
    }

    /// <summary>Para a tela: "Shift + F5", "Seta direita".</summary>
    public string Rotulo()
    {
        var s = ToString().Split('+');
        s[^1] = Teclas.Rotulo(s[^1]);
        return string.Join(" + ", s);
    }
}

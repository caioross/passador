using Passador.Core;
using static Passador.Motor.Nativo;

namespace Passador.Motor;

/// <summary>
/// Um passador de mentira: manda pelo SendInput as mesmas teclas que o aparelho mandaria, com a marca do passador
/// simulado n. O motor trata como se viesse de um aparelho USB (Raw Input, gancho, gestos, ação): é o que prova o
/// motor sem passador conectado, e o que a tela usa no modo "testar".
/// </summary>
public static class Simulador
{
    static readonly (Mod, ushort)[] Mods = [(Mod.Ctrl, 0xA2), (Mod.Shift, 0xA0), (Mod.Alt, 0xA4), (Mod.Win, 0x5B)];

    public static void Descer(int n, Atalho a)
    {
        var m = MarcaSimulado + (uint)n;
        var l = new List<INPUT>();
        foreach (var (f, vk) in Mods) if (a.Mods.HasFlag(f)) l.Add(Executor.TeclaCom(vk, false, m));
        l.Add(Executor.TeclaCom(a.Vk, false, m));
        Executor.Mandar(l);
    }

    public static void Subir(int n, Atalho a)
    {
        var m = MarcaSimulado + (uint)n;
        var l = new List<INPUT> { Executor.TeclaCom(a.Vk, true, m) };
        foreach (var (f, vk) in Mods.Reverse()) if (a.Mods.HasFlag(f)) l.Add(Executor.TeclaCom(vk, true, m));
        Executor.Mandar(l);
    }
}

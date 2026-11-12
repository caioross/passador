using System.Collections.Concurrent;
using System.Diagnostics;
using Passador.Core;
using static Passador.Motor.Nativo;

namespace Passador.Motor;

/// <summary>
/// Faz as ações (teclas, textos, macros, abrir, mouse) numa thread própria: uma macro com espera nunca segura o motor.
/// Toda tecla sai com a <see cref="Nativo.MarcaPassador"/>, para o motor e o gancho não a confundirem com o passador.
/// </summary>
public sealed class Executor : IDisposable
{
    readonly BlockingCollection<Action> _fila = new();
    readonly Thread _thread;
    readonly uint _marca;

    public Executor(uint marca = MarcaPassador)
    {
        _marca = marca;
        _thread = new Thread(() =>
        {
            foreach (var a in _fila.GetConsumingEnumerable())
            {
                try { a(); }
                catch (Exception ex) { Log.Escrever("executor: " + ex.Message); }
            }
        }) { IsBackground = true, Name = "passador-executor" };
        _thread.Start();
    }

    public void Enfileirar(Action a) { if (!_fila.IsAddingCompleted) _fila.Add(a); }

    public void Fazer(Acao acao, Atalho? original) => Enfileirar(() => Executar(acao, original));

    void Executar(Acao acao, Atalho? original)
    {
        switch (acao.Tipo)
        {
            case TipoAcao.Original when original is { } o: Tocar(o); break;
            case TipoAcao.Tecla when Atalho.Ler(acao.Tecla) is { } a: Tocar(a); break;
            case TipoAcao.Texto: Digitar(acao.Texto ?? ""); break;
            case TipoAcao.Abrir: Abrir(acao.Caminho, acao.Argumentos); break;
            case TipoAcao.Mouse: Mouse(acao.Mouse); break;
            case TipoAcao.Sequencia:
                foreach (var p in acao.Passos ?? [])
                {
                    switch (p.Tipo)
                    {
                        case TipoPasso.Tecla when Atalho.Ler(p.Valor) is { } a: Tocar(a); break;
                        case TipoPasso.Texto: Digitar(p.Valor); break;
                        case TipoPasso.Espera when int.TryParse(p.Valor, out var ms): Thread.Sleep(Math.Clamp(ms, 0, 30_000)); break;
                        case TipoPasso.Abrir: Abrir(p.Valor, null); break;
                        case TipoPasso.Mouse: Mouse(p.Valor); break;
                    }
                    Thread.Sleep(15); // programas lentos perdem tecla colada na outra
                }
                break;
        }
    }

    /// <summary>Aperta e solta um atalho (modificadores em volta).</summary>
    public void Tocar(Atalho a)
    {
        var l = new List<INPUT>();
        Mods(l, a.Mods, false);
        l.Add(Tecla(a.Vk, false));
        l.Add(Tecla(a.Vk, true));
        Mods(l, a.Mods, true);
        Mandar(l);
    }

    /// <summary>Só aperta (ou só solta): segurar "enquanto" e o repasse fiel do passador.</summary>
    public void Descer(Atalho a, bool comMods = true)
    {
        var l = new List<INPUT>();
        if (comMods) Mods(l, a.Mods, false);
        l.Add(Tecla(a.Vk, false));
        Mandar(l);
    }

    public void Subir(Atalho a, bool comMods = true)
    {
        var l = new List<INPUT> { Tecla(a.Vk, true) };
        if (comMods) Mods(l, a.Mods, true);
        Mandar(l);
    }

    void Mods(List<INPUT> l, Mod m, bool soltar)
    {
        var ordem = new (Mod, ushort)[] { (Mod.Ctrl, 0xA2), (Mod.Shift, 0xA0), (Mod.Alt, 0xA4), (Mod.Win, 0x5B) };
        foreach (var (f, vk) in soltar ? ordem.Reverse() : ordem)
            if (m.HasFlag(f)) l.Add(Tecla(vk, soltar));
    }

    public INPUT Tecla(ushort vk, bool soltar) => TeclaCom(vk, soltar, _marca);

    public static INPUT TeclaCom(ushort vk, bool soltar, uint marca)
    {
        var _marca = marca;
        var flags = soltar ? KEYEVENTF_KEYUP : 0;
        if (Teclas.Estendida(vk)) flags |= KEYEVENTF_EXTENDEDKEY;
        return new INPUT
        {
            Type = INPUT_KEYBOARD,
            Ki = new KEYBDINPUT { Vk = vk, Scan = (ushort)MapVirtualKeyW(vk, 0), Flags = flags, Extra = (IntPtr)_marca },
        };
    }

    public void Digitar(string texto)
    {
        var l = new List<INPUT>();
        foreach (var ch in texto.Replace("\r\n", "\n"))
        {
            if (ch == '\n') { l.Add(Tecla(0x0D, false)); l.Add(Tecla(0x0D, true)); continue; }
            foreach (var soltar in new[] { false, true })
                l.Add(new INPUT
                {
                    Type = INPUT_KEYBOARD,
                    Ki = new KEYBDINPUT { Vk = 0, Scan = ch, Flags = KEYEVENTF_UNICODE | (soltar ? KEYEVENTF_KEYUP : 0), Extra = (IntPtr)_marca },
                });
        }
        Mandar(l);
    }

    void Mouse(string? gesto)
    {
        INPUT M(uint f, int dado = 0) => new() { Type = INPUT_MOUSE, Mi = new MOUSEINPUT { Flags = f, MouseData = dado, Extra = (IntPtr)_marca } };
        var l = gesto switch
        {
            "clique-esquerdo" => new List<INPUT> { M(MOUSEEVENTF_LEFTDOWN), M(MOUSEEVENTF_LEFTUP) },
            "clique-direito" => [M(MOUSEEVENTF_RIGHTDOWN), M(MOUSEEVENTF_RIGHTUP)],
            "clique-meio" => [M(MOUSEEVENTF_MIDDLEDOWN), M(MOUSEEVENTF_MIDDLEUP)],
            "rolar-cima" => [M(MOUSEEVENTF_WHEEL, 120)],
            "rolar-baixo" => [M(MOUSEEVENTF_WHEEL, -120)],
            _ => [],
        };
        Mandar(l);
    }

    static void Abrir(string? caminho, string? args)
    {
        if (string.IsNullOrWhiteSpace(caminho)) return;
        try { Process.Start(new ProcessStartInfo(Environment.ExpandEnvironmentVariables(caminho), args ?? "") { UseShellExecute = true }); }
        catch (Exception ex) { Log.Escrever($"não abriu {Path.GetFileName(caminho)}: {ex.Message}"); }
    }

    public static void Mandar(List<INPUT> l)
    {
        if (l.Count == 0) return;
        var arr = l.ToArray();
        if (SendInput((uint)arr.Length, arr, TamanhoInput) != arr.Length)
            Log.Escrever("SendInput não mandou tudo (programa elevado na frente?)");
    }

    public void Dispose()
    {
        _fila.CompleteAdding();
        _thread.Join(1000);
    }
}

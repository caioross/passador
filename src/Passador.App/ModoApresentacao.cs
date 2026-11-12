using System.Runtime.InteropServices;
using Passador.Core;

namespace Passador.App;

/// <summary>
/// Modo apresentação (menu da bandeja): enquanto ligado, a tela não apaga nem o PC dorme (SetThreadExecutionState,
/// documentado) e o Passador tenta silenciar as notificações do Windows sem pedir administrador, pelo mesmo estado que
/// o botão "Não perturbe" usa (WNF quiet hours, não documentado). Depois confere com SHQueryUserNotificationState se o
/// Windows parou mesmo de aceitar notificações; se não parou, volta o estado como estava e fica só o lembrete.
/// Ao desligar (ou ao sair do Passador), tudo volta ao que era antes.
/// </summary>
public sealed class ModoApresentacao : IDisposable
{
    const ulong WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED = 0x0D83063EA3BF1C75;
    const uint ES_CONTINUOUS = 0x80000000, ES_SYSTEM_REQUIRED = 0x1, ES_DISPLAY_REQUIRED = 0x2;
    const int QUNS_ACCEPTS_NOTIFICATIONS = 5;

    [DllImport("ntdll.dll")]
    static extern int NtUpdateWnfStateData(ref ulong nome, byte[] dados, int tam, IntPtr tipo, IntPtr escopo, uint carimbo, uint conferir);
    [DllImport("ntdll.dll")]
    static extern int NtQueryWnfStateData(ref ulong nome, IntPtr tipo, IntPtr escopo, out uint carimbo, byte[] dados, ref int tam);
    [DllImport("shell32.dll")]
    static extern int SHQueryUserNotificationState(out int estado);
    [DllImport("kernel32.dll")]
    static extern uint SetThreadExecutionState(uint flags);

    byte[]? _antes;
    public bool Ligado { get; private set; }
    /// <summary>true = o Windows confirmou que as notificações pararam; false = ficou só o lembrete.</summary>
    public bool Silenciou { get; private set; }

    /// <summary>Liga e devolve a frase para o aviso da bandeja.</summary>
    public string Ligar()
    {
        if (Ligado) return Frase();
        Ligado = true;
        SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
        Silenciou = false;
        try
        {
            _antes = LerQuiet();
            if (_antes is not null && Gravar(2)) // 2 = só alarmes (o mais silencioso do "Não perturbe")
            {
                Thread.Sleep(400);
                Silenciou = SHQueryUserNotificationState(out var q) == 0 && q != QUNS_ACCEPTS_NOTIFICATIONS;
                if (!Silenciou) Restaurar();
            }
        }
        catch (Exception ex) { Log.Escrever("modo apresentação: " + ex.Message); }
        Log.Escrever($"modo apresentação ligado (notificações {(Silenciou ? "silenciadas" : "só com lembrete")})");
        return Frase();
    }

    string Frase() => Silenciou
        ? "Modo apresentação ligado: notificações do Windows em silêncio e a tela não apaga."
        : "Modo apresentação ligado: a tela não apaga. O Windows não deixou silenciar as notificações sozinho; ligue o Não perturbe pela central (Win+N).";

    public void Desligar()
    {
        if (!Ligado) return;
        Ligado = false;
        SetThreadExecutionState(ES_CONTINUOUS);
        if (Silenciou) Restaurar();
        Silenciou = false;
        Log.Escrever("modo apresentação desligado");
    }

    void Restaurar()
    {
        try
        {
            var nome = WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED;
            var dados = _antes is { Length: > 0 } a ? a : new byte[4];
            NtUpdateWnfStateData(ref nome, dados, dados.Length, IntPtr.Zero, IntPtr.Zero, 0, 0);
        }
        catch (Exception ex) { Log.Escrever("modo apresentação, ao restaurar: " + ex.Message); }
    }

    static byte[]? LerQuiet()
    {
        var nome = WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED;
        var buf = new byte[16];
        var tam = buf.Length;
        if (NtQueryWnfStateData(ref nome, IntPtr.Zero, IntPtr.Zero, out _, buf, ref tam) != 0) return null;
        return buf[..Math.Max(tam, 0)];
    }

    static bool Gravar(int valor)
    {
        var nome = WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED;
        var dados = BitConverter.GetBytes(valor);
        return NtUpdateWnfStateData(ref nome, dados, dados.Length, IntPtr.Zero, IntPtr.Zero, 0, 0) == 0;
    }

    public void Dispose() => Desligar();
}

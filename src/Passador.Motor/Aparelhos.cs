using System.Text.RegularExpressions;
using static Passador.Motor.Nativo;

namespace Passador.Motor;

public enum TipoAparelho { Teclado, Midia, Mouse, Outro }

/// <summary>Um aparelho de entrada que o Windows mostra pelo Raw Input.</summary>
public sealed record AparelhoVisto
{
    public IntPtr Handle { get; init; }
    public string Caminho { get; init; } = "";
    public string Vid { get; init; } = "";
    public string Pid { get; init; } = "";
    public string? Interface { get; init; }
    public string? Produto { get; init; }
    public string? Fabricante { get; init; }
    public TipoAparelho Tipo { get; init; }
    public bool Bluetooth { get; init; }
    public bool Simulado { get; init; }

    /// <summary>"046D:C52D" (um receptor com várias interfaces aparece como um aparelho só).</summary>
    public string Chave => Simulado ? "SIM:" + Pid : $"{Vid}:{Pid}";

    /// <summary>O nome diz que é passador (para aparelho fora do catálogo).</summary>
    public bool NomeDePassador => Produto is { } p && Regex.IsMatch(p,
        @"present|remote|pointer|laser|ring|clicker|spotlight|passador|apresent|2\.4g|wireless receiver", RegexOptions.IgnoreCase);
}

public static unsafe class Aparelhos
{
    static readonly Regex RxIds = new(@"VID[_&]?(?:\w*?&)?(?<v>[0-9A-F]{4}).*?PID[_&]?(?<p>[0-9A-F]{4})(?:.*?MI_(?<mi>[0-9A-F]{2}))?",
        RegexOptions.IgnoreCase);
    static readonly Regex RxBt = new(@"_VID&(?:0002|0001|02|01)(?<v>[0-9A-F]{4})_PID&(?<p>[0-9A-F]{4})", RegexOptions.IgnoreCase);

    /// <summary>Lista teclados e controles de mídia ligados agora (sem os teclados virtuais sem VID).</summary>
    public static List<AparelhoVisto> Listar()
    {
        var saida = new List<AparelhoVisto>();
        uint n = 0;
        var tam = (uint)sizeof(RAWINPUTDEVICELIST);
        GetRawInputDeviceList(null, ref n, tam);
        if (n == 0) return saida;
        var lista = new RAWINPUTDEVICELIST[n];
        n = GetRawInputDeviceList(lista, ref n, tam);
        if (n == uint.MaxValue) return saida;
        foreach (var d in lista.Take((int)n))
        {
            var a = Descrever(d.Device, d.Type);
            if (a is not null) saida.Add(a);
        }
        return saida;
    }

    public static AparelhoVisto? Descrever(IntPtr h, uint tipo)
    {
        var caminho = Nome(h);
        if (caminho is null) return null;
        string? vid = null, pid = null, mi = null;
        var bt = RxBt.Match(caminho);
        if (bt.Success) { vid = bt.Groups["v"].Value; pid = bt.Groups["p"].Value; }
        else if (RxIds.Match(caminho) is { Success: true } m)
        {
            vid = m.Groups["v"].Value; pid = m.Groups["p"].Value;
            mi = m.Groups["mi"].Success ? m.Groups["mi"].Value : null;
        }
        if (vid is null || pid is null) return null; // teclado do sistema/virtual sem identificação USB
        var t = tipo switch { RIM_TYPEKEYBOARD => TipoAparelho.Teclado, RIM_TYPEMOUSE => TipoAparelho.Mouse, _ => TipoAparelho.Outro };
        if (tipo == RIM_TYPEHID)
        {
            var info = new RID_DEVICE_INFO { Size = (uint)sizeof(RID_DEVICE_INFO) };
            var sz = info.Size;
            if (GetRawInputDeviceInfoW(h, RIDI_DEVICEINFO, &info, ref sz) != uint.MaxValue && info.Hid.UsagePage == 0x0C)
                t = TipoAparelho.Midia;
        }
        var (produto, fabricante) = Textos(caminho);
        return new AparelhoVisto
        {
            Handle = h, Caminho = caminho, Vid = vid.ToUpperInvariant(), Pid = pid.ToUpperInvariant(), Interface = mi,
            Produto = produto, Fabricante = fabricante, Tipo = t, Bluetooth = bt.Success || caminho.Contains("BTH", StringComparison.OrdinalIgnoreCase),
        };
    }

    static string? Nome(IntPtr h)
    {
        uint sz = 0;
        GetRawInputDeviceInfoW(h, RIDI_DEVICENAME, null, ref sz);
        if (sz == 0) return null;
        var buf = new char[sz + 1];
        fixed (char* p = buf)
            if (GetRawInputDeviceInfoW(h, RIDI_DEVICENAME, p, ref sz) == uint.MaxValue) return null;
        return new string(buf).TrimEnd('\0');
    }

    /// <summary>Nome do produto e do fabricante gravados no aparelho (abrir sem acesso de leitura basta).</summary>
    static (string?, string?) Textos(string caminho)
    {
        var h = CreateFileW(caminho, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
        if (h == new IntPtr(-1)) return (null, null);
        try
        {
            var b = new char[128];
            string? produto = HidD_GetProductString(h, b, (uint)b.Length * 2) ? new string(b).TrimEnd('\0').Trim() : null;
            Array.Clear(b);
            string? fab = HidD_GetManufacturerString(h, b, (uint)b.Length * 2) ? new string(b).TrimEnd('\0').Trim() : null;
            return (string.IsNullOrEmpty(produto) ? null : produto, string.IsNullOrEmpty(fab) ? null : fab);
        }
        finally { CloseHandle(h); }
    }
}

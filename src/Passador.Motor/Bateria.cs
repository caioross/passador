using System.Runtime.InteropServices;

namespace Passador.Motor;

/// <summary>
/// Nível de bateria que o próprio Windows mostra em Configurações > Bluetooth: a propriedade
/// {104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2 (um byte, 0 a 100) no nó do aparelho Bluetooth. O caminho do Raw Input é
/// a interface HID; a propriedade mora num nó acima dela (o aparelho Bluetooth), então sobe pela árvore até achar.
/// Receptor USB 2.4 GHz quase nunca informa: aí volta nulo e a bandeja diz "não informada". Sem driver e sem
/// administrador: só leitura no gerenciador de configuração (cfgmgr32).
/// </summary>
public static unsafe class Bateria
{
    const int CR_SUCCESS = 0;
    const uint DEVPROP_TYPE_BYTE = 0x00000003;
    const uint DEVPROP_TYPE_STRING = 0x00000012;

    [StructLayout(LayoutKind.Sequential)]
    struct DEVPROPKEY { public Guid Fmtid; public uint Pid; }

    static readonly DEVPROPKEY ChaveBateria = new() { Fmtid = new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"), Pid = 2 };
    static readonly DEVPROPKEY ChaveInstancia = new() { Fmtid = new Guid("78C34FC8-104A-4ACA-9EA4-524D52996E57"), Pid = 256 };

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    static extern int CM_Get_Device_Interface_PropertyW(string iface, ref DEVPROPKEY key, out uint tipo, byte* buf, ref uint tam, uint flags);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    static extern int CM_Locate_DevNodeW(out uint no, string id, uint flags);
    [DllImport("cfgmgr32.dll")]
    static extern int CM_Get_Parent(out uint pai, uint no, uint flags);
    [DllImport("cfgmgr32.dll")]
    static extern int CM_Get_DevNode_PropertyW(uint no, ref DEVPROPKEY key, out uint tipo, byte* buf, ref uint tam, uint flags);

    /// <summary>Percentual de bateria do aparelho deste caminho de interface (nulo = o aparelho não informa).</summary>
    public static int? Ler(string caminhoInterface)
    {
        try
        {
            var id = Instancia(caminhoInterface);
            if (id is null || CM_Locate_DevNodeW(out var no, id, 0) != CR_SUCCESS) return null;
            for (var subida = 0; subida < 8; subida++)
            {
                var k = ChaveBateria;
                byte valor = 0;
                uint tam = 1;
                if (CM_Get_DevNode_PropertyW(no, ref k, out var tipo, &valor, ref tam, 0) == CR_SUCCESS && tipo == DEVPROP_TYPE_BYTE)
                    return valor <= 100 ? valor : null;
                if (CM_Get_Parent(out var pai, no, 0) != CR_SUCCESS) break;
                no = pai;
            }
        }
        catch (Exception) { }
        return null;
    }

    static string? Instancia(string caminho)
    {
        var k = ChaveInstancia;
        uint tam = 0;
        CM_Get_Device_Interface_PropertyW(caminho, ref k, out _, null, ref tam, 0);
        if (tam == 0 || tam > 4096) return null;
        var buf = stackalloc byte[(int)tam];
        if (CM_Get_Device_Interface_PropertyW(caminho, ref k, out var tipo, buf, ref tam, 0) != CR_SUCCESS || tipo != DEVPROP_TYPE_STRING)
            return null;
        return new string((char*)buf).TrimEnd('\0');
    }
}

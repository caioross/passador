using System.Text.Json;

namespace Batuta.Core;

/// <summary>Um aparelho que já passou por este PC e o modelo que ele é.</summary>
public sealed class AparelhoConhecido
{
    /// <summary>"VID:PID" (o mesmo receptor em outra porta continua sendo ele).</summary>
    public string Chave { get; set; } = "";
    public string ModeloId { get; set; } = "";
    public string? Apelido { get; set; }
    /// <summary>Falso = o Batuta deixa este aparelho em paz.</summary>
    public bool Ativo { get; set; } = true;
}

public enum ModoBloqueio
{
    /// <summary>Só os botões do passador mudam; o teclado de verdade fica igual (gancho por aparelho).</summary>
    PorAparelho,
    /// <summary>Reserva: as teclas configuradas mudam venham de onde vierem (o teclado também perde Page Down etc.).</summary>
    Simples,
}

public sealed class Ajustes
{
    public int DuploToqueMs { get; set; } = 300;
    public int SegurarMs { get; set; } = 550;
    public ModoBloqueio Bloqueio { get; set; } = ModoBloqueio.PorAparelho;
    public bool IniciarComWindows { get; set; } = true;
    public bool MotorLigado { get; set; } = true;
    public bool AtualizacaoAutomatica { get; set; } = true;
    /// <summary>Mostra um aviso pequeno na tela quando o perfil troca sozinho.</summary>
    public bool AvisarTrocaDePerfil { get; set; } = true;
}

public sealed class Configuracao
{
    public int Versao { get; set; } = 1;
    public Ajustes Ajustes { get; set; } = new();
    public List<AparelhoConhecido> Aparelhos { get; set; } = [];
    public List<Perfil> Perfis { get; set; } = PerfisProntos.Todos();
    /// <summary>Modelos ensinados neste PC pelo modo aprender.</summary>
    public List<Modelo> ModelosAprendidos { get; set; } = [];
    /// <summary>O modelo que aparece na tela quando nada está conectado.</summary>
    public string? UltimoModelo { get; set; }

    public static Configuracao Ler(string arquivo)
    {
        try
        {
            if (!File.Exists(arquivo)) return new Configuracao();
            var c = JsonSerializer.Deserialize<Configuracao>(File.ReadAllText(arquivo), Catalogo.Json) ?? new Configuracao();
            if (!c.Perfis.Any(p => p.Id == Perfil.IdPadrao)) c.Perfis.Insert(0, PerfisProntos.Todos()[0]);
            return c;
        }
        catch (Exception ex)
        {
            // Arquivo estragado: guarda o que havia e começa de novo (nunca perde o arquivo dele).
            try { File.Copy(arquivo, arquivo + $".estragado-{DateTime.Now:yyyyMMdd-HHmmss}", true); } catch { }
            Log.Escrever("configuração ilegível, começando do zero: " + ex.Message);
            return new Configuracao();
        }
    }

    /// <summary>Grava sem risco de meio arquivo: escreve ao lado e troca.</summary>
    public void Gravar(string arquivo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
        var tmp = arquivo + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, Catalogo.Json));
        File.Move(tmp, arquivo, true);
    }
}

/// <summary>
/// Onde ficam os dados: %LOCALAPPDATA%\Batuta.Dados (nunca na pasta do Velopack, que o desinstalador apaga inteira).
/// BATUTA_DADOS troca a pasta (testes e capturas não tocam na configuração real).
/// </summary>
public static class Armazenamento
{
    public static string PastaPadrao =>
        Environment.GetEnvironmentVariable("BATUTA_DADOS") is { Length: > 0 } d ? d
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Batuta.Dados");

    public static string ArquivoConfig => Path.Combine(PastaPadrao, "config.json");
    public static string ArquivoLog => Path.Combine(PastaPadrao, "batuta.log");
}

/// <summary>Log simples, uma linha por evento. Nunca recebe segredo (o Batuta não tem nenhum) nem texto digitado.</summary>
public static class Log
{
    static string? _arquivo;
    static readonly object Trava = new();

    public static void Iniciar(string arquivo)
    {
        _arquivo = arquivo;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
            var f = new FileInfo(arquivo);
            if (f.Exists && f.Length > 2_000_000) File.Move(arquivo, arquivo + ".antigo", true);
        }
        catch { }
    }

    public static void Escrever(string texto)
    {
        if (_arquivo is null) return;
        lock (Trava)
        {
            try { File.AppendAllText(_arquivo, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Environment.ProcessId}] {texto}{Environment.NewLine}"); }
            catch { }
        }
    }
}

using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using Batuta.Core;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;

namespace Batuta.App;

/// <summary>
/// Atualização automática pela b75 (Velopack): o espelho dos releases do GitHub (repositório privado) em
/// clarify-atualizacoes.caiorossi.com/batuta, o mesmo espelho dos apps da Clarify. Procura ao abrir e a cada 4 horas,
/// baixa em segundo plano (só o que mudou) e aplica quando o Batuta fecha, ou na hora se ninguém estiver apertando
/// o passador. Volta de versão: se a versão nova cai seguidas vezes (o vigia decide) ou se o release foi apagado no
/// GitHub (recolhidas.json), desce para a anterior. (Mesmo molde do Clarify Avaliador.)
/// </summary>
public sealed class Atualizador
{
    public const string FeedPadrao = "https://clarify-atualizacoes.caiorossi.com/batuta";

    /// <summary>BATUTA_FEED troca o endereço (uma pasta também vale), para testar sem tocar no espelho.</summary>
    public static string Feed => Environment.GetEnvironmentVariable("BATUTA_FEED") is { Length: > 0 } f ? f : FeedPadrao;

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    readonly UpdateManager? _um;
    UpdateManager? _umBaixou;
    UpdateInfo? _baixada;
    bool _ocupado;

    public string Status { get; private set; } = "";
    public string? NovaVersao => _baixada?.TargetFullRelease.Version.ToString();
    public bool Pronta => _baixada is not null;
    public bool Instalado => _um?.IsInstalled == true;
    public event Action? Mudou;

    public static string VersaoAtual => Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "?";

    public Atualizador()
    {
        try { _um = Gerente(permitirDescer: false); }
        catch (Exception ex) { Log.Escrever("atualizador: " + ex.Message); }
        Status = Instalado ? $"Versão {VersaoAtual}" : $"Versão {VersaoAtual} (rodando fora do instalador: sem atualização)";
    }

    static UpdateManager Gerente(bool permitirDescer) =>
        new(new FonteEspelho(Feed, SaudeDaVersao.Ruins), new UpdateOptions { AllowVersionDowngrade = permitirDescer });

    void Definir(string s)
    {
        Status = s;
        Mudou?.Invoke();
    }

    public async Task Verificar(bool manual = false)
    {
        if (_um is null || !Instalado || _ocupado || Pronta) return;
        _ocupado = true;
        try
        {
            if (manual) Definir("Procurando atualização…");
            var descer = SaudeDaVersao.Ruins().Contains(VersaoAtual) || (await Recolhidas()).Contains(VersaoAtual);
            var um = descer ? Gerente(permitirDescer: true) : _um;
            var info = await um.CheckForUpdatesAsync();
            if (info is null)
            {
                Definir($"Versão {VersaoAtual}, a mais recente (conferido às {DateTime.Now:HH:mm})");
                return;
            }
            var v = info.TargetFullRelease.Version;
            var verbo = info.IsDowngrade ? "Voltando para" : "Baixando";
            Definir($"{verbo} a versão {v}…");
            await um.DownloadUpdatesAsync(info, p => { if (p % 10 == 0) Definir($"{verbo} a versão {v}… {p}%"); });
            _umBaixou = um;
            _baixada = info;
            Definir(info.IsDowngrade
                ? $"A versão {VersaoAtual} foi recolhida; a {v} entra quando o Batuta reiniciar"
                : $"Versão {v} pronta: entra quando o Batuta reiniciar (ou agora, em Ajustes)");
            Log.Escrever(info.IsDowngrade ? $"versão {VersaoAtual} recolhida: {v} baixada para voltar" : $"atualização {v} baixada");
        }
        catch (Exception ex)
        {
            Definir($"Versão {VersaoAtual}. Não consegui procurar agora ({ex.Message.Split('\n')[0]})");
            Log.Escrever("atualizador: " + ex.Message);
        }
        finally { _ocupado = false; }
    }

    public void AplicarAgora()
    {
        if (_umBaixou is null || _baixada is null) return;
        Vigia.SaidaLimpa();
        _umBaixou.ApplyUpdatesAndRestart(_baixada.TargetFullRelease, ["--bandeja"]);
    }

    public void AplicarAoSair()
    {
        if (_umBaixou is null || _baixada is null) return;
        try { _umBaixou.WaitExitThenApplyUpdates(_baixada.TargetFullRelease, silent: true, restart: false); }
        catch (Exception ex) { Log.Escrever("atualizador ao sair: " + ex.Message); }
    }

    /// <summary>Volta para a versão anterior (o vigia chama quando a nova cai sem parar). false = não há para onde voltar.</summary>
    public async Task<bool> Reverter()
    {
        if (_um is null || !Instalado) return false;
        var ruim = VersaoAtual;
        SaudeDaVersao.MarcarRuim(ruim);
        try
        {
            var um = Gerente(permitirDescer: true);
            var info = await um.CheckForUpdatesAsync();
            if (info is null || info.TargetFullRelease.Version.ToString() == ruim) return false;
            Log.Escrever($"vigia: voltando da {ruim} para a {info.TargetFullRelease.Version}");
            await um.DownloadUpdatesAsync(info);
            um.ApplyUpdatesAndRestart(info.TargetFullRelease, ["--bandeja", "--revertido=" + ruim]);
            return true;
        }
        catch (Exception ex)
        {
            Log.Escrever("vigia: não consegui voltar de versão: " + ex.Message);
            return false;
        }
    }

    static async Task<IReadOnlyCollection<string>> Recolhidas()
    {
        try
        {
            var json = Directory.Exists(Feed)
                ? await File.ReadAllTextAsync(Path.Combine(Feed, "recolhidas.json"))
                : await Http.GetStringAsync(Feed.TrimEnd('/') + "/recolhidas.json");
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch { return []; }
    }
}

/// <summary>A fonte do Velopack: o espelho da b75 (ou uma pasta, para teste), sem as versões que caíram neste PC.</summary>
sealed class FonteEspelho(string feed, Func<IReadOnlyCollection<string>> ruins) : IUpdateSource
{
    readonly IUpdateSource _fonte = Directory.Exists(feed) ? new SimpleFileSource(new DirectoryInfo(feed)) : new SimpleWebSource(feed);

    public async Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string? appId, string channel,
        Guid? stagingId = null, VelopackAsset? latestLocalRelease = null)
    {
        var lista = await _fonte.GetReleaseFeed(logger, appId, channel, stagingId, latestLocalRelease);
        var fora = ruins();
        return new VelopackAssetFeed { Assets = [.. lista.Assets.Where(a => !fora.Contains(a.Version.ToString()))] };
    }

    public Task DownloadReleaseEntry(IVelopackLogger logger, VelopackAsset releaseEntry, string localFile,
        Action<int> progress, CancellationToken cancelToken = default) =>
        _fonte.DownloadReleaseEntry(logger, releaseEntry, localFile, progress, cancelToken);
}

using System.IO;
using System.Windows;
using Batuta.App.Desenho;
using Batuta.Core;
using Batuta.Motor;
using Microsoft.Win32;

namespace Batuta.App;

/// <summary>O estado do programa: catálogo, configuração, motor e atualizador, com os eventos do motor já na thread da tela.</summary>
public sealed class Servicos : IDisposable
{
    public static Servicos Atual { get; private set; } = null!;

    public Catalogo Catalogo { get; }
    public Configuracao Config { get; }
    public MotorDeEntrada Motor { get; }
    public Atualizador Atualizador { get; } = new();

    public event Action? AparelhosMudaram;
    public event Action<EventoTecla>? TeclaVista;
    public event Action<EventoAcao>? AcaoFeita;
    public event Action<string>? PerfilMudou;
    public event Action? GanchoMudou;
    public event Action? ConfigMudou;

    public string PerfilEmUso { get; private set; } = Perfil.IdPadrao;
    public static string PastaCatalogo => Path.Combine(AppContext.BaseDirectory, "catalogo");

    Servicos()
    {
        Catalogo = Catalogo.Ler(Path.Combine(PastaCatalogo, "modelos.json"));
        Config = Configuracao.Ler(Armazenamento.ArquivoConfig);
        Motor = new MotorDeEntrada();
        var tela = Application.Current.Dispatcher;
        Motor.AparelhosMudaram += _ => tela.BeginInvoke(() => { Conhecer(); AparelhosMudaram?.Invoke(); });
        Motor.TeclaVista += e => tela.BeginInvoke(() => TeclaVista?.Invoke(e));
        Motor.AcaoFeita += e => tela.BeginInvoke(() => AcaoFeita?.Invoke(e));
        Motor.PerfilMudou += p => tela.BeginInvoke(() => { PerfilEmUso = p; PerfilMudou?.Invoke(p); });
        Motor.GanchoMudou += (_, _) => tela.BeginInvoke(() => GanchoMudou?.Invoke());
        Motor.Configurar(Catalogo, Config);
        Conhecer();
        AplicarAutoinicio();
    }

    public static Servicos Iniciar() => Atual = new Servicos();

    public IEnumerable<Modelo> TodosModelos => Catalogo.Modelos.Concat(Config.ModelosAprendidos);
    public Modelo? ModeloPorId(string? id) => id is null ? null : TodosModelos.FirstOrDefault(m => m.Id == id);

    /// <summary>O passador ligado agora (o de verdade antes do simulado), com o modelo dele.</summary>
    public (AparelhoVisto Aparelho, Modelo Modelo)? PassadorConectado =>
        Motor.Conectados.OrderBy(a => a.Simulado).Select(a => (a, m: Motor.ModeloDe(a))).Where(x => x.m is not null)
            .Select(x => ((AparelhoVisto, Modelo)?)(x.a, x.m!)).FirstOrDefault();

    /// <summary>Teclados que o nome entrega como passador, mas que não estão no catálogo nem foram ensinados.</summary>
    public IEnumerable<AparelhoVisto> PassadoresDesconhecidos =>
        Motor.Conectados.Where(a => !a.Simulado && a.NomeDePassador && Motor.ModeloDe(a) is null)
            .GroupBy(a => a.Chave).Select(g => g.First());

    /// <summary>O modelo a mostrar: o do passador ligado ou o último escolhido.</summary>
    public Modelo ModeloEmFoco => PassadorConectado?.Modelo ?? ModeloPorId(Config.UltimoModelo) ?? Catalogo.Modelos[0];

    public DesenhoSvg Desenho(Modelo m)
    {
        if (m.Desenho.StartsWith("generico", StringComparison.Ordinal)) return DesenhoSvg.Ler(DesenhoGenerico.Svg(m));
        var arq = Path.Combine(PastaCatalogo, "desenhos", m.Desenho + ".svg");
        return File.Exists(arq) ? DesenhoSvg.Arquivo(arq) : DesenhoSvg.Ler(DesenhoGenerico.Svg(m));
    }

    /// <summary>Aparelho do catálogo que chegou: anota que ele é aquele modelo (o dono pode trocar depois).</summary>
    void Conhecer()
    {
        var mudou = false;
        foreach (var a in Motor.Conectados.Where(a => !a.Simulado))
        {
            if (Config.Aparelhos.Any(c => c.Chave.Equals(a.Chave, StringComparison.OrdinalIgnoreCase))) continue;
            var m = Catalogo.Reconhecer(a.Vid, a.Pid, a.NomeDePassador);
            if (m is null) continue;
            Config.Aparelhos.Add(new AparelhoConhecido { Chave = a.Chave, ModeloId = m.Id, Apelido = a.Produto });
            Log.Escrever($"aparelho reconhecido: {a.Chave} = {m.NomeCompleto}");
            mudou = true;
        }
        if (mudou) Salvar();
    }

    /// <summary>Este aparelho é aquele modelo (escolha no catálogo ou no modo aprender).</summary>
    public void Associar(string chave, string modeloId)
    {
        var c = Config.Aparelhos.FirstOrDefault(x => x.Chave.Equals(chave, StringComparison.OrdinalIgnoreCase));
        if (c is null) Config.Aparelhos.Add(new AparelhoConhecido { Chave = chave, ModeloId = modeloId });
        else { c.ModeloId = modeloId; c.Ativo = true; }
        Config.UltimoModelo = modeloId;
        Salvar();
    }

    public void Salvar()
    {
        try { Config.Gravar(Armazenamento.ArquivoConfig); }
        catch (Exception ex) { Log.Escrever("não gravei a configuração: " + ex.Message); }
        Motor.Configurar(Catalogo, Config);
        AplicarAutoinicio();
        ConfigMudou?.Invoke();
    }

    /// <summary>Abrir com o Windows (chave Run do usuário, nunca do sistema). Só vale instalado (o atalho estável do Velopack).</summary>
    void AplicarAutoinicio()
    {
        try
        {
            var exe = Environment.ProcessPath ?? "";
            var pasta = Path.GetDirectoryName(exe) ?? "";
            if (!Path.GetFileName(pasta).Equals("current", StringComparison.OrdinalIgnoreCase)) return; // fora do instalador
            var estavel = Path.Combine(Path.GetDirectoryName(pasta)!, "Batuta.exe");
            using var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (run is null) return;
            if (Config.Ajustes.IniciarComWindows) run.SetValue("Batuta", $"\"{estavel}\" --bandeja");
            else if (run.GetValue("Batuta") is not null) run.DeleteValue("Batuta");
        }
        catch (Exception ex) { Log.Escrever("início com o Windows: " + ex.Message); }
    }

    public void Dispose()
    {
        Motor.Dispose();
    }
}

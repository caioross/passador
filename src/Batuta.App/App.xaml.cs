using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Batuta.Core;
using Velopack;

namespace Batuta.App;

public partial class App : Application
{
    const string NomeMutex = @"Local\Batuta.Unico";
    const string NomeEvento = @"Local\Batuta.Mostrar";
    Mutex? _mutex;
    EventWaitHandle? _mostrar;
    Bandeja? _bandeja;
    JanelaPrincipal? _janela;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
#if TESTE_QUEDA
        throw new InvalidOperationException("versão de teste que cai ao abrir (TesteQueda=true)");
#endif
        var args = e.Args;
        var prova = args.Contains("--prova");
        var captura = Array.IndexOf(args, "--capturar") is var i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        if (!prova && captura is null)
        {
            _mutex = new Mutex(true, NomeMutex, out var primeiro);
            _mostrar = new EventWaitHandle(false, EventResetMode.AutoReset, NomeEvento);
            if (!primeiro)
            {
                _mostrar.Set(); // já está aberto: traz a janela
                Vigia.SaidaLimpa();
                Shutdown();
                return;
            }
        }

        var s = Servicos.Iniciar();
        DispatcherUnhandledException += (_, ev) =>
        {
            Log.Escrever("erro inesperado: " + ev.Exception);
            ev.Handled = true;
        };

        if (prova)
        {
            _ = ProvaDoMotor.Rodar(s, args.Contains("--agora")).ContinueWith(t => Dispatcher.Invoke(() =>
            {
                Vigia.SaidaLimpa();
                s.Dispose();
                Shutdown(t.Result ? 0 : 1);
            }));
            return;
        }

        _janela = new JanelaPrincipal(s);
        MainWindow = _janela;
        if (captura is not null)
        {
            _ = Capturas.Rodar(_janela, s, captura).ContinueWith(_ => Dispatcher.Invoke(() => { s.Dispose(); Shutdown(); }));
            return;
        }

        _bandeja = new Bandeja(s, Trazer, Sair);
        if (!args.Contains("--bandeja")) _janela.Show();
        else if (args.Contains("--reaberto")) _bandeja.Avisar("Batuta", "O Batuta tinha fechado sozinho e foi reaberto. O passador continua funcionando.");
        if (args.FirstOrDefault(a => a.StartsWith("--revertido=")) is { } rev)
            _bandeja.Avisar("Batuta", $"A versão {rev["--revertido=".Length..]} caía ao abrir; o Batuta voltou para a anterior.");

        Travamento.Vigiar(Dispatcher);
        var saude = new DispatcherTimer { Interval = SaudeDaVersao.Prazo };
        saude.Tick += (_, _) => { saude.Stop(); SaudeDaVersao.MarcarSaudavel(); };
        saude.Start();

        // Atualização: 30 s depois de abrir e a cada 4 horas
        var atualiza = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        atualiza.Tick += async (_, _) =>
        {
            atualiza.Interval = TimeSpan.FromHours(4);
            if (s.Config.Ajustes.AtualizacaoAutomatica) await s.Atualizador.Verificar();
        };
        atualiza.Start();

        new Thread(() =>
        {
            while (_mostrar!.WaitOne()) Dispatcher.BeginInvoke(Trazer);
        }) { IsBackground = true, Name = "mostrar" }.Start();
    }

    void Trazer()
    {
        if (_janela is null) return;
        if (!_janela.IsVisible) _janela.Show();
        if (_janela.WindowState == WindowState.Minimized) _janela.WindowState = WindowState.Normal;
        _janela.Activate();
        _janela.Topmost = true;
        _janela.Topmost = false;
    }

    public void Sair()
    {
        Vigia.SaidaLimpa();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Vigia.SaidaLimpa();
        Servicos.Atual?.Atualizador.AplicarAoSair();
        _bandeja?.Dispose();
        Servicos.Atual?.Dispose();
        _mutex?.Dispose();
        _mostrar?.Dispose();
        base.OnExit(e);
    }
}

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        if (args.Length >= 2 && args[0] == "--vigia" && int.TryParse(args[1], out var pid))
        {
            Vigia.Rodar(pid);
            return;
        }
        Log.Iniciar(Armazenamento.ArquivoLog);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Escrever("queda: " + e.ExceptionObject);
            Process.GetCurrentProcess().Kill();
        };
        var semVigia = args.Contains("--prova") || args.Contains("--capturar") || Environment.GetEnvironmentVariable("BATUTA_SEM_VIGIA") == "1";
        if (!semVigia) Vigia.Acompanhar();
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}

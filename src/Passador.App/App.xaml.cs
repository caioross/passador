using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Passador.Core;
using Velopack;

namespace Passador.App;

public partial class App : Application
{
    const string NomeMutex = @"Local\Passador.Unico";
    const string NomeEvento = @"Local\Passador.Mostrar";
    const string NomeSair = @"Local\Passador.Sair";
    EventWaitHandle? _sair;
    long _ultimoToque = Environment.TickCount64;
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
            _sair = new EventWaitHandle(false, EventResetMode.AutoReset, NomeSair);
            if (!primeiro)
            {
                if (args.Contains("--sair")) _sair.Set(); // Passador.exe --sair fecha o que está aberto
                else _mostrar.Set(); // já está aberto: traz a janela
                Vigia.SaidaLimpa();
                Shutdown();
                return;
            }
            Migracao.DoBatuta();
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

        _bandeja = new Bandeja(s, Trazer, () => { Trazer(); _janela.Testar(); }, Sair);
        if (!args.Contains("--bandeja")) _janela.Show();
        else if (args.Contains("--reaberto")) _bandeja.Avisar("Passador", "O Passador tinha fechado sozinho e foi reaberto. Os botões continuam funcionando.");
        if (args.FirstOrDefault(a => a.StartsWith("--revertido=")) is { } rev)
            _bandeja.Avisar("Passador", $"A versão {rev["--revertido=".Length..]} caía ao abrir; o Passador voltou para a anterior.");

        Travamento.Vigiar(Dispatcher);
        var saude = new DispatcherTimer { Interval = SaudeDaVersao.Prazo };
        saude.Tick += (_, _) => { saude.Stop(); SaudeDaVersao.MarcarSaudavel(); };
        saude.Start();

        // Atualização: 30 s depois de abrir e a cada 4 horas. O Passador vive na bandeja e quase nunca fecha: a versão
        // baixada entra sozinha quando ninguém mexe no passador há 10 minutos e a janela está fechada (reabre na bandeja).
        s.TeclaVista += _ => _ultimoToque = Environment.TickCount64;
        var atualiza = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        atualiza.Tick += async (_, _) =>
        {
            atualiza.Interval = TimeSpan.FromMinutes(15);
            if (!s.Config.Ajustes.AtualizacaoAutomatica) return;
            if (!s.Atualizador.Pronta && (DateTime.Now - _ultimaProcura).TotalHours >= 4)
            {
                _ultimaProcura = DateTime.Now;
                await s.Atualizador.Verificar();
            }
            if (s.Atualizador.Pronta && Environment.TickCount64 - _ultimoToque > 10 * 60_000 && _janela?.IsVisible != true)
            {
                Log.Escrever($"atualização: instalando a {s.Atualizador.NovaVersao} (passador parado, janela fechada)");
                s.Atualizador.AplicarAgora();
            }
        };
        atualiza.Start();
        new Thread(() =>
        {
            if (_sair!.WaitOne()) Dispatcher.BeginInvoke(Sair);
        }) { IsBackground = true, Name = "sair" }.Start();

        new Thread(() =>
        {
            while (_mostrar!.WaitOne()) Dispatcher.BeginInvoke(Trazer);
        }) { IsBackground = true, Name = "mostrar" }.Start();
    }

    DateTime _ultimaProcura = DateTime.MinValue;

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
        _sair?.Dispose();
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
        // Passador.exe --aparelhos <arquivo.json>: lista os teclados e controles ligados (VID, PID, nome, bateria) e sai.
        // É o que a pessoa manda quando quer pôr um passador novo no catálogo (CONTRIBUTING.md).
        if (args.Length >= 1 && args[0] == "--aparelhos")
        {
            Diagnostico.ListarAparelhos(args.Length >= 2 ? args[1] : null);
            return;
        }
        Log.Iniciar(Armazenamento.ArquivoLog);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Escrever("queda: " + e.ExceptionObject);
            Process.GetCurrentProcess().Kill();
        };
        var semVigia = args.Contains("--prova") || args.Contains("--capturar") || Environment.GetEnvironmentVariable("PASSADOR_SEM_VIGIA") == "1";
        if (!semVigia) Vigia.Acompanhar();
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}

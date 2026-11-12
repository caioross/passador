using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Batuta.Core;
using Batuta.Motor;

namespace Batuta.App;

/// <summary>
/// Prova do motor sem aparelho (Batuta.exe --prova): um passador simulado aperta botões numa janelinha do próprio
/// Batuta e a prova confere o que chegou nela. Passa pelo caminho inteiro: SendInput → Raw Input (com a marca do
/// aparelho) → gestos → ação → gancho WH_KEYBOARD engolindo a tecla original → a tecla nova na janela. E confere que a
/// mesma tecla vinda do "teclado" passa intacta.
///
/// Mexe na tela: abre a janelinha e manda teclas para ela. Só roda se o Windows aceita notificações (ninguém em tela
/// cheia) e o PC está ocioso há 60 s, ou com --agora. Antes de cada tecla confere que a janelinha está na frente;
/// se não estiver, para (nenhuma tecla vai para outro programa). Resultado em prova.json na pasta de dados.
/// </summary>
static class ProvaDoMotor
{
    [DllImport("shell32")] static extern int SHQueryUserNotificationState(out int s);
    [DllImport("user32")] static extern bool GetLastInputInfo(ref LII l);
    [DllImport("user32")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32")] static extern bool SetForegroundWindow(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] struct LII { public uint Tam, Tempo; }

    sealed record Caso(string Nome, bool Passou, string Esperado, string Chegou, long Ms);

    public static async Task<bool> Rodar(Servicos s, bool agora)
    {
        var saida = Path.Combine(Armazenamento.PastaPadrao, "prova.json");
        var casos = new List<Caso>();
        var nota = "";
        try
        {
            SHQueryUserNotificationState(out var estado);
            var l = new LII { Tam = 8 };
            GetLastInputInfo(ref l);
            var ocioso = (Environment.TickCount - (int)l.Tempo) / 1000;
            if (!agora && (estado != 5 || ocioso < 60))
            {
                nota = $"adiada: tela ocupada (estado {estado}, ocioso {ocioso} s)";
                return false;
            }

            // Configuração só da prova (em memória; o arquivo do dono não muda)
            var modelo = s.Catalogo.PorId("exbom-laser")!;
            var cfg = s.Config;
            var padrao = cfg.Perfis.First(p => p.Id == Perfil.IdPadrao);
            padrao.Mapas.Clear();
            padrao.Combinacoes.Clear();
            padrao.Mapas[Perfil.ChavePapel("avancar")] = new MapaBotao { Toque = Acao.DeTecla("Right"), Segurar = Acao.DeTecla("Home") };
            padrao.Mapas[Perfil.ChavePapel("tela-preta")] = new MapaBotao { Duplo = Acao.DeTecla("W") };
            padrao.Mapas[Perfil.ChavePapel("iniciar")] = new MapaBotao { Toque = new Acao { Tipo = TipoAcao.Texto, Texto = "ok" } };
            cfg.Ajustes.MotorLigado = true;
            cfg.Ajustes.Bloqueio = ModoBloqueio.PorAparelho;
            s.Motor.Configurar(s.Catalogo, cfg);
            s.Motor.TestarComo(null);

            var recebidas = new List<string>();
            var caixa = new TextBlock { Margin = new Thickness(20), FontSize = 16, Foreground = System.Windows.Media.Brushes.White, Text = "Prova do motor do Batuta: não mexa por 10 segundos." };
            var janela = new Window
            {
                Title = "Batuta: prova do motor", Width = 460, Height = 140, WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true, Content = caixa, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x20, 0x1B, 0x27)),
                ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false,
            };
            janela.PreviewKeyDown += (_, e) =>
            {
                var k = e.Key == Key.System ? e.SystemKey : e.Key;
                if (k is Key.None or Key.ImeProcessed or Key.DeadCharProcessed || KeyInterop.VirtualKeyFromKey(k) == 0xE7) return; // texto: chega no TextInput
                recebidas.Add(Teclas.Nome((ushort)KeyInterop.VirtualKeyFromKey(k)));
                e.Handled = true;
            };
            janela.PreviewTextInput += (_, e) => { recebidas.Add("texto:" + e.Text); e.Handled = true; };
            janela.Show();
            janela.Activate();
            var hwnd = new WindowInteropHelper(janela).Handle;
            SetForegroundWindow(hwnd);
            for (var i = 0; i < 20 && GetForegroundWindow() != hwnd; i++) await Task.Delay(100);
            if (GetForegroundWindow() != hwnd) { nota = "a janela da prova não veio para a frente (o Windows não deixou)"; janela.Close(); return false; }

            s.Motor.Simular(1, modelo.Id);
            for (var i = 0; i < 30 && s.Motor.Gancho != EstadoGancho.PorAparelho; i++) await Task.Delay(100);
            nota = $"gancho: {s.Motor.Gancho} ({s.Motor.DetalheGancho})";

            bool Frente() => GetForegroundWindow() == hwnd;
            var R = Atalho.Ler;

            async Task Prova(string nome, Func<Task> agir, int esperaMs, string esperado)
            {
                if (!Frente()) { casos.Add(new Caso(nome, false, esperado, "parada: a janela perdeu a frente", 0)); return; }
                recebidas.Clear();
                var t0 = Environment.TickCount64;
                await agir();
                await Task.Delay(esperaMs);
                var chegou = string.Join(" ", recebidas.Where(x => !x.StartsWith("texto:") || esperado.Contains("texto")));
                casos.Add(new Caso(nome, chegou == esperado, esperado, chegou, Environment.TickCount64 - t0));
            }

            async Task Toque(Atalho a, int segurarMs = 40)
            {
                Simulador.Descer(1, a);
                await Task.Delay(segurarMs);
                Simulador.Subir(1, a);
            }

            await Prova("Avançar, um toque: vira Seta direita", () => Toque(R("PageDown")!.Value), 250, "Right");
            await Prova("Avançar, segurar: vira Home (e nada de Seta direita)", () => Toque(R("PageDown")!.Value, 800), 250, "Home");
            await Prova("Voltar, sem nada configurado: o Page Up de fábrica passa uma vez", () => Toque(R("PageUp")!.Value), 250, "PageUp");
            await Prova("Page Down do TECLADO passa intacto", async () =>
            {
                Executor.Mandar([Executor.TeclaCom(0x22, false, 0), Executor.TeclaCom(0x22, true, 0)]);
                await Task.CompletedTask;
            }, 250, "PageDown");
            await Prova("Tela preta, dois toques: vira W", async () =>
            {
                await Toque(R("B")!.Value, 40);
                await Task.Delay(90);
                await Toque(R("B")!.Value, 40);
            }, 450, "W");
            await Prova("Tela preta, um toque: espera os dois toques e manda o B de fábrica", () => Toque(R("B")!.Value), 500, "B");
            await Prova("Iniciar com Shift+F5: digita o texto, sem vazar Shift nem F5", () => Toque(R("Shift+F5")!.Value), 300, "texto:o texto:k");

            s.Motor.Simular(1, null);
            janela.Close();
            return casos.All(c => c.Passou);
        }
        catch (Exception ex)
        {
            nota += " erro: " + ex.Message;
            return false;
        }
        finally
        {
            try
            {
                Directory.CreateDirectory(Armazenamento.PastaPadrao);
                File.WriteAllText(saida, JsonSerializer.Serialize(new { quando = DateTime.Now, nota, passou = casos.Count > 0 && casos.All(c => c.Passou), casos },
                    new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
            }
            catch { }
        }
    }
}

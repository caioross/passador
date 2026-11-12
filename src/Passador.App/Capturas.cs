using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Passador.App.Paginas;
using Passador.Core;
using Passador.Motor;

namespace Passador.App;

/// <summary>
/// Capturas das telas (Passador.exe --capturar &lt;pasta&gt;), sem mexer na tela de ninguém: a janela abre fora da área
/// visível, sem foco, e cada página é renderizada direto para PNG. Usar com PASSADOR_DADOS apontando para uma pasta de
/// demonstração (nada da configuração real aparece).
/// </summary>
static class Capturas
{
    public static async Task Rodar(JanelaPrincipal j, Servicos s, string pasta)
    {
        Directory.CreateDirectory(pasta);
        j.ShowActivated = false;
        j.WindowStartupLocation = WindowStartupLocation.Manual;
        j.Left = -32000;
        j.Top = 0;
        j.Width = 1440;
        j.Height = 900;
        j.ShowInTaskbar = false;
        j.Show();
        await Esperar(900);

        s.Config.UltimoModelo = "exbom-laser";
        var c = j.PaginaDe<PaginaControle>("Controle")!;
        c.Trocar(s.ModeloPorId("exbom-laser")!);
        j.Ir("Controle");
        await Esperar(500);
        c.Demonstrar("powerpoint", "avancar",
        [
            new("SIM", "exbom-laser", "voltar", Gesto.Toque, "Page Up", "powerpoint", null),
            new("SIM", "exbom-laser", "iniciar", Gesto.Duplo, "Shift + F5", "powerpoint", null),
            new("SIM", "exbom-laser", "avancar", Gesto.Toque, "Page Down", "powerpoint", null),
        ], aceso: "avancar");
        await Esperar(700);
        Salvar(j, Path.Combine(pasta, "01-controle.png"));

        c.Trocar(s.ModeloPorId("norwii-n95")!);
        await Esperar(400);
        c.Demonstrar("youtube", "tab",
        [
            new("SIM", "norwii-n95", "baixo", Gesto.Toque, "L", "youtube", null),
            new("SIM", "norwii-n95", "baixo", Gesto.SegurarInicio, "Shift + N", "youtube", null),
        ]);
        await Esperar(700);
        Salvar(j, Path.Combine(pasta, "02-controle-youtube.png"));

        c.Trocar(s.ModeloPorId("anel-5-botoes")!);
        await Esperar(400);
        c.Demonstrar("google-slides", "iniciar", [new("SIM", "anel-5-botoes", "avancar", Gesto.Toque, "Seta direita", "google-slides", null)], aceso: "avancar");
        await Esperar(700);
        Salvar(j, Path.Combine(pasta, "03-controle-anel.png"));

        j.Ir("Catálogo");
        await Esperar(900);
        Salvar(j, Path.Combine(pasta, "04-catalogo.png"));

        j.Ir("Perfis");
        await Esperar(700);
        Salvar(j, Path.Combine(pasta, "05-perfis.png"));

        j.Ir("Aprender");
        await Esperar(400);
        j.PaginaDe<PaginaAprender>("Aprender")!.Demonstrar();
        await Esperar(900);
        Salvar(j, Path.Combine(pasta, "06-aprender.png"));

        j.Ir("Ajustes");
        await Esperar(600);
        Salvar(j, Path.Combine(pasta, "07-ajustes.png"));

        j.SairDeVerdade = true;
        j.Close();
    }

    static async Task Esperar(int ms)
    {
        await Task.Delay(ms);
        await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
    }

    static void Salvar(Window j, string arquivo)
    {
        var raiz = (FrameworkElement)j.Content;
        var w = (int)raiz.ActualWidth;
        var h = (int)raiz.ActualHeight;
        var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(raiz);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        using var f = File.Create(arquivo);
        enc.Save(f);
        Log.Escrever("captura: " + Path.GetFileName(arquivo));
    }
}

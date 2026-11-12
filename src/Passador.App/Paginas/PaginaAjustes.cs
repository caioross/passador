using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Passador.Core;
using static Passador.App.Paginas.Ui;

namespace Passador.App.Paginas;

/// <summary>Tempos dos gestos, como as teclas são trocadas, início com o Windows, atualizações e sobre.</summary>
public sealed class PaginaAjustes : ScrollViewer, IPagina
{
    readonly Servicos _s;
    readonly StackPanel _c = new() { Margin = new Thickness(48, 4, 48, 40), MaxWidth = 860, HorizontalAlignment = HorizontalAlignment.Left };
    readonly TextBlock _status = T("", "Explica");
    readonly TextBlock _gancho = T("", "Explica");
    readonly DispatcherTimer _gravar = new() { Interval = TimeSpan.FromMilliseconds(400) };

    public PaginaAjustes(Servicos s)
    {
        _s = s;
        Content = _c;
        Margin = new Thickness(0, 36, 0, 0);
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _gravar.Tick += (_, _) => { _gravar.Stop(); _s.Salvar(); };
        s.Atualizador.Mudou += () => Dispatcher.BeginInvoke(() => _status.Text = s.Atualizador.Status);
        s.GanchoMudou += () => _gancho.Text = s.Motor.DetalheGancho;
        Montar();
    }

    public void Entrou() => Montar();
    public void Saiu() { }

    void Montar()
    {
        var a = _s.Config.Ajustes;
        _c.Children.Clear();
        _c.Children.Add(T("Ajustes", "Titulo", margem: new Thickness(0, 0, 0, 24)));

        Secao("relogio", "Tempo dos gestos");
        _c.Children.Add(Regua("Dois toques valem se o segundo vier em até", a.DuploToqueMs, 180, 600, v => a.DuploToqueMs = v));
        _c.Children.Add(Regua("Segurar começa depois de", a.SegurarMs, 300, 1500, v => a.SegurarMs = v));
        _c.Children.Add(T("Tempo menor deixa o passador mais ágil; maior evita disparar sem querer.", "Rotulo", 12, "Texto3", new Thickness(0, 0, 0, 30)));

        Secao("setas", "Como o Passador troca as teclas");
        _c.Children.Add(Ficha("Só o passador (recomendado)", "bloqueio", a.Bloqueio == ModoBloqueio.PorAparelho, () => { a.Bloqueio = ModoBloqueio.PorAparelho; Gravar(); }));
        _c.Children.Add(T("O teclado do computador continua igual: só as teclas que vêm do passador mudam.", "Rotulo", 12.5, margem: new Thickness(0, 0, 0, 12)));
        _c.Children.Add(Ficha("Modo simples", "bloqueio", a.Bloqueio == ModoBloqueio.Simples, () => { a.Bloqueio = ModoBloqueio.Simples; Gravar(); }));
        _c.Children.Add(T("Reserva para programas em que o modo recomendado não pega: a tecla configurada muda venha de onde vier, inclusive do teclado.", "Rotulo", 12.5, margem: new Thickness(0, 0, 0, 12)));
        _gancho.Text = _s.Motor.DetalheGancho;
        _gancho.Margin = new Thickness(0, 4, 0, 30);
        _c.Children.Add(_gancho);

        Secao("liga", "Com o Windows");
        _c.Children.Add(Margem(Interruptor("Abrir com o Windows, direto na bandeja", a.IniciarComWindows, v => { a.IniciarComWindows = v; Gravar(); }), 10));
        _c.Children.Add(Margem(Interruptor("Motor ligado (desligado, o passador volta a ser como de fábrica)", a.MotorLigado, v => { a.MotorLigado = v; Gravar(); }), 30));

        Secao("onda", "Atualizações");
        _status.Text = _s.Atualizador.Status;
        _c.Children.Add(_status);
        _c.Children.Add(Margem(Interruptor("Atualizar sozinho (baixa em segundo plano, troca ao reiniciar)", a.AtualizacaoAutomatica, v => { a.AtualizacaoAutomatica = v; Gravar(); }), 10, 12));
        var linha = new WrapPanel { Margin = new Thickness(0, 0, 0, 30) };
        linha.Children.Add(Botao("Procurar agora", "BotaoBase", "onda", async () => await _s.Atualizador.Verificar(manual: true)));
        if (_s.Atualizador.Pronta)
        {
            var aplicar = Botao($"Instalar a {_s.Atualizador.NovaVersao} agora", "BotaoPrimario", "check", _s.Atualizador.AplicarAgora);
            aplicar.Margin = new Thickness(8, 0, 0, 0);
            linha.Children.Add(aplicar);
        }
        _c.Children.Add(linha);

        Secao("pasta", "Sobre");
        _c.Children.Add(T($"Passador {Atualizador.VersaoAtual}. © 2026 Caio Rossi. Todos os direitos reservados.", "Explica"));
        _c.Children.Add(T("Os desenhos dos passadores são nossos, feitos a partir de fotos e fichas dos fabricantes; marcas citadas são dos donos delas.", "Rotulo", 12, "Texto3", new Thickness(0, 4, 0, 10)));
        var sobre = new WrapPanel();
        sobre.Children.Add(Botao("Abrir a pasta de dados", "BotaoFantasma", "pasta", () => Abrir(Armazenamento.PastaPadrao)));
        sobre.Children.Add(Botao("Ver o registro", "BotaoFantasma", "file-text", () => Abrir(Armazenamento.ArquivoLog)));
        _c.Children.Add(sobre);
    }

    static FrameworkElement Margem(FrameworkElement e, double baixo, double cima = 0) { e.Margin = new Thickness(0, cima, 0, baixo); return e; }

    void Secao(string icone, string titulo)
    {
        var ic = Icone.Criar(icone, 18);
        ic.Margin = new Thickness(0, 0, 12, 0);
        System.Windows.Documents.TextElement.SetForeground(ic, P("Texto2"));
        _c.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14), Children = { ic, T(titulo, "Subtitulo") } });
    }

    FrameworkElement Regua(string rotulo, int valor, int min, int max, Action<int> mudou)
    {
        var numero = T($"{valor} ms", tamanho: 15, cor: "Ambar");
        numero.FontFamily = (System.Windows.Media.FontFamily)R("Display");
        numero.Width = 80;
        var s = new Slider { Minimum = min, Maximum = max, Value = valor, Width = 320, VerticalAlignment = VerticalAlignment.Center, SmallChange = 10, LargeChange = 50, IsSnapToTickEnabled = true, TickFrequency = 10 };
        s.ValueChanged += (_, e) => { numero.Text = $"{(int)e.NewValue} ms"; mudou((int)e.NewValue); Gravar(); };
        var g = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var t = T(rotulo, tamanho: 13.5);
        t.VerticalAlignment = VerticalAlignment.Center;
        g.Children.Add(t);
        Grid.SetColumn(s, 1);
        g.Children.Add(s);
        numero.Margin = new Thickness(18, 0, 0, 0);
        Grid.SetColumn(numero, 2);
        g.Children.Add(numero);
        return g;
    }

    void Gravar() { _gravar.Stop(); _gravar.Start(); }

    static void Abrir(string caminho)
    {
        try { Process.Start(new ProcessStartInfo(caminho) { UseShellExecute = true }); }
        catch (Exception ex) { Log.Escrever("abrir: " + ex.Message); }
    }
}

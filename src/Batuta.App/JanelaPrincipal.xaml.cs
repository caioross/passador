using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Batuta.App.Paginas;
using Batuta.Motor;

namespace Batuta.App;

public partial class JanelaPrincipal : Window
{
    readonly Servicos _s;
    readonly Dictionary<string, Func<FrameworkElement>> _fabricas;
    readonly Dictionary<string, FrameworkElement> _paginas = [];
    public bool SairDeVerdade { get; set; }

    public JanelaPrincipal(Servicos s)
    {
        _s = s;
        InitializeComponent();
        _fabricas = new()
        {
            ["Controle"] = () => new PaginaControle(s, this),
            ["Catálogo"] = () => new PaginaCatalogo(s, this),
            ["Perfis"] = () => new PaginaPerfis(s),
            ["Aprender"] = () => new PaginaAprender(s, this),
            ["Ajustes"] = () => new PaginaAjustes(s),
        };
        s.GanchoMudou += AtualizarMotor;
        s.ConfigMudou += AtualizarMotor;
        s.AparelhosMudaram += AtualizarMotor;
        StateChanged += (_, _) => Raiz.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
        Loaded += (_, _) => { Ir("Controle"); AtualizarMotor(); };
    }

    public string PaginaAtual { get; private set; } = "";

    public void Ir(string nome)
    {
        var nav = nome switch
        {
            "Catálogo" => NavCatalogo, "Perfis" => NavPerfis, "Aprender" => NavAprender, "Ajustes" => NavAjustes, _ => NavControle,
        };
        if (nav.IsChecked != true) { nav.IsChecked = true; return; }
        Mostrar(nome);
    }

    void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || sender is not RadioButton r) return;
        Mostrar((string)r.Tag);
    }

    void Mostrar(string nome)
    {
        if (PaginaAtual == nome) return;
        if (_paginas.GetValueOrDefault(PaginaAtual) is IPagina antiga) antiga.Saiu();
        PaginaAtual = nome;
        if (!_paginas.TryGetValue(nome, out var p)) _paginas[nome] = p = _fabricas[nome]();
        Pagina.Content = p;
        if (p is IPagina nova) nova.Entrou();
        // Entrada curta: a página sobe 8 px e aparece (só a página que mudou, nada em lote)
        p.Opacity = 0;
        var t = new TranslateTransform(0, 8);
        p.RenderTransform = t;
        var dur = new Duration(TimeSpan.FromMilliseconds(220));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        p.BeginAnimation(OpacityProperty, new DoubleAnimation(1, dur) { EasingFunction = ease });
        t.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, dur) { EasingFunction = ease });
    }

    public T? PaginaDe<T>(string nome) where T : FrameworkElement
    {
        if (!_paginas.TryGetValue(nome, out var p)) _paginas[nome] = p = _fabricas[nome]();
        return p as T;
    }

    void AtualizarMotor()
    {
        var (cor, texto, dica) = _s.Motor.Gancho switch
        {
            EstadoGancho.PorAparelho => ((SolidColorBrush)FindResource("Ok"), "Ligado", _s.Motor.DetalheGancho),
            EstadoGancho.Simples => ((SolidColorBrush)FindResource("Ambar"), "Simples", _s.Motor.DetalheGancho),
            _ when !_s.Config.Ajustes.MotorLigado => ((SolidColorBrush)FindResource("Erro"), "Desligado", "Motor desligado. Clique para ligar."),
            _ => ((SolidColorBrush)FindResource("Texto3"), "Esperando", _s.Motor.DetalheGancho),
        };
        MotorPonto.Fill = cor;
        MotorHalo.Fill = cor;
        MotorTexto.Text = texto;
        Motor.ToolTip = dica;
    }

    void Motor_Clique(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _s.Config.Ajustes.MotorLigado = !_s.Config.Ajustes.MotorLigado;
        _s.Salvar();
    }

    void Minimizar_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    void Maximizar_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    void Fechar_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        // Fechar a janela não para o motor: o Batuta segue na bandeja.
        if (!SairDeVerdade)
        {
            e.Cancel = true;
            if (_paginas.GetValueOrDefault(PaginaAtual) is IPagina p) p.Saiu();
            PaginaAtual = "";
            Hide();
            NavControle.IsChecked = true;
        }
        base.OnClosing(e);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        if (PaginaAtual == "" && IsVisible) Mostrar(NavControle.IsChecked == true ? "Controle" : "Controle");
    }
}

/// <summary>Página que precisa saber quando aparece e quando sai (modo aprender, modo testar).</summary>
public interface IPagina
{
    void Entrou();
    void Saiu();
}

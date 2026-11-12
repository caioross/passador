using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Passador.Core;

namespace Passador.App.Desenho;

/// <summary>
/// O passador sob o refletor: o desenho grande no centro, cada botão clicável, as legendas dos lados ligadas ao
/// botão por uma linha fina, e o botão que acabou de ser apertado acende. É a peça central do Passador.
/// </summary>
public sealed class Palco : Grid
{
    static readonly Color Ambar = Color.FromRgb(0xF4, 0xA9, 0x40);

    readonly Canvas _luz = new() { IsHitTestVisible = false };
    readonly Viewbox _caixa = new() { Stretch = Stretch.Uniform };
    readonly Grid _desenho = new();
    readonly Canvas _botoes = new();
    readonly Canvas _legendas = new();
    readonly Canvas _linhas = new() { IsHitTestVisible = false };
    readonly Canvas _frente = new() { IsHitTestVisible = false };
    readonly Dictionary<string, Path> _formas = [];
    readonly Dictionary<string, Path> _brilhos = [];
    readonly Dictionary<string, FrameworkElement> _rotulos = [];
    Modelo? _modelo;
    string? _selecionado;
    string? _apertado;

    /// <summary>Clique num botão (ou na legenda dele).</summary>
    public event Action<string>? BotaoEscolhido;
    /// <summary>No modo testar: apertou (true) e soltou (false) no desenho.</summary>
    public event Action<string, bool>? BotaoTestado;

    public bool ModoTeste { get; set; }
    public bool MostrarLegendas { get; set; } = true;
    /// <summary>Texto da legenda de cada botão: (nome, o que faz agora).</summary>
    public Func<Botao, (string Nome, string Faz)>? Legenda { get; set; }
    /// <summary>Altura máxima do desenho (o resto é palco).</summary>
    public double Ocupacao { get; set; } = 0.86;

    public Palco()
    {
        ClipToBounds = true;
        _desenho.Children.Add(new Image());
        _desenho.Children.Add(_botoes);
        _caixa.Child = _desenho;
        _caixa.VerticalAlignment = VerticalAlignment.Center;
        _caixa.HorizontalAlignment = HorizontalAlignment.Center;
        Children.Add(_luz);
        Children.Add(_linhas);   // as linhas passam POR TRÁS do aparelho: nunca riscam um botão
        Children.Add(_caixa);
        Children.Add(_frente);   // a luz do refletor batendo no topo da peça
        Children.Add(_legendas);
        SizeChanged += (_, _) => Dispatcher.BeginInvoke(Recompor, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    public Modelo? Modelo => _modelo;

    public void Mostrar(Modelo m, DesenhoSvg d)
    {
        _modelo = m;
        _formas.Clear();
        _brilhos.Clear();
        _botoes.Children.Clear();
        var img = (Image)_desenho.Children[0];
        img.Source = new DrawingImage(d.Corpo);
        img.Width = d.Caixa.Width;
        img.Height = d.Caixa.Height;
        _desenho.Width = d.Caixa.Width;
        _desenho.Height = d.Caixa.Height;
        _botoes.Width = d.Caixa.Width;
        _botoes.Height = d.Caixa.Height;
        foreach (var (id, geo) in d.Botoes)
        {
            var b = m.BotaoPorId(id);
            var brilho = new Path
            {
                Data = geo, Fill = new SolidColorBrush(Color.FromArgb(0x48, 0xFF, 0xEC, 0xCC)), Opacity = 0, IsHitTestVisible = false,
                Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0xF1, 0xDC)), StrokeThickness = 1.6,
                Effect = new DropShadowEffect { Color = Color.FromRgb(0xFF, 0xC9, 0x7A), BlurRadius = 30, ShadowDepth = 0, Opacity = 1 },
            };
            var forma = new Path
            {
                Data = geo, Fill = Brushes.Transparent, Cursor = Cursors.Hand, StrokeThickness = 2.2, Tag = id,
                ToolTip = b is null ? id : b.SoHardware ?? $"{b.Nome}: manda {string.Join(" ou ", b.Atalhos.Select(a => a.Rotulo()))}",
            };
            forma.MouseEnter += (_, _) => Pintar(id);
            forma.MouseLeave += (_, _) => Pintar(id);
            forma.MouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;
                if (ModoTeste && b is { Configuravel: true })
                {
                    forma.CaptureMouse();
                    _apertado = id;
                    BotaoTestado?.Invoke(id, true);
                }
                else BotaoEscolhido?.Invoke(id);
            };
            forma.MouseLeftButtonUp += (_, e) =>
            {
                if (_apertado != id) return;
                forma.ReleaseMouseCapture();
                _apertado = null;
                BotaoTestado?.Invoke(id, false);
                e.Handled = true;
            };
            _botoes.Children.Add(brilho);
            _botoes.Children.Add(forma);
            _formas[id] = forma;
            _brilhos[id] = brilho;
        }
        foreach (var id in _formas.Keys) Pintar(id);
        Dispatcher.BeginInvoke(Recompor, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    public string? Selecionado
    {
        get => _selecionado;
        set
        {
            var antigo = _selecionado;
            _selecionado = value;
            if (antigo is not null) Pintar(antigo);
            if (value is not null) Pintar(value);
            AtualizarLegendas();
        }
    }

    void Pintar(string id)
    {
        if (!_formas.TryGetValue(id, out var f)) return;
        var sel = id == _selecionado;
        var hover = f.IsMouseOver;
        f.Stroke = sel ? new SolidColorBrush(Ambar) : hover ? new SolidColorBrush(Color.FromArgb(0x90, 0xFF, 0xFF, 0xFF)) : null;
        f.Fill = hover && !sel ? new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF)) : Brushes.Transparent;
        f.Effect = sel ? new DropShadowEffect { Color = Ambar, BlurRadius = 18, ShadowDepth = 0, Opacity = 0.9 } : null;
    }

    /// <summary>O botão foi apertado (no aparelho ou no teste): acende e apaga devagar.</summary>
    public void Acender(string id, bool segurando = false)
    {
        if (!_brilhos.TryGetValue(id, out var b)) return;
        b.BeginAnimation(OpacityProperty, null);
        if (segurando) { b.Opacity = 0.95; return; }
        var a = new DoubleAnimationUsingKeyFrames();
        a.KeyFrames.Add(new LinearDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(40))));
        a.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(900)), new CubicEase { EasingMode = EasingMode.EaseOut }));
        b.BeginAnimation(OpacityProperty, a);
    }

    /// <summary>Soltou depois de segurar: apaga devagar.</summary>
    public void Apagar(string id)
    {
        if (!_brilhos.TryGetValue(id, out var b)) return;
        b.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(500)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    void Recompor()
    {
        if (ActualWidth < 10 || _modelo is null) return;
        // O desenho ocupa a altura útil; a largura sobra para as legendas.
        var alturaUtil = ActualHeight * Ocupacao;
        _caixa.Height = alturaUtil;
        _caixa.Width = double.NaN;
        _caixa.MaxWidth = MostrarLegendas ? Math.Max(160, ActualWidth - 2 * 250) : ActualWidth;
        UpdateLayout();
        DesenharLuz();
        AtualizarLegendas();
    }

    void DesenharLuz()
    {
        _luz.Children.Clear();
        if (_caixa.ActualWidth < 1) return;
        var r = _caixa.TransformToAncestor(this).TransformBounds(new Rect(0, 0, _caixa.ActualWidth, _caixa.ActualHeight));
        var cx = r.X + r.Width / 2;
        // O facho do refletor, de cima, e a poça de luz em volta do aparelho
        var facho = new Polygon
        {
            Points = [new Point(cx - 50, 0), new Point(cx + 50, 0), new Point(cx + r.Height * 0.55, ActualHeight), new Point(cx - r.Height * 0.55, ActualHeight)],
            Fill = new LinearGradientBrush(
                [new GradientStop(Color.FromArgb(0x1C, Ambar.R, Ambar.G, Ambar.B), 0), new GradientStop(Color.FromArgb(0x00, Ambar.R, Ambar.G, Ambar.B), 1)],
                new Point(0, 0), new Point(0, 1)),
            Effect = new BlurEffect { Radius = 40 },
            OpacityMask = new LinearGradientBrush([new GradientStop(Colors.Transparent, 0), new GradientStop(Colors.Black, 0.22)], new Point(0, 0), new Point(0, 1)),
        };
        _luz.Children.Add(facho);
        _frente.Children.Clear();
        var topo = new Ellipse
        {
            Width = r.Width * 0.9, Height = Math.Max(60, r.Height * 0.22),
            Fill = new RadialGradientBrush([new GradientStop(Color.FromArgb(0x26, 0xFF, 0xC9, 0x7A), 0), new GradientStop(Colors.Transparent, 1)]),
        };
        Canvas.SetLeft(topo, cx - topo.Width / 2);
        Canvas.SetTop(topo, r.Y - topo.Height * 0.15);
        _frente.Children.Add(topo);
        var poca = new Ellipse
        {
            Width = r.Height * 1.05, Height = r.Height * 1.05,
            Fill = new RadialGradientBrush([new GradientStop(Color.FromArgb(0x30, Ambar.R, Ambar.G, Ambar.B), 0), new GradientStop(Color.FromArgb(0x10, 0x6C, 0x52, 0xBE), 0.55), new GradientStop(Colors.Transparent, 1)]),
        };
        Canvas.SetLeft(poca, cx - poca.Width / 2);
        Canvas.SetTop(poca, r.Y + r.Height / 2 - poca.Height / 2);
        _luz.Children.Add(poca);
        var chao = new Ellipse
        {
            Width = Math.Max(120, r.Width * 0.9), Height = 22,
            Fill = new RadialGradientBrush([new GradientStop(Color.FromArgb(0xC0, 0, 0, 0), 0), new GradientStop(Color.FromArgb(0x70, 0, 0, 0), 0.4),
                new GradientStop(Color.FromArgb(0x20, 0, 0, 0), 0.75), new GradientStop(Colors.Transparent, 1)]),
            Effect = new BlurEffect { Radius = 6 },
        };
        Canvas.SetLeft(chao, cx - chao.Width / 2);
        Canvas.SetTop(chao, r.Bottom - 22);
        _luz.Children.Add(chao);
    }

    public void AtualizarLegendas()
    {
        _legendas.Children.Clear();
        _linhas.Children.Clear();
        _rotulos.Clear();
        if (!MostrarLegendas || _modelo is null || Legenda is null || _caixa.ActualWidth < 1) return;
        var caixa = _caixa.TransformToAncestor(this).TransformBounds(new Rect(0, 0, _caixa.ActualWidth, _caixa.ActualHeight));
        var itens = new List<(Botao b, Point p, Rect br)>();
        foreach (var (id, f) in _formas)
        {
            var b = _modelo.BotaoPorId(id);
            if (b is null) continue;
            var br = f.TransformToAncestor(this).TransformBounds(f.Data.Bounds);
            itens.Add((b, new Point(br.X + br.Width / 2, br.Y + br.Height / 2), br));
        }
        if (itens.Count == 0) return;
        var meio = caixa.X + caixa.Width / 2;
        // Lado de cada legenda: pelo lado do botão; botões no eixo se dividem para equilibrar.
        var esquerda = new List<(Botao, Point, Rect)>();
        var direita = new List<(Botao, Point, Rect)>();
        foreach (var it in itens.OrderBy(i => i.p.Y))
        {
            var d = it.p.X - meio;
            if (Math.Abs(d) > caixa.Width * 0.12) (d < 0 ? esquerda : direita).Add(it);
            else (esquerda.Count < direita.Count ? esquerda : direita).Add(it);
        }
        Colocar(esquerda, caixa, true);
        Colocar(direita, caixa, false);
    }

    void Colocar(List<(Botao b, Point p, Rect br)> lado, Rect caixa, bool esquerda)
    {
        const double alturaRotulo = 44, folga = 10;
        // Espaço que sobra do lado: a legenda encolhe antes de invadir o desenho
        var espaco = esquerda ? caixa.Left - 8 : ActualWidth - caixa.Right - 8;
        var largura = Math.Clamp(espaco - 44, 96, 210);
        var distancia = Math.Max(24, Math.Min(48, espaco - largura));
        var ys = new List<double>();
        foreach (var (_, p, _) in lado)
        {
            var y = p.Y - alturaRotulo / 2;
            if (ys.Count > 0) y = Math.Max(y, ys[^1] + alturaRotulo + folga);
            ys.Add(y);
        }
        // Se passou do fundo, sobe tudo o que for preciso
        var excesso = ys.Count > 0 ? ys[^1] + alturaRotulo - (ActualHeight - 8) : 0;
        for (var i = ys.Count - 1; i >= 0 && excesso > 0; i--)
        {
            ys[i] -= excesso;
            if (i > 0 && ys[i - 1] + alturaRotulo + folga > ys[i]) excesso = ys[i - 1] + alturaRotulo + folga - ys[i];
            else excesso = 0;
        }
        var xRotulo = esquerda ? caixa.Left - distancia - largura : caixa.Right + distancia;
        var cotovelo = esquerda ? caixa.Left - distancia * 0.4 : caixa.Right + distancia * 0.4;
        for (var i = 0; i < lado.Count; i++)
        {
            var (b, p, br) = lado[i];
            var (nome, faz) = Legenda!(b);
            var sel = b.Id == _selecionado;
            var yMeio = ys[i] + alturaRotulo / 2;
            // A linha sai do centro do botão em reta até o cotovelo (por trás do aparelho: só aparece fora dele) e segue
            // reta até a legenda. O ponto em cima do botão diz de qual botão é cada linha.
            var inicio = p;
            var fim = new Point(esquerda ? xRotulo + largura + 6 : xRotulo - 6, yMeio);
            var cor = sel ? Color.FromArgb(0xE0, Ambar.R, Ambar.G, Ambar.B) : Color.FromArgb(0x58, 0xFF, 0xFF, 0xFF);
            var linha = new Polyline
            {
                Points = [inicio, new Point(cotovelo, yMeio), fim],
                Stroke = new SolidColorBrush(cor), StrokeThickness = sel ? 1.4 : 1, IsHitTestVisible = false,
            };
            _linhas.Children.Add(linha);
            var ponto = new Ellipse
            {
                Width = 6, Height = 6, Fill = new SolidColorBrush(sel ? Ambar : Color.FromRgb(0xEE, 0xE9, 0xF3)), IsHitTestVisible = false,
                Stroke = new SolidColorBrush(Color.FromArgb(0xA0, 0, 0, 0)), StrokeThickness = 1,
            };
            var borda = new Point(esquerda ? br.Left + Math.Min(8, br.Width / 4) : br.Right - Math.Min(8, br.Width / 4), p.Y);
            Canvas.SetLeft(ponto, borda.X - 3);
            Canvas.SetTop(ponto, borda.Y - 3);
            _legendas.Children.Add(ponto);
            linha.Points[0] = borda;

            var t1 = new TextBlock
            {
                Text = nome, FontWeight = FontWeights.Bold, FontSize = 13.5, TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = sel ? new SolidColorBrush(Ambar) : (Brush)FindResource("Texto"),
                TextAlignment = esquerda ? TextAlignment.Right : TextAlignment.Left,
            };
            var t2 = new TextBlock
            {
                Text = faz, FontSize = 12.5, Margin = new Thickness(0, 2, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = (Brush)FindResource(b.Configuravel ? "Texto2" : "Texto3"),
                TextAlignment = esquerda ? TextAlignment.Right : TextAlignment.Left,
            };
            var r = new StackPanel { Width = largura, Children = { t1, t2 }, Background = Brushes.Transparent, Cursor = Cursors.Hand };
            var id = b.Id;
            r.MouseLeftButtonDown += (_, e) => { e.Handled = true; BotaoEscolhido?.Invoke(id); };
            Canvas.SetLeft(r, xRotulo);
            Canvas.SetTop(r, ys[i] + 4);
            _legendas.Children.Add(r);
            _rotulos[id] = r;
        }
    }
}

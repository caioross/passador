using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Batuta.Core;
using Microsoft.Win32;

namespace Batuta.App.Paginas;

/// <summary>Escolher o que um gesto faz: tecla ou atalho, texto, sequência, abrir, mouse, nada, ou deixar como está.</summary>
public sealed class EditorDeAcao : StackPanel
{
    sealed record Opcao(string Rotulo, TipoAcao? Tipo, string Icone);

    readonly ComboBox _tipo = new();
    readonly StackPanel _detalhe = new() { Margin = new Thickness(0, 10, 0, 0) };
    readonly string _rotuloVazio;
    Acao? _acao;
    bool _montando;

    /// <summary>Mudou (nulo = "deixar como está": herda do Padrão ou segue de fábrica).</summary>
    public event Action<Acao?>? Mudou;

    public static readonly string[] TeclasComuns =
    [
        "PageDown", "PageUp", "Right", "Left", "Down", "Up", "F5", "Shift+F5", "Ctrl+F5", "Esc", "B", "W", "Ponto", "Space", "Enter",
        "Tab", "Alt+Tab", "Win+Tab", "Home", "End", "Ctrl+L", "Ctrl+P", "VolumeUp", "VolumeDown", "VolumeMute", "MediaPlayPause",
        "MediaNext", "MediaPrev", "F13", "F14", "F15", "F16", "F17", "F18", "Ctrl+Z", "Ctrl+Mais", "Ctrl+Menos", "Win+D",
    ];

    public EditorDeAcao(string rotuloVazio)
    {
        _rotuloVazio = rotuloVazio;
        var opcoes = new List<Opcao>
        {
            new(rotuloVazio, null, "fabrica"),
            new("Tecla ou atalho", TipoAcao.Tecla, "teclado"),
            new("Sequência de passos", TipoAcao.Sequencia, "sequencia"),
            new("Digitar um texto", TipoAcao.Texto, "texto"),
            new("Abrir programa, arquivo ou site", TipoAcao.Abrir, "abrir"),
            new("Clique ou rolagem do mouse", TipoAcao.Mouse, "mouse"),
            new("A tecla de fábrica do botão", TipoAcao.Original, "fabrica"),
            new("Não fazer nada", TipoAcao.Nada, "nada"),
        };
        _tipo.ItemsSource = opcoes;
        _tipo.ItemTemplate = Modelo();
        _tipo.SelectionChanged += (_, _) =>
        {
            if (_montando) return;
            var o = (Opcao)_tipo.SelectedItem;
            _acao = o.Tipo is null ? null : Novo(o.Tipo.Value);
            MontarDetalhe();
            Mudou?.Invoke(_acao);
        };
        Children.Add(_tipo);
        Children.Add(_detalhe);
    }

    static DataTemplate Modelo()
    {
        var f = new FrameworkElementFactory(typeof(StackPanel));
        f.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        var ic = new FrameworkElementFactory(typeof(Icone));
        ic.SetBinding(Icone.NomeProperty, new System.Windows.Data.Binding("Icone"));
        ic.SetValue(WidthProperty, 15.0);
        ic.SetValue(HeightProperty, 15.0);
        ic.SetValue(MarginProperty, new Thickness(0, 0, 10, 0));
        ic.SetValue(TextElement.ForegroundProperty, Application.Current.FindResource("Texto2"));
        var t = new FrameworkElementFactory(typeof(TextBlock));
        t.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Rotulo"));
        f.AppendChild(ic);
        f.AppendChild(t);
        return new DataTemplate { VisualTree = f };
    }

    static Acao Novo(TipoAcao t) => t switch
    {
        TipoAcao.Tecla => new Acao { Tipo = t, Tecla = "Right" },
        TipoAcao.Sequencia => new Acao { Tipo = t, Passos = [new Passo { Tipo = TipoPasso.Tecla, Valor = "Right" }] },
        TipoAcao.Texto => new Acao { Tipo = t, Texto = "" },
        TipoAcao.Abrir => new Acao { Tipo = t, Caminho = "" },
        TipoAcao.Mouse => new Acao { Tipo = t, Mouse = "clique-esquerdo" },
        _ => new Acao { Tipo = t },
    };

    public void Definir(Acao? acao)
    {
        _montando = true;
        _acao = acao is null ? null : System.Text.Json.JsonSerializer.Deserialize<Acao>(System.Text.Json.JsonSerializer.Serialize(acao, Catalogo.Json), Catalogo.Json);
        var lista = (List<Opcao>)_tipo.ItemsSource;
        _tipo.SelectedItem = lista.First(o => o.Tipo == _acao?.Tipo);
        _montando = false;
        MontarDetalhe();
    }

    void Avisar() => Mudou?.Invoke(_acao);

    void MontarDetalhe()
    {
        _detalhe.Children.Clear();
        var a = _acao;
        if (a is null) { _detalhe.Visibility = Visibility.Collapsed; return; }
        _detalhe.Visibility = Visibility.Visible;
        switch (a.Tipo)
        {
            case TipoAcao.Tecla:
                _detalhe.Children.Add(new CapturaDeAtalho(a.Tecla, v => { a.Tecla = v; Avisar(); }));
                break;
            case TipoAcao.Texto:
                var tb = new TextBox { Text = a.Texto ?? "", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 64, VerticalContentAlignment = VerticalAlignment.Top };
                tb.TextChanged += (_, _) => { a.Texto = tb.Text; Avisar(); };
                _detalhe.Children.Add(tb);
                break;
            case TipoAcao.Abrir:
                var caminho = new TextBox { Text = a.Caminho ?? "" };
                caminho.TextChanged += (_, _) => { a.Caminho = caminho.Text; Avisar(); };
                var procurar = new Button { Content = "Procurar…", Style = (Style)FindResource("BotaoBase"), Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(12, 7, 12, 7) };
                procurar.Click += (_, _) =>
                {
                    var d = new OpenFileDialog { Title = "O que o botão abre", Filter = "Programas e arquivos|*.*" };
                    if (d.ShowDialog() == true) caminho.Text = d.FileName;
                };
                var linha = new DockPanel();
                DockPanel.SetDock(procurar, Dock.Right);
                linha.Children.Add(procurar);
                linha.Children.Add(caminho);
                _detalhe.Children.Add(linha);
                _detalhe.Children.Add(Dica("Um programa (.exe), um arquivo ou um endereço como https://…"));
                break;
            case TipoAcao.Mouse:
                var cm = new ComboBox { ItemsSource = Acao.GestosDeMouse, DisplayMemberPath = "Value", SelectedValuePath = "Key", SelectedValue = a.Mouse };
                cm.SelectionChanged += (_, _) => { a.Mouse = (string?)cm.SelectedValue; Avisar(); };
                _detalhe.Children.Add(cm);
                break;
            case TipoAcao.Sequencia:
                MontarSequencia(a);
                break;
            case TipoAcao.Original:
                _detalhe.Children.Add(Dica("Manda a tecla que o passador manda sozinho."));
                break;
            case TipoAcao.Nada:
                _detalhe.Children.Add(Dica("O botão fica mudo neste gesto."));
                break;
        }
    }

    static TextBlock Dica(string t) => new()
    {
        Text = t, Margin = new Thickness(2, 6, 0, 0), FontSize = 12.5, TextWrapping = TextWrapping.Wrap,
        Foreground = (Brush)Application.Current.FindResource("Texto3"),
    };

    void MontarSequencia(Acao a)
    {
        a.Passos ??= [];
        var lista = new StackPanel();
        for (var i = 0; i < a.Passos.Count; i++)
        {
            var p = a.Passos[i];
            var idx = i;
            var linha = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            var num = new TextBlock { Text = (i + 1).ToString(), Width = 18, VerticalAlignment = VerticalAlignment.Center, Foreground = (Brush)FindResource("Texto3"), FontSize = 12.5 };
            DockPanel.SetDock(num, Dock.Left);
            var tirar = new Button { Style = (Style)FindResource("BotaoFantasma"), Content = Icone.Criar("fechar", 13), ToolTip = "Tirar este passo", Padding = new Thickness(6) };
            tirar.Click += (_, _) => { a.Passos.RemoveAt(idx); MontarDetalhe(); Avisar(); };
            DockPanel.SetDock(tirar, Dock.Right);
            linha.Children.Add(num);
            linha.Children.Add(tirar);
            FrameworkElement campo = p.Tipo switch
            {
                TipoPasso.Tecla => new CapturaDeAtalho(p.Valor, v => { p.Valor = v ?? ""; Avisar(); }, compacto: true),
                TipoPasso.Espera => CampoTexto(p, "milissegundos", true),
                TipoPasso.Mouse => CampoMouse(p),
                _ => CampoTexto(p, p.Tipo == TipoPasso.Abrir ? "programa, arquivo ou site" : "texto", false),
            };
            linha.Children.Add(campo);
            lista.Children.Add(linha);
        }
        _detalhe.Children.Add(lista);
        var botoes = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
        foreach (var (rot, tipo, valor) in new[] { ("Tecla", TipoPasso.Tecla, "Right"), ("Texto", TipoPasso.Texto, ""), ("Espera", TipoPasso.Espera, "300"), ("Abrir", TipoPasso.Abrir, ""), ("Mouse", TipoPasso.Mouse, "clique-esquerdo") })
        {
            var b = new Button
            {
                Style = (Style)FindResource("BotaoFantasma"), Margin = new Thickness(0, 0, 4, 0),
                Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { Icone.Criar("mais", 13), new TextBlock { Text = rot, Margin = new Thickness(5, 0, 0, 0), Foreground = (Brush)FindResource("Texto2") } } },
            };
            b.Click += (_, _) => { a.Passos.Add(new Passo { Tipo = tipo, Valor = valor }); MontarDetalhe(); Avisar(); };
            botoes.Children.Add(b);
        }
        _detalhe.Children.Add(botoes);
    }

    TextBox CampoTexto(Passo p, string dica, bool numero)
    {
        var t = new TextBox { Text = p.Valor, ToolTip = dica };
        t.TextChanged += (_, _) =>
        {
            if (numero && !int.TryParse(t.Text, out _)) return;
            p.Valor = t.Text;
            Avisar();
        };
        return t;
    }

    ComboBox CampoMouse(Passo p)
    {
        var c = new ComboBox { ItemsSource = Acao.GestosDeMouse, DisplayMemberPath = "Value", SelectedValuePath = "Key", SelectedValue = p.Valor };
        c.SelectionChanged += (_, _) => { p.Valor = (string?)c.SelectedValue ?? ""; Avisar(); };
        return c;
    }
}

/// <summary>Campo que grava um atalho: clique e aperte as teclas (ou escolha na lista as que o teclado não tem).</summary>
public sealed class CapturaDeAtalho : DockPanel
{
    readonly Border _caixa;
    readonly WrapPanel _teclas = new() { VerticalAlignment = VerticalAlignment.Center };
    readonly Action<string?> _mudou;
    string? _valor;
    bool _ouvindo;

    public CapturaDeAtalho(string? valor, Action<string?> mudou, bool compacto = false)
    {
        _valor = valor;
        _mudou = mudou;
        var lista = new ComboBox
        {
            Width = compacto ? 58 : 132, ToolTip = "Teclas que o teclado não tem (F13, volume, mídia) e as mais usadas",
            ItemsSource = new[] { new KeyValuePair<string, string>("", compacto ? "…" : "Mais teclas") }
                .Concat(EditorDeAcao.TeclasComuns.Select(t => new KeyValuePair<string, string>(t, Atalho.Ler(t)?.Rotulo() ?? t))).ToList(),
            DisplayMemberPath = "Value", SelectedValuePath = "Key", SelectedIndex = 0,
        };
        lista.SelectionChanged += (_, _) =>
        {
            if (lista.SelectedValue is string { Length: > 0 } s) { Definir(s); lista.SelectedIndex = 0; }
        };
        DockPanel.SetDock(lista, Dock.Right);
        lista.Margin = new Thickness(8, 0, 0, 0);
        _caixa = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x16, 0x13, 0x1B)), BorderBrush = (Brush)Application.Current.FindResource("Borda"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(8, 6, 8, 2), MinHeight = 38,
            Focusable = true, Cursor = Cursors.Hand, Child = _teclas, ToolTip = "Clique e aperte o atalho no teclado",
        };
        _caixa.MouseLeftButtonDown += (_, e) => { _caixa.Focus(); e.Handled = true; };
        _caixa.GotKeyboardFocus += (_, _) => { _ouvindo = true; Desenhar(); };
        _caixa.LostKeyboardFocus += (_, _) => { _ouvindo = false; Desenhar(); };
        _caixa.PreviewKeyDown += Tecla;
        Children.Add(lista);
        Children.Add(_caixa);
        Desenhar();
    }

    void Definir(string? v)
    {
        _valor = v;
        Desenhar();
        _mudou(v);
    }

    void Tecla(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var k = e.Key == Key.System ? e.SystemKey : e.Key;
        if (k is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin) return;
        var vk = (ushort)KeyInterop.VirtualKeyFromKey(k);
        var mods = Mod.Nenhum;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) mods |= Mod.Ctrl;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) mods |= Mod.Shift;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) mods |= Mod.Alt;
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)) mods |= Mod.Win;
        Definir(new Atalho(mods, vk).ToString());
        Keyboard.ClearFocus();
    }

    void Desenhar()
    {
        _teclas.Children.Clear();
        _caixa.BorderBrush = (Brush)Application.Current.FindResource(_ouvindo ? "Ambar" : "Borda");
        if (_ouvindo)
        {
            _teclas.Children.Add(new TextBlock { Text = "Aperte o atalho…", Foreground = (Brush)Application.Current.FindResource("Ambar"), Margin = new Thickness(2, 2, 0, 6) });
            return;
        }
        var a = Atalho.Ler(_valor);
        if (a is null)
        {
            _teclas.Children.Add(new TextBlock { Text = "Clique e aperte", Foreground = (Brush)Application.Current.FindResource("Texto3"), Margin = new Thickness(2, 2, 0, 6) });
            return;
        }
        foreach (var parte in a.Value.Rotulo().Split(" + ")) _teclas.Children.Add(Teclinha(parte));
    }

    public static Border Teclinha(string texto, bool destaque = false) => new()
    {
        Style = (Style)Application.Current.FindResource("Tecla"),
        Child = new TextBlock
        {
            Text = texto, FontSize = 12.5, FontWeight = FontWeights.Bold,
            Foreground = (Brush)Application.Current.FindResource(destaque ? "Ambar" : "Texto"),
        },
    };
}

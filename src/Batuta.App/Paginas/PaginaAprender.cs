using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Batuta.App.Desenho;
using Batuta.Core;
using Batuta.Motor;
using static Batuta.App.Paginas.Ui;

namespace Batuta.App.Paginas;

/// <summary>
/// Modo aprender: para o passador fora do catálogo. A pessoa aperta cada botão; o Batuta anota o aparelho (VID e PID)
/// e o que cada botão manda, monta o desenho e guarda como modelo deste PC. Enquanto aprende, nada é bloqueado.
/// </summary>
public sealed class PaginaAprender : Grid, IPagina
{
    readonly Servicos _s;
    readonly JanelaPrincipal _janela;
    readonly Palco _palco = new() { Ocupacao = 0.8, MostrarLegendas = true };
    readonly StackPanel _passos = new();
    readonly List<Botao> _aprendidos = [];
    readonly Dictionary<string, Atalho> _ultimo = [];
    AparelhoVisto? _aparelho;
    Mod _mods;
    readonly TextBox _marca = new() { Text = "", Tag = "Ex.: Exbom" };
    readonly TextBox _nome = new() { Text = "", Tag = "Ex.: Caneta da sala 3" };

    static readonly (string Papel, string Nome)[] Papeis =
    [
        ("avancar", "Avançar"), ("voltar", "Voltar"), ("iniciar", "Iniciar apresentação"), ("tela-preta", "Tela preta"),
        ("cima", "Cima"), ("baixo", "Baixo"), ("tab", "Tab"), ("mais", "Volume +"), ("menos", "Volume −"), ("outro", "Outro"),
    ];

    public PaginaAprender(Servicos s, JanelaPrincipal janela)
    {
        _s = s;
        _janela = janela;
        ColumnDefinitions.Add(new ColumnDefinition());
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(500) });
        var esq = new Grid { Margin = new Thickness(44, 34, 24, 24) };
        esq.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        esq.RowDefinitions.Add(new RowDefinition());
        esq.Children.Add(new StackPanel
        {
            Children =
            {
                T("Aprender um passador", "Titulo"),
                T("Para o modelo que não está no catálogo: aperte cada botão, um de cada vez. Enquanto esta tela está aberta, nenhum botão muda nada.",
                    "Explica", margem: new Thickness(0, 8, 0, 0)),
            },
        });
        Grid.SetRow(_palco, 1);
        esq.Children.Add(_palco);
        _palco.Legenda = b => (b.Nome, string.Join(" ou ", b.Atalhos.Select(a => a.Rotulo())));
        Children.Add(esq);
        var dir = new Border
        {
            Background = P("Painel"), BorderBrush = P("Borda"), BorderThickness = new Thickness(1, 0, 0, 0),
            Child = new ScrollViewer { Content = _passos, Padding = new Thickness(34, 56, 34, 30), VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
        };
        SetColumn(dir, 1);
        Children.Add(dir);
        s.TeclaVista += Tecla;
        Montar();
    }

    public void Entrou()
    {
        _s.Motor.Aprender(true);
        Montar();
    }

    public void Saiu() => _s.Motor.Aprender(false);

    void Tecla(EventoTecla e)
    {
        if (!_s.Motor.Aprendendo || e.Aparelho.Simulado || e.Atalho.Vk == 0) return;
        if (System.Windows.Input.Keyboard.FocusedElement is TextBox) return; // digitando o nome: é o teclado
        if (_aparelho is not null && e.Aparelho.Chave != _aparelho.Chave) return;
        var mod = Teclas.ModificadorDe(e.Atalho.Vk);
        if (mod != Mod.Nenhum)
        {
            _mods = e.Soltou ? _mods & ~mod : _mods | mod;
            return;
        }
        if (e.Soltou || e.Repeticao) return;
        var a = new Atalho(_mods, e.Atalho.Vk);
        _aparelho ??= e.Aparelho;
        var ja = _aprendidos.FirstOrDefault(b => b.Atalhos.Contains(a));
        if (ja is not null)
        {
            _palco.Acender(ja.Id);
            return;
        }
        var n = _aprendidos.Count + 1;
        var papel = a.Vk switch { 0x22 or 0x27 => "avancar", 0x21 or 0x25 => "voltar", 0x74 or 0x1B => "iniciar", 0x42 or 0xBE => "tela-preta", 0x09 => "tab", 0xAF => "mais", 0xAE => "menos", _ => "outro" };
        var nome = Papeis.First(p => p.Papel == papel).Nome;
        if (papel == "outro" || _aprendidos.Any(b => b.Papel == papel)) { nome = $"Botão {n}"; papel = papel == "outro" ? "" : papel + n; }
        var b = new Botao { Id = "b" + n, Nome = nome, Papel = papel, Envia = [a.ToString()] };
        _aprendidos.Add(b);
        Montar();
        Dispatcher.BeginInvoke(() => _palco.Acender(b.Id), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>Para as capturas: um aparelho de exemplo com alguns botões já ensinados.</summary>
    public void Demonstrar()
    {
        _aparelho = new AparelhoVisto { Vid = "1A2B", Pid = "3C4D", Produto = "2.4G Wireless Presenter", Caminho = "demo", Tipo = TipoAparelho.Teclado };
        _aprendidos.Clear();
        _aprendidos.AddRange(
        [
            new Botao { Id = "b1", Nome = "Avançar", Papel = "avancar", Envia = ["PageDown"] },
            new Botao { Id = "b2", Nome = "Voltar", Papel = "voltar", Envia = ["PageUp"] },
            new Botao { Id = "b3", Nome = "Iniciar apresentação", Papel = "iniciar", Envia = ["F5", "Esc"] },
            new Botao { Id = "b4", Nome = "Botão 4", Papel = "", Envia = ["Tab"] },
        ]);
        _marca.Text = "Sem marca";
        _nome.Text = "Caneta da sala 3";
        Montar();
        Dispatcher.BeginInvoke(() => _palco.Acender("b4", segurando: true), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    Modelo ModeloAtual() => new()
    {
        Id = "aprendido-" + (_aparelho?.Chave.Replace(":", "-").ToLowerInvariant() ?? "novo"),
        Marca = _marca.Text.Trim(), Nome = _nome.Text.Trim().Length > 0 ? _nome.Text.Trim() : "Meu passador",
        Desenho = "generico", Conexao = _aparelho is null ? "" : _aparelho.Bluetooth ? "Bluetooth" : "Receptor USB",
        Ids = _aparelho is null || _aparelho.Simulado ? [] : [new IdUsb { Vid = _aparelho.Vid, Pid = _aparelho.Pid, Nota = _aparelho.Produto }],
        Botoes = [.. _aprendidos], Confianca = "aprendido",
        Notas = $"Ensinado neste PC em {DateTime.Now:dd/MM/yyyy}.",
    };

    void Montar()
    {
        var m = ModeloAtual();
        _palco.Mostrar(m, _s.Desenho(m));
        _passos.Children.Clear();

        // 1. o aparelho
        _passos.Children.Add(Passo(1, "Aperte um botão do passador", _aparelho is not null));
        if (_aparelho is null)
        {
            var anel = new Ellipse { Width = 46, Height = 46, Stroke = P("Ambar"), StrokeThickness = 2, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(38, 6, 0, 20) };
            anel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.15, TimeSpan.FromSeconds(1.1)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
            _passos.Children.Add(anel);
            _passos.Children.Add(T("Esperando a primeira tecla. Se aparecer o seu teclado, use o botão Recomeçar e aperte no passador.", "Explica", margem: new Thickness(38, 0, 0, 0)));
            return;
        }
        var ap = _aparelho;
        _passos.Children.Add(T($"{ap.Produto ?? "Aparelho sem nome"}   {ap.Vid}:{ap.Pid}{(ap.Bluetooth ? "   Bluetooth" : "")}", "Rotulo", 13, "Texto", new Thickness(38, 0, 0, 6)));
        var sugestao = _s.Catalogo.Candidatos(ap.Vid, ap.Pid).FirstOrDefault()
            ?? _s.Catalogo.Modelos.FirstOrDefault(c => _aprendidos.Count >= 2 && _aprendidos.All(b => b.Atalhos.All(a => c.BotaoDoAtalho(a) is not null)));
        if (sugestao is not null)
        {
            _passos.Children.Add(T($"Parece um {sugestao.NomeCompleto}, do catálogo.", "Rotulo", 13, "Texto2", new Thickness(38, 4, 0, 2)));
            var usar = Botao("Usar o desenho e as teclas do catálogo", "BotaoFantasma", "catalogo", () =>
            {
                _s.Associar(ap.Chave, sugestao.Id);
                _janela.PaginaDe<PaginaControle>("Controle")?.Trocar(sugestao);
                _janela.Ir("Controle");
            });
            usar.HorizontalAlignment = HorizontalAlignment.Left;
            usar.Margin = new Thickness(30, 0, 0, 0);
            _passos.Children.Add(usar);
        }

        // 2. os botões
        _passos.Children.Add(Passo(2, "Aperte cada botão, um de cada vez", _aprendidos.Count >= 2));
        _passos.Children.Add(T("Botão que alterna (como Iniciar, que manda F5 e depois Esc) aparece duas vezes: junte com o de cima.", "Rotulo", 12, "Texto3", new Thickness(38, 0, 0, 10)));
        for (var i = 0; i < _aprendidos.Count; i++)
        {
            var b = _aprendidos[i];
            var idx = i;
            var linha = new DockPanel { Margin = new Thickness(38, 0, 0, 8) };
            var teclas = new WrapPanel { Width = 110, VerticalAlignment = VerticalAlignment.Center };
            foreach (var a in b.Atalhos) teclas.Children.Add(CapturaDeAtalho.Teclinha(a.Rotulo()));
            DockPanel.SetDock(teclas, Dock.Left);
            linha.Children.Add(teclas);
            if (i > 0)
            {
                var juntar = new Button { Style = S("BotaoFantasma"), Content = Icone.Criar("junto", 14), ToolTip = "É o mesmo botão que o de cima", Padding = new Thickness(6) };
                juntar.Click += (_, _) =>
                {
                    _aprendidos[idx - 1].Envia.AddRange(b.Envia);
                    _aprendidos.RemoveAt(idx);
                    Montar();
                };
                DockPanel.SetDock(juntar, Dock.Right);
                linha.Children.Add(juntar);
            }
            var nome = new TextBox { Text = b.Nome };
            nome.TextChanged += (_, _) => { b.Nome = nome.Text; _palco.AtualizarLegendas(); };
            linha.Children.Add(nome);
            _passos.Children.Add(linha);
        }

        // 3. salvar
        _passos.Children.Add(Passo(3, "Dê um nome e salve", false));
        var nomes = new Grid { Margin = new Thickness(38, 0, 0, 12) };
        nomes.ColumnDefinitions.Add(new ColumnDefinition());
        nomes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        nomes.ColumnDefinitions.Add(new ColumnDefinition());
        nomes.Children.Add(new StackPanel { Children = { T("Marca", "Rotulo", margem: new Thickness(0, 0, 0, 4)), Solto(_marca) } });
        var col2 = new StackPanel { Children = { T("Modelo", "Rotulo", margem: new Thickness(0, 0, 0, 4)), Solto(_nome) } };
        Grid.SetColumn(col2, 2);
        nomes.Children.Add(col2);
        _passos.Children.Add(nomes);
        var botoes = new WrapPanel { Margin = new Thickness(38, 4, 0, 0) };
        var salvar = Botao("Salvar e configurar", "BotaoPrimario", "check", Salvar);
        salvar.IsEnabled = _aprendidos.Count > 0;
        botoes.Children.Add(salvar);
        var recomecar = Botao("Recomeçar", "BotaoFantasma", "fabrica", () => { _aparelho = null; _aprendidos.Clear(); _mods = Mod.Nenhum; Montar(); });
        recomecar.Margin = new Thickness(8, 0, 0, 0);
        botoes.Children.Add(recomecar);
        _passos.Children.Add(botoes);
    }

    static TextBox Solto(TextBox t)
    {
        if (t.Parent is Panel p) p.Children.Remove(t);
        return t;
    }

    static FrameworkElement Passo(int n, string texto, bool feito)
    {
        var num = new Border
        {
            Width = 26, Height = 26, CornerRadius = new CornerRadius(13), Margin = new Thickness(0, 0, 12, 0),
            Background = feito ? P("Ambar") : System.Windows.Media.Brushes.Transparent, BorderBrush = P(feito ? "Ambar" : "BordaForte"), BorderThickness = new Thickness(1),
            Child = feito
                ? new Viewbox { Width = 12, Child = WithFg(Icone.Criar("check", 12), "#1B1408") }
                : T(n.ToString(), tamanho: 12.5, cor: "Texto2"),
        };
        if (num.Child is TextBlock tb) { tb.HorizontalAlignment = HorizontalAlignment.Center; tb.VerticalAlignment = VerticalAlignment.Center; }
        var titulo = T(texto, "Subtitulo", 16);
        titulo.VerticalAlignment = VerticalAlignment.Center;
        return new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 10), Children = { num, titulo } };
    }

    static UIElement WithFg(Icone i, string cor)
    {
        System.Windows.Documents.TextElement.SetForeground(i, new SolidColorBrush((Color)ColorConverter.ConvertFromString(cor)));
        return i;
    }

    void Salvar()
    {
        var m = ModeloAtual();
        if (_s.Config.ModelosAprendidos.FirstOrDefault(x => x.Id == m.Id) is { } antigo) _s.Config.ModelosAprendidos.Remove(antigo);
        _s.Config.ModelosAprendidos.Add(m);
        if (_aparelho is { Simulado: false } ap) _s.Associar(ap.Chave, m.Id);
        else { _s.Config.UltimoModelo = m.Id; _s.Salvar(); }
        Log.Escrever($"modelo ensinado: {m.NomeCompleto}, {m.Botoes.Count} botões");
        _aparelho = null;
        _aprendidos.Clear();
        _janela.PaginaDe<PaginaControle>("Controle")?.Trocar(m);
        _janela.Ir("Controle");
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Passador.Core;
using static Passador.App.Paginas.Ui;

namespace Passador.App.Paginas;

/// <summary>Marca e modelo: o passador ligado em destaque no topo, depois o catálogo por marca, com o desenho de cada um.</summary>
public sealed class PaginaCatalogo : ScrollViewer, IPagina
{
    readonly Servicos _s;
    readonly JanelaPrincipal _janela;
    readonly StackPanel _conteudo = new() { Margin = new Thickness(48, 4, 48, 40) };
    readonly StackPanel _lista = new();
    readonly TextBox _busca = new() { Width = 300, HorizontalAlignment = HorizontalAlignment.Left };

    public PaginaCatalogo(Servicos s, JanelaPrincipal janela)
    {
        _s = s;
        _janela = janela;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        Margin = new Thickness(0, 36, 0, 0); // a barra de rolagem começa abaixo dos botões da janela
        Content = _conteudo;
        _conteudo.Children.Add(T("Qual é o seu passador?", "Titulo"));
        _conteudo.Children.Add(T("Escolha a marca e o modelo: o Passador mostra o desenho e o que cada botão manda. Se o seu não estiver aqui, a página Aprender descobre sozinha.",
            "Explica", margem: new Thickness(0, 8, 0, 20)));
        var linha = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 26) };
        var lupa = Icone.Criar("busca", 16);
        lupa.Margin = new Thickness(0, 0, 10, 0);
        System.Windows.Documents.TextElement.SetForeground(lupa, P("Texto3"));
        linha.Children.Add(lupa);
        _busca.Tag = "Procurar marca ou modelo";
        _busca.TextChanged += (_, _) => Montar();
        linha.Children.Add(_busca);
        _conteudo.Children.Add(linha);
        _conteudo.Children.Add(_lista);
        s.AparelhosMudaram += Montar;
        Montar();
    }

    public void Entrou() => Montar();
    public void Saiu() { }

    void Montar()
    {
        _lista.Children.Clear();
        var filtro = _busca.Text.Trim();
        var conectado = _s.PassadorConectado;
        var emFoco = _s.ModeloEmFoco;

        // Destaque: o que está ligado agora (reconhecido ou não)
        if (filtro.Length == 0)
        {
            if (conectado is { } c && !c.Aparelho.Simulado)
                _lista.Children.Add(Destaque(c.Modelo, $"Ligado agora: {c.Aparelho.Produto ?? "passador"} ({c.Aparelho.Vid}:{c.Aparelho.Pid})",
                    "Reconhecido pelo código do receptor. Se não for este, escolha o certo abaixo."));
            else if (_marca is null)
                _lista.Children.Add(Faixa("Nenhum passador ligado agora. Ligue o receptor: se ele estiver aqui, o Passador reconhece sozinho; se não, ensine em Aprender."));
            foreach (var d in _s.PassadoresDesconhecidos)
                _lista.Children.Add(Aviso($"Ligado e fora do catálogo: {d.Produto ?? "aparelho"} ({d.Vid}:{d.Pid})",
                    "Escolha abaixo um modelo parecido ou ensine os botões em Aprender."));
        }

        var modelos = _s.TodosModelos.Where(m => filtro.Length == 0
            || m.NomeCompleto.Contains(filtro, StringComparison.CurrentCultureIgnoreCase)
            || m.Marca.Contains(filtro, StringComparison.CurrentCultureIgnoreCase)).ToList();
        if (modelos.Count == 0)
        {
            _lista.Children.Add(T("Nenhum modelo com esse nome. Ensine o seu na página Aprender.", "Explica"));
            return;
        }
        // Marcas: fichas para filtrar (o catálogo cresce; a busca e a marca acham rápido)
        var marcas = new WrapPanel { Margin = new Thickness(0, 4, 0, 18) };
        foreach (var marca in new[] { "Todas" }.Concat(_s.TodosModelos.Select(x => x.Confianca == "aprendido" ? "Ensinados aqui" : x.Marca).Distinct()
                     .OrderBy(x => x == "Ensinados aqui" ? 0 : x == "Genérico" ? 2 : 1).ThenBy(x => x)))
        {
            var mm = marca;
            marcas.Children.Add(Ficha(marca, "marca-catalogo", (_marca ?? "Todas") == marca, () => { _marca = mm == "Todas" ? null : mm; Montar(); }));
        }
        _lista.Children.Add(marcas);
        var wrap = new WrapPanel();
        foreach (var m in modelos.Where(x => _marca is null || (x.Confianca == "aprendido" ? "Ensinados aqui" : x.Marca) == _marca)
                     .OrderBy(x => x.Confianca == "aprendido" ? 0 : x.Marca == "Genérico" ? 2 : 1).ThenBy(x => x.Marca).ThenBy(x => x.Nome))
            wrap.Children.Add(Cartao(m, m.Id == emFoco.Id));
        _lista.Children.Add(wrap);
    }

    string? _marca;
    FrameworkElement Miniatura(Modelo m, double altura)
    {
        var d = _s.Desenho(m);
        return new Image { Source = new DrawingImage(d.Corpo), Height = altura, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center };
    }

    Border Cartao(Modelo m, bool escolhido)
    {
        var ids = m.Ids.Count == 0 ? "sem código publicado" : string.Join("  ", m.Ids.Take(2).Select(i => i.ToString())) + (m.Ids.Count > 2 ? $"  +{m.Ids.Count - 2}" : "");
        var luz = new Border
        {
            Height = 178, CornerRadius = new CornerRadius(12, 12, 0, 0), Margin = new Thickness(-18, -18, -18, 12),
            Background = new RadialGradientBrush(Color.FromArgb(escolhido ? (byte)0x40 : (byte)0x1C, 0xF4, 0xA9, 0x40), Colors.Transparent)
            { Center = new Point(0.5, 0.45), RadiusX = 0.6, RadiusY = 0.6 },
            Child = Miniatura(m, 150),
        };
        var info = new StackPanel
        {
            Children =
            {
                luz,
                T(m.Marca, "Rotulo", 12, "Texto3"),
                T(m.Nome, tamanho: 15, margem: new Thickness(0, 1, 0, 2)),
                T(m.Conexao, "Rotulo", 12),
                T(PaginaControle.Confianca(m.Confianca), "Rotulo", 12, "Texto3", new Thickness(0, 6, 0, 0)),
            },
        };
        ((TextBlock)info.Children[2]).FontWeight = FontWeights.Bold;
        foreach (var t in info.Children.OfType<TextBlock>()) { t.TextTrimming = TextTrimming.CharacterEllipsis; t.TextWrapping = TextWrapping.NoWrap; }
        var b = new Border
        {
            Width = 226, Margin = new Thickness(0, 0, 16, 16), Padding = new Thickness(18), CornerRadius = new CornerRadius(14),
            Background = P("Painel"), BorderThickness = new Thickness(escolhido ? 1.5 : 1),
            BorderBrush = escolhido ? P("Ambar") : P("Borda"), Cursor = Cursors.Hand, Child = info, ClipToBounds = true,
            ToolTip = $"Código do receptor: {ids}" + (m.Notas is { } n ? "\n\n" + n : ""), Focusable = true,
        };
        b.MouseEnter += (_, _) => { if (!escolhido) b.BorderBrush = P("BordaForte"); };
        b.MouseLeave += (_, _) => { if (!escolhido) b.BorderBrush = P("Borda"); };
        b.MouseLeftButtonUp += (_, _) => Escolher(m);
        b.KeyDown += (_, e) => { if (e.Key is Key.Enter or Key.Space) Escolher(m); };
        return b;
    }

    Border Destaque(Modelo m, string titulo, string texto)
    {
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        g.ColumnDefinitions.Add(new ColumnDefinition());
        g.Children.Add(Miniatura(m, 170));
        var col = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(26, 0, 0, 0) };
        col.Children.Add(T(titulo, "Rotulo", 13, titulo.StartsWith("Ligado") ? "Ok" : "Texto2"));
        col.Children.Add(T(m.NomeCompleto, "Titulo", 26, margem: new Thickness(0, 4, 0, 6)));
        col.Children.Add(T(texto, "Explica"));
        var ir = Botao("Configurar os botões", "BotaoPrimario", "controle", () => Escolher(m));
        ir.HorizontalAlignment = HorizontalAlignment.Left;
        ir.Margin = new Thickness(0, 14, 0, 0);
        col.Children.Add(ir);
        Grid.SetColumn(col, 1);
        g.Children.Add(col);
        var c = Ui.Cartao(g, new Thickness(26));
        c.Margin = new Thickness(0, 0, 0, 28);
        c.BorderBrush = P("AmbarMedio");
        c.Effect = new DropShadowEffect { Color = Color.FromRgb(0xF4, 0xA9, 0x40), BlurRadius = 40, ShadowDepth = 0, Opacity = 0.12 };
        return c;
    }

    static Border Faixa(string texto)
    {
        var ic = Icone.Criar("usb", 18);
        ic.Margin = new Thickness(0, 0, 14, 0);
        System.Windows.Documents.TextElement.SetForeground(ic, P("Info"));
        var t = T(texto, "Explica");
        t.VerticalAlignment = VerticalAlignment.Center;
        var c = Ui.Cartao(new StackPanel { Orientation = Orientation.Horizontal, Children = { ic, t } }, new Thickness(18, 14, 18, 14));
        c.Margin = new Thickness(0, 0, 0, 22);
        c.HorizontalAlignment = HorizontalAlignment.Left;
        return c;
    }

    static Border Aviso(string titulo, string texto)
    {
        var col = new StackPanel { Children = { T(titulo, tamanho: 14), T(texto, "Explica", margem: new Thickness(0, 4, 0, 0)) } };
        ((TextBlock)col.Children[0]).FontWeight = FontWeights.Bold;
        var c = Ui.Cartao(col, new Thickness(20, 16, 20, 16));
        c.Margin = new Thickness(0, 0, 0, 20);
        c.BorderBrush = P("AmbarMedio");
        return c;
    }

    void Escolher(Modelo m)
    {
        // Se um passador de verdade está ligado, ele passa a ser este modelo; senão, é só o que a tela mostra.
        var real = _s.Motor.Conectados.FirstOrDefault(a => !a.Simulado && (_s.Motor.ModeloDe(a) is not null || a.NomeDePassador));
        if (real is not null) _s.Associar(real.Chave, m.Id);
        else
        {
            _s.Config.UltimoModelo = m.Id;
            _s.Salvar();
        }
        _janela.PaginaDe<PaginaControle>("Controle")?.Trocar(m);
        _janela.Ir("Controle");
    }
}

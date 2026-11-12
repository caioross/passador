using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Batuta.Core;
using Batuta.Motor;
using static Batuta.App.Paginas.Ui;

namespace Batuta.App.Paginas;

/// <summary>Perfis por programa: quando cada um vale (programa na frente e, no navegador, o título da aba).</summary>
public sealed class PaginaPerfis : Grid, IPagina
{
    readonly Servicos _s;
    readonly JanelaPrincipal _janela;
    readonly StackPanel _lista = new();
    readonly StackPanel _editor = new();
    readonly TextBlock _frente = T("", "Rotulo", 12.5);
    readonly DispatcherTimer _relogio = new() { Interval = TimeSpan.FromSeconds(1) };
    readonly DispatcherTimer _gravar = new() { Interval = TimeSpan.FromMilliseconds(500) };
    string _escolhido = "powerpoint";
    (string proc, string titulo) _ultimaFrente;

    public PaginaPerfis(Servicos s, JanelaPrincipal janela)
    {
        _s = s;
        _janela = janela;
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(400) });
        ColumnDefinitions.Add(new ColumnDefinition());
        var esq = new StackPanel { Margin = new Thickness(48, 40, 24, 30) };
        esq.Children.Add(T("Perfis", "Titulo"));
        esq.Children.Add(T("Cada programa pode ter o seu jeito de usar o passador. O Batuta troca sozinho pelo programa que está na frente.",
            "Explica", margem: new Thickness(0, 8, 0, 22)));
        esq.Children.Add(_lista);
        var botoes = new WrapPanel { Margin = new Thickness(0, 18, 0, 0) };
        botoes.Children.Add(Botao("Novo perfil", "BotaoBase", "mais", Novo));
        var restaurar = Botao("Restaurar os prontos", "BotaoFantasma", "fabrica", Restaurar);
        restaurar.Margin = new Thickness(8, 0, 0, 0);
        botoes.Children.Add(restaurar);
        esq.Children.Add(botoes);
        Children.Add(new ScrollViewer { Content = esq, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });

        var dir = new Border
        {
            Background = P("Painel"), BorderBrush = P("Borda"), BorderThickness = new Thickness(1, 0, 0, 0),
            Child = new ScrollViewer { Content = _editor, Padding = new Thickness(40, 56, 40, 30), VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
        };
        SetColumn(dir, 1);
        Children.Add(dir);
        _relogio.Tick += (_, _) => Frente();
        _gravar.Tick += (_, _) => { _gravar.Stop(); _s.Salvar(); MontarLista(); };
        MontarLista();
        MontarEditor();
    }

    public void Entrou() { _relogio.Start(); MontarLista(); MontarEditor(); }
    public void Saiu() => _relogio.Stop();

    void Frente()
    {
        var (proc, titulo) = MotorDeEntrada.JanelaDaFrenteAgora();
        if (proc.Length > 0 && !proc.Equals(Process.GetCurrentProcess().ProcessName, StringComparison.OrdinalIgnoreCase))
            _ultimaFrente = (proc, titulo);
        if (_ultimaFrente.proc is null) { _frente.Text = "Abra o programa e volte aqui: ele aparece nesta linha."; return; }
        var p = Resolucao.Escolher(_s.Config.Perfis, _ultimaFrente.proc, _ultimaFrente.titulo);
        var t = _ultimaFrente.titulo.Length > 60 ? _ultimaFrente.titulo[..59] + "…" : _ultimaFrente.titulo;
        _frente.Text = $"Último programa na frente: {_ultimaFrente.proc} (“{t}”), que usa o perfil {p.Nome}.";
    }

    void MontarLista()
    {
        _lista.Children.Clear();
        foreach (var p in _s.Config.Perfis)
        {
            var sel = p.Id == _escolhido;
            var ic = Icone.Criar(p.Icone, 18);
            ic.Margin = new Thickness(0, 0, 14, 0);
            System.Windows.Documents.TextElement.SetForeground(ic, P(sel ? "Ambar" : "Texto2"));
            var nome = T(p.Nome, tamanho: 14.5);
            nome.FontWeight = FontWeights.Bold;
            var resumo = T(Resumo(p), "Rotulo", 12, margem: new Thickness(0, 2, 0, 0));
            resumo.TextTrimming = TextTrimming.CharacterEllipsis;
            var col = new StackPanel { Children = { nome, resumo } };
            var dp = new DockPanel();
            if (p.Id != Perfil.IdPadrao)
            {
                var liga = new CheckBox { Style = S("Interruptor"), IsChecked = p.Ativo, ToolTip = p.Ativo ? "Ligado" : "Desligado", VerticalAlignment = VerticalAlignment.Center };
                liga.Checked += (_, _) => { p.Ativo = true; AgendarGravar(); };
                liga.Unchecked += (_, _) => { p.Ativo = false; AgendarGravar(); };
                DockPanel.SetDock(liga, Dock.Right);
                dp.Children.Add(liga);
            }
            DockPanel.SetDock(ic, Dock.Left);
            dp.Children.Add(ic);
            dp.Children.Add(col);
            var item = new Border
            {
                Child = dp, Padding = new Thickness(16, 12, 14, 12), CornerRadius = new CornerRadius(12), Margin = new Thickness(0, 0, 0, 6),
                Background = sel ? P("AmbarSuave") : System.Windows.Media.Brushes.Transparent, Cursor = Cursors.Hand,
                Opacity = p.Ativo ? 1 : 0.55,
            };
            var id = p.Id;
            item.MouseEnter += (_, _) => { if (id != _escolhido) item.Background = P("Hover"); };
            item.MouseLeave += (_, _) => { if (id != _escolhido) item.Background = System.Windows.Media.Brushes.Transparent; };
            item.MouseLeftButtonUp += (_, _) => { _escolhido = id; MontarLista(); MontarEditor(); };
            _lista.Children.Add(item);
        }
    }

    static string Resumo(Perfil p)
    {
        if (p.Id == Perfil.IdPadrao) return "Qualquer programa sem perfil";
        var r = p.Regras.FirstOrDefault();
        if (r is null) return "Sem regra: nunca entra sozinho";
        var proc = r.Processos.Count > 2 ? "navegadores" : string.Join(", ", r.Processos);
        return r.TituloContem.Count > 0 ? $"{proc}, aba com “{r.TituloContem[0]}”" : proc;
    }

    void MontarEditor()
    {
        _editor.Children.Clear();
        var p = _s.Config.Perfis.FirstOrDefault(x => x.Id == _escolhido) ?? _s.Config.Perfis[0];
        _escolhido = p.Id;
        _editor.Children.Add(T("Perfil", "Rotulo"));
        var nome = new TextBox { Text = p.Nome, FontSize = 20, Width = 420, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 10) };
        nome.TextChanged += (_, _) => { p.Nome = nome.Text; AgendarGravar(); };
        _editor.Children.Add(nome);
        if (p.Dica is not null) _editor.Children.Add(T(p.Dica, "Explica", margem: new Thickness(0, 0, 0, 20)));
        if (p.Id == Perfil.IdPadrao)
        {
            _editor.Children.Add(T("O Padrão vale sempre que nenhum outro perfil casa com o programa da frente. O que ficar vazio num perfil vem dele.", "Explica"));
            return;
        }
        _editor.Children.Add(T("Vale quando", "Subtitulo", 16, margem: new Thickness(0, 10, 0, 10)));
        if (p.Regras.Count == 0) p.Regras.Add(new Regra());
        for (var i = 0; i < p.Regras.Count; i++)
        {
            var r = p.Regras[i];
            if (i > 0) _editor.Children.Add(T("ou quando", "Rotulo", margem: new Thickness(0, 4, 0, 10)));
            var cartao = new StackPanel();
            cartao.Children.Add(T("O programa na frente é (nome do processo, separado por vírgula)", "Rotulo", margem: new Thickness(0, 0, 0, 6)));
            var procs = new TextBox { Text = string.Join(", ", r.Processos) };
            procs.TextChanged += (_, _) => { r.Processos = Lista(procs.Text); AgendarGravar(); };
            cartao.Children.Add(procs);
            cartao.Children.Add(T("e o título da janela tem (opcional, separado por vírgula)", "Rotulo", margem: new Thickness(0, 14, 0, 6)));
            var tit = new TextBox { Text = string.Join(", ", r.TituloContem) };
            tit.TextChanged += (_, _) => { r.TituloContem = Lista(tit.Text); AgendarGravar(); };
            cartao.Children.Add(tit);
            var c = Ui.Cartao(cartao, new Thickness(20), "Painel2");
            c.Margin = new Thickness(0, 0, 0, 8);
            _editor.Children.Add(c);
        }
        var maisRegra = Botao("Outra regra", "BotaoFantasma", "mais", () => { p.Regras.Add(new Regra()); MontarEditor(); });
        maisRegra.HorizontalAlignment = HorizontalAlignment.Left;
        _editor.Children.Add(maisRegra);
        _frente.Margin = new Thickness(0, 18, 0, 0);
        _frente.TextWrapping = TextWrapping.Wrap;
        _editor.Children.Add(_frente);
        Frente();
        _editor.Children.Add(T("O que este perfil muda", "Subtitulo", 16, margem: new Thickness(0, 28, 0, 10)));
        var mudancas = Mudancas(p).ToList();
        if (mudancas.Count == 0) _editor.Children.Add(T("Nada ainda: todos os botões ficam como no Padrão.", "Explica"));
        foreach (var (botao, gesto, faz) in mudancas)
        {
            var g = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            var b1 = T(botao, tamanho: 13.5);
            b1.FontWeight = FontWeights.Bold;
            g.Children.Add(b1);
            var g2 = T(gesto, "Rotulo", 13);
            Grid.SetColumn(g2, 1);
            g.Children.Add(g2);
            var f3 = T(faz, tamanho: 13.5, cor: "Ambar");
            Grid.SetColumn(f3, 2);
            g.Children.Add(f3);
            _editor.Children.Add(g);
        }
        var configurar = Botao("Configurar os botões deste perfil", "BotaoPrimario", "controle", () =>
        {
            _janela.PaginaDe<PaginaControle>("Controle")?.EditarPerfil(p.Id);
            _janela.Ir("Controle");
        });
        configurar.HorizontalAlignment = HorizontalAlignment.Left;
        configurar.Margin = new Thickness(0, 16, 0, 0);
        _editor.Children.Add(configurar);
        if (!PerfisProntos.Todos().Any(x => x.Id == p.Id))
        {
            var apagar = Botao("Apagar este perfil", "BotaoFantasma", "lixo", () =>
            {
                _s.Config.Perfis.Remove(p);
                _escolhido = Perfil.IdPadrao;
                _s.Salvar();
                MontarLista();
                MontarEditor();
            });
            apagar.HorizontalAlignment = HorizontalAlignment.Left;
            apagar.Margin = new Thickness(-8, 18, 0, 0);
            _editor.Children.Add(apagar);
        }
    }

    /// <summary>As mudanças do perfil em linhas de gente: (botão, gesto, o que faz).</summary>
    IEnumerable<(string, string, string)> Mudancas(Perfil p)
    {
        var nomes = new Dictionary<string, string> { ["avancar"] = "Avançar", ["voltar"] = "Voltar", ["iniciar"] = "Iniciar apresentação", ["tela-preta"] = "Tela preta", ["cima"] = "Cima", ["baixo"] = "Baixo", ["tab"] = "Tab", ["mais"] = "Volume +", ["menos"] = "Volume −" };
        foreach (var (chave, m) in p.Mapas.OrderBy(k => k.Key.StartsWith("botao:") ? 1 : 0))
        {
            var nome = chave.StartsWith("papel:") ? nomes.GetValueOrDefault(chave[6..], chave[6..])
                : _s.ModeloPorId(chave[6..].Split('/')[0]) is { } mo && mo.BotaoPorId(chave.Split('/')[^1]) is { } bt ? $"{bt.Nome} ({mo.Nome})" : chave[6..];
            if (m.Toque is { } t) yield return (nome, "Um toque", t.Descricao);
            if (m.Duplo is { } d) yield return (nome, "Dois toques", d.Descricao);
            if (m.Segurar is { } s) yield return (nome, m.ModoSegurar switch { ModoSegurar.Repetir => "Segurar, repete", ModoSegurar.Enquanto => "Enquanto segura", _ => "Segurar" }, s.Descricao);
        }
    }

    static List<string> Lista(string t) => [.. t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    void AgendarGravar() { _gravar.Stop(); _gravar.Start(); }

    void Novo()
    {
        var p = new Perfil
        {
            Id = "perfil-" + Guid.NewGuid().ToString("N")[..6], Nome = _ultimaFrente.proc is { Length: > 0 } pr ? pr : "Novo perfil",
            Icone = "app-window", Regras = [new Regra { Processos = _ultimaFrente.proc is { Length: > 0 } x ? [x] : [] }],
        };
        _s.Config.Perfis.Add(p);
        _escolhido = p.Id;
        _s.Salvar();
        MontarLista();
        MontarEditor();
    }

    void Restaurar()
    {
        var prontos = PerfisProntos.Todos();
        var proprios = _s.Config.Perfis.Where(p => !prontos.Any(x => x.Id == p.Id)).ToList();
        _s.Config.Perfis = [.. prontos, .. proprios];
        _s.Salvar();
        MontarLista();
        MontarEditor();
    }
}

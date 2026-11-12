using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Batuta.App.Desenho;
using Batuta.Core;
using Batuta.Motor;
using static Batuta.App.Paginas.Ui;

namespace Batuta.App.Paginas;

/// <summary>
/// A tela principal: o passador sob o refletor, as legendas do que cada botão faz no perfil escolhido, e à direita o
/// botão escolhido com toque, dois toques, segurar e junto com outro. "Testar no desenho" aperta o botão de mentira
/// (passador simulado) e mostra o que chegou no programa.
/// </summary>
public sealed class PaginaControle : Grid, IPagina
{
    const int NumeroSimulado = 1;
    readonly Servicos _s;
    readonly JanelaPrincipal _janela;
    readonly Palco _palco = new() { Margin = new Thickness(0, 8, 0, 0) };
    readonly TextBlock _titulo = T("", "Titulo");
    readonly TextBlock _marca = T("", "Rotulo", 14);
    readonly StackPanel _estado = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
    readonly WrapPanel _perfis = new() { Margin = new Thickness(0, 18, 0, 0) };
    readonly StackPanel _inspetor = new();
    readonly StackPanel _ticker = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, ClipToBounds = true };
    readonly Border _recebido = new() { Visibility = Visibility.Collapsed };
    readonly WrapPanel _recebidoTeclas = new();
    readonly CheckBox _testar;
    readonly DispatcherTimer _gravar = new() { Interval = TimeSpan.FromMilliseconds(500) };
    Modelo _modelo;
    string _perfilEditado = Perfil.IdPadrao;
    string? _botao;

    public PaginaControle(Servicos s, JanelaPrincipal janela)
    {
        _s = s;
        _janela = janela;
        _modelo = s.ModeloEmFoco;
        ColumnDefinitions.Add(new ColumnDefinition());
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(430) });

        // ---------- palco
        var esquerda = new Grid { Margin = new Thickness(48, 34, 24, 22) };
        esquerda.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        esquerda.RowDefinitions.Add(new RowDefinition());
        esquerda.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var cabeca = new DockPanel();
        _testar = Interruptor("Testar no desenho", false, LigarTeste);
        _testar.ToolTip = "Aperte os botões do desenho como se fosse o passador: o motor faz a ação de verdade.";
        _testar.VerticalAlignment = VerticalAlignment.Bottom;
        _testar.Margin = new Thickness(0, 0, 0, 2);
        DockPanel.SetDock(_testar, Dock.Right);
        cabeca.Children.Add(_testar);
        var nomes = new StackPanel { Children = { _marca, _titulo, _estado } };
        _marca.Margin = new Thickness(0, 0, 0, 2);
        cabeca.Children.Add(nomes);
        var topo = new StackPanel { Children = { cabeca, _perfis } };
        esquerda.Children.Add(topo);

        Grid.SetRow(_palco, 1);
        esquerda.Children.Add(_palco);
        _palco.Legenda = Legenda;
        _palco.BotaoEscolhido += Escolher;
        _palco.BotaoTestado += Testado;

        var historico = new DockPanel { Margin = new Thickness(0, 6, 0, 0) };
        var rotHist = T("Últimos toques", "Rotulo", 12.5, "Texto3", new Thickness(0, 0, 18, 0));
        rotHist.VerticalAlignment = VerticalAlignment.Center;
        DockPanel.SetDock(rotHist, Dock.Left);
        historico.Children.Add(rotHist);
        historico.Children.Add(_ticker);
        _ticker.OpacityMask = new LinearGradientBrush([new GradientStop(Colors.Black, 0.82), new GradientStop(Colors.Transparent, 1)], new Point(0, 0), new Point(1, 0));
        var rodape = new StackPanel { Children = { _recebido, new Border { Height = 1, Background = P("Borda"), Margin = new Thickness(0, 0, 0, 10) }, historico } };
        Grid.SetRow(rodape, 2);
        esquerda.Children.Add(rodape);
        _recebido.Child = new StackPanel
        {
            Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center,
            Children = { T("Chegou no programa:", "Rotulo", margem: new Thickness(0, 4, 12, 0)), _recebidoTeclas },
        };
        _recebido.Margin = new Thickness(0, 0, 0, 10);
        _recebido.Focusable = true;
        _recebido.FocusVisualStyle = null;
        Children.Add(esquerda);

        // ---------- inspetor
        var lado = new Border
        {
            Background = P("Painel"), BorderBrush = P("Borda"), BorderThickness = new Thickness(1, 0, 0, 0),
            Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _inspetor, Padding = new Thickness(30, 54, 30, 30) },
        };
        SetColumn(lado, 1);
        Children.Add(lado);

        _gravar.Tick += (_, _) => { _gravar.Stop(); _s.Salvar(); };
        s.AparelhosMudaram += () => { if (_s.ModeloEmFoco.Id != _modelo.Id) Trocar(_s.ModeloEmFoco); else MontarCabeca(); };
        s.ConfigMudou += () => { MontarCabeca(); _palco.AtualizarLegendas(); };
        s.PerfilMudou += _ => MontarPerfis();
        s.TeclaVista += TeclaVista;
        s.AcaoFeita += AcaoFeita;
        PreviewKeyDown += Recebeu;
        PreviewTextInput += (_, e) =>
        {
            if (_testar.IsChecked != true) return;
            e.Handled = true;
            AdicionarRecebido(e.Text == " " ? "Espaço" : e.Text);
        };
        Trocar(_modelo);
    }

    public void Entrou() => Trocar(_s.ModeloEmFoco);

    public void Saiu()
    {
        if (_testar.IsChecked == true) _testar.IsChecked = false;
    }

    /// <summary>Mostrar outro modelo (escolhido no catálogo ou o passador que acabou de chegar).</summary>
    public void Trocar(Modelo m)
    {
        _modelo = m;
        _palco.Mostrar(m, _s.Desenho(m));
        _botao = m.Botoes.FirstOrDefault(b => b.Papel == "avancar")?.Id ?? m.Botoes.FirstOrDefault(b => b.Configuravel)?.Id;
        _palco.Selecionado = _botao;
        if (_testar.IsChecked == true) _s.Motor.Simular(NumeroSimulado, m.Id);
        MontarCabeca();
        MontarPerfis();
        MontarInspetor();
    }

    void MontarCabeca()
    {
        _titulo.Text = _modelo.Nome;
        _marca.Text = _modelo.Marca;
        _estado.Children.Clear();
        var ligado = _s.PassadorConectado;
        var real = ligado is { } l && !l.Aparelho.Simulado && l.Modelo.Id == _modelo.Id ? l.Aparelho : null;
        Brush cor;
        string texto;
        if (real is not null)
        {
            cor = P("Ok");
            texto = $"Ligado agora {(real.Bluetooth ? "por Bluetooth" : "pelo receptor USB")}  {real.Vid}:{real.Pid}";
        }
        else if (_testar.IsChecked == true)
        {
            cor = P("Ambar");
            texto = "Passador simulado: aperte os botões do desenho";
        }
        else
        {
            cor = P("Texto3");
            texto = _s.PassadoresDesconhecidos.Any()
                ? "Não ligado. Há um passador fora do catálogo ligado: ensine em Aprender"
                : "Não ligado agora. Ligue o receptor e ele aparece aqui.";
        }
        _estado.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = cor, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 9, 0) });
        _estado.Children.Add(T(texto, "Rotulo", 13));
        var trocar = Botao("Trocar modelo", "BotaoFantasma", "catalogo", () => _janela.Ir("Catálogo"));
        trocar.Margin = new Thickness(14, -6, 0, -6);
        _estado.Children.Add(trocar);
        _estado.Children.Add(new TextBlock
        {
            Text = Confianca(_modelo.Confianca), FontSize = 12, Foreground = P("Texto3"), VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0),
            ToolTip = (_modelo.Confianca switch
            {
                "conferido" => "As teclas de cada botão foram conferidas em fonte técnica.",
                "comunidade" => "As teclas vêm de relatos de quem tem o aparelho.",
                "aprendido" => "Você ensinou este modelo na tela Aprender.",
                _ => "As teclas foram deduzidas pelo formato e por aparelhos parecidos. Se algum botão não acender ao apertar, ensine o seu na tela Aprender.",
            }) + (_modelo.Notas is { } n ? "\n\n" + n : ""),
        });
    }

    public static string Confianca(string c) => c switch
    {
        "conferido" => "Teclas conferidas",
        "comunidade" => "Teclas por relatos",
        "aprendido" => "Ensinado neste PC",
        _ => "Teclas a confirmar",
    };

    void MontarPerfis()
    {
        _perfis.Children.Clear();
        foreach (var p in _s.Config.Perfis.Where(p => p.Ativo))
        {
            var emUso = p.Id == _s.PerfilEmUso;
            UIElement? ponto = emUso ? new Ellipse { Width = 6, Height = 6, Fill = P("Info"), Margin = new Thickness(8, 1, 0, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = "Em uso agora" } : null;
            var id = p.Id;
            var f = Ficha(p.Nome, "perfil-controle", id == _perfilEditado, () =>
            {
                _perfilEditado = id;
                _s.Motor.TestarComo(id);
                _palco.AtualizarLegendas();
                MontarInspetor();
            }, p.Icone, ponto);
            f.ToolTip = (emUso ? "Em uso agora. " : "") + (p.Dica ?? "");
            _perfis.Children.Add(f);
        }
    }

    Perfil PerfilEditado => _s.Config.Perfis.FirstOrDefault(p => p.Id == _perfilEditado) ?? _s.Config.Perfis[0];

    (string, string) Legenda(Botao b)
    {
        if (!b.Configuravel) return (b.Nome, b.Envia.Count == 0 ? "Só no aparelho" : "Não configurável");
        var m = Resolucao.MapaEfetivo(_s.Config.Perfis, PerfilEditado, _modelo.Id, b);
        var toque = m?.Toque is { } t ? t.Descricao : "De fábrica: " + (b.Atalhos.FirstOrDefault() is var a && a.Vk != 0 ? a.Rotulo() : "?");
        var extras = (m?.TemDuplo == true ? 1 : 0) + (m?.TemSegurar == true ? 1 : 0);
        return (b.Nome, extras switch { 0 => toque, 1 => $"{toque}, e mais 1 gesto", _ => $"{toque}, e mais {extras} gestos" });
    }

    void Escolher(string id)
    {
        _botao = id;
        _palco.Selecionado = id;
        MontarInspetor();
    }

    // ---------------------------------------------------------------- inspetor

    void MontarInspetor()
    {
        _inspetor.Children.Clear();
        var b = _botao is null ? null : _modelo.BotaoPorId(_botao);
        if (b is null)
        {
            _inspetor.Children.Add(T("Clique num botão do desenho para escolher o que ele faz.", "Explica"));
            return;
        }
        _inspetor.Children.Add(T("Botão", "Rotulo"));
        _inspetor.Children.Add(T(b.Nome, "Subtitulo", 26, margem: new Thickness(0, 2, 0, 12)));
        var fabrica = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
        if (b.Configuravel)
        {
            fabrica.Children.Add(T("Manda de fábrica", "Rotulo", margem: new Thickness(0, 4, 10, 0)));
            foreach (var a in b.Atalhos) fabrica.Children.Add(CapturaDeAtalho.Teclinha(a.Rotulo()));
        }
        _inspetor.Children.Add(fabrica);
        if (!b.Configuravel)
        {
            _inspetor.Children.Add(T(b.SoHardware ?? "Este botão não chega ao computador.", "Explica", margem: new Thickness(0, 10, 0, 0)));
            return;
        }
        if (b.Segurar is { } seg)
            _inspetor.Children.Add(T("Ao segurar, de fábrica: " + seg, "Rotulo", 12, "Texto3", new Thickness(0, 0, 0, 0)));

        var perfil = PerfilEditado;
        var padrao = _s.Config.Perfis.First(p => p.Id == Perfil.IdPadrao);
        var proprio = perfil.Mapa(_modelo.Id, b);
        var herdado = perfil == padrao ? null : padrao.Mapa(_modelo.Id, b);
        var vazio = perfil == padrao ? "Como de fábrica" : "Igual ao Padrão";

        _inspetor.Children.Add(new Border { Height = 1, Background = P("Borda"), Margin = new Thickness(0, 20, 0, 18) });
        _inspetor.Children.Add(T(perfil == padrao
            ? "Em qualquer programa sem perfil próprio:"
            : $"Quando {perfil.Nome} estiver na frente:", "Rotulo", 13, margem: new Thickness(0, 0, 0, 14)));

        LinhaDeGesto("toque", "Um toque", vazio, proprio?.Toque, herdado?.Toque, a => Gravar(b, m => m.Toque = a));
        LinhaDeGesto("duplo", "Dois toques", vazio, proprio?.Duplo, herdado?.Duplo, a => Gravar(b, m => m.Duplo = a),
            $"Dentro de {_s.Config.Ajustes.DuploToqueMs} ms. Com isso ligado, o toque simples espera esse tempo para sair.");
        LinhaDeGesto("segurar", "Segurar", vazio, proprio?.Segurar, herdado?.Segurar, a => Gravar(b, m => m.Segurar = a),
            $"Depois de {_s.Config.Ajustes.SegurarMs} ms apertado.");
        var modo = proprio?.ModoSegurar ?? herdado?.ModoSegurar ?? ModoSegurar.UmaVez;
        var modos = new WrapPanel { Margin = new Thickness(26, 0, 0, 22) };
        foreach (var (rot, m) in new[] { ("Uma vez", ModoSegurar.UmaVez), ("Repetindo", ModoSegurar.Repetir), ("Enquanto segurar", ModoSegurar.Enquanto) })
            modos.Children.Add(Ficha(rot, "modo-" + b.Id, modo == m, () => Gravar(b, x => x.ModoSegurar = m)));
        _inspetor.Children.Add(modos);

        Junto(b, perfil);

        var limpar = Botao("Voltar este botão ao " + (perfil == padrao ? "de fábrica" : "Padrão"), "BotaoFantasma", "fabrica", () =>
        {
            perfil.Mapas.Remove(Perfil.ChaveBotao(_modelo.Id, b.Id));
            perfil.Mapas.Remove(Perfil.ChavePapel(b.Papel));
            perfil.Combinacoes.RemoveAll(c => c.BotaoA == b.Id || c.BotaoB == b.Id);
            _s.Salvar();
            MontarInspetor();
            _palco.AtualizarLegendas();
        });
        limpar.HorizontalAlignment = HorizontalAlignment.Left;
        limpar.Margin = new Thickness(-8, 14, 0, 0);
        _inspetor.Children.Add(limpar);
    }

    void LinhaDeGesto(string icone, string titulo, string vazio, Acao? proprio, Acao? herdado, Action<Acao?> gravar, string? dica = null)
    {
        var cab = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        var ic = Icone.Criar(icone, 16);
        ic.Margin = new Thickness(0, 0, 10, 0);
        TextElement_Set(ic, P("Ambar"));
        cab.Children.Add(ic);
        cab.Children.Add(T(titulo, tamanho: 15, margem: new Thickness(0, 0, 0, 0)));
        ((TextBlock)cab.Children[^1]).FontWeight = FontWeights.Bold;
        _inspetor.Children.Add(cab);
        var rotuloVazio = herdado is not null ? $"{vazio}: {herdado.Descricao}" : vazio;
        var ed = new EditorDeAcao(rotuloVazio) { Margin = new Thickness(26, 0, 0, dica is null ? 22 : 4) };
        ed.Definir(proprio);
        ed.Mudou += gravar;
        _inspetor.Children.Add(ed);
        if (dica is not null) _inspetor.Children.Add(T(dica, "Rotulo", 12, "Texto3", new Thickness(26, 0, 0, 14)));
    }

    static void TextElement_Set(DependencyObject d, Brush b) => System.Windows.Documents.TextElement.SetForeground(d, b);

    void Junto(Botao b, Perfil perfil)
    {
        var outros = _modelo.Botoes.Where(x => x.Configuravel && x.Id != b.Id).ToList();
        if (outros.Count == 0) return;
        var cab = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        var ic = Icone.Criar("junto", 16);
        ic.Margin = new Thickness(0, 0, 10, 0);
        TextElement_Set(ic, P("Ambar"));
        cab.Children.Add(ic);
        var tt = T("Junto com outro botão", tamanho: 15);
        tt.FontWeight = FontWeights.Bold;
        cab.Children.Add(tt);
        _inspetor.Children.Add(cab);
        var atual = perfil.Combinacoes.FirstOrDefault(c => c.BotaoA == b.Id || c.BotaoB == b.Id);
        var outro = new ComboBox { Margin = new Thickness(26, 0, 0, 8) };
        var itens = new List<KeyValuePair<string, string>> { new("", "Nenhum") };
        itens.AddRange(outros.Select(o => new KeyValuePair<string, string>(o.Id, o.Nome)));
        outro.ItemsSource = itens;
        outro.DisplayMemberPath = "Value";
        outro.SelectedValuePath = "Key";
        outro.SelectedValue = atual is null ? "" : atual.BotaoA == b.Id ? atual.BotaoB : atual.BotaoA;
        _inspetor.Children.Add(outro);
        var ed = new EditorDeAcao("Escolha o que os dois fazem juntos") { Margin = new Thickness(26, 0, 0, 4), Visibility = atual is null ? Visibility.Collapsed : Visibility.Visible };
        ed.Definir(atual?.Acao);
        _inspetor.Children.Add(ed);
        _inspetor.Children.Add(T("Os dois apertados ao mesmo tempo. Segurar Avançar e Voltar juntos é um bom atalho escondido.", "Rotulo", 12, "Texto3", new Thickness(26, 0, 0, 0)));
        outro.SelectionChanged += (_, _) =>
        {
            perfil.Combinacoes.RemoveAll(c => c.BotaoA == b.Id || c.BotaoB == b.Id);
            var o = outro.SelectedValue as string;
            ed.Visibility = string.IsNullOrEmpty(o) ? Visibility.Collapsed : Visibility.Visible;
            if (!string.IsNullOrEmpty(o))
                perfil.Combinacoes.Add(new Combinacao { BotaoA = b.Id, BotaoB = o, Acao = Acao.DeTecla("B") });
            ed.Definir(perfil.Combinacoes.FirstOrDefault(c => c.BotaoA == b.Id)?.Acao);
            AgendarGravar();
        };
        ed.Mudou += a =>
        {
            var c = perfil.Combinacoes.FirstOrDefault(c => c.BotaoA == b.Id || c.BotaoB == b.Id);
            if (c is null) return;
            c.Acao = a ?? new Acao { Tipo = TipoAcao.Nada };
            AgendarGravar();
        };
    }

    /// <summary>Muda um gesto deste botão no perfil editado (cria a entrada própria do botão a partir do que valia pelo papel).</summary>
    void Gravar(Botao b, Action<MapaBotao> mudar)
    {
        var perfil = PerfilEditado;
        var chave = Perfil.ChaveBotao(_modelo.Id, b.Id);
        if (!perfil.Mapas.TryGetValue(chave, out var m))
        {
            m = perfil.Mapas.TryGetValue(Perfil.ChavePapel(b.Papel), out var pp) ? pp.Clonar() : new MapaBotao();
            perfil.Mapas[chave] = m;
        }
        mudar(m);
        if (m.Vazio) perfil.Mapas.Remove(chave);
        _palco.AtualizarLegendas();
        AgendarGravar();
    }

    void AgendarGravar()
    {
        _gravar.Stop();
        _gravar.Start();
    }

    // ---------------------------------------------------------------- ao vivo

    void TeclaVista(EventoTecla e)
    {
        if (e.ModeloId != _modelo.Id || e.BotaoId is null || e.Repeticao) return;
        if (!e.Soltou) _palco.Acender(e.BotaoId);
    }

    void AcaoFeita(EventoAcao e)
    {
        if (e.ModeloId != _modelo.Id) return;
        if (e.Gesto == Gesto.SegurarInicio) _palco.Acender(e.BotaoId, segurando: true);
        if (e.Gesto == Gesto.SegurarFim) { _palco.Apagar(e.BotaoId); return; }
        var b = _modelo.BotaoPorId(e.BotaoId);
        var gesto = e.Gesto switch
        {
            Gesto.Duplo => "dois toques", Gesto.SegurarInicio => "segurou", Gesto.Combinacao => "junto com " + (_modelo.BotaoPorId(e.Outro ?? "")?.Nome ?? e.Outro),
            _ => "um toque",
        };
        var perfil = _s.Config.Perfis.FirstOrDefault(p => p.Id == e.PerfilId)?.Nome ?? "";
        var item = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 28, 0) };
        item.Children.Add(T(b?.Nome ?? e.BotaoId, tamanho: 13));
        ((TextBlock)item.Children[^1]).FontWeight = FontWeights.Bold;
        item.Children.Add(T(gesto, "Rotulo", 13, margem: new Thickness(7, 0, 9, 0)));
        var seta = Icone.Criar("seta-dir", 13);
        TextElement_Set(seta, P("Ambar"));
        item.Children.Add(seta);
        item.Children.Add(T(e.Descricao, tamanho: 13, cor: "Ambar", margem: new Thickness(9, 0, 9, 0)));
        item.Children.Add(T(perfil, "Rotulo", 12, "Texto3"));
        _ticker.Children.Insert(0, item);
        while (_ticker.Children.Count > 3) _ticker.Children.RemoveAt(_ticker.Children.Count - 1);
        for (var i = 0; i < _ticker.Children.Count; i++) ((FrameworkElement)_ticker.Children[i]).Opacity = 1 - i * 0.28;
        item.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));
    }

    /// <summary>Abrir a tela já editando um perfil (vindo da tela Perfis).</summary>
    public void EditarPerfil(string perfilId)
    {
        _perfilEditado = perfilId;
        MontarPerfis();
        _palco.AtualizarLegendas();
        MontarInspetor();
    }

    /// <summary>Para as capturas da documentação: escolhe perfil e botão e mostra ações como se o passador tivesse sido usado.</summary>
    public void Demonstrar(string perfilId, string botao, IEnumerable<EventoAcao> eventos, string? aceso = null)
    {
        _perfilEditado = perfilId;
        MontarPerfis();
        Escolher(botao);
        _palco.AtualizarLegendas();
        foreach (var e in eventos) AcaoFeita(e);
        if (aceso is not null) _palco.Acender(aceso, segurando: true);
    }

    // ---------------------------------------------------------------- testar no desenho

    void LigarTeste(bool ligado)
    {
        _palco.ModoTeste = ligado;
        _recebido.Visibility = ligado ? Visibility.Visible : Visibility.Collapsed;
        _recebidoTeclas.Children.Clear();
        _s.Motor.Simular(NumeroSimulado, ligado ? _modelo.Id : null);
        _s.Motor.TestarComo(ligado ? _perfilEditado : null);
        if (ligado) _recebido.Focus();
        MontarCabeca();
    }

    void Testado(string id, bool apertou)
    {
        var b = _modelo.BotaoPorId(id);
        if (b?.Atalhos.FirstOrDefault() is not { Vk: not 0 } a) return;
        _recebido.Focus(); // as teclas de resposta chegam aqui, não num campo da tela
        if (apertou)
        {
            if (_recebidoTeclas.Children.Count > 8) _recebidoTeclas.Children.Clear();
            Simulador.Descer(NumeroSimulado, a);
        }
        else Simulador.Subir(NumeroSimulado, a);
    }

    void Recebeu(object sender, KeyEventArgs e)
    {
        if (_testar.IsChecked != true) return;
        if (Keyboard.FocusedElement is TextBox or ComboBox) return;
        var k = e.Key == Key.System ? e.SystemKey : e.Key;
        if (k is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt) return;
        if (k is Key.None or Key.ImeProcessed or Key.DeadCharProcessed || KeyInterop.VirtualKeyFromKey(k) == 0xE7) return; // texto: chega no TextInput
        e.Handled = true;
        if (e.IsRepeat) return;
        var vk = (ushort)KeyInterop.VirtualKeyFromKey(k);
        var mods = Mod.Nenhum;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) mods |= Mod.Ctrl;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) mods |= Mod.Shift;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) mods |= Mod.Alt;
        AdicionarRecebido(new Atalho(mods, vk).Rotulo());
    }

    void AdicionarRecebido(string texto)
    {
        var t = CapturaDeAtalho.Teclinha(texto, destaque: _recebidoTeclas.Children.Count == 0);
        _recebidoTeclas.Children.Insert(0, t);
        for (var i = 1; i < _recebidoTeclas.Children.Count; i++)
            ((TextBlock)((Border)_recebidoTeclas.Children[i]).Child).Foreground = P("Texto2");
        ((TextBlock)t.Child).Foreground = P("Ambar");
        while (_recebidoTeclas.Children.Count > 8) _recebidoTeclas.Children.RemoveAt(_recebidoTeclas.Children.Count - 1);
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Passador.App.Paginas;

/// <summary>Atalhos para montar tela em código com os estilos da casa (App.xaml).</summary>
static class Ui
{
    public static object R(string chave) => Application.Current.FindResource(chave);
    public static Brush P(string chave) => (Brush)R(chave);
    public static Style S(string chave) => (Style)R(chave);

    public static TextBlock T(string texto, string? estilo = null, double? tamanho = null, string? cor = null, Thickness? margem = null)
    {
        var t = new TextBlock { Text = texto };
        if (estilo is not null) t.Style = S(estilo);
        if (tamanho is { } s) t.FontSize = s;
        if (cor is not null) t.Foreground = P(cor);
        if (margem is { } m) t.Margin = m;
        return t;
    }

    public static Button Botao(string texto, string estilo = "BotaoBase", string? icone = null, Action? clique = null)
    {
        var b = new Button { Style = S(estilo) };
        var conteudo = new StackPanel { Orientation = Orientation.Horizontal };
        conteudo.SetBinding(TextElement.ForegroundProperty, new System.Windows.Data.Binding("Foreground") { Source = b });
        if (icone is not null)
        {
            var ic = Icone.Criar(icone, 15);
            ic.Margin = new Thickness(0, 0, 8, 0);
            conteudo.Children.Add(ic);
        }
        var tb = new TextBlock { Text = texto, VerticalAlignment = VerticalAlignment.Center };
        tb.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("Foreground") { Source = b });
        conteudo.Children.Add(tb);
        b.Content = conteudo;
        if (clique is not null) b.Click += (_, _) => clique();
        return b;
    }

    public static Border Cartao(UIElement filho, Thickness? padding = null, string fundo = "Painel") => new()
    {
        Background = P(fundo), BorderBrush = P("Borda"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(16),
        Padding = padding ?? new Thickness(22), Child = filho,
    };

    public static RadioButton Ficha(string texto, string grupo, bool marcado, Action aoMarcar, string? icone = null, UIElement? extra = null)
    {
        var conteudo = new StackPanel { Orientation = Orientation.Horizontal };
        if (icone is not null)
        {
            var ic = Icone.Criar(icone, 12);
            ic.Margin = new Thickness(0, 0, 6, 0);
            conteudo.Children.Add(ic);
        }
        var tb = new TextBlock { Text = texto };
        conteudo.Children.Add(tb);
        if (extra is not null) conteudo.Children.Add(extra);
        var r = new RadioButton { Style = S("Ficha"), GroupName = grupo, Content = conteudo, IsChecked = marcado, HorizontalAlignment = HorizontalAlignment.Left };
        r.Checked += (_, _) => aoMarcar();
        conteudo.SetBinding(TextElement.ForegroundProperty, new System.Windows.Data.Binding("Foreground") { Source = r });
        tb.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("Foreground") { Source = r });
        return r;
    }

    public static CheckBox Interruptor(string texto, bool marcado, Action<bool> mudou)
    {
        var c = new CheckBox { Style = S("Interruptor"), Content = new TextBlock { Text = texto, TextWrapping = TextWrapping.Wrap }, IsChecked = marcado };
        c.Checked += (_, _) => mudou(true);
        c.Unchecked += (_, _) => mudou(false);
        return c;
    }
}

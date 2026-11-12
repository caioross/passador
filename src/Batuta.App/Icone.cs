using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Batuta.App;

/// <summary>
/// Ícone de traço (desenhos do Lucide, licença ISC, redesenhados em 24x24). Nada de emoji: o dono vetou.
/// A cor vem do Foreground de quem está em volta.
/// </summary>
public sealed class Icone : Viewbox
{
    public static readonly Dictionary<string, string> Desenhos = new()
    {
        ["controle"] = "M9 2 L15 2 A3 3 0 0 1 18 5 L18 19 A3 3 0 0 1 15 22 L9 22 A3 3 0 0 1 6 19 L6 5 A3 3 0 0 1 9 2 Z M12 6.5 A1.8 1.8 0 1 0 12 10.1 A1.8 1.8 0 1 0 12 6.5 Z M12 13.5 L12 17",
        ["catalogo"] = "M4 3 L10 3 L10 10 L4 10 Z M14 3 L20 3 L20 10 L14 10 Z M14 14 L20 14 L20 21 L14 21 Z M4 14 L10 14 L10 21 L4 21 Z",
        ["perfis"] = "M12 2 L2 7 L12 12 L22 7 Z M2 17 L12 22 L22 17 M2 12 L12 17 L22 12",
        ["aprender"] = "M4.9 19.1 A10 10 0 0 1 4.9 4.9 M7.8 16.2 A6 6 0 0 1 7.8 7.8 M16.2 7.8 A6 6 0 0 1 16.2 16.2 M19.1 4.9 A10 10 0 0 1 19.1 19.1 M12 10 A2 2 0 1 0 12 14 A2 2 0 1 0 12 10 Z",
        ["ajustes"] = "M21 4 L14 4 M10 4 L3 4 M21 12 L12 12 M8 12 L3 12 M21 20 L16 20 M12 20 L3 20 M14 2 L14 6 M8 10 L8 14 M16 18 L16 22",
        ["mais"] = "M12 5 L12 19 M5 12 L19 12",
        ["fechar"] = "M18 6 L6 18 M6 6 L18 18",
        ["minimizar"] = "M5 12 L19 12",
        ["maximizar"] = "M5 5 L19 5 L19 19 L5 19 Z",
        ["check"] = "M20 6 L9 17 L4 12",
        ["teclado"] = "M4 6 L20 6 A2 2 0 0 1 22 8 L22 16 A2 2 0 0 1 20 18 L4 18 A2 2 0 0 1 2 16 L2 8 A2 2 0 0 1 4 6 Z M6 10 L6 10.01 M10 10 L10 10.01 M14 10 L14 10.01 M18 10 L18 10.01 M7 14 L17 14",
        ["texto"] = "M4 7 L4 4 L20 4 L20 7 M9 20 L15 20 M12 4 L12 20",
        ["sequencia"] = "M8 6 L21 6 M8 12 L21 12 M8 18 L21 18 M3 6 L3.01 6 M3 12 L3.01 12 M3 18 L3.01 18",
        ["abrir"] = "M15 3 L21 3 L21 9 M10 14 L21 3 M18 13 L18 19 A2 2 0 0 1 16 21 L5 21 A2 2 0 0 1 3 19 L3 8 A2 2 0 0 1 5 6 L11 6",
        ["mouse"] = "M12 2 A6 6 0 0 1 18 8 L18 16 A6 6 0 0 1 6 16 L6 8 A6 6 0 0 1 12 2 Z M12 6 L12 10",
        ["nada"] = "M12 2 A10 10 0 1 0 12 22 A10 10 0 1 0 12 2 Z M4.9 4.9 L19.1 19.1",
        ["fabrica"] = "M3 12 A9 9 0 1 0 6 5.3 L3 8 M3 3 L3 8 L8 8",
        ["monitor"] = "M4 3 L20 3 A2 2 0 0 1 22 5 L22 15 A2 2 0 0 1 20 17 L4 17 A2 2 0 0 1 2 15 L2 5 A2 2 0 0 1 4 3 Z M8 21 L16 21 M12 17 L12 21",
        ["presentation"] = "M2 3 L22 3 M3 3 L3 14 A2 2 0 0 0 5 16 L19 16 A2 2 0 0 0 21 14 L21 3 M7 21 L12 16 L17 21",
        ["app-window"] = "M4 4 L20 4 A2 2 0 0 1 22 6 L22 18 A2 2 0 0 1 20 20 L4 20 A2 2 0 0 1 2 18 L2 6 A2 2 0 0 1 4 4 Z M10 4 L10 8 M2 8 L22 8 M6 4 L6 8",
        ["file-text"] = "M15 2 L6 2 A2 2 0 0 0 4 4 L4 20 A2 2 0 0 0 6 22 L18 22 A2 2 0 0 0 20 20 L20 7 Z M14 2 L14 8 L20 8 M16 13 L8 13 M16 17 L8 17 M10 9 L8 9",
        ["youtube"] = "M2.5 17 A24 24 0 0 1 2.5 7 A2 2 0 0 1 4 5.5 A49 49 0 0 1 20 5.5 A2 2 0 0 1 21.5 7 A24 24 0 0 1 21.5 17 A2 2 0 0 1 20 18.5 A49 49 0 0 1 4 18.5 A2 2 0 0 1 2.5 17 Z M10 15 L15 12 L10 9 Z",
        ["video"] = "M16 13 L21.2 16.5 A.5 .5 0 0 0 22 16.1 L22 7.9 A.5 .5 0 0 0 21.2 7.5 L16 11 M4 6 L14 6 A2 2 0 0 1 16 8 L16 16 A2 2 0 0 1 14 18 L4 18 A2 2 0 0 1 2 16 L2 8 A2 2 0 0 1 4 6 Z",
        ["layers"] = "M12 2 L2 7 L12 12 L22 7 Z M2 17 L12 22 L22 17 M2 12 L12 17 L22 12",
        ["busca"] = "M11 4 A7 7 0 1 0 11 18 A7 7 0 1 0 11 4 Z M21 21 L16.7 16.7",
        ["lixo"] = "M3 6 L21 6 M19 6 L19 20 A2 2 0 0 1 17 22 L7 22 A2 2 0 0 1 5 20 L5 6 M8 6 L8 4 A2 2 0 0 1 10 2 L14 2 A2 2 0 0 1 16 4 L16 6",
        ["liga"] = "M12 2 L12 12 M18.4 6.6 A9 9 0 1 1 5.6 6.6",
        ["toque"] = "M12 9 A3 3 0 1 0 12 15 A3 3 0 1 0 12 9 Z",
        ["duplo"] = "M7 9 A3 3 0 1 0 7 15 A3 3 0 1 0 7 9 Z M17 9 A3 3 0 1 0 17 15 A3 3 0 1 0 17 9 Z",
        ["segurar"] = "M5 9 L19 9 A3 3 0 0 1 19 15 L5 15 A3 3 0 0 1 5 9 Z",
        ["junto"] = "M8 8 A4 4 0 1 0 8 16 A4 4 0 1 0 8 8 Z M16 8 A4 4 0 1 0 16 16 A4 4 0 1 0 16 8 Z",
        ["setas"] = "M8 3 L4 7 L8 11 M4 7 L20 7 M16 21 L20 17 L16 13 M20 17 L4 17",
        ["usb"] = "M10 7 L14 7 L14 3 L10 3 Z M8 7 L16 7 L16 15 A4 4 0 0 1 12 19 A4 4 0 0 1 8 15 Z M12 19 L12 22",
        ["bluetooth"] = "M7 7 L17 17 L12 22 L12 2 L17 7 L7 17",
        ["alerta"] = "M10.3 3.9 L1.8 18 A2 2 0 0 0 3.5 21 L20.5 21 A2 2 0 0 0 22.2 18 L13.7 3.9 A2 2 0 0 0 10.3 3.9 Z M12 9 L12 13 M12 17 L12.01 17",
        ["onda"] = "M2 12 L6 12 L9 3 L15 21 L18 12 L22 12",
        ["pasta"] = "M20 20 A2 2 0 0 0 22 18 L22 8 A2 2 0 0 0 20 6 L12.1 6 A2 2 0 0 1 10.4 5.1 L9.6 3.9 A2 2 0 0 0 7.9 3 L4 3 A2 2 0 0 0 2 5 L2 18 A2 2 0 0 0 4 20 Z",
        ["seta-dir"] = "M5 12 L19 12 M12 5 L19 12 L12 19",
        ["relogio"] = "M12 2 A10 10 0 1 0 12 22 A10 10 0 1 0 12 2 Z M12 6 L12 12 L16 14",
    };

    public static readonly DependencyProperty NomeProperty = DependencyProperty.Register(nameof(Nome), typeof(string), typeof(Icone),
        new PropertyMetadata("", (d, _) => ((Icone)d).Montar()));
    public static readonly DependencyProperty EspessuraProperty = DependencyProperty.Register(nameof(Espessura), typeof(double), typeof(Icone),
        new PropertyMetadata(1.9, (d, _) => ((Icone)d).Montar()));

    public string Nome { get => (string)GetValue(NomeProperty); set => SetValue(NomeProperty, value); }
    public double Espessura { get => (double)GetValue(EspessuraProperty); set => SetValue(EspessuraProperty, value); }

    public Icone()
    {
        Width = Height = 20;
        Stretch = Stretch.Uniform;
        SnapsToDevicePixels = true;
    }

    public static Icone Criar(string nome, double tamanho = 18) => new() { Nome = nome, Width = tamanho, Height = tamanho };

    void Montar()
    {
        if (!Desenhos.TryGetValue(Nome ?? "", out var d)) { Child = null; return; }
        var p = new Path
        {
            Data = Geometry.Parse(d), StrokeThickness = Espessura, StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round,
        };
        p.SetBinding(Shape.StrokeProperty, new Binding("(TextElement.Foreground)") { RelativeSource = RelativeSource.Self });
        Child = new Canvas { Width = 24, Height = 24, Children = { p } };
    }
}

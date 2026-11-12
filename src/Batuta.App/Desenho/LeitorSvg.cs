using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace Batuta.App.Desenho;

/// <summary>
/// O desenho de um passador lido de um SVG nosso (catalogo/desenhos): o corpo inteiro como Drawing e a forma de cada
/// botão (<c>&lt;g data-botao="id"&gt;</c>, primeiro filho) para clicar e acender. Leitor pequeno de propósito: só o
/// que tools/desenhos.py escreve (path, rect, circle, ellipse, line, text, g, gradientes).
/// </summary>
public sealed class DesenhoSvg
{
    public Rect Caixa { get; private init; }
    public DrawingGroup Corpo { get; private set; } = new();
    public Dictionary<string, Geometry> Botoes { get; } = [];

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly Dictionary<string, DesenhoSvg> Cache = [];

    public static DesenhoSvg Arquivo(string caminho)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(caminho, out var d)) return d;
            d = Ler(File.ReadAllText(caminho));
            return Cache[caminho] = d;
        }
    }

    public static DesenhoSvg Ler(string texto)
    {
        var raiz = XElement.Parse(texto);
        var vb = ((string?)raiz.Attribute("viewBox") ?? "0 0 100 100").Split(' ', ',', StringSplitOptions.RemoveEmptyEntries)
            .Select(n => double.Parse(n, Inv)).ToArray();
        var gradientes = new Dictionary<string, Brush>();
        foreach (var g in raiz.Descendants().Where(e => e.Name.LocalName is "linearGradient" or "radialGradient"))
            gradientes[(string)g.Attribute("id")!] = Gradiente(g);
        var d = new DesenhoSvg { Caixa = new Rect(vb[0], vb[1], vb[2], vb[3]) };
        var ctx = new Contexto(gradientes, d);
        var grupo = new DrawingGroup();
        foreach (var e in raiz.Elements()) Elemento(e, grupo, ctx, Herdado.Raiz, Matrix.Identity);
        // Garante a caixa inteira (para o desenho não "pular" quando o conteúdo não encosta nas bordas)
        grupo.Children.Insert(0, new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(d.Caixa)));
        grupo.Freeze();
        d.Corpo = grupo;
        return d;
    }

    sealed record Contexto(Dictionary<string, Brush> Gradientes, DesenhoSvg Destino);

    sealed record Herdado(string? Fill, string? Stroke, double StrokeWidth, double FillOpacity, double StrokeOpacity,
        string Cap, string Join, string? FontFamily, double FontSize, string? FontWeight, string? Anchor)
    {
        public static readonly Herdado Raiz = new("#000000", null, 1, 1, 1, "butt", "miter", "Segoe UI", 12, null, null);

        public Herdado Com(XElement e) => new(
            A(e, "fill") ?? Fill, A(e, "stroke") ?? Stroke, N(e, "stroke-width") ?? StrokeWidth,
            N(e, "fill-opacity") ?? FillOpacity, N(e, "stroke-opacity") ?? StrokeOpacity,
            A(e, "stroke-linecap") ?? Cap, A(e, "stroke-linejoin") ?? Join, A(e, "font-family") ?? FontFamily,
            N(e, "font-size") ?? FontSize, A(e, "font-weight") ?? FontWeight, A(e, "text-anchor") ?? Anchor);
    }

    static string? A(XElement e, string n) => (string?)e.Attribute(n);
    static double? N(XElement e, string n) => A(e, n) is { } s ? double.Parse(s.Replace("px", ""), Inv) : null;

    static void Elemento(XElement e, DrawingGroup pai, Contexto ctx, Herdado herdado, Matrix acumulado)
    {
        var nome = e.Name.LocalName;
        if (nome is "defs" or "linearGradient" or "radialGradient" or "title" or "desc") return;
        var h = herdado.Com(e);
        var local = Transformacao(A(e, "transform"));
        var total = local * acumulado;

        if (nome == "g")
        {
            var g = new DrawingGroup();
            if (!local.IsIdentity) g.Transform = new MatrixTransform(local);
            if (N(e, "opacity") is { } op) g.Opacity = op;
            var filhos = e.Elements().ToList();
            if (A(e, "data-botao") is { } botao && filhos.Count > 0 && Geometria(filhos[0], h) is { } forma)
            {
                var gf = forma.Clone();
                var m = Transformacao(A(filhos[0], "transform")) * total;
                if (!m.IsIdentity) gf.Transform = new MatrixTransform(m);
                ctx.Destino.Botoes[botao] = gf;
            }
            foreach (var f in filhos) Elemento(f, g, ctx, h, total);
            pai.Children.Add(g);
            return;
        }

        var geo = Geometria(e, h);
        if (geo is null) return;
        if (!local.IsIdentity) geo.Transform = new MatrixTransform(local);
        var fill = Pincel(h.Fill, h.FillOpacity, ctx);
        Pen? pen = null;
        if (Pincel(h.Stroke, h.StrokeOpacity, ctx) is { } sb)
        {
            pen = new Pen(sb, h.StrokeWidth)
            {
                StartLineCap = Cap(h.Cap), EndLineCap = Cap(h.Cap),
                LineJoin = h.Join switch { "round" => PenLineJoin.Round, "bevel" => PenLineJoin.Bevel, _ => PenLineJoin.Miter },
            };
        }
        var desenho = new GeometryDrawing(nome == "line" ? null : fill, pen, geo);
        if (N(e, "opacity") is { } o)
            pai.Children.Add(new DrawingGroup { Opacity = o, Children = { desenho } });
        else pai.Children.Add(desenho);
    }

    static PenLineCap Cap(string c) => c switch { "round" => PenLineCap.Round, "square" => PenLineCap.Square, _ => PenLineCap.Flat };

    static Geometry? Geometria(XElement e, Herdado h)
    {
        double V(string n) => N(e, n) ?? 0;
        switch (e.Name.LocalName)
        {
            case "path":
                return A(e, "d") is { } d ? Geometry.Parse("F1 " + d) : null;
            case "rect":
                var rx = N(e, "rx") ?? N(e, "ry") ?? 0;
                return new RectangleGeometry(new Rect(V("x"), V("y"), V("width"), V("height")), rx, N(e, "ry") ?? rx);
            case "circle":
                return new EllipseGeometry(new Point(V("cx"), V("cy")), V("r"), V("r"));
            case "ellipse":
                return new EllipseGeometry(new Point(V("cx"), V("cy")), V("rx"), V("ry"));
            case "line":
                return new LineGeometry(new Point(V("x1"), V("y1")), new Point(V("x2"), V("y2")));
            case "text":
                var ft = new FormattedText(e.Value, Inv, FlowDirection.LeftToRight,
                    new Typeface(new FontFamily(h.FontFamily ?? "Segoe UI"), FontStyles.Normal,
                        h.FontWeight is "bold" or "700" ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                    h.FontSize, Brushes.Black, 1.0);
                var x = V("x") - (h.Anchor == "middle" ? ft.Width / 2 : h.Anchor == "end" ? ft.Width : 0);
                return ft.BuildGeometry(new Point(x, V("y") - ft.Baseline));
            default:
                return null;
        }
    }

    static Brush? Pincel(string? valor, double opacidade, Contexto ctx)
    {
        if (valor is null or "none") return null;
        Brush b;
        if (valor.StartsWith("url(#"))
        {
            var id = valor[5..^1];
            if (!ctx.Gradientes.TryGetValue(id, out var g)) return null;
            b = g;
        }
        else b = new SolidColorBrush(Cor(valor));
        if (opacidade < 1)
        {
            b = b.Clone();
            b.Opacity = opacidade;
        }
        if (b.CanFreeze) b.Freeze();
        return b;
    }

    static Color Cor(string s)
    {
        s = s.Trim();
        if (s.Equals("white", StringComparison.OrdinalIgnoreCase)) return Colors.White;
        if (s.Equals("black", StringComparison.OrdinalIgnoreCase)) return Colors.Black;
        if (s.Length == 4) s = $"#{s[1]}{s[1]}{s[2]}{s[2]}{s[3]}{s[3]}";
        return (Color)ColorConverter.ConvertFromString(s);
    }

    static Brush Gradiente(XElement g)
    {
        var paradas = new GradientStopCollection();
        foreach (var s in g.Elements().Where(x => x.Name.LocalName == "stop"))
        {
            var c = Cor(A(s, "stop-color") ?? "#000000");
            if (N(s, "stop-opacity") is { } op) c.A = (byte)Math.Round(op * 255);
            var off = A(s, "offset") ?? "0";
            var o = off.EndsWith('%') ? double.Parse(off[..^1], Inv) / 100 : double.Parse(off, Inv);
            paradas.Add(new GradientStop(c, o));
        }
        double F(string n, double padrao) => A(g, n) is { } v ? (v.EndsWith('%') ? double.Parse(v[..^1], Inv) / 100 : double.Parse(v, Inv)) : padrao;
        Brush b = g.Name.LocalName == "linearGradient"
            ? new LinearGradientBrush(paradas, new Point(F("x1", 0), F("y1", 0)), new Point(F("x2", 1), F("y2", 0)))
            : new RadialGradientBrush(paradas)
            {
                Center = new Point(F("cx", 0.5), F("cy", 0.5)), GradientOrigin = new Point(F("fx", F("cx", 0.5)), F("fy", F("cy", 0.5))),
                RadiusX = F("r", 0.5), RadiusY = F("r", 0.5),
            };
        b.Freeze();
        return b;
    }

    static Matrix Transformacao(string? t)
    {
        var m = Matrix.Identity;
        if (string.IsNullOrWhiteSpace(t)) return m;
        foreach (System.Text.RegularExpressions.Match x in System.Text.RegularExpressions.Regex.Matches(t, @"(\w+)\s*\(([^)]*)\)"))
        {
            var n = x.Groups[2].Value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).Select(v => double.Parse(v, Inv)).ToArray();
            var op = Matrix.Identity;
            switch (x.Groups[1].Value)
            {
                case "translate": op.Translate(n[0], n.Length > 1 ? n[1] : 0); break;
                case "scale": op.Scale(n[0], n.Length > 1 ? n[1] : n[0]); break;
                case "rotate":
                    if (n.Length == 3) op.RotateAt(n[0], n[1], n[2]); else op.Rotate(n[0]);
                    break;
                case "matrix": op = new Matrix(n[0], n[1], n[2], n[3], n[4], n[5]); break;
            }
            // SVG aplica da direita para a esquerda: a última transformação da lista é a primeira no ponto
            m = op * m;
        }
        return m;
    }
}

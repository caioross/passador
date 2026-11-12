using System.Globalization;
using System.Security;
using Passador.Core;

namespace Passador.App.Desenho;

/// <summary>
/// Desenho para o modelo ensinado no modo aprender (ou sem desenho próprio): um bastão escuro com um botão redondo por
/// botão ensinado, na ordem em que foram apertados, com as iniciais do nome.
/// </summary>
public static class DesenhoGenerico
{
    public static string Svg(Modelo m)
    {
        var botoes = m.Botoes.Count == 0 ? [new Botao { Id = "vazio", Nome = "?" }] : m.Botoes;
        var n = botoes.Count;
        var passo = 56.0;
        var topo = 110.0;
        var altura = Math.Max(420, topo + n * passo + 140);
        var inv = CultureInfo.InvariantCulture;
        string F(double v) => v.ToString("0.##", inv);
        var sb = new System.Text.StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 160 {F(altura)}\">");
        sb.Append("<defs><linearGradient id=\"corpo\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"0\"><stop offset=\"0\" stop-color=\"#0b0b0e\"/>"
                  + "<stop offset=\"0.18\" stop-color=\"#2c2b33\"/><stop offset=\"0.4\" stop-color=\"#1b1a20\"/><stop offset=\"1\" stop-color=\"#050506\"/></linearGradient>"
                  + "<radialGradient id=\"botao\" cx=\"0.38\" cy=\"0.3\" r=\"0.85\"><stop offset=\"0\" stop-color=\"#3d3c45\"/><stop offset=\"1\" stop-color=\"#16151a\"/></radialGradient>"
                  + "<linearGradient id=\"brilho\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#ffffff\" stop-opacity=\"0.2\"/><stop offset=\"1\" stop-color=\"#ffffff\" stop-opacity=\"0\"/></linearGradient></defs>");
        sb.Append($"<rect x=\"36\" y=\"20\" width=\"88\" height=\"{F(altura - 40)}\" rx=\"44\" fill=\"url(#corpo)\" stroke=\"#000000\" stroke-width=\"1.5\"/>");
        sb.Append($"<path d=\"M48 60 C44 100 44 160 44 240 L46 {F(altura - 70)} L56 {F(altura - 70)} L54 240 C54 160 54 100 58 64 Z\" fill=\"url(#brilho)\"/>");
        for (var i = 0; i < n; i++)
        {
            var b = botoes[i];
            var y = topo + i * passo;
            var ini = (i + 1).ToString();
            sb.Append($"<g data-botao=\"{SecurityElement.Escape(b.Id)}\"><circle cx=\"80\" cy=\"{F(y)}\" r=\"21\" fill=\"url(#botao)\" stroke=\"#000000\" stroke-opacity=\"0.6\" stroke-width=\"1.2\"/>");
            sb.Append($"<text x=\"80\" y=\"{F(y + 5)}\" font-family=\"Segoe UI\" font-weight=\"bold\" font-size=\"13\" fill=\"#e9e7ef\" text-anchor=\"middle\">{ini}</text></g>");
        }
        sb.Append("</svg>");
        return sb.ToString();
    }

    static string Iniciais(string nome)
    {
        var p = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (p.Length == 0) return "?";
        return p.Length == 1 ? p[0][..Math.Min(2, p[0].Length)] : $"{p[0][0]}{p[1][0]}".ToUpperInvariant();
    }
}

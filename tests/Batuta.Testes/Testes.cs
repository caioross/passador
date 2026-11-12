using System.Xml.Linq;
using Batuta.Core;

namespace Batuta.Testes;

public class AtalhoTestes
{
    [Theory]
    [InlineData("PageDown", "PageDown")]
    [InlineData("Shift+F5", "Shift+F5")]
    [InlineData("ctrl+shift+f5", "Ctrl+Shift+F5")]
    [InlineData("Ctrl++", "Ctrl+Mais")]
    [InlineData(".", "Ponto")]
    [InlineData("Alt+Tab", "Alt+Tab")]
    [InlineData("Win", "Win")]
    public void LeEEscreve(string entrada, string saida) => Assert.Equal(saida, Atalho.Ler(entrada)!.Value.ToString());

    [Fact]
    public void TeclaQueNaoExisteDaNulo() => Assert.Null(Atalho.Ler("Ctrl+Banana"));
}

public class GestosTestes
{
    static MapaBotao Mapa(bool duplo = false, bool segurar = false, ModoSegurar modo = ModoSegurar.UmaVez) => new()
    {
        Toque = Acao.DeTecla("Right"),
        Duplo = duplo ? Acao.DeTecla("End") : null,
        Segurar = segurar ? Acao.DeTecla("Home") : null,
        ModoSegurar = modo,
        RepetirMs = 100,
    };

    static List<Gesto> Rodar(DetectorDeGestos d, params (long t, string acao)[] passos)
    {
        var saida = new List<Gesto>();
        foreach (var (t, a) in passos)
        {
            var ev = a switch { "desce" => d.Apertou("b", t), "sobe" => d.Soltou("b", t), _ => d.Tick(t) };
            saida.AddRange(ev.Select(e => e.Gesto));
        }
        return saida;
    }

    [Fact]
    public void SemDuploNemSegurarDisparaNoApertar()
    {
        var d = new DetectorDeGestos(_ => Mapa());
        Assert.Equal([Gesto.Toque], d.Apertou("b", 0).Select(e => e.Gesto));
        Assert.Empty(d.Soltou("b", 50));
    }

    [Fact]
    public void ComSegurarOToqueSaiNoSoltar()
    {
        var d = new DetectorDeGestos(_ => Mapa(segurar: true));
        Assert.Equal([Gesto.Toque], Rodar(d, (0, "desce"), (80, "sobe"), (200, "tick")));
    }

    [Fact]
    public void SegurarUmaVez()
    {
        var d = new DetectorDeGestos(_ => Mapa(segurar: true)) { SegurarMs = 500 };
        Assert.Equal([Gesto.SegurarInicio, Gesto.SegurarFim], Rodar(d, (0, "desce"), (400, "tick"), (520, "tick"), (900, "tick"), (1000, "sobe"), (1100, "tick")));
    }

    [Fact]
    public void SegurarRepetindo()
    {
        var d = new DetectorDeGestos(_ => Mapa(segurar: true, modo: ModoSegurar.Repetir)) { SegurarMs = 500 };
        var g = Rodar(d, (0, "desce"), (500, "tick"), (600, "tick"), (700, "tick"), (750, "sobe"), (900, "tick"));
        Assert.Equal([Gesto.SegurarInicio, Gesto.SegurarRepete, Gesto.SegurarRepete, Gesto.SegurarFim], g);
    }

    [Fact]
    public void DoisToques()
    {
        var d = new DetectorDeGestos(_ => Mapa(duplo: true)) { DuploMs = 300 };
        Assert.Equal([Gesto.Duplo], Rodar(d, (0, "desce"), (60, "sobe"), (180, "desce"), (240, "sobe"), (700, "tick")));
    }

    [Fact]
    public void UmToqueComDuploLigadoEsperaAJanela()
    {
        var d = new DetectorDeGestos(_ => Mapa(duplo: true)) { DuploMs = 300 };
        Assert.Empty(Rodar(d, (0, "desce"), (60, "sobe"), (300, "tick")));
        Assert.Equal([Gesto.Toque], Rodar(d, (400, "tick")));
    }

    [Fact]
    public void RajadaDeApertaSoltaViraSegurar()
    {
        // clones que, segurados, mandam apertar-soltar a cada 30 ms
        var d = new DetectorDeGestos(_ => Mapa(segurar: true)) { SegurarMs = 500 };
        var passos = new List<(long, string)>();
        for (long t = 0; t < 900; t += 30) { passos.Add((t, "desce")); passos.Add((t + 10, "sobe")); passos.Add((t + 11, "tick")); }
        passos.Add((1100, "tick"));
        var g = Rodar(d, [.. passos]);
        Assert.Equal([Gesto.SegurarInicio, Gesto.SegurarFim], g);
    }

    [Fact]
    public void DoisBotoesJuntos()
    {
        var d = new DetectorDeGestos(_ => Mapa(segurar: true), (a, b) => true);
        Assert.Empty(d.Apertou("a", 0));
        var ev = d.Apertou("b", 40);
        Assert.Single(ev);
        Assert.Equal(Gesto.Combinacao, ev[0].Gesto);
        Assert.Empty(d.Soltou("a", 100).Concat(d.Soltou("b", 110)).Concat(d.Tick(2000)));
    }
}

public class PerfisTestes
{
    static readonly Botao Avancar = new() { Id = "avancar", Papel = "avancar", Envia = ["PageDown"] };

    [Fact]
    public void EscolhePeloProgramaEPeloTitulo()
    {
        var p = PerfisProntos.Todos();
        Assert.Equal("powerpoint", Resolucao.Escolher(p, "POWERPNT.EXE", "Apresentação1").Id);
        Assert.Equal("google-slides", Resolucao.Escolher(p, "chrome", "Aula 3 - Apresentações Google - Google Chrome").Id);
        Assert.Equal("youtube", Resolucao.Escolher(p, "msedge", "Música - YouTube").Id);
        Assert.Equal("pdf", Resolucao.Escolher(p, "chrome", "manual.pdf - Google Chrome").Id);
        Assert.Equal(Perfil.IdPadrao, Resolucao.Escolher(p, "notepad", "sem título").Id);
    }

    [Fact]
    public void PerfilDesligadoNaoEntra()
    {
        var p = PerfisProntos.Todos();
        p.First(x => x.Id == "powerpoint").Ativo = false;
        Assert.Equal(Perfil.IdPadrao, Resolucao.Escolher(p, "POWERPNT", "").Id);
    }

    [Fact]
    public void OQueOPerfilNaoDizVemDoPadrao()
    {
        var p = PerfisProntos.Todos();
        var padrao = p.First(x => x.Id == Perfil.IdPadrao);
        padrao.Mapas[Perfil.ChavePapel("avancar")] = new MapaBotao { Duplo = Acao.DeTecla("W") };
        var m = Resolucao.MapaEfetivo(p, p.First(x => x.Id == "powerpoint"), "qualquer", Avancar)!;
        Assert.Equal("End", m.Segurar!.Tecla);   // do PowerPoint
        Assert.Equal("W", m.Duplo!.Tecla);        // do Padrão
        Assert.Null(m.Toque);                     // de fábrica
    }

    [Fact]
    public void BotaoDoModeloGanhaDoPapel()
    {
        var p = PerfisProntos.Todos();
        var yt = p.First(x => x.Id == "youtube");
        yt.Mapas[Perfil.ChaveBotao("m", "avancar")] = new MapaBotao { Toque = Acao.DeTecla("Right") };
        Assert.Equal("Right", Resolucao.MapaEfetivo(p, yt, "m", Avancar)!.Toque!.Tecla);
        Assert.Equal("L", Resolucao.MapaEfetivo(p, yt, "outro", Avancar)!.Toque!.Tecla);
    }
}

public class CatalogoTestes
{
    static readonly string Pasta = Path.Combine(AppContext.BaseDirectory, "catalogo");

    [Fact]
    public void TodoModeloTemDesenhoComOsBotoesDele()
    {
        var c = Catalogo.Ler(Path.Combine(Pasta, "modelos.json"));
        Assert.True(c.Modelos.Count >= 15);
        foreach (var m in c.Modelos)
        {
            var svg = Path.Combine(Pasta, "desenhos", m.Desenho + ".svg");
            Assert.True(File.Exists(svg), $"{m.Id}: falta {m.Desenho}.svg");
            var ids = XDocument.Load(svg).Descendants().Select(e => (string?)e.Attribute("data-botao")).Where(x => x is not null).ToHashSet();
            foreach (var b in m.Botoes) Assert.True(ids.Contains(b.Id), $"{m.Id}: o desenho não tem o botão {b.Id}");
            foreach (var b in m.Botoes) foreach (var e in b.Envia) Assert.True(Atalho.Ler(e) is not null, $"{m.Id}/{b.Id}: tecla {e}");
        }
        Assert.Equal(c.Modelos.Count, c.Modelos.Select(m => m.Id).Distinct().Count());
    }

    [Fact]
    public void ReconheceOR400PeloReceptor()
    {
        var c = Catalogo.Ler(Path.Combine(Pasta, "modelos.json"));
        Assert.Equal("logitech-r400", c.Candidatos("046D", "C52D")[0].Id);
        Assert.Equal("norwii-n95", c.Candidatos("3243", "0382")[0].Id);
        Assert.Contains(c.Candidatos("3243", "9999"), m => m.Id == "norwii-n95"); // só o fabricante: palpite
    }

    [Fact]
    public void BotaoQueAlternaF5EEscEhOMesmo()
    {
        var c = Catalogo.Ler(Path.Combine(Pasta, "modelos.json"));
        var r400 = c.PorId("logitech-r400")!;
        Assert.Equal("iniciar", r400.BotaoDoAtalho(Atalho.Ler("F5")!.Value)!.Id);
        Assert.Equal("iniciar", r400.BotaoDoAtalho(Atalho.Ler("Esc")!.Value)!.Id);
        Assert.Equal("tela-preta", r400.BotaoDoAtalho(Atalho.Ler("Ponto")!.Value)!.Id);
    }

    [Fact]
    public void ConfiguracaoIdaEVolta()
    {
        var arq = Path.Combine(Path.GetTempPath(), $"batuta-teste-{Guid.NewGuid():N}.json");
        var cfg = new Configuracao();
        cfg.Perfis[0].Mapas[Perfil.ChaveBotao("x", "y")] = new MapaBotao
        {
            Toque = new Acao { Tipo = TipoAcao.Sequencia, Passos = [new Passo { Tipo = TipoPasso.Texto, Valor = "olá" }, new Passo { Tipo = TipoPasso.Espera, Valor = "200" }] },
            ModoSegurar = ModoSegurar.Enquanto,
        };
        cfg.Gravar(arq);
        var lido = Configuracao.Ler(arq);
        File.Delete(arq);
        var m = lido.Perfis[0].Mapas[Perfil.ChaveBotao("x", "y")];
        Assert.Equal(ModoSegurar.Enquanto, m.ModoSegurar);
        Assert.Equal("olá", m.Toque!.Passos![0].Valor);
    }
}

public class ReceptorCompartilhadoTestes
{
    [Fact]
    public void ReceptorBoltSoViraSpotlightSeONomeDisser()
    {
        var c = Catalogo.Ler(Path.Combine(AppContext.BaseDirectory, "catalogo", "modelos.json"));
        Assert.Null(c.Reconhecer("046D", "C548", nomeDePassador: false));            // teclado num receptor Bolt: não toca
        Assert.Equal("logitech-spotlight-2", c.Reconhecer("046D", "C548", true)!.Id);
        Assert.Equal("logitech-r400", c.Reconhecer("046D", "C52D", false)!.Id);      // receptor só de passador
    }
}

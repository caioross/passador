using System.Windows;
using Passador.Core;
using Passador.Motor;
using D = System.Drawing;
using F = System.Windows.Forms;

namespace Passador.App;

/// <summary>
/// Ícone na bandeja. Clique abre a janela. O menu (montado na hora de abrir, sempre com o estado de agora) tem:
/// o aparelho ligado e a bateria, o perfil em uso com troca rápida, pausar e retomar, abrir, testar, iniciar com o
/// Windows, procurar atualização, modo apresentação e sair.
/// </summary>
sealed class Bandeja : IDisposable
{
    readonly F.NotifyIcon _icone = new();
    readonly Servicos _s;
    readonly Action _abrir, _testar, _sair;
    readonly D.Icon _normal, _pausado;

    public Bandeja(Servicos s, Action abrir, Action testar, Action sair)
    {
        _s = s;
        _abrir = abrir;
        _testar = testar;
        _sair = sair;
        using (var st = Application.GetResourceStream(new Uri("pack://application:,,,/passador.ico")).Stream)
            _normal = new D.Icon(st, F.SystemInformation.SmallIconSize);
        _pausado = Esmaecido(_normal);
        _icone.Icon = _normal;
        _icone.ContextMenuStrip = new F.ContextMenuStrip { Font = new D.Font("Segoe UI", 9.5f) };
        _icone.ContextMenuStrip.Opening += (_, e) => { Montar(); e.Cancel = false; };
        _icone.MouseClick += (_, e) => { if (e.Button == F.MouseButtons.Left) _abrir(); };
        Atualizar();
        _icone.Visible = true;
        s.PerfilMudou += _ => Atualizar();
        s.GanchoMudou += Atualizar;
        s.AparelhosMudaram += Atualizar;
        s.ConfigMudou += Atualizar;
    }

    string NomePerfil(string? id) => _s.Config.Perfis.FirstOrDefault(x => x.Id == id)?.Nome ?? "Padrão";

    void Atualizar()
    {
        var p = _s.PassadorConectado;
        var pausado = !_s.Config.Ajustes.MotorLigado;
        _icone.Icon = pausado ? _pausado : _normal;
        var dica = pausado ? "Passador: pausado"
            : p is null ? "Passador: nenhum passador ligado"
            : $"Passador: {p.Value.Modelo.NomeCompleto}, perfil {NomePerfil(_s.PerfilEmUso)}";
        if (_s.Apresentacao.Ligado) dica += " (modo apresentação)";
        _icone.Text = dica.Length > 127 ? dica[..126] + "…" : dica;
    }

    static F.ToolStripMenuItem Titulo(string texto) => new(texto) { Enabled = false };

    void Montar()
    {
        var m = _icone.ContextMenuStrip!;
        m.Items.Clear();

        // Aparelho e bateria
        var p = _s.PassadorConectado;
        if (p is { } ligado)
        {
            m.Items.Add(Titulo(ligado.Modelo.NomeCompleto));
            var bat = ligado.Aparelho.Simulado ? null : Bateria.Ler(ligado.Aparelho.Caminho);
            var conexao = ligado.Aparelho.Simulado ? "simulado" : ligado.Aparelho.Bluetooth ? "Bluetooth" : "receptor USB";
            m.Items.Add(Titulo(bat is { } b ? $"Bateria: {b}%  ·  {conexao}" : $"Bateria: não informada  ·  {conexao}"));
        }
        else m.Items.Add(Titulo("Nenhum passador ligado"));
        m.Items.Add(new F.ToolStripSeparator());

        // Perfil com troca rápida
        var fixo = _s.Config.Ajustes.PerfilFixo;
        var perfis = new F.ToolStripMenuItem(fixo is null
            ? $"Perfil: automático ({NomePerfil(_s.PerfilEmUso)})"
            : $"Perfil: {NomePerfil(fixo)}");
        var auto = new F.ToolStripMenuItem("Automático (segue o programa da frente)") { Checked = fixo is null };
        auto.Click += (_, _) => _s.EscolherPerfil(null);
        perfis.DropDownItems.Add(auto);
        perfis.DropDownItems.Add(new F.ToolStripSeparator());
        foreach (var perfil in _s.Config.Perfis)
        {
            var id = perfil.Id;
            var item = new F.ToolStripMenuItem(perfil.Nome) { Checked = fixo == id };
            item.Click += (_, _) => _s.EscolherPerfil(id);
            perfis.DropDownItems.Add(item);
        }
        m.Items.Add(perfis);

        var pausado = !_s.Config.Ajustes.MotorLigado;
        m.Items.Add(pausado ? "Retomar o remapeamento" : "Pausar o remapeamento", null, (_, _) =>
        {
            _s.Config.Ajustes.MotorLigado = pausado;
            _s.Salvar();
            Avisar("Passador", pausado ? "Remapeamento ligado de novo." : "Remapeamento pausado: o passador volta a mandar as teclas de fábrica.");
        });
        m.Items.Add(new F.ToolStripSeparator());

        m.Items.Add("Abrir o Passador", null, (_, _) => _abrir());
        m.Items.Add("Testar os botões", null, (_, _) => _testar());
        m.Items.Add(new F.ToolStripSeparator());

        var apresentacao = new F.ToolStripMenuItem("Modo apresentação") { Checked = _s.Apresentacao.Ligado };
        apresentacao.Click += (_, _) =>
        {
            if (_s.Apresentacao.Ligado) { _s.Apresentacao.Desligar(); Avisar("Passador", "Modo apresentação desligado."); }
            else Avisar("Passador", _s.Apresentacao.Ligar());
            Atualizar();
        };
        m.Items.Add(apresentacao);

        var iniciar = new F.ToolStripMenuItem("Iniciar com o Windows") { Checked = _s.Config.Ajustes.IniciarComWindows };
        iniciar.Click += (_, _) =>
        {
            _s.Config.Ajustes.IniciarComWindows = !_s.Config.Ajustes.IniciarComWindows;
            _s.Salvar();
        };
        m.Items.Add(iniciar);

        if (_s.Atualizador.Pronta)
            m.Items.Add($"Instalar a versão {_s.Atualizador.NovaVersao} (reinicia o Passador)", null, (_, _) => _s.Atualizador.AplicarAgora());
        else
            m.Items.Add("Procurar atualização", null, async (_, _) =>
            {
                if (!_s.Atualizador.Instalado) { Avisar("Passador", _s.Atualizador.Status); return; }
                await _s.Atualizador.Verificar(manual: true);
                Avisar("Passador", _s.Atualizador.Status);
            });
        m.Items.Add(new F.ToolStripSeparator());
        m.Items.Add("Sair", null, (_, _) => _sair());
    }

    public void Avisar(string titulo, string texto) => _icone.ShowBalloonTip(5000, titulo, texto, F.ToolTipIcon.Info);

    /// <summary>O mesmo ícone em cinza e mais apagado (pausado).</summary>
    static D.Icon Esmaecido(D.Icon icone)
    {
        using var bmp = icone.ToBitmap();
        for (var y = 0; y < bmp.Height; y++)
            for (var x = 0; x < bmp.Width; x++)
            {
                var c = bmp.GetPixel(x, y);
                var g = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
                bmp.SetPixel(x, y, D.Color.FromArgb(c.A * 55 / 100, g, g, g));
            }
        return D.Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _icone.Visible = false;
        _icone.Dispose();
    }
}

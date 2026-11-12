using System.Windows;
using Batuta.Core;
using D = System.Drawing;
using F = System.Windows.Forms;

namespace Batuta.App;

/// <summary>Ícone na bandeja: clique abre a janela; o menu tem o perfil em uso, ligar e desligar o motor e sair.</summary>
sealed class Bandeja : IDisposable
{
    readonly F.NotifyIcon _icone = new();
    readonly Servicos _s;
    readonly Action _abrir, _sair;

    public Bandeja(Servicos s, Action abrir, Action sair)
    {
        _s = s;
        _abrir = abrir;
        _sair = sair;
        using (var st = Application.GetResourceStream(new Uri("pack://application:,,,/batuta.ico")).Stream)
            _icone.Icon = new D.Icon(st, F.SystemInformation.SmallIconSize);
        _icone.ContextMenuStrip = new F.ContextMenuStrip { ShowImageMargin = false, Font = new D.Font("Segoe UI", 9.5f) };
        _icone.ContextMenuStrip.Opening += (_, e) => { Montar(); e.Cancel = false; };
        _icone.MouseClick += (_, e) => { if (e.Button == F.MouseButtons.Left) _abrir(); };
        Atualizar();
        _icone.Visible = true;
        s.PerfilMudou += _ => Atualizar();
        s.GanchoMudou += Atualizar;
        s.AparelhosMudaram += Atualizar;
    }

    void Atualizar()
    {
        var p = _s.PassadorConectado;
        var perfil = _s.Config.Perfis.FirstOrDefault(x => x.Id == _s.PerfilEmUso)?.Nome ?? "Padrão";
        var dica = !_s.Config.Ajustes.MotorLigado ? "Batuta: motor desligado"
            : p is null ? "Batuta: nenhum passador ligado"
            : $"Batuta: {p.Value.Modelo.NomeCompleto}, perfil {perfil}";
        _icone.Text = dica.Length > 127 ? dica[..126] + "…" : dica;
    }

    void Montar()
    {
        var m = _icone.ContextMenuStrip!;
        m.Items.Clear();
        m.Items.Add("Abrir o Batuta", null, (_, _) => _abrir());
        m.Items.Add(new F.ToolStripSeparator());
        var perfil = _s.Config.Perfis.FirstOrDefault(x => x.Id == _s.PerfilEmUso)?.Nome ?? "Padrão";
        m.Items.Add(new F.ToolStripMenuItem($"Perfil em uso: {perfil}") { Enabled = false });
        var ligado = new F.ToolStripMenuItem("Motor ligado") { Checked = _s.Config.Ajustes.MotorLigado, CheckOnClick = true };
        ligado.CheckedChanged += (_, _) =>
        {
            _s.Config.Ajustes.MotorLigado = ligado.Checked;
            _s.Salvar();
        };
        m.Items.Add(ligado);
        m.Items.Add(new F.ToolStripSeparator());
        m.Items.Add("Sair", null, (_, _) => _sair());
    }

    public void Avisar(string titulo, string texto) => _icone.ShowBalloonTip(5000, titulo, texto, F.ToolTipIcon.Info);

    public void Dispose()
    {
        _icone.Visible = false;
        _icone.Dispose();
    }
}

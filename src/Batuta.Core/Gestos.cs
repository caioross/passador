namespace Batuta.Core;

public enum Gesto { Toque, Duplo, SegurarInicio, SegurarRepete, SegurarFim, Combinacao }

public readonly record struct EventoGesto(Gesto Gesto, string Botao, MapaBotao? Mapa, string? Outro = null);

/// <summary>
/// Transforma apertar e soltar de cada botão em gestos: toque, dois toques, segurar (uma vez, repetindo ou enquanto
/// estiver apertado) e dois botões juntos. Sem relógio próprio: quem usa passa o tempo (ms) e chama
/// <see cref="Tick"/> no <see cref="ProximoPrazo"/>, o que deixa tudo testável.
///
/// Regras que dão a sensação certa:
/// - botão sem duplo nem segurar dispara no APERTAR (o slide anda na hora);
/// - com segurar, o toque sai no soltar; com duplo, sai quando a janela do segundo toque fecha;
/// - o segundo toque dispara no apertar;
/// - aparelho que, segurado, manda apertar-soltar em rajada (alguns clones fazem isso em vez de repetir a tecla) é
///   lido como um segurar contínuo: soltura seguida de novo aperto em menos de <see cref="RajadaMs"/> é emendada.
/// </summary>
public sealed class DetectorDeGestos(Func<string, MapaBotao?> mapaDe, Func<string, string, bool>? combinacaoExiste = null)
{
    public const int RajadaMs = 45;
    public int DuploMs { get; set; } = 300;
    public int SegurarMs { get; set; } = 550;

    sealed class Estado
    {
        public bool Baixo, Consumido, Segurando, EsperandoSegundo, SoltandoPendente;
        public long DesceuEm, ProxRepete, PrazoSegundo, SoltarEm;
        public MapaBotao? Mapa;
    }

    readonly Dictionary<string, Estado> _estados = [];
    Estado E(string b) => _estados.TryGetValue(b, out var s) ? s : _estados[b] = new Estado();

    public bool AlgumApertado => _estados.Values.Any(s => s.Baixo);

    public List<EventoGesto> Apertou(string botao, long t)
    {
        var saida = new List<EventoGesto>();
        var s = E(botao);
        if (s.SoltandoPendente)
        {
            // rajada: o aparelho soltou e apertou de novo depressa demais para ser gente; continua segurando
            s.SoltandoPendente = false;
            return saida;
        }
        if (s.Baixo) return saida; // repetição da própria tecla (autorepeat)

        var outro = _estados.FirstOrDefault(kv => kv.Key != botao && kv.Value.Baixo && !kv.Value.SoltandoPendente);
        if (outro.Value is not null && combinacaoExiste?.Invoke(outro.Key, botao) == true)
        {
            saida.Add(new EventoGesto(Gesto.Combinacao, outro.Key, null, botao));
            outro.Value.Consumido = true;
            outro.Value.EsperandoSegundo = false;
            s.Baixo = true; s.Consumido = true; s.Segurando = false; s.DesceuEm = t; s.Mapa = null;
            return saida;
        }

        var mapa = mapaDe(botao);
        s.Mapa = mapa; s.Baixo = true; s.DesceuEm = t; s.Segurando = false; s.Consumido = false;

        if (s.EsperandoSegundo)
        {
            s.EsperandoSegundo = false;
            s.Consumido = true;
            saida.Add(new EventoGesto(Gesto.Duplo, botao, mapa));
            return saida;
        }
        if (mapa is null || (!mapa.TemDuplo && !mapa.TemSegurar))
        {
            s.Consumido = true;
            saida.Add(new EventoGesto(Gesto.Toque, botao, mapa));
        }
        return saida;
    }

    public List<EventoGesto> Soltou(string botao, long t)
    {
        var saida = new List<EventoGesto>();
        var s = E(botao);
        if (!s.Baixo || s.SoltandoPendente) return saida;
        if (s.Mapa?.TemSegurar == true && !s.Consumido)
        {
            s.SoltandoPendente = true;
            s.SoltarEm = t + RajadaMs;
            return saida;
        }
        ProcessarSoltura(botao, s, t, saida);
        return saida;
    }

    void ProcessarSoltura(string botao, Estado s, long t, List<EventoGesto> saida)
    {
        s.Baixo = false;
        s.SoltandoPendente = false;
        if (s.Segurando)
        {
            s.Segurando = false;
            saida.Add(new EventoGesto(Gesto.SegurarFim, botao, s.Mapa));
            return;
        }
        if (s.Consumido) return;
        if (s.Mapa?.TemDuplo == true)
        {
            s.EsperandoSegundo = true;
            s.PrazoSegundo = t + DuploMs;
            return;
        }
        saida.Add(new EventoGesto(Gesto.Toque, botao, s.Mapa));
    }

    public List<EventoGesto> Tick(long t)
    {
        var saida = new List<EventoGesto>();
        foreach (var (botao, s) in _estados)
        {
            if (s.SoltandoPendente && t >= s.SoltarEm) ProcessarSoltura(botao, s, s.SoltarEm, saida);
            if (s.Baixo && !s.Consumido && !s.Segurando && s.Mapa?.TemSegurar == true && t - s.DesceuEm >= SegurarMs)
            {
                s.Segurando = true;
                s.ProxRepete = t + Math.Max(30, s.Mapa.RepetirMs);
                saida.Add(new EventoGesto(Gesto.SegurarInicio, botao, s.Mapa));
            }
            else if (s.Segurando && s.Mapa?.ModoSegurar == ModoSegurar.Repetir && t >= s.ProxRepete)
            {
                s.ProxRepete += Math.Max(30, s.Mapa.RepetirMs);
                saida.Add(new EventoGesto(Gesto.SegurarRepete, botao, s.Mapa));
            }
            if (s.EsperandoSegundo && t > s.PrazoSegundo)
            {
                s.EsperandoSegundo = false;
                saida.Add(new EventoGesto(Gesto.Toque, botao, s.Mapa));
            }
        }
        return saida;
    }

    /// <summary>Quando chamar o próximo <see cref="Tick"/> (nulo = nada esperando).</summary>
    public long? ProximoPrazo
    {
        get
        {
            long? p = null;
            void Min(long v) => p = p is null ? v : Math.Min(p.Value, v);
            foreach (var s in _estados.Values)
            {
                if (s.SoltandoPendente) Min(s.SoltarEm);
                if (s.Baixo && !s.Consumido && !s.Segurando && s.Mapa?.TemSegurar == true) Min(s.DesceuEm + SegurarMs);
                if (s.Segurando && s.Mapa?.ModoSegurar == ModoSegurar.Repetir) Min(s.ProxRepete);
                if (s.EsperandoSegundo) Min(s.PrazoSegundo + 1);
            }
            return p;
        }
    }

    /// <summary>Esquece tudo (aparelho desconectado, motor desligado).</summary>
    public List<EventoGesto> Zerar()
    {
        var saida = _estados.Where(kv => kv.Value.Segurando).Select(kv => new EventoGesto(Gesto.SegurarFim, kv.Key, kv.Value.Mapa)).ToList();
        _estados.Clear();
        return saida;
    }
}

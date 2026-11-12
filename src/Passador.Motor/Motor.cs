using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Passador.Core;
using static Passador.Motor.Nativo;

namespace Passador.Motor;

public enum EstadoGancho { Desligado, PorAparelho, Simples }

/// <summary>Uma tecla que chegou de um aparelho (para a tela acender o botão e para o modo aprender).</summary>
public sealed record EventoTecla(AparelhoVisto Aparelho, string? ModeloId, string? BotaoId, Atalho Atalho, bool Soltou, bool Repeticao);

/// <summary>Um gesto virou ação (para a tela mostrar o que aconteceu).</summary>
public sealed record EventoAcao(string Aparelho, string ModeloId, string BotaoId, Gesto Gesto, string Descricao, string PerfilId, string? Outro);

/// <summary>
/// O motor: uma thread com janela invisível que
/// 1. lê o Raw Input de todos os teclados (RIDEV_INPUTSINK: mesmo sem foco), com o aparelho de cada tecla;
/// 2. passa as teclas dos passadores pelo <see cref="DetectorDeGestos"/> e manda o <see cref="Executor"/> fazer a ação
///    do perfil do programa da frente;
/// 3. responde ao gancho WH_KEYBOARD (passador_gancho.dll, nos outros programas) se a tecla que ele está vendo deve ser
///    engolida: só as que vieram de um passador. O teclado de verdade nunca é tocado;
/// 4. na reserva (<see cref="EstadoGancho.Simples"/>), um WH_KEYBOARD_LL engole as teclas configuradas de qualquer
///    aparelho (quando a DLL não carrega ou o dono escolhe).
/// Ver docs/TECNICA.md.
/// </summary>
public sealed unsafe class MotorDeEntrada : IDisposable
{
    const string Classe = "PassadorMotorDecisor";
    const int TimerGestos = 1, TimerAparelhos = 2;
    const long ValidadeMs = 500;

    readonly Thread _thread;
    readonly ManualResetEventSlim _pronto = new();
    readonly ConcurrentQueue<Action> _tarefas = new();
    readonly Executor _executor = new();
    IntPtr _hwnd;
    WndProc? _wndProc;
    HookProc? _llProc;
    IntPtr _ganchoDll, _ganchoLL, _modDll;
    string? _erroGancho;

    Catalogo _catalogo = new();
    Configuracao _cfg = new();
    readonly Dictionary<int, string> _simuladores = [];
    Dictionary<IntPtr, AparelhoVisto> _aparelhos = [];
    readonly Dictionary<string, EstadoAparelho> _estados = [];
    readonly List<Pendente> _pendentes = [];
    readonly Dictionary<uint, string> _processos = [];
    string _perfilAtual = Perfil.IdPadrao;

    public event Action<IReadOnlyList<AparelhoVisto>>? AparelhosMudaram;
    public event Action<EventoTecla>? TeclaVista;
    public event Action<EventoAcao>? AcaoFeita;
    public event Action<string>? PerfilMudou;
    public event Action<EstadoGancho, string>? GanchoMudou;

    /// <summary>Modo aprender: o motor não bloqueia nem faz ação, e mostra as teclas de todos os aparelhos.</summary>
    public bool Aprendendo { get; private set; }
    public EstadoGancho Gancho { get; private set; }
    public string DetalheGancho { get; private set; } = "";
    public IReadOnlyList<AparelhoVisto> Conectados { get; private set; } = [];

    sealed class Pendente(ushort vk, bool soltou, long t)
    {
        public readonly ushort Vk = vk;
        public readonly bool Soltou = soltou;
        public readonly long T = t;
        public bool Bloquear, Consumido;
        public List<INPUT>? Repassar;
    }

    sealed record TeclaViva(string? Botao, Atalho Atalho, bool Mapeado);

    sealed class EstadoAparelho
    {
        public required AparelhoVisto Aparelho;
        public required Modelo Modelo;
        public required DetectorDeGestos Detector;
        public Perfil Perfil = null!;
        public Mod Mods;
        public readonly Dictionary<ushort, TeclaViva> Teclas = [];
        public readonly Dictionary<string, Atalho> UltimoAtalho = [];
        public readonly Dictionary<string, Atalho> Segurando = [];
    }

    public MotorDeEntrada()
    {
        _thread = new Thread(Rodar) { IsBackground = true, Name = "passador-motor" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _pronto.Wait(5000);
    }

    // ---------------------------------------------------------------- chamadas de fora (qualquer thread)

    /// <summary>Troca catálogo e configuração (uma cópia: a tela pode continuar editando a dela).</summary>
    public void Configurar(Catalogo catalogo, Configuracao cfg)
    {
        var copia = JsonSerializer.Deserialize<Configuracao>(JsonSerializer.Serialize(cfg, Catalogo.Json), Catalogo.Json)!;
        Rodar(() =>
        {
            _catalogo = catalogo;
            _cfg = copia;
            _estados.Clear();
            AtualizarGancho();
        });
    }

    /// <summary>Liga um passador simulado (n = 1..9) como se fosse o modelo dado; nulo desliga.</summary>
    public void Simular(int n, string? modeloId) => Rodar(() =>
    {
        if (modeloId is null) _simuladores.Remove(n); else _simuladores[n] = modeloId;
        _estados.Remove("SIM:" + n);
        Publicar();
        AtualizarGancho();
    });

    string? _perfilForcado;
    readonly string _proprio = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

    /// <summary>Quando o próprio Passador está na frente (testando pelo desenho), usar este perfil (nulo = o Padrão).</summary>
    public void TestarComo(string? perfilId) => Rodar(() => _perfilForcado = perfilId);

    public void Aprender(bool ligado) => Rodar(() =>
    {
        Aprendendo = ligado;
        foreach (var e in _estados.Values) Gestos(e, e.Detector.Zerar());
        AtualizarGancho();
    });

    /// <summary>O modelo que o motor usa para este aparelho (nulo = não é passador configurado).</summary>
    public Modelo? ModeloDe(AparelhoVisto ap)
    {
        var cfg = _cfg;
        if (ap.Simulado) return int.TryParse(ap.Pid, out var n) && _simuladores.TryGetValue(n, out var id) ? ModeloPorId(id) : null;
        var conhecido = cfg.Aparelhos.FirstOrDefault(a => a.Chave.Equals(ap.Chave, StringComparison.OrdinalIgnoreCase));
        if (conhecido is not null) return conhecido.Ativo ? ModeloPorId(conhecido.ModeloId) : null;
        return _catalogo.Reconhecer(ap.Vid, ap.Pid, ap.NomeDePassador);
    }

    Modelo? ModeloPorId(string id) => _catalogo.PorId(id) ?? _cfg.ModelosAprendidos.FirstOrDefault(m => m.Id == id);

    void Rodar(Action a)
    {
        _tarefas.Enqueue(a);
        if (_hwnd != IntPtr.Zero) PostMessageW(_hwnd, WM_PASSADOR_TAREFA, IntPtr.Zero, IntPtr.Zero);
    }

    // ---------------------------------------------------------------- a thread do motor

    void Rodar()
    {
        _wndProc = Janela;
        var wc = new WNDCLASSEX
        {
            Size = (uint)Marshal.SizeOf<WNDCLASSEX>(), WndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            Instance = GetModuleHandleW(IntPtr.Zero), ClassName = Classe,
        };
        RegisterClassExW(ref wc);
        _hwnd = CreateWindowExW(0, Classe, "Passador", 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, wc.Instance, IntPtr.Zero);
        var regs = new[]
        {
            new RAWINPUTDEVICE { UsagePage = 1, Usage = 6, Flags = RIDEV_INPUTSINK | RIDEV_DEVNOTIFY, Target = _hwnd },
            new RAWINPUTDEVICE { UsagePage = 0x0C, Usage = 1, Flags = RIDEV_INPUTSINK | RIDEV_DEVNOTIFY, Target = _hwnd },
        };
        if (!RegisterRawInputDevices(regs, (uint)regs.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
            Log.Escrever("motor: RegisterRawInputDevices falhou, erro " + Marshal.GetLastWin32Error());
        Enumerar();
        _pronto.Set();
        while (GetMessageW(out var m, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref m);
            DispatchMessageW(ref m);
        }
        TirarGanchos();
        DestroyWindow(_hwnd);
    }

    IntPtr Janela(IntPtr h, uint msg, IntPtr w, IntPtr l)
    {
        try
        {
            switch (msg)
            {
                case WM_INPUT:
                    ProcessarRaw(l);
                    break;
                case WM_INPUT_DEVICE_CHANGE:
                    SetTimer(_hwnd, TimerAparelhos, 400, IntPtr.Zero);
                    return IntPtr.Zero;
                case WM_TIMER when w == TimerGestos:
                    KillTimer(_hwnd, TimerGestos);
                    var t = Environment.TickCount64;
                    foreach (var e in _estados.Values.ToList()) Gestos(e, e.Detector.Tick(t));
                    Reagendar();
                    return IntPtr.Zero;
                case WM_TIMER when w == TimerAparelhos:
                    KillTimer(_hwnd, TimerAparelhos);
                    Enumerar();
                    AtualizarGancho();
                    return IntPtr.Zero;
                case WM_PASSADOR_DECIDIR:
                    return Decidir((uint)w, l);
                case WM_PASSADOR_TAREFA:
                    while (_tarefas.TryDequeue(out var a)) a();
                    return IntPtr.Zero;
            }
        }
        catch (Exception ex) { Log.Escrever("motor: " + ex); }
        return DefWindowProcW(h, msg, w, l);
    }

    void Enumerar()
    {
        try
        {
            _aparelhos = Aparelhos.Listar().ToDictionary(a => a.Handle);
            Publicar();
        }
        catch (Exception ex) { Log.Escrever("motor: listar aparelhos: " + ex.Message); }
    }

    void Publicar()
    {
        var lista = _aparelhos.Values.Concat(_simuladores.Keys.Select(Simulado)).ToList();
        Conectados = lista;
        AparelhosMudaram?.Invoke(lista);
    }

    static AparelhoVisto Simulado(int n) => new()
    {
        Simulado = true, Vid = "SIM", Pid = n.ToString(), Produto = $"Passador simulado {n}", Tipo = TipoAparelho.Teclado,
        Caminho = "simulado",
    };

    // ---------------------------------------------------------------- Raw Input

    void ProcessarRaw(IntPtr hRaw)
    {
        var hdr = (uint)sizeof(RAWINPUTHEADER);
        uint sz = 0;
        GetRawInputData(hRaw, RID_INPUT, null, ref sz, hdr);
        if (sz == 0 || sz > 4096) return;
        var buf = stackalloc byte[(int)sz];
        if (GetRawInputData(hRaw, RID_INPUT, buf, ref sz, hdr) == uint.MaxValue) return;
        var h = *(RAWINPUTHEADER*)buf;
        if (h.Type == RIM_TYPEKEYBOARD) Tecla(h.Device, *(RAWKEYBOARD*)(buf + hdr));
        else if (h.Type == RIM_TYPEHID && Aprendendo && _aparelhos.TryGetValue(h.Device, out var ap))
            TeclaVista?.Invoke(new EventoTecla(ap, null, null, default, false, false));
    }

    void Tecla(IntPtr dispositivo, RAWKEYBOARD kb)
    {
        var vk = kb.VKey;
        if (vk is 0 or 0xFF) return;
        var t = Environment.TickCount64;
        var soltou = (kb.Flags & RI_KEY_BREAK) != 0;
        LimparPendentes(t);
        var p = new Pendente(vk, soltou, t);
        _pendentes.Add(p);
        if (kb.ExtraInformation == MarcaPassador) return; // é nossa
        var ap = dispositivo != IntPtr.Zero ? _aparelhos.GetValueOrDefault(dispositivo)
            : (kb.ExtraInformation & 0xFFFFFF00) == MarcaSimulado && _simuladores.ContainsKey((int)(kb.ExtraInformation & 0xFF))
                ? Simulado((int)(kb.ExtraInformation & 0xFF)) : null;
        if (ap is null) return;
        var modelo = ModeloDe(ap);
        if (modelo is null || Gancho != EstadoGancho.PorAparelho)
        {
            // Na reserva quem processa é o gancho de baixo nível; aqui só mostra (e só passador, ou no modo aprender)
            if (Gancho == EstadoGancho.Desligado && (modelo is not null || Aprendendo))
                Mostrar(ap, modelo, null, new Atalho(Mod.Nenhum, vk), soltou, false);
            return;
        }
        p.Bloquear = Processar(ap, modelo, vk, soltou, t, p, baixoNivel: false);
    }

    void LimparPendentes(long t) =>
        _pendentes.RemoveAll(x => x.Consumido || t - x.T > ValidadeMs);

    /// <summary>
    /// Uma tecla do passador. Devolve se ela deve ser engolida. Botão sem nada configurado: engolida e mandada de
    /// novo pelo Passador, igualzinha (assim o modificador que veio antes, como o Shift de Shift+F5, chega junto).
    /// </summary>
    bool Processar(AparelhoVisto ap, Modelo modelo, ushort vk, bool soltou, long t, Pendente? p, bool baixoNivel)
    {
        var st = Estado(ap, modelo);
        var mod = Teclas.ModificadorDe(vk);
        if (mod != Mod.Nenhum)
        {
            st.Mods = soltou ? st.Mods & ~mod : st.Mods | mod;
            return !baixoNivel;
        }
        if (!soltou)
        {
            if (st.Teclas.TryGetValue(vk, out var ja))
            {
                if (!ja.Mapeado && p is not null) p.Repassar = [_executor.Tecla(vk, false)];
                Mostrar(ap, modelo, ja.Botao, ja.Atalho, false, true);
                return !(baixoNivel && !ja.Mapeado);
            }
            var atalho = new Atalho(st.Mods, vk);
            var botao = modelo.BotaoDoAtalho(atalho) ?? (st.Mods != Mod.Nenhum ? modelo.BotaoDoAtalho(new Atalho(Mod.Nenhum, vk)) : null);
            st.Perfil = PerfilDaFrente();
            var mapa = botao is null ? null : Resolucao.MapaEfetivo(_cfg.Perfis, st.Perfil, modelo.Id, botao);
            var combo = botao is not null && (st.Perfil.Combinacoes.Concat(Padrao?.Combinacoes ?? [])
                .Any(c => c.BotaoA == botao.Id || c.BotaoB == botao.Id));
            var mapeado = botao is not null && ((mapa is not null && !mapa.Vazio) || combo);
            st.Teclas[vk] = new TeclaViva(botao?.Id, atalho, mapeado);
            Mostrar(ap, modelo, botao?.Id, atalho, false, false);
            if (!mapeado)
            {
                if (p is not null) p.Repassar = Descer(atalho);
                return !baixoNivel;
            }
            st.UltimoAtalho[botao!.Id] = atalho;
            Gestos(st, st.Detector.Apertou(botao.Id, t));
            return true;
        }
        if (!st.Teclas.Remove(vk, out var viva)) return false; // soltou algo que desceu antes do Passador abrir
        Mostrar(ap, modelo, viva.Botao, viva.Atalho, true, false);
        if (!viva.Mapeado)
        {
            if (p is not null) p.Repassar = Subir(viva.Atalho);
            return !baixoNivel;
        }
        Gestos(st, st.Detector.Soltou(viva.Botao!, t));
        return true;
    }

    List<INPUT> Descer(Atalho a)
    {
        var l = new List<INPUT>();
        foreach (var (f, v) in new (Mod, ushort)[] { (Mod.Ctrl, 0xA2), (Mod.Shift, 0xA0), (Mod.Alt, 0xA4), (Mod.Win, 0x5B) })
            if (a.Mods.HasFlag(f)) l.Add(_executor.Tecla(v, false));
        l.Add(_executor.Tecla(a.Vk, false));
        return l;
    }

    List<INPUT> Subir(Atalho a)
    {
        var l = new List<INPUT> { _executor.Tecla(a.Vk, true) };
        foreach (var (f, v) in new (Mod, ushort)[] { (Mod.Win, 0x5B), (Mod.Alt, 0xA4), (Mod.Shift, 0xA0), (Mod.Ctrl, 0xA2) })
            if (a.Mods.HasFlag(f)) l.Add(_executor.Tecla(v, true));
        return l;
    }

    Perfil? Padrao => _cfg.Perfis.FirstOrDefault(p => p.Id == Perfil.IdPadrao);

    EstadoAparelho Estado(AparelhoVisto ap, Modelo modelo)
    {
        if (_estados.TryGetValue(ap.Chave, out var e) && e.Modelo == modelo) return e;
        EstadoAparelho? novo = null;
        var det = new DetectorDeGestos(
            b => novo!.Modelo.BotaoPorId(b) is { } bt ? Resolucao.MapaEfetivo(_cfg.Perfis, novo.Perfil, novo.Modelo.Id, bt) : null,
            (a, b) => Resolucao.CombinacaoDe(_cfg.Perfis, novo!.Perfil, a, b) is not null)
        { DuploMs = _cfg.Ajustes.DuploToqueMs, SegurarMs = _cfg.Ajustes.SegurarMs };
        novo = new EstadoAparelho { Aparelho = ap, Modelo = modelo, Detector = det, Perfil = PerfilDaFrente() };
        return _estados[ap.Chave] = novo;
    }

    void Gestos(EstadoAparelho st, List<EventoGesto> eventos)
    {
        foreach (var ev in eventos)
        {
            Acao? acao = null;
            var original = st.UltimoAtalho.TryGetValue(ev.Botao, out var o) ? o : (Atalho?)null;
            var mapa = ev.Mapa;
            switch (ev.Gesto)
            {
                case Gesto.Toque: acao = mapa?.Toque ?? Acao.Original(); break;
                case Gesto.Duplo: acao = mapa?.Duplo; break;
                case Gesto.SegurarInicio when mapa?.ModoSegurar == ModoSegurar.Enquanto:
                    var alvo = mapa.Segurar?.Tipo == TipoAcao.Tecla ? Atalho.Ler(mapa.Segurar.Tecla) : mapa.Segurar?.Tipo == TipoAcao.Original ? original : null;
                    if (alvo is { } a)
                    {
                        st.Segurando[ev.Botao] = a;
                        _executor.Enfileirar(() => _executor.Descer(a));
                    }
                    else acao = mapa.Segurar;
                    break;
                case Gesto.SegurarInicio:
                case Gesto.SegurarRepete: acao = mapa?.Segurar; break;
                case Gesto.SegurarFim:
                    if (st.Segurando.Remove(ev.Botao, out var s)) _executor.Enfileirar(() => _executor.Subir(s));
                    break;
                case Gesto.Combinacao:
                    acao = Resolucao.CombinacaoDe(_cfg.Perfis, st.Perfil, ev.Botao, ev.Outro!)?.Acao;
                    break;
            }
            if (acao is not null && !Aprendendo) _executor.Fazer(acao, original);
            if (ev.Gesto != Gesto.SegurarRepete)
            {
                var desc = ev.Gesto == Gesto.SegurarFim ? "soltou" : acao?.Descricao ?? (st.Segurando.ContainsKey(ev.Botao) ? "segurando" : "");
                AcaoFeita?.Invoke(new EventoAcao(st.Aparelho.Chave, st.Modelo.Id, ev.Botao, ev.Gesto, desc, st.Perfil.Id, ev.Outro));
            }
        }
        Reagendar();
    }

    void Reagendar()
    {
        long? prazo = null;
        foreach (var e in _estados.Values)
            if (e.Detector.ProximoPrazo is { } p) prazo = prazo is null ? p : Math.Min(prazo.Value, p);
        if (prazo is null) { KillTimer(_hwnd, TimerGestos); return; }
        var ms = Math.Max(1, prazo.Value - Environment.TickCount64);
        SetTimer(_hwnd, TimerGestos, (uint)ms, IntPtr.Zero);
    }

    void Mostrar(AparelhoVisto ap, Modelo? modelo, string? botao, Atalho a, bool soltou, bool repeticao) =>
        TeclaVista?.Invoke(new EventoTecla(ap, modelo?.Id, botao, a, soltou, repeticao));

    // ---------------------------------------------------------------- perfil da janela da frente

    Perfil PerfilDaFrente()
    {
        var (proc, titulo) = JanelaDaFrente();
        // Testando dentro do próprio Passador: vale o perfil que a pessoa está editando
        // Perfil escolhido à mão na bandeja: vale em qualquer programa (menos no teste dentro do próprio Passador)
        var p = _perfilForcado is not null && proc.Equals(_proprio, StringComparison.OrdinalIgnoreCase)
            && _cfg.Perfis.FirstOrDefault(x => x.Id == _perfilForcado) is { } f ? f
            : _cfg.Ajustes.PerfilFixo is { } fixo && _cfg.Perfis.FirstOrDefault(x => x.Id == fixo) is { } pf ? pf
            : Resolucao.Escolher(_cfg.Perfis, proc, titulo);
        if (p.Id != _perfilAtual)
        {
            _perfilAtual = p.Id;
            PerfilMudou?.Invoke(p.Id);
        }
        return p;
    }

    public static (string processo, string titulo) JanelaDaFrenteAgora() => new MotorDeEntrada.Leitor().Ler();

    (string, string) JanelaDaFrente() => _leitor.Ler(_processos);
    readonly Leitor _leitor = new();

    sealed class Leitor
    {
        readonly char[] _buf = new char[512];
        public (string, string) Ler(Dictionary<uint, string>? cache = null)
        {
            var h = GetForegroundWindow();
            if (h == IntPtr.Zero) return ("", "");
            GetWindowThreadProcessId(h, out var pid);
            var n = InternalGetWindowText(h, _buf, _buf.Length);
            var titulo = new string(_buf, 0, Math.Max(0, n));
            if (cache is not null && cache.TryGetValue(pid, out var nome)) return (nome, titulo);
            nome = "";
            var hp = OpenProcess(0x1000, false, pid);
            if (hp != IntPtr.Zero)
            {
                var nb = new char[1024];
                uint sz = (uint)nb.Length;
                if (QueryFullProcessImageNameW(hp, 0, nb, ref sz)) nome = Path.GetFileNameWithoutExtension(new string(nb, 0, (int)sz));
                CloseHandle(hp);
            }
            if (cache is not null)
            {
                if (cache.Count > 200) cache.Clear();
                cache[pid] = nome;
            }
            return (nome, titulo);
        }
    }

    // ---------------------------------------------------------------- o gancho pergunta

    IntPtr Decidir(uint w, IntPtr lParam)
    {
        var vk = (ushort)(w & 0xFFFF);
        var soltou = (w & 0x10000) != 0;
        var espiando = (w & 0x20000) != 0;
        DrenarRaw();
        var p = Achar(vk, soltou);
        if (p is null)
        {
            // O WM_INPUT pode chegar um tiquinho depois da mensagem de tecla: espera até 25 ms por ele.
            var fim = Environment.TickCount64 + 25;
            while (p is null && Environment.TickCount64 < fim)
            {
                MsgWaitForMultipleObjects(0, IntPtr.Zero, false, 5, QS_RAWINPUT);
                DrenarRaw();
                p = Achar(vk, soltou);
            }
        }
        if (p is null) return IntPtr.Zero;
        if (espiando) return p.Bloquear ? 1 : 0;
        p.Consumido = true;
        if (p.Bloquear && p.Repassar is { } r) _executor.Enfileirar(() => Executor.Mandar(r));
        return p.Bloquear ? 1 : 0;
    }

    Pendente? Achar(ushort vk, bool soltou)
    {
        var t = Environment.TickCount64;
        var g = Generica(vk);
        foreach (var p in _pendentes)
            if (!p.Consumido && p.Soltou == soltou && Generica(p.Vk) == g && t - p.T <= ValidadeMs) return p;
        return null;
    }

    static ushort Generica(ushort vk) => Teclas.ModificadorDe(vk) switch
    {
        Mod.Ctrl => 0x11, Mod.Shift => 0x10, Mod.Alt => 0x12, Mod.Win => 0x5B, _ => vk,
    };

    void DrenarRaw()
    {
        while (PeekMessageW(out var m, _hwnd, WM_INPUT, WM_INPUT, PM_REMOVE))
        {
            ProcessarRaw(m.LParam);
            DefWindowProcW(_hwnd, WM_INPUT, m.WParam, m.LParam);
        }
    }

    // ---------------------------------------------------------------- ganchos

    void AtualizarGancho()
    {
        var algumPassador = Conectados.Any(a => ModeloDe(a) is not null);
        var desejado = !_cfg.Ajustes.MotorLigado || Aprendendo || !algumPassador ? EstadoGancho.Desligado
            : _cfg.Ajustes.Bloqueio == ModoBloqueio.Simples ? EstadoGancho.Simples : EstadoGancho.PorAparelho;
        if (desejado == EstadoGancho.PorAparelho && !PorGanchoDll()) desejado = EstadoGancho.Simples;
        if (desejado != EstadoGancho.PorAparelho) TirarGanchoDll();
        if (desejado == EstadoGancho.Simples) PorGanchoLL(); else TirarGanchoLL();
        var detalhe = desejado switch
        {
            EstadoGancho.PorAparelho => "Só os botões do passador mudam; o teclado fica igual.",
            EstadoGancho.Simples when _erroGancho is not null => "Reserva: o gancho por aparelho não carregou (" + _erroGancho + "). As teclas configuradas mudam em qualquer teclado.",
            EstadoGancho.Simples => "Modo simples: as teclas configuradas mudam em qualquer teclado.",
            _ when !_cfg.Ajustes.MotorLigado => "Motor desligado.",
            _ when Aprendendo => "Aprendendo: nada muda enquanto você ensina os botões.",
            _ => "Nenhum passador configurado ligado agora.",
        };
        if (desejado != Gancho || detalhe != DetalheGancho)
        {
            Gancho = desejado;
            DetalheGancho = detalhe;
            if (desejado == EstadoGancho.Desligado) _estados.Clear();
            Log.Escrever($"motor: {desejado} ({detalhe})");
            GanchoMudou?.Invoke(desejado, detalhe);
        }
    }

    bool PorGanchoDll()
    {
        if (_ganchoDll != IntPtr.Zero) return true;
        try
        {
            var origem = Path.Combine(AppContext.BaseDirectory, "passador_gancho.dll");
            if (!File.Exists(origem)) { _erroGancho = "passador_gancho.dll não está junto do programa"; return false; }
            // Copia para a pasta de dados com o hash no nome: os programas seguram a DLL aberta, e a atualização do
            // Passador não pode depender de todos eles fecharem.
            var bytes = File.ReadAllBytes(origem);
            var hash = Convert.ToHexString(SHA256.HashData(bytes))[..10].ToLowerInvariant();
            var pasta = Path.Combine(Armazenamento.PastaPadrao, "gancho");
            Directory.CreateDirectory(pasta);
            var destino = Path.Combine(pasta, $"passador_gancho-{hash}.dll");
            if (!File.Exists(destino)) File.WriteAllBytes(destino, bytes);
            if (_modDll == IntPtr.Zero) _modDll = LoadLibraryW(destino);
            if (_modDll == IntPtr.Zero) { _erroGancho = "LoadLibrary " + Marshal.GetLastWin32Error(); return false; }
            var proc = GetProcAddress(_modDll, "PassadorTeclado");
            if (proc == IntPtr.Zero) { _erroGancho = "função do gancho não encontrada"; return false; }
            _ganchoDll = SetWindowsHookExW(WH_KEYBOARD, proc, _modDll, 0);
            if (_ganchoDll == IntPtr.Zero) { _erroGancho = "SetWindowsHookEx " + Marshal.GetLastWin32Error(); return false; }
            _erroGancho = null;
            return true;
        }
        catch (Exception ex) { _erroGancho = ex.Message; return false; }
    }

    void TirarGanchoDll()
    {
        if (_ganchoDll == IntPtr.Zero) return;
        UnhookWindowsHookEx(_ganchoDll);
        _ganchoDll = IntPtr.Zero;
    }

    void PorGanchoLL()
    {
        if (_ganchoLL != IntPtr.Zero) return;
        _llProc = BaixoNivel;
        _ganchoLL = SetWindowsHookExW(WH_KEYBOARD_LL, _llProc, GetModuleHandleW(IntPtr.Zero), 0);
        if (_ganchoLL == IntPtr.Zero) Log.Escrever("motor: gancho de baixo nível falhou, erro " + Marshal.GetLastWin32Error());
    }

    void TirarGanchoLL()
    {
        if (_ganchoLL == IntPtr.Zero) return;
        UnhookWindowsHookEx(_ganchoLL);
        _ganchoLL = IntPtr.Zero;
    }

    void TirarGanchos() { TirarGanchoDll(); TirarGanchoLL(); }

    /// <summary>A reserva: sem saber o aparelho, engole as teclas que algum botão do passador ligado tem configuradas.</summary>
    IntPtr BaixoNivel(int code, IntPtr w, IntPtr l)
    {
        if (code >= 0)
        {
            try
            {
                var k = *(KBDLLHOOKSTRUCT*)l;
                var extra = (uint)(ulong)k.Extra;
                if (extra != MarcaPassador)
                {
                    var soltou = (int)w is 0x101 or 0x105;
                    AparelhoVisto? ap = (extra & 0xFFFFFF00) == MarcaSimulado && _simuladores.ContainsKey((int)(extra & 0xFF))
                        ? Simulado((int)(extra & 0xFF))
                        : Conectados.FirstOrDefault(a => !a.Simulado && ModeloDe(a) is not null);
                    if (ap is not null && ModeloDe(ap) is { } modelo &&
                        Processar(ap, modelo, Generica((ushort)k.Vk), soltou, Environment.TickCount64, null, baixoNivel: true))
                        return 1;
                }
            }
            catch (Exception ex) { Log.Escrever("motor (baixo nível): " + ex.Message); }
        }
        return CallNextHookEx(IntPtr.Zero, code, w, l);
    }

    public void Dispose()
    {
        if (_hwnd != IntPtr.Zero) PostMessageW(_hwnd, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _thread.Join(2000);
        _executor.Dispose();
    }
}

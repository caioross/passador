using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using Batuta.Core;

namespace Batuta.App;

/// <summary>
/// O vigia: um processo pequeno (o mesmo exe, com --vigia) que espera o Batuta terminar. Se ele terminou sem ser
/// fechado de propósito (caiu, travou), abre de novo na bandeja: o passador não pode ficar sem motor no meio de uma
/// apresentação. Se a versão acabou de chegar e cai três vezes antes de ficar 3 minutos aberta, ela é a culpada e o
/// vigia volta para a anterior. Cinco quedas em dez minutos de uma versão que já funcionou: para de insistir.
/// (A mesma lógica do Clarify Instructor e do Avaliador.)
/// </summary>
static class Vigia
{
    static string Marca(int pid) => Path.Combine(Armazenamento.PastaPadrao, $"saida-limpa-{pid}");
    static string Quedas => Path.Combine(Armazenamento.PastaPadrao, "quedas.txt");

    public static void SaidaLimpa()
    {
        try
        {
            Directory.CreateDirectory(Armazenamento.PastaPadrao);
            File.WriteAllText(Marca(Environment.ProcessId), DateTime.Now.ToString("o"));
        }
        catch { }
    }

    public static void Acompanhar()
    {
        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, $"--vigia {Environment.ProcessId}")
            { UseShellExecute = false, CreateNoWindow = true });
        }
        catch (Exception ex) { Log.Escrever("vigia: não consegui iniciar: " + ex.Message); }
    }

    public static void Rodar(int pid)
    {
        Log.Iniciar(Armazenamento.ArquivoLog);
        Process app;
        try { app = Process.GetProcessById(pid); }
        catch { return; }
        app.WaitForExit();
        Thread.Sleep(2000);
        if (File.Exists(Marca(pid)))
        {
            try { File.Delete(Marca(pid)); } catch { }
            return;
        }

        var agora = DateTime.Now;
        var recentes = new List<DateTime>();
        try
        {
            if (File.Exists(Quedas))
                recentes = File.ReadAllLines(Quedas).Select(l => DateTime.TryParse(l, out var d) ? d : DateTime.MinValue)
                    .Where(d => agora - d < TimeSpan.FromMinutes(10)).ToList();
            recentes.Add(agora);
            File.WriteAllLines(Quedas, recentes.Select(d => d.ToString("o")));
        }
        catch { }
        if (recentes.Count >= 3 && !SaudeDaVersao.Saudavel())
        {
            Log.Escrever($"vigia: a versão {Atualizador.VersaoAtual} caiu {recentes.Count} vezes sem ficar 3 minutos aberta");
            try { File.Delete(Quedas); } catch { }
            if (new Atualizador().Reverter().GetAwaiter().GetResult()) return;
        }
        if (recentes.Count >= 5)
        {
            Log.Escrever("vigia: o Batuta caiu 5 vezes em 10 minutos; parei de reabrir");
            return;
        }
        Log.Escrever($"vigia: o Batuta (pid {pid}) terminou sem ser fechado; reabrindo na bandeja");
        Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "--bandeja --reaberto") { UseShellExecute = false });
    }
}

/// <summary>Quais versões funcionam neste PC: 3 minutos aberta sem cair = saudável; a que caiu e motivou a volta = ruim.</summary>
static class SaudeDaVersao
{
    public static readonly TimeSpan Prazo = TimeSpan.FromMinutes(3);
    static string ArqSaudavel => Path.Combine(Armazenamento.PastaPadrao, "versao-saudavel.txt");
    static string ArqRuins => Path.Combine(Armazenamento.PastaPadrao, "versoes-ruins.txt");

    public static bool Saudavel()
    {
        try { return File.Exists(ArqSaudavel) && File.ReadAllText(ArqSaudavel).Trim() == Atualizador.VersaoAtual; }
        catch { return true; }
    }

    public static void MarcarSaudavel()
    {
        if (Saudavel()) return;
        try { File.WriteAllText(ArqSaudavel, Atualizador.VersaoAtual); } catch { }
        Log.Escrever($"versão {Atualizador.VersaoAtual}: {Prazo.TotalMinutes:0} minutos aberta, funcionando neste PC");
    }

    public static IReadOnlyCollection<string> Ruins()
    {
        try { return File.Exists(ArqRuins) ? [.. File.ReadAllLines(ArqRuins).Select(l => l.Trim()).Where(l => l.Length > 0)] : []; }
        catch { return []; }
    }

    public static void MarcarRuim(string versao)
    {
        if (Ruins().Contains(versao)) return;
        try { File.AppendAllLines(ArqRuins, [versao]); } catch { }
    }
}

/// <summary>Cão de guarda da tela: thread da janela presa por mais de 2 minutos = encerra sem a marca, e o vigia reabre.</summary>
static class Travamento
{
    static readonly TimeSpan Limite = TimeSpan.FromMinutes(2);
    static long _ultimoSinal;

    public static void Vigiar(Dispatcher tela)
    {
        Interlocked.Exchange(ref _ultimoSinal, Environment.TickCount64);
        new Thread(() =>
        {
            var antes = Environment.TickCount64;
            while (true)
            {
                Thread.Sleep(10_000);
                var agora = Environment.TickCount64;
                if (agora - antes > 60_000) Interlocked.Exchange(ref _ultimoSinal, agora); // o PC dormiu
                antes = agora;
                tela.BeginInvoke(DispatcherPriority.Normal, () => Interlocked.Exchange(ref _ultimoSinal, Environment.TickCount64));
                if (agora - Interlocked.Read(ref _ultimoSinal) > Limite.TotalMilliseconds)
                {
                    Log.Escrever($"travamento: a janela não responde há {Limite.TotalMinutes:0} minutos; fechando para o vigia reabrir");
                    Thread.Sleep(500);
                    Process.GetCurrentProcess().Kill();
                }
            }
        }) { IsBackground = true, Name = "travamento" }.Start();
    }
}

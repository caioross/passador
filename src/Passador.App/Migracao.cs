using System.Threading;
using Microsoft.Win32;
using Passador.Core;

namespace Passador.App;

/// <summary>
/// O Passador se chamou Batuta até a 0.1.2 (outro packId do Velopack, outra pasta de dados). Ao abrir:
/// 1. copia a configuração de %LOCALAPPDATA%\Batuta.Dados para Passador.Dados (uma vez, sem apagar a antiga);
/// 2. pede ao Batuta que ainda esteja aberto para fechar (dois motores brigariam pelo mesmo passador);
/// 3. tira o Batuta do início com o Windows (o Passador põe a chave dele).
/// Não desinstala o Batuta: isso fica para o instalador ou para a pessoa, e a pasta antiga continua lá para voltar atrás.
/// </summary>
static class Migracao
{
    public static void DoBatuta()
    {
        if (Environment.GetEnvironmentVariable("PASSADOR_DADOS") is { Length: > 0 }) return; // teste: nunca mexe no real
        try
        {
            if (Armazenamento.MigrarDoNomeAntigo())
                Log.Escrever($"configuração do Batuta copiada de {Armazenamento.PastaAntiga}");
        }
        catch (Exception ex) { Log.Escrever("migração do Batuta: " + ex.Message); }

        try
        {
            if (EventWaitHandle.TryOpenExisting(@"Local\Batuta.Sair", out var sair))
            {
                using (sair) sair.Set();
                Log.Escrever("o Batuta (nome antigo) estava aberto: pedi para fechar");
            }
        }
        catch (Exception ex) { Log.Escrever("fechar o Batuta: " + ex.Message); }

        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (run?.GetValue("Batuta") is string v && v.Contains("Batuta.exe", StringComparison.OrdinalIgnoreCase))
            {
                run.DeleteValue("Batuta");
                Log.Escrever("início com o Windows do Batuta removido (o Passador assume)");
            }
        }
        catch (Exception ex) { Log.Escrever("chave Run do Batuta: " + ex.Message); }
    }
}

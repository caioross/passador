using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Passador.Core;
using Passador.Motor;

namespace Passador.App;

/// <summary>
/// Passador.exe --aparelhos [arquivo.json]: o retrato dos teclados e controles ligados agora, para quem quer pôr um
/// passador novo no catálogo. Sem arquivo, grava em Documentos\passador-aparelhos.json e abre no Bloco de Notas.
/// Não guarda o caminho completo do aparelho (pode ter número de série): só VID, PID, nome, tipo, conexão e bateria.
/// </summary>
static class Diagnostico
{
    public static void ListarAparelhos(string? arquivo)
    {
        Catalogo? catalogo = null;
        try { catalogo = Catalogo.Ler(Path.Combine(Servicos.PastaCatalogo, "modelos.json")); } catch { }
        var lista = Passador.Motor.Aparelhos.Listar()
            .GroupBy(a => (a.Chave, a.Tipo)).Select(g => g.First())
            .Select(a => new
            {
                vid = a.Vid, pid = a.Pid, produto = a.Produto, fabricante = a.Fabricante, tipo = a.Tipo.ToString(),
                conexao = a.Bluetooth ? "Bluetooth" : "USB", bateria = Bateria.Ler(a.Caminho),
                pareceSerPassador = a.NomeDePassador,
                noCatalogo = catalogo?.Candidatos(a.Vid, a.Pid).Select(m => m.Id).ToArray() ?? [],
            }).ToList();
        var json = JsonSerializer.Serialize(new { versao = Atualizador.VersaoAtual, geradoEm = DateTime.Now, aparelhos = lista },
            new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        var abrir = arquivo is null;
        arquivo ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "passador-aparelhos.json");
        File.WriteAllText(arquivo, json);
        if (abrir) Process.Start(new ProcessStartInfo("notepad.exe", $"\"{arquivo}\"") { UseShellExecute = true });
    }
}

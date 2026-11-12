# Como contribuir

Obrigado por querer ajudar. A contribuição mais útil é **pôr um passador novo no catálogo** (ou corrigir um que já
está lá). Também valem consertos, tradução da interface e testes em aparelhos de verdade. Leia o
[Código de Conduta](CODE_OF_CONDUCT.md) antes.

*English speakers: the steps below are the same in any language. Open an issue with the "Novo passador / New clicker"
template, or send a pull request with the JSON entry and the SVG drawing described here.*

## Sem programar: abra uma issue

Use o modelo **Novo passador** em [Issues](https://github.com/caioross/passador/issues/new/choose). O que mais ajuda:

1. Marca, modelo e um link do anúncio ou do manual.
2. O que o Windows vê. Com o passador ligado, rode `Passador.exe --aparelhos` (o atalho fica na pasta de instalação;
   pode abrir o Executar com Win+R e colar `"%LOCALAPPDATA%\Passador\current\Passador.exe" --aparelhos`). Ele grava
   `Documentos\passador-aparelhos.json` e abre no Bloco de Notas. Cole o trecho do passador na issue: tem VID, PID,
   nome do produto e bateria. Não tem número de série nem nada seu.
3. O que cada botão manda. No Passador, a tela **Aprender** mostra a tecla de cada botão quando você aperta. Diga também
   o que acontece ao **segurar** (repete? manda outra tecla? nada?).
4. Uma foto sua do aparelho, de frente (opcional). Foto de anúncio não serve: tem direito autoral.

## Programando: o pull request

Um modelo novo tem duas partes: a entrada no `catalogo/modelos.json` e o desenho em `catalogo/desenhos/<id>.svg`.

### 1. A entrada no catálogo

```json
{
  "id": "marca-modelo",
  "marca": "Marca",
  "nome": "Nome do modelo",
  "desenho": "marca-modelo",
  "conexao": "Receptor USB 2.4 GHz",
  "ids": [
    { "vid": "046D", "pid": "C52D", "nota": "receptor (de onde veio o número)" }
  ],
  "botoes": [
    { "id": "voltar",  "nome": "Voltar",  "papel": "voltar",  "envia": ["PageUp"], "segurar": "repete (autorepeat do Windows)" },
    { "id": "avancar", "nome": "Avançar", "papel": "avancar", "envia": ["PageDown"] },
    { "id": "iniciar", "nome": "Iniciar apresentação", "papel": "iniciar", "envia": ["F5", "Esc", "Shift+F5"] },
    { "id": "laser",   "nome": "Laser",   "papel": "laser",   "envia": [], "soHardware": "O laser é só luz: o botão não chega ao computador." }
  ],
  "confianca": "conferido",
  "fontes": ["https://link-do-manual-ou-da-lista-de-ids"],
  "notas": "Uma frase curta, sem emoji."
}
```

| Campo | Regra |
|---|---|
| `id` | kebab-case, único. Igual ao nome do arquivo do desenho. |
| `desenho` | Igual ao `id`. Cada modelo tem o seu desenho. |
| `ids` | VID e PID em hexadecimal, 4 dígitos, maiúsculas. Só o fabricante? `{ "vid": "3243", "nota": "PID a confirmar" }`. Nada? Lista vazia e `"notas": "VID e PID a confirmar."`. Receptor que a marca usa também em teclado e mouse (ex.: Logi Bolt) leva `"compartilhado": true`. |
| `botoes[].id` | Curto e único no modelo. **Tem de ser igual** ao `data-botao` do desenho. |
| `botoes[].papel` | Um destes: `avancar`, `voltar`, `iniciar`, `tela-preta`, `laser`, `foco`, `mouse`, `lupa`, `caneta`, `tab`, `mais`, `menos`, `cronometro`, `liga`, `volume`, `midia`, `midia-seguinte`, `midia-anterior`, `cima`, `baixo`, `curtir`, `outro`. Os perfis prontos funcionam pelo papel. |
| `botoes[].envia` | As teclas de fábrica, com os nomes de `src/Passador.Core/Teclas.cs`: `A`–`Z`, `0`–`9`, `F1`–`F24`, `PageUp`, `PageDown`, `Home`, `End`, `Left`, `Up`, `Right`, `Down`, `Esc`, `Enter`, `Tab`, `Space`, `Ponto`, `Virgula`, `VolumeUp`, `VolumeDown`, `VolumeMute`, `MediaPlayPause`, `MediaNext`, `MediaPrev`, e combinações como `Shift+F5`, `Alt+Tab`. Botão que alterna (F5 num toque, Esc no outro): liste as duas. |
| `soHardware` | Obrigatório quando `envia` está vazio: laser, liga/desliga, botão que o Windows vê como mouse, toque na tela. |
| `confianca` | `conferido` (manual, ficha técnica ou lista de IDs), `comunidade` (relatos de quem tem), `estimado` (pelo formato e por clones). |

### 2. O desenho (SVG)

Os desenhos são **nossos e genéricos**: nada de logo, nome de marca ou foto copiada dentro do SVG. O nome da marca só
aparece como texto no catálogo. O jeito mais fácil é acrescentar uma função em `tools/desenhos.py` (há geradores de
corpo prontos: `corpo_cunha`, `corpo_gota`, `corpo_reto`, `corpo_cintura`, `corpo_oval`, `corpo_bastao`, e de botão:
`b_circ`, `b_ret`, `par`, `gangorra`, `cruz`, `b_laser_oval`, `b_lateral`) e rodar `python tools/desenhos.py`.

O app tem um leitor de SVG próprio e pequeno (`src/Passador.App/Desenho/LeitorSvg.cs`), então:

- só `path`, `rect`, `circle`, `ellipse`, `line`, `text` e `g`; gradientes `linearGradient` e `radialGradient` em `<defs>`;
- atributos, nunca `style=""`; `transform` só com `translate`, `scale` e `rotate`;
- cada botão é um `<g data-botao="id">` cujo **primeiro filho** é a forma do botão (a área clicável, que acende); o resto
  do grupo é o ícone;
- `viewBox` em pé, com o aparelho de frente, botões de cima para baixo como no aparelho de verdade.

```xml
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 220 566" width="220" height="566">
  <defs>…</defs>
  <path d="M110 22 C158 22 176 50 174 96 L162 486 …" fill="url(#corpo)"/>
  <g data-botao="voltar">
    <rect x="62" y="112" width="44" height="94" rx="12" fill="url(#botao)"/>
    <path d="…" fill="none" stroke="#e9e7ef"/>
  </g>
  <g data-botao="avancar">…</g>
</svg>
```

### 3. Confira antes de mandar

```powershell
python tools\desenhos.py          # gera os SVGs
python tools\confere_catalogo.py  # JSON, desenhos, botões, teclas, papéis e marca dentro do SVG: saída 0 = ok
dotnet test tests\Passador.Testes # os testes do catálogo rodam aqui também
```

E abra o Passador (Catálogo > o seu modelo) para ver o desenho com os botões acesos.

## Código

- C# 13 / .NET 10, WPF. O Core não depende do Windows e tem testes; o Motor e o App, sim.
- Nomes em português, como no resto do código. Comentário explica o porquê, não o quê.
- Um assunto por pull request. Rode os testes antes.
- Mudou algo que o usuário vê? Anote em `docs/novidades/` da próxima versão.

## Segurança

Achou uma falha de segurança? Não abra issue pública: use **Security > Report a vulnerability** neste repositório.

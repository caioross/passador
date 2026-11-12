# Batuta: pesquisa técnica

Como um programa de Windows reconhece o passador de slides, troca o que cada botão faz e deixa o teclado de verdade em
paz, sem driver. Escrito em 11/11/2026, com medida nesta máquina onde diz "medido".

## 1. O que um passador é para o Windows

Quase todo passador é um **teclado HID** (USB ou Bluetooth). O laser é só uma luz no aparelho e nunca chega ao PC.
Os botões mandam teclas comuns:

| Botão | Tecla mais comum | Variantes que aparecem |
|---|---|---|
| Avançar | Page Down | Seta direita, Seta baixo, Espaço |
| Voltar | Page Up | Seta esquerda, Seta cima |
| Iniciar apresentação | F5, e Esc no toque seguinte (o aparelho guarda o estado) | Shift+F5 |
| Tela preta | Ponto (.) no Logitech | B |
| Tab (Norwii) | Tab; segurar = Alt+Tab | |
| + e − (Norwii) | Volume + e − (Consumer Control, usage page 0x0C) | |

Outros casos:
- **Mouse.** O botão de destaque do Logitech Spotlight e do Baseus Orange Dot é um mouse. Enquanto está apertado, o
  cursor segue o giroscópio. O efeito de holofote é desenhado pelo programa do fabricante.
- **Toque na tela.** Os anéis de TikTok mandam toque/arrasto (digitizer, usage page 0x0D) e, segurados, teclas de mídia.
- **Modos de tecla.** Alguns aparelhos trocam o conjunto de teclas por um gesto. No Norwii N95, segurar Avançar e
  Voltar juntos por 3 s alterna entre Page Up/Down, setas e rolagem.

**Segurar:** nenhum fabricante documenta o que o aparelho manda quando o botão fica apertado. Há dois comportamentos
esperados. A maioria deve mandar o "apertou" e só soltar depois; a repetição, nesse caso, é o autorepeat do
Windows. Alguns clones mandam apertar-soltar seguidos, em rajada. O detector de gestos do Batuta trata os dois casos
(seção 4). **Isto precisa de aparelho real para confirmar.**

## 2. Saber de qual aparelho veio a tecla, e engolir só a dele

### Raw Input diz o aparelho

`RegisterRawInputDevices` com `RIDEV_INPUTSINK` (usage page 1, usage 6) entrega cada tecla de cada teclado, mesmo
sem foco. `RAWINPUTHEADER.hDevice` identifica o aparelho, e `GetRawInputDeviceInfo(RIDI_DEVICENAME)` dá o caminho:
`\\?\HID#VID_046D&PID_C52D&MI_00#...`. Desse caminho saem o **VID** e o **PID**; no Bluetooth, o caminho traz
`_VID&0002046d_PID&b503`. O nome do produto sai de `HidD_GetProductString`, que funciona abrindo o aparelho sem
pedir acesso. `RIDEV_DEVNOTIFY` avisa quando um aparelho entra ou sai.

### O gancho de baixo nível NÃO serve para isso (medido)

A técnica que se costuma citar junta o Raw Input com o `WH_KEYBOARD_LL`. Medimos nesta máquina
(`E:\Cache\build\Batuta\sonda`, 11/11/2026) com o mesmo processo ouvindo os dois:

```
--- F24 sem bloquear
  415,12 HOOK vk=87 msg=100 injected=True bloqueia=False
  426,42 HOOK vk=87 msg=101 injected=True bloqueia=False
  427,62 RAW  vk=87 flags=0 dev=0
  428,41 RAW  vk=87 flags=1 dev=0
--- F24 bloqueado no gancho
  820,11 HOOK vk=87 msg=100 injected=True bloqueia=True
  820,16 HOOK vk=87 msg=101 injected=True bloqueia=True
```

O `WH_KEYBOARD_LL` roda **antes** do Raw Input. Quando ele bloqueia, o Raw Input **nem recebe** a tecla. Ou seja:
na hora em que o gancho de baixo nível precisa decidir, ele não tem como saber o aparelho, e o que ele bloqueia some
para o Raw Input também.

### O que funciona: WH_KEYBOARD (a correlação de verdade)

O `WH_KEYBOARD` comum (não o LL) é chamado **dentro do programa da frente**, quando ele tira a mensagem de tecla da
fila. Nessa hora o `WM_INPUT` daquela tecla já foi entregue ao Batuta. É a técnica do artigo "Combining Raw Input
and keyboard Hook to selectively block input from multiple keyboards" (V. Blecha, CodeProject).

No Batuta ela funciona assim:

1. `native/gancho/gancho.c` vira `batuta_gancho.dll` (3 KB, sem CRT, x64). Ele só pergunta ao motor:
   `SendMessageTimeout(janela do motor, WM_APP+0x51, vk|soltou|espiando, lParam, 60 ms)`. Se a resposta for 1, ele
   engole a tecla.
2. O motor (`src/Batuta.Motor/Motor.cs`) guarda numa fila as teclas que chegaram pelo Raw Input nos últimos 500 ms,
   cada uma marcada "engolir ou não". Ao receber a pergunta, ele primeiro esvazia os `WM_INPUT` pendentes, depois
   acha na fila a tecla igual mais antiga ainda não usada e responde. Se nada aparecer em 25 ms, a tecla passa.
3. Tecla de teclado comum: nunca é marcada, passa intacta. Tecla de passador com ação configurada: é engolida, e a
   ação sai pelo `SendInput`. Tecla de passador sem nada configurado: é engolida e o Batuta a manda de novo, igual.
   Assim o modificador que veio antes (o Shift de Shift+F5) continua chegando junto.
4. Tudo o que o Batuta manda leva `dwExtraInfo = 0xBA7A0000`. O Raw Input repete esse valor em
   `RAWKEYBOARD.ExtraInformation`, e por ele o motor e o gancho reconhecem a tecla como do próprio Batuta.
5. Programa de 32 bits: o Windows não carrega a DLL de 64 bits nele e encaminha a chamada para a thread que
   instalou o gancho, onde a DLL também está carregada. Funciona igual, só com um pouco mais de latência.
6. A DLL é copiada para `%LOCALAPPDATA%\Batuta.Dados\gancho\batuta_gancho-<hash>.dll`. Os outros programas
   seguram a DLL aberta, e assim a atualização do Batuta não depende de todos eles fecharem.
7. O gancho só fica instalado enquanto há um passador configurado ligado. Sem passador, o custo é zero.

Achado da prova de 12/11: o motor não pode mandar mensagem a nenhuma janela enquanto decide. O `GetWindowText`, por exemplo, manda `WM_GETTEXT`, e a thread dona da janela está parada no gancho esperando o motor. O resultado era espera de 60 ms e a tecla vazando. O título agora sai do `InternalGetWindowText`, e o gancho espera com `SMTO_NORMAL`.

Resultado da prova (`Batuta.exe --prova`, 12/11 00:12): 7 de 7 casos passaram. Toque, segurar e dois toques viram a ação sem vazar a tecla de fábrica. O botão sem configuração passa uma vez. O Page Down do teclado passa intacto. Shift+F5 vira texto sem vazar Shift nem F5. Latência medida entre 266 e 375 ms por caso, incluindo as esperas da própria prova.

Limites:
- O gancho não chega a programa que roda como administrador quando o Batuta não roda, por causa do UIPI. Nesse
  programa a tecla original passa junto com a ação.
- A DLL x86 fica para a fase 2; pelo item 5, ela só melhoraria a latência.

**Reserva** (`ModoBloqueio.Simples`, ou quando a DLL não carrega): um `WH_KEYBOARD_LL` engole as teclas configuradas
do passador ligado, venham de onde vierem, inclusive do teclado. É explícito na tela de Ajustes.

**Fora de questão:** driver de kernel, Interception e HidHide. Não instalamos driver.

## 3. Prova sem aparelho: o passador simulado

O `SendInput` também gera Raw Input, com `hDevice = 0` e o `dwExtraInfo` que se pediu. O Batuta trata
`0xBA7A5100 + n` como "passador simulado n", com o modelo que se escolher. Daí para a frente o caminho é o mesmo de
um passador de verdade: fila, gestos, ação, pergunta do gancho, bloqueio. Duas coisas usam isso:
- `Batuta.exe --prova`: uma janelinha recebe as teclas, e a prova confere sete casos, gravando o resultado em
  `prova.json`. São eles: toque, segurar, botão sem configuração, Page Down do teclado intacto, dois toques, toque
  com dois toques ligado, e Shift+F5 virando texto sem vazar Shift.
- "Testar no desenho", na tela Controle: aperta o botão do desenho e mostra o que chegou no programa.

Só o aparelho real confirma:
- os VID/PID e as teclas dos modelos marcados "estimado";
- o que cada aparelho manda ao segurar (repetição ou rajada);
- a latência do Bluetooth;
- os botões que são mouse.

## 4. Gestos

`src/Batuta.Core/Gestos.cs`, sem relógio próprio (testável):
- Botão sem dois toques nem segurar configurados: a ação sai no **apertar**, sem atraso.
- Com segurar: o toque sai no soltar; segurar dispara em `SegurarMs` (padrão 550 ms) e pode valer **uma vez**,
  **repetindo** a cada `RepetirMs`, ou **enquanto segurar** (aperta a tecla da ação e só solta junto com o botão).
- Com dois toques: o toque simples espera a janela de `DuploToqueMs` (padrão 300 ms), e o segundo toque dispara no
  apertar.
- Rajada: soltar e apertar de novo em menos de 45 ms é emendado num segurar só (clones).
- Dois botões juntos: o segundo apertado com o primeiro ainda embaixo vira "combinação".
- Autorepeat (o mesmo "apertou" repetido) é ignorado.

## 5. Catálogo (`catalogo/modelos.json`)

| Marca e modelo | VID:PID | Confiança | Fonte |
|---|---|---|---|
| Logitech R400 | 046D:C52D (também 046D:C538) | conferido | usb.ids; teclas pelo hwdb do systemd e pelo blog de D. Rethans |
| Logitech R800 / R700 | 046D:C52D | estimado (mesmo receptor do R400) | ficha Logitech |
| Logitech R500s | 046D (PID não publicado) | estimado | ficha Logitech |
| Logitech Spotlight | 046D:C53E (receptor), 046D:B503 (Bluetooth) | conferido | ficha Logitech, Projecteur |
| Logitech Spotlight 2 | 046D:C548 (Bolt), 046D:B506 (Bluetooth) | estimado | Projecteur |
| Kensington Wireless Presenter | 047D:2012, 047D:2010 | estimado (teclas) | usb.ids |
| Kensington Expert Presenter | 047D (PID não achado) | estimado | |
| Baseus Orange Dot | ABC8:CA08 | comunidade | discussão no Projecteur |
| Exbom (foto 29), Multilaser (foto 32), Knup | não publicado | estimado | formato R400 |
| Genérico 2.4G tipo R400 (foto 33) | 0C45:6900, 0C45:8101, 2571:4109, 2312:863D, 69A7:9803, 1D57:0006, 05B8:3126, 05B8:3223, 348F:2322, 1894:5641, 1784:0014, 1EA7:0002 | IDs conferidos, teclas estimadas | usb.ids, linux-hardware.org |
| Genius Ring Presenter | 0458:00F1 | estimado | usb.ids |
| Anel de 3 botões (foto 30), anel de 5 (foto 31) | não publicado | estimado | |
| Norwii N95 (foto 34) | 3243:0382 (receptor), 3243:03A2 (Bluetooth) | comunidade | usb.ids, manual N76 |
| Norwii caneta (foto 35) | 3243:0122, 0111, 0131 | estimado | usb.ids |
| Doosl caneta (foto 36) | 8EA3:A02C | estimado | usb.ids |

Não confunda:
- **1D57:AD03** é um air mouse, não um passador.
- **3151:4011** não aparece em nenhuma fonte como Norwii.

A pesquisa completa, com URL e trecho, está em `E:\Cache\build\Batuta\pesquisa\catalogo-pesquisa.json`.

Botão que alterna teclas (F5, depois Esc) lista as duas em `envia`, e as duas viram o mesmo botão. Quando a marca
varia entre clones (B ou Ponto, Page Down ou seta), o catálogo lista as variantes: qualquer uma acende o mesmo
botão.

**Reconhecimento:**
- PID exato no catálogo: o Batuta associa sozinho.
- Só o VID (fabricante): fica como palpite, e o modo aprender sugere.
- Aparelho cujo nome do produto diz "Presenter", "Remote", "Laser" etc.: aparece como "passador fora do catálogo".

## 6. Modo aprender

Na tela Aprender, o motor para de bloquear e de fazer ação e mostra as teclas de todos os aparelhos. O primeiro
aparelho que mandar uma tecla vira o aprendido: o Batuta anota o VID, o PID e o nome. A partir daí:
- cada combinação nova (modificadores mais tecla) vira um botão, com nome e papel deduzidos (Page Down = Avançar);
- botão que alterna aparece duas vezes, e "juntar com o de cima" o une;
- se as teclas batem com um modelo do catálogo, ele sugere usar aquele desenho;
- o modelo salvo vai para `config.json`, com um desenho gerado (`DesenhoGenerico.cs`): um bastão com um botão
  por tecla.

## 7. Perfis por programa

O programa da frente é lido no momento do aperto, pelo nome do processo e pelo título da janela. Os perfis prontos
estão em `PerfisProntos.cs`:

| Perfil | Quando entra |
|---|---|
| PowerPoint | processo POWERPNT |
| Google Slides | navegador com "Google Slides" ou "Apresentações Google" no título |
| Keynote no navegador | navegador com "Keynote" no título |
| PDF | Acrobat, Sumatra ou Foxit, ou ".pdf" no título |
| YouTube | navegador com "YouTube" no título |
| OBS Studio | processo obs64; usa F13 a F17, teclas livres que se ligam a cenas no OBS |

Em cada perfil, o que ficar vazio vem do Padrão, e o que o Padrão não disser segue de fábrica. O mapa por botão de
um modelo ganha do mapa por papel (o papel serve para qualquer passador).

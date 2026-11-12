# -*- coding: utf-8 -*-
"""
Gera os desenhos vetoriais dos passadores (catalogo/desenhos/*.svg), feitos por nós a partir das fotos de referência
(docs/referencias) e das fichas dos fabricantes. Uso:  python tools/desenhos.py

Regras do formato (o app tem um leitor de SVG próprio, pequeno, em src/Batuta.App/Desenho/LeitorSvg.cs):
- só path, rect, circle, ellipse, line, text e g; gradientes linear e radial em <defs>;
- atributos, nunca style=""; transform só translate/scale/rotate;
- cada botão é um <g data-botao="id"> cujo PRIMEIRO filho é a forma do botão (a área clicável e o brilho);
  o resto do grupo é o ícone. O id do botão é o mesmo do catálogo (catalogo/modelos.json).
"""
import math
import os

PASTA = os.path.join(os.path.dirname(__file__), "..", "catalogo", "desenhos")

DEFS = """<defs>
  <linearGradient id="corpo" x1="0" y1="0" x2="1" y2="0">
    <stop offset="0" stop-color="#0b0b0e"/><stop offset="0.16" stop-color="#2c2b33"/>
    <stop offset="0.34" stop-color="#1b1a20"/><stop offset="0.82" stop-color="#121116"/><stop offset="1" stop-color="#050506"/>
  </linearGradient>
  <linearGradient id="corpoClaro" x1="0" y1="0" x2="1" y2="0">
    <stop offset="0" stop-color="#26262c"/><stop offset="0.2" stop-color="#5a5a64"/>
    <stop offset="0.45" stop-color="#3a3a42"/><stop offset="1" stop-color="#16161a"/>
  </linearGradient>
  <linearGradient id="brilho" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#ffffff" stop-opacity="0.22"/><stop offset="1" stop-color="#ffffff" stop-opacity="0"/>
  </linearGradient>
  <radialGradient id="botao" cx="0.38" cy="0.3" r="0.85">
    <stop offset="0" stop-color="#3d3c45"/><stop offset="0.7" stop-color="#232228"/><stop offset="1" stop-color="#16151a"/>
  </radialGradient>
  <radialGradient id="botaoCinza" cx="0.38" cy="0.3" r="0.9">
    <stop offset="0" stop-color="#9c9ca6"/><stop offset="0.75" stop-color="#6c6c76"/><stop offset="1" stop-color="#4a4a52"/>
  </radialGradient>
  <radialGradient id="laranja" cx="0.38" cy="0.3" r="0.9">
    <stop offset="0" stop-color="#ffb066"/><stop offset="0.6" stop-color="#ff7a1a"/><stop offset="1" stop-color="#c2500a"/>
  </radialGradient>
  <radialGradient id="laser" cx="0.5" cy="0.5" r="0.5">
    <stop offset="0" stop-color="#ff5a4a"/><stop offset="1" stop-color="#7a1010"/>
  </radialGradient>
  <radialGradient id="ledVerde" cx="0.5" cy="0.5" r="0.5">
    <stop offset="0" stop-color="#b6ffcf"/><stop offset="0.5" stop-color="#2fd36b"/><stop offset="1" stop-color="#2fd36b" stop-opacity="0"/>
  </radialGradient>
  <radialGradient id="ledAzul" cx="0.5" cy="0.5" r="0.5">
    <stop offset="0" stop-color="#d6e6ff"/><stop offset="0.45" stop-color="#3d7bff"/><stop offset="1" stop-color="#3d7bff" stop-opacity="0"/>
  </radialGradient>
  <linearGradient id="visor" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#050507"/><stop offset="1" stop-color="#121218"/>
  </linearGradient>
</defs>"""

TRACO = 'fill="none" stroke="#e9e7ef" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"'
TRACO_ESCURO = 'fill="none" stroke="#1d1d22" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"'

# Ícones no espaço 24x24 (traço de Lucide, ISC)
ICONES = {
    "esq": '<path d="M15 18 L9 12 L15 6"/>',
    "dir": '<path d="M9 18 L15 12 L9 6"/>',
    "cima": '<path d="M18 15 L12 9 L6 15"/>',
    "baixo": '<path d="M6 9 L12 15 L18 9"/>',
    "play": '<path d="M7 5 L19 12 L7 19 Z"/>',
    "tela": '<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20 L16 20"/><path d="M12 16 L12 20"/>',
    "telaplay": '<rect x="3" y="4" width="18" height="13" rx="2"/><path d="M10 8 L15 10.5 L10 13 Z"/>',
    "preta": '<rect x="4" y="5" width="16" height="14" rx="2"/><path d="M4 5 L20 19"/>',
    "janelas": '<rect x="3" y="7" width="13" height="11" rx="2"/><path d="M8 7 L8 5 Q8 4 9 4 L20 4 Q21 4 21 5 L21 14 Q21 15 20 15 L16 15"/>',
    "sol": '<circle cx="12" cy="12" r="4"/><path d="M12 2 L12 4"/><path d="M12 20 L12 22"/><path d="M4.9 4.9 L6.3 6.3"/><path d="M17.7 17.7 L19.1 19.1"/><path d="M2 12 L4 12"/><path d="M20 12 L22 12"/><path d="M4.9 19.1 L6.3 17.7"/><path d="M17.7 6.3 L19.1 4.9"/>',
    "laser": '<path d="M12 3 L12 8"/><path d="M12 16 L12 21"/><path d="M3 12 L8 12"/><path d="M16 12 L21 12"/><path d="M5.6 5.6 L8.5 8.5"/><path d="M15.5 15.5 L18.4 18.4"/><path d="M5.6 18.4 L8.5 15.5"/><path d="M15.5 8.5 L18.4 5.6"/>',
    "mais": '<path d="M12 5 L12 19"/><path d="M5 12 L19 12"/>',
    "menos": '<path d="M5 12 L19 12"/>',
    "lupa": '<circle cx="11" cy="11" r="7"/><path d="M21 21 L16.7 16.7"/>',
    "caneta": '<path d="M17 3 L21 7 L8 20 L3 21 L4 16 Z"/>',
    "girar": '<path d="M21 12 A9 9 0 1 1 18.4 5.6"/><path d="M21 3 L21 8 L16 8"/>',
    "foco": '<circle cx="12" cy="12" r="8"/><circle cx="12" cy="12" r="2.5"/>',
    "anel": '<circle cx="12" cy="12" r="7"/>',
    "liga": '<path d="M12 3 L12 11"/><path d="M6.4 6.4 A8 8 0 1 0 17.6 6.4"/>',
}


def icone(nome, cx, cy, tam=18.0, escuro=False, cor=None):
    s = tam / 24.0
    estilo = TRACO_ESCURO if escuro else TRACO
    if cor:
        estilo = estilo.replace("#e9e7ef", cor)
    return (f'<g transform="translate({cx - 12 * s:.2f} {cy - 12 * s:.2f}) scale({s:.4f})" {estilo}>'
            f'{ICONES[nome]}</g>')


def botao(id_, forma, conteudo=""):
    return f'<g data-botao="{id_}">{forma}{conteudo}</g>'


def circulo(cx, cy, r, fill="url(#botao)"):
    return (f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{fill}" stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>')


def retang(x, y, w, h, rx, fill="url(#botao)"):
    return (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{rx}" fill="{fill}" '
            f'stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>')


def segmento(cx, cy, r1, r2, a0, a1, fill="url(#botao)", folga=3.0):
    """Fatia de anel (graus, 0 = direita, sentido horário na tela)."""
    def p(r, a):
        rad = math.radians(a)
        return cx + r * math.cos(rad), cy + r * math.sin(rad)
    g1 = math.degrees(folga / r1)
    g2 = math.degrees(folga / r2)
    x1, y1 = p(r2, a0 + g2)
    x2, y2 = p(r2, a1 - g2)
    x3, y3 = p(r1, a1 - g1)
    x4, y4 = p(r1, a0 + g1)
    grande = 1 if (a1 - a0) > 180 else 0
    d = (f"M{x1:.2f} {y1:.2f} A{r2} {r2} 0 {grande} 1 {x2:.2f} {y2:.2f} L{x3:.2f} {y3:.2f} "
         f"A{r1} {r1} 0 {grande} 0 {x4:.2f} {y4:.2f} Z")
    return f'<path d="{d}" fill="{fill}" stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>'


def centro_seg(cx, cy, r1, r2, a0, a1):
    a = math.radians((a0 + a1) / 2)
    r = (r1 + r2) / 2
    return cx + r * math.cos(a), cy + r * math.sin(a)


def led(cx, cy, grad="ledVerde", r=7):
    return (f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="url(#{grad})"/>'
            f'<circle cx="{cx}" cy="{cy}" r="{r * 0.32:.2f}" fill="#ffffff" fill-opacity="0.9"/>')


def brilho_lateral(d):
    return f'<path d="{d}" fill="url(#brilho)" opacity="0.9"/>'


def svg(w, h, corpo, botoes, extras=""):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}">\n{DEFS}\n'
            f'{corpo}\n{extras}\n' + "\n".join(botoes) + "\n</svg>\n")


def corpo_path(d, fill="url(#corpo)"):
    return (f'<path d="{d}" fill="{fill}" stroke="#000000" stroke-width="1.5"/>'
            f'<path d="{d}" fill="none" stroke="#ffffff" stroke-opacity="0.10" stroke-width="1"/>')


# ------------------------------------------------------------------ modelos

def exbom():
    # Foto 29: corpo largo em cima, afinando; laser no topo; setas grandes lado a lado; play e tela preta embaixo.
    d = ("M110 18 C168 18 188 52 186 112 L172 470 C169 522 146 548 110 548 C74 548 51 522 48 470 "
         "L34 112 C32 52 52 18 110 18 Z")
    corpo = corpo_path(d) + brilho_lateral("M60 40 C48 70 46 120 50 200 L56 470 C58 500 66 520 76 530 L64 200 C60 120 62 70 74 44 Z")
    extras = ('<rect x="96" y="14" width="28" height="7" rx="3" fill="#5a0c0c"/>' + led(110, 58)
              + '<path d="M70 330 L150 330" stroke="#000000" stroke-opacity="0.5" stroke-width="1.5"/>'
              + "".join(f'<path d="M78 {y} L142 {y}" stroke="#ffffff" stroke-opacity="0.05" stroke-width="2"/>' for y in range(360, 470, 10)))
    b = [
        botao("laser", circulo(110, 94, 18, "url(#botao)"), icone("laser", 110, 94, 18, cor="#ff6a5a")),
        botao("voltar", retang(50, 126, 54, 76, 16), icone("esq", 77, 164, 24)),
        botao("avancar", retang(116, 126, 54, 76, 16), icone("dir", 143, 164, 24)),
        botao("iniciar", retang(70, 214, 80, 30, 15), icone("telaplay", 110, 229, 18)),
        botao("tela-preta", retang(70, 254, 80, 30, 15), icone("preta", 110, 269, 18)),
    ]
    return svg(220, 566, corpo, b, extras)


def r400():
    # Logitech R400: cunha mais fina, laser oval no topo, voltar/avançar grandes, play e tela preta redondos.
    d = ("M110 22 C158 22 176 50 174 96 L162 486 C160 526 140 546 110 546 C80 546 60 526 58 486 "
         "L46 96 C44 50 62 22 110 22 Z")
    corpo = corpo_path(d) + brilho_lateral("M66 44 C56 70 54 110 58 190 L66 480 C68 506 74 522 82 530 L74 190 C70 110 72 70 80 46 Z")
    extras = '<rect x="98" y="18" width="24" height="6" rx="3" fill="#5a0c0c"/>' + led(110, 270, r=6)
    b = [
        botao("laser", retang(86, 66, 48, 26, 13), icone("laser", 110, 79, 16, cor="#ff6a5a")),
        botao("voltar", f'<path d="M62 112 L106 112 L106 206 L74 206 C66 206 62 200 62 192 Z" fill="url(#botao)" stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>', icone("esq", 86, 159, 24)),
        botao("avancar", f'<path d="M114 112 L158 112 L158 192 C158 200 154 206 146 206 L114 206 Z" fill="url(#botao)" stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>', icone("dir", 134, 159, 24)),
        botao("iniciar", circulo(86, 234, 16), icone("play", 86, 234, 14)),
        botao("tela-preta", circulo(134, 234, 16), icone("preta", 134, 234, 14)),
    ]
    return svg(220, 566, corpo, b, extras)


def r800():
    d = ("M110 22 C158 22 176 50 174 96 L162 486 C160 526 140 546 110 546 C80 546 60 526 58 486 "
         "L46 96 C44 50 62 22 110 22 Z")
    corpo = corpo_path(d) + brilho_lateral("M66 44 C56 70 54 110 58 190 L66 480 C68 506 74 522 82 530 L74 190 C70 110 72 70 80 46 Z")
    extras = ('<rect x="98" y="18" width="24" height="6" rx="3" fill="#0c4a1c"/>'
              + '<rect x="72" y="292" width="76" height="44" rx="8" fill="url(#visor)" stroke="#3a3a44" stroke-width="1.2"/>'
              + '<text x="110" y="322" font-family="Consolas" font-weight="bold" font-size="20" fill="#9fe8b6" text-anchor="middle">12:00</text>')
    b = [
        botao("laser", retang(86, 66, 48, 26, 13), icone("laser", 110, 79, 16, cor="#7dff9a")),
        botao("voltar", f'<path d="M62 112 L106 112 L106 206 L74 206 C66 206 62 200 62 192 Z" fill="url(#botao)" stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>', icone("esq", 86, 159, 24)),
        botao("avancar", f'<path d="M114 112 L158 112 L158 192 C158 200 154 206 146 206 L114 206 Z" fill="url(#botao)" stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>', icone("dir", 134, 159, 24)),
        botao("iniciar", circulo(86, 234, 16), icone("play", 86, 234, 14)),
        botao("tela-preta", circulo(134, 234, 16), icone("preta", 134, 234, 14)),
        botao("cronometro", retang(90, 350, 40, 18, 9), ""),
    ]
    return svg(220, 566, corpo, b, extras)


def r500s():
    d = "M100 22 C150 22 162 50 162 100 L162 360 C162 396 136 414 100 414 C64 414 38 396 38 360 L38 100 C38 50 50 22 100 22 Z"
    corpo = corpo_path(d) + brilho_lateral("M52 50 C46 80 46 120 46 200 L48 360 C50 380 56 394 64 400 L56 200 C56 120 58 80 64 54 Z")
    extras = '<rect x="88" y="18" width="24" height="6" rx="3" fill="#5a0c0c"/>'
    b = [
        botao("laser", circulo(100, 88, 22), icone("laser", 100, 88, 18, cor="#ff6a5a")),
        botao("voltar", '<path d="M54 134 L97 134 L97 236 L66 236 C58 236 54 230 54 222 Z" fill="url(#botao)" stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>', icone("esq", 76, 185, 22)),
        botao("avancar", '<path d="M103 134 L146 134 L146 222 C146 230 142 236 134 236 L103 236 Z" fill="url(#botao)" stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>', icone("dir", 124, 185, 22)),
    ]
    return svg(200, 436, corpo, b, extras)


def spotlight():
    corpo = (f'<rect x="40" y="20" width="80" height="490" rx="40" fill="url(#corpoClaro)" stroke="#000000" stroke-width="1.5"/>'
             + brilho_lateral("M52 50 C48 80 48 120 48 200 L48 470 C50 486 54 494 60 498 L58 200 C58 120 58 80 62 54 Z"))
    b = [
        botao("foco", circulo(80, 96, 27, "url(#botao)"), icone("foco", 80, 96, 22)),
        botao("avancar", circulo(80, 176, 31, "url(#botao)"), icone("dir", 80, 176, 24)),
        botao("voltar", circulo(80, 252, 22, "url(#botao)"), icone("esq", 80, 252, 18)),
    ]
    return svg(160, 530, corpo, b)


def kensington():
    d = ("M110 24 C170 24 190 70 188 140 L176 440 C172 500 146 530 110 530 C74 530 48 500 44 440 L32 140 "
         "C30 70 50 24 110 24 Z")
    corpo = corpo_path(d) + brilho_lateral("M54 60 C44 90 42 140 44 220 L54 440 C58 476 66 496 78 508 L62 220 C58 140 60 90 70 62 Z")
    cx, cy, r1, r2 = 110, 170, 28, 70
    extras = ('<rect x="98" y="20" width="24" height="6" rx="3" fill="#5a0c0c"/>'
              + f'<circle cx="{cx}" cy="{cy}" r="{r2 + 6}" fill="#0a0a0d" stroke="#ffffff" stroke-opacity="0.06"/>'
              + led(110, 270, r=6))
    segs = [("iniciar", 225, 315, "telaplay"), ("avancar", -45, 45, "dir"), ("tela-preta", 45, 135, "preta"), ("voltar", 135, 225, "esq")]
    b = [botao(i, segmento(cx, cy, r1, r2, a0, a1), icone(ic, *centro_seg(cx, cy, r1, r2, a0, a1), 20)) for i, a0, a1, ic in segs]
    b.append(botao("laser", circulo(cx, cy, r1 - 4, "url(#laser)"), icone("laser", cx, cy, 16, cor="#ffd9d4")))
    return svg(220, 550, corpo, b, extras)


def baseus():
    corpo = (f'<rect x="36" y="18" width="68" height="530" rx="34" fill="url(#corpoClaro)" stroke="#000000" stroke-width="1.5"/>'
             + brilho_lateral("M46 46 C42 80 42 120 42 200 L42 510 C44 524 48 532 54 536 L52 200 C52 120 52 80 56 50 Z"))
    b = [
        botao("laser", circulo(70, 96, 22, "url(#laranja)"), ""),
        botao("mouse", circulo(70, 156, 17), icone("foco", 70, 156, 16)),
        botao("voltar", circulo(70, 206, 16), icone("esq", 70, 206, 16)),
        botao("avancar", circulo(70, 252, 16), icone("dir", 70, 252, 16)),
    ]
    return svg(140, 566, corpo, b)


def anel3():
    # Foto 30: corpo arredondado com três botões numa faixa, LED azul e a argola embaixo.
    corpo = ('<path d="M70 258 C40 300 60 350 130 352 C200 350 220 300 190 258" fill="none" stroke="#0e0e12" stroke-width="22" stroke-linecap="round"/>'
             '<path d="M70 258 C40 300 60 350 130 352 C200 350 220 300 190 258" fill="none" stroke="#ffffff" stroke-opacity="0.07" stroke-width="2"/>'
             + corpo_path("M130 22 C206 22 236 62 236 136 C236 214 204 268 130 268 C56 268 24 214 24 136 C24 62 54 22 130 22 Z")
             + brilho_lateral("M52 70 C38 100 36 150 46 196 C52 220 62 236 74 246 C60 210 54 160 60 110 C62 90 66 78 72 70 Z")
             + '<rect x="82" y="44" width="96" height="208" rx="42" fill="#08080a"/>')
    extras = led(198, 52, "ledAzul", 9) + '<rect x="232" y="128" width="6" height="22" rx="2" fill="#2a2a30"/>'
    b = [
        botao("cima", '<path d="M88 112 L88 92 C88 66 106 50 130 50 C154 50 172 66 172 92 L172 112 Z" fill="url(#botao)" stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>', icone("cima", 130, 84, 24)),
        botao("baixo", retang(88, 118, 84, 62, 2), icone("baixo", 130, 148, 24)),
        botao("sol", '<path d="M88 186 L172 186 L172 204 C172 230 154 246 130 246 C106 246 88 230 88 204 Z" fill="url(#botao)" stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>', icone("sol", 130, 214, 24, cor="#ffb547")),
    ]
    return svg(260, 372, corpo, b, extras)


def anel5():
    # Foto 31: cruz de quatro botões com o laser no centro, argola e receptor USB-C.
    cx, cy, r1, r2 = 130, 128, 30, 82
    corpo = ('<path d="M66 236 C40 290 64 346 130 348 C196 346 220 290 194 236" fill="none" stroke="#0e0e12" stroke-width="24" stroke-linecap="round"/>'
             + "".join(f'<path d="M{70 + i * 4} {262 + i * 12} C{60 + i * 3} {290 + i * 9} {90} {330} {130} {336 - i}" fill="none" stroke="#ffffff" stroke-opacity="0.05" stroke-width="2"/>' for i in range(4))
             + corpo_path("M130 26 C200 26 228 70 228 130 C228 196 196 244 130 244 C64 244 32 196 32 130 C32 70 60 26 130 26 Z")
             + f'<circle cx="{cx}" cy="{cy}" r="{r2 + 6}" fill="#08080a"/>')
    extras = '<rect x="226" y="140" width="8" height="26" rx="3" fill="#2a2a30"/>' + led(56, 192, "ledAzul", 6)
    segs = [("tela-preta", 225, 315, "tela"), ("avancar", -45, 45, "dir"), ("iniciar", 45, 135, "telaplay"), ("voltar", 135, 225, "esq")]
    b = [botao(i, segmento(cx, cy, r1, r2, a0, a1, folga=3.5), icone(ic, *centro_seg(cx, cy, r1, r2, a0, a1), 22)) for i, a0, a1, ic in segs]
    b.append(botao("laser", circulo(cx, cy, r1 - 4, "url(#botao)"), icone("laser", cx, cy, 20, cor="#ff4a3a")))
    return svg(260, 372, corpo, b, extras)


def r400_generico():
    # Foto 33: corpo em "amendoim", botões cinza em volta do laser triangular.
    d = ("M120 22 C190 22 204 74 198 128 C194 170 176 200 172 240 C168 290 182 330 182 400 C182 470 156 512 120 512 "
         "C84 512 58 470 58 400 C58 330 72 290 68 240 C64 200 46 170 42 128 C36 74 50 22 120 22 Z")
    corpo = corpo_path(d) + brilho_lateral("M62 60 C50 90 50 130 60 170 C66 194 78 214 80 240 C74 214 66 194 62 170 C58 130 58 96 70 64 Z")
    b = [
        botao("iniciar", retang(88, 56, 64, 26, 13, "url(#botaoCinza)"), icone("telaplay", 120, 69, 16, escuro=True)),
        botao("voltar", '<path d="M54 156 L86 132 C90 129 94 132 94 136 L94 178 C94 182 90 185 86 182 Z" fill="url(#botaoCinza)" stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>', icone("esq", 80, 157, 16, escuro=True)),
        botao("avancar", '<path d="M186 156 L154 132 C150 129 146 132 146 136 L146 178 C146 182 150 185 154 182 Z" fill="url(#botaoCinza)" stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"/>', icone("dir", 160, 157, 16, escuro=True)),
        botao("laser", circulo(120, 157, 32, "url(#botao)"), '<path d="M120 140 L136 168 L104 168 Z" fill="none" stroke="#c9c7d2" stroke-width="2.4" stroke-linejoin="round"/><path d="M114 160 L126 160" stroke="#c9c7d2" stroke-width="2"/>'),
        botao("tela-preta", retang(88, 232, 64, 26, 13, "url(#botaoCinza)"), icone("janelas", 120, 245, 16, escuro=True)),
    ]
    return svg(240, 530, corpo, b)


def visor_tab():
    # Foto 34: visor de dois dígitos, LED verde, cruz com laser, Tab, + e −.
    d = ("M100 18 C146 18 162 40 162 84 L156 560 C154 594 134 610 100 610 C66 610 46 594 44 560 L38 84 C38 40 54 18 100 18 Z")
    corpo = corpo_path(d) + brilho_lateral("M52 40 C46 70 46 110 46 190 L52 560 C54 580 58 592 64 598 L58 190 C56 110 58 70 62 44 Z")
    cx, cy, r1, r2 = 100, 170, 21, 52
    extras = ('<rect x="62" y="40" width="76" height="40" rx="8" fill="url(#visor)" stroke="#3a3a44" stroke-width="1.2"/>'
              '<text x="100" y="70" font-family="Consolas" font-weight="bold" font-size="26" fill="#ff5a4a" text-anchor="middle">88</text>'
              + led(100, 98, r=6) + f'<circle cx="{cx}" cy="{cy}" r="{r2 + 5}" fill="#08080a"/>'
              + '<path d="M66 352 L134 352" stroke="#000000" stroke-opacity="0.5" stroke-width="1.5"/>')
    segs = [("cima", 225, 315, "cima"), ("direita", -45, 45, "tela"), ("baixo", 45, 135, "baixo"), ("esquerda", 135, 225, "lupa")]
    b = [botao(i, segmento(cx, cy, r1, r2, a0, a1, folga=2.5), icone(ic, *centro_seg(cx, cy, r1, r2, a0, a1), 15)) for i, a0, a1, ic in segs]
    b.append(botao("laser", circulo(cx, cy, r1 - 3, "url(#botao)"), icone("sol", cx, cy, 16)))
    b += [
        botao("caneta", circulo(62, 254, 15), icone("caneta", 62, 254, 13)),
        botao("tab", retang(82, 241, 36, 26, 8), '<text x="100" y="259" font-family="Segoe UI" font-weight="bold" font-size="11" fill="#e9e7ef" text-anchor="middle">Tab</text>'),
        botao("girar", circulo(138, 254, 15), icone("girar", 138, 254, 13)),
        botao("mais", circulo(70, 302, 17), icone("mais", 70, 302, 15)),
        botao("menos", circulo(130, 302, 17), icone("menos", 130, 302, 15)),
    ]
    return svg(200, 628, corpo, b, extras)


def caneta_visor():
    # Foto 35: caneta fina de metal escuro, "99" no topo, cinco botões numa trilha.
    corpo = (f'<rect x="40" y="18" width="60" height="566" rx="30" fill="url(#corpoClaro)" stroke="#000000" stroke-width="1.5"/>'
             + brilho_lateral("M48 44 C46 80 46 120 46 200 L46 550 C48 566 50 572 54 576 L54 200 C54 120 54 80 56 48 Z")
             + '<rect x="51" y="84" width="38" height="222" rx="19" fill="#0b0b0e"/>')
    extras = ('<text x="70" y="62" font-family="Consolas" font-weight="bold" font-size="16" fill="#f2f0f6" text-anchor="middle">99</text>'
              '<rect x="96" y="360" width="5" height="40" rx="2" fill="#1a1a1e"/>')
    b = [
        botao("foco", circulo(70, 106, 14), icone("anel", 70, 106, 13)),
        botao("voltar", circulo(70, 150, 14), icone("cima", 70, 150, 14)),
        botao("laser", circulo(70, 194, 14), icone("laser", 70, 194, 13, cor="#ff6a5a")),
        botao("avancar", circulo(70, 238, 14), icone("baixo", 70, 238, 14)),
        botao("iniciar", circulo(70, 282, 14), icone("telaplay", 70, 282, 13)),
    ]
    return svg(140, 600, corpo, b, extras)


def caneta_fina():
    # Foto 36: bastão fino preto com quatro botões redondos.
    corpo = (f'<rect x="38" y="18" width="48" height="530" rx="24" fill="url(#corpo)" stroke="#000000" stroke-width="1.5"/>'
             + brilho_lateral("M44 40 C42 80 42 120 42 200 L42 520 C44 532 46 538 50 542 L50 200 C50 120 50 80 52 44 Z"))
    extras = led(62, 70, r=5)
    b = [
        botao("laser", circulo(62, 110, 12), icone("laser", 62, 110, 12, cor="#ff6a5a")),
        botao("voltar", circulo(62, 150, 13), icone("cima", 62, 150, 13)),
        botao("avancar", circulo(62, 196, 15), icone("baixo", 62, 196, 14)),
        botao("iniciar", circulo(62, 244, 15), icone("play", 62, 244, 12)),
    ]
    return svg(124, 566, corpo, b, extras)


def caneta_laranja():
    # Foto 32: caneta achatada com o botão laranja grande e < > e □ menores.
    corpo = ('<rect x="34" y="18" width="56" height="560" rx="18" fill="url(#corpo)" stroke="#000000" stroke-width="1.5"/>'
             + brilho_lateral("M40 40 C38 80 38 120 38 200 L38 550 C40 562 42 568 46 572 L46 200 C46 120 46 80 48 44 Z")
             + '<rect x="46" y="120" width="32" height="190" rx="12" fill="#09090b"/>'
             + '<rect x="50" y="470" width="24" height="60" rx="4" fill="#ffffff" fill-opacity="0.05"/>')
    extras = led(62, 96, r=5)
    b = [
        botao("iniciar", retang(50, 132, 24, 24, 6), icone("tela", 62, 144, 13)),
        botao("avancar", retang(50, 166, 24, 26, 6), icone("dir", 62, 179, 14)),
        botao("voltar", retang(50, 202, 24, 26, 6), icone("esq", 62, 215, 14)),
        botao("laser", retang(46, 246, 32, 46, 10, "url(#laranja)"), ""),
    ]
    return svg(124, 596, corpo, b, extras)


MODELOS = {
    "exbom": exbom, "r400": r400, "r800": r800, "r500s": r500s, "spotlight": spotlight, "kensington": kensington,
    "baseus": baseus, "anel-3": anel3, "anel-5": anel5, "r400-generico": r400_generico, "visor-tab": visor_tab,
    "caneta-visor": caneta_visor, "caneta-fina": caneta_fina, "caneta-laranja": caneta_laranja,
}

if __name__ == "__main__":
    os.makedirs(PASTA, exist_ok=True)
    for nome, f in MODELOS.items():
        with open(os.path.join(PASTA, nome + ".svg"), "w", encoding="utf-8", newline="\n") as arq:
            arq.write(f())
        print("desenho", nome)

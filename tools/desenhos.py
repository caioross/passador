# -*- coding: utf-8 -*-
"""
Gera os desenhos vetoriais dos passadores (catalogo/desenhos/*.svg), feitos por nós a partir das fotos de referência
(fotos de referência guardadas fora do repositório: as de anúncio têm direito de terceiros) e das fichas dos fabricantes. Uso:  python tools/desenhos.py

Regras do formato (o app tem um leitor de SVG próprio, pequeno, em src/Passador.App/Desenho/LeitorSvg.cs):
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
  <linearGradient id="corpoPrata" x1="0" y1="0" x2="1" y2="0">
    <stop offset="0" stop-color="#6e7078"/><stop offset="0.18" stop-color="#e2e3e8"/>
    <stop offset="0.42" stop-color="#b8bac2"/><stop offset="0.82" stop-color="#9a9ca5"/><stop offset="1" stop-color="#55575e"/>
  </linearGradient>
  <linearGradient id="corpoGrafite" x1="0" y1="0" x2="1" y2="0">
    <stop offset="0" stop-color="#121419"/><stop offset="0.18" stop-color="#454b56"/>
    <stop offset="0.4" stop-color="#2b3039"/><stop offset="0.85" stop-color="#1b1e24"/><stop offset="1" stop-color="#0a0b0e"/>
  </linearGradient>
  <linearGradient id="corpoFosco" x1="0" y1="0" x2="1" y2="0">
    <stop offset="0" stop-color="#0c0c0e"/><stop offset="0.2" stop-color="#1f1f23"/>
    <stop offset="0.5" stop-color="#18181b"/><stop offset="0.85" stop-color="#111113"/><stop offset="1" stop-color="#060607"/>
  </linearGradient>
  <linearGradient id="corpoAzul" x1="0" y1="0" x2="1" y2="0">
    <stop offset="0" stop-color="#0b1220"/><stop offset="0.18" stop-color="#2c3e5e"/>
    <stop offset="0.42" stop-color="#1c2a44"/><stop offset="0.85" stop-color="#121c2e"/><stop offset="1" stop-color="#070b14"/>
  </linearGradient>
  <linearGradient id="cromo" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0" stop-color="#5c5f68"/><stop offset="0.35" stop-color="#eceef3"/>
    <stop offset="0.6" stop-color="#8d9099"/><stop offset="1" stop-color="#3c3e45"/>
  </linearGradient>
  <radialGradient id="botaoPrata" cx="0.38" cy="0.3" r="0.9">
    <stop offset="0" stop-color="#f6f6f9"/><stop offset="0.7" stop-color="#c9cad1"/><stop offset="1" stop-color="#9c9ea6"/>
  </radialGradient>
  <radialGradient id="botaoBranco" cx="0.38" cy="0.3" r="0.9">
    <stop offset="0" stop-color="#ffffff"/><stop offset="0.75" stop-color="#e4e4ea"/><stop offset="1" stop-color="#b9b9c2"/>
  </radialGradient>
  <radialGradient id="borracha" cx="0.4" cy="0.32" r="0.9">
    <stop offset="0" stop-color="#2e2e34"/><stop offset="0.8" stop-color="#1a1a1e"/><stop offset="1" stop-color="#101013"/>
  </radialGradient>
  <radialGradient id="laserVerde" cx="0.5" cy="0.5" r="0.5">
    <stop offset="0" stop-color="#7dff9a"/><stop offset="1" stop-color="#0c5a1c"/>
  </radialGradient>
  <radialGradient id="ledVermelho" cx="0.5" cy="0.5" r="0.5">
    <stop offset="0" stop-color="#ffd6d0"/><stop offset="0.45" stop-color="#ff3b2e"/><stop offset="1" stop-color="#ff3b2e" stop-opacity="0"/>
  </radialGradient>
  <linearGradient id="visorAzul" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#0a1626"/><stop offset="1" stop-color="#14263e"/>
  </linearGradient>
  <linearGradient id="visorVerde" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#b9c9a0"/><stop offset="1" stop-color="#93a67a"/>
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
    "volmais": '<path d="M3 9 L7 9 L12 5 L12 19 L7 15 L3 15 Z"/><path d="M16 12 L22 12"/><path d="M19 9 L19 15"/>',
    "volmenos": '<path d="M3 9 L7 9 L12 5 L12 19 L7 15 L3 15 Z"/><path d="M16 12 L22 12"/>',
    "playpausa": '<path d="M4 5 L12 12 L4 19 Z"/><path d="M16 5 L16 19"/><path d="M20 5 L20 19"/>',
    "proxima": '<path d="M5 5 L15 12 L5 19 Z"/><path d="M19 5 L19 19"/>',
    "anterior": '<path d="M19 5 L9 12 L19 19 Z"/><path d="M5 5 L5 19"/>',
    "coracao": '<path d="M12 20 C5 15 3 11.5 3 8.5 C3 5.5 5.5 3.5 8 3.5 C9.8 3.5 11.2 4.6 12 6 C12.8 4.6 14.2 3.5 16 3.5 C18.5 3.5 21 5.5 21 8.5 C21 11.5 19 15 12 20 Z"/>',
    "mouse": '<rect x="6" y="3" width="12" height="18" rx="6"/><path d="M12 7 L12 10"/>',
    "ok": '<path d="M5 12.5 L10 17 L19 7"/>',
    "link": '<path d="M10 14 L14 10"/><path d="M9 7 L11 5 Q14 2 17 5 L19 7 Q22 10 19 13 L17 15"/><path d="M15 17 L13 19 Q10 22 7 19 L5 17 Q2 14 5 11 L7 9"/>',
    "expandir": '<path d="M4 9 L4 4 L9 4"/><path d="M15 4 L20 4 L20 9"/><path d="M20 15 L20 20 L15 20"/><path d="M9 20 L4 20 L4 15"/>',
    "cadeado": '<rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11 L8 7.5 Q8 3.5 12 3.5 Q16 3.5 16 7.5 L16 11"/>',
    "relogio": '<circle cx="12" cy="13" r="8"/><path d="M12 9 L12 13 L15 15"/><path d="M10 2 L14 2"/>',
    "microfone": '<rect x="9" y="3" width="6" height="11" rx="3"/><path d="M5 11 Q5 18 12 18 Q19 18 19 11"/><path d="M12 18 L12 21"/>',
    "mudo": '<rect x="9" y="3" width="6" height="11" rx="3"/><path d="M5 11 Q5 18 12 18 Q19 18 19 11"/><path d="M12 18 L12 21"/><path d="M4 4 L20 20"/>',
    "pessoas": '<circle cx="9" cy="8" r="3.5"/><path d="M3 20 Q3 14 9 14 Q15 14 15 20"/><circle cx="17" cy="9" r="2.5"/><path d="M16.5 14 Q21 14 21 19"/>',
    "teclado": '<rect x="2" y="6" width="20" height="12" rx="2"/><path d="M6 10 L7 10"/><path d="M10 10 L11 10"/><path d="M14 10 L15 10"/><path d="M18 10 L18.5 10"/><path d="M8 14 L16 14"/>',
    "sinal": '<path d="M4 11 Q12 3.5 20 11"/><path d="M7.5 14.5 Q12 10.5 16.5 14.5"/><path d="M12 18.5 L12 18.6"/>',
    "brilhos": '<path d="M12 3 L13.8 10.2 L21 12 L13.8 13.8 L12 21 L10.2 13.8 L3 12 L10.2 10.2 Z"/>',
    "camera": '<path d="M3 8 Q3 6 5 6 L8 6 L10 3.5 L14 3.5 L16 6 L19 6 Q21 6 21 8 L21 18 Q21 20 19 20 L5 20 Q3 20 3 18 Z"/><circle cx="12" cy="13" r="3.5"/>',
    "rolagem": '<path d="M12 3 L12 21"/><path d="M8 7 L12 3 L16 7"/><path d="M8 17 L12 21 L16 17"/>',
    "modo": '<rect x="2" y="7" width="20" height="10" rx="5"/><circle cx="16" cy="12" r="2.6"/>',
    "ponto": '<circle cx="12" cy="12" r="2"/>',
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
              + '<path d="M66 500 C80 506 140 506 154 500" fill="none" stroke="#000000" stroke-opacity="0.7" stroke-width="1.5"/>'
              + '<path d="M68 503 C82 509 138 509 152 503" fill="none" stroke="#ffffff" stroke-opacity="0.08" stroke-width="1"/>'
              + '<rect x="92" y="520" width="36" height="8" rx="3" fill="#0a0a0c" stroke="#ffffff" stroke-opacity="0.06"/>')
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
    # Foto 31: corpo oval mais largo que alto, cruz de quatro botões com o laser no centro, nervuras na pega,
    # alça fina e o receptor USB-C na lateral.
    cx, cy, r1, r2 = 130, 122, 28, 76
    alca = "M86 236 C64 282 80 334 130 336 C180 334 196 282 174 236"
    corpo = (f'<path d="{alca}" fill="none" stroke="#0e0e12" stroke-width="15" stroke-linecap="round"/>'
             f'<path d="{alca}" fill="none" stroke="#ffffff" stroke-opacity="0.07" stroke-width="1.5"/>'
             + corpo_path("M130 24 C206 24 236 62 236 126 C236 172 226 206 206 230 C186 252 160 262 130 262 C100 262 74 252 54 230 C34 206 24 172 24 126 C24 62 54 24 130 24 Z")
             + brilho_lateral("M46 70 C34 100 32 140 40 180 C46 204 56 222 68 234 C56 200 50 160 54 120 C56 98 62 82 70 72 Z")
             + "".join(f'<path d="M{78 + i * 2} {214 + i * 9} C110 {222 + i * 9} 150 {222 + i * 9} {182 - i * 2} {214 + i * 9}" fill="none" stroke="#000000" stroke-opacity="0.55" stroke-width="1.6"/>'
                       f'<path d="M{78 + i * 2} {215.5 + i * 9} C110 {223.5 + i * 9} 150 {223.5 + i * 9} {182 - i * 2} {215.5 + i * 9}" fill="none" stroke="#ffffff" stroke-opacity="0.06" stroke-width="1"/>' for i in range(4))
             + f'<circle cx="{cx}" cy="{cy}" r="{r2 + 6}" fill="#08080a"/>')
    extras = '<rect x="234" y="120" width="8" height="26" rx="3" fill="#2a2a30"/>' + led(60, 180, "ledAzul", 6)
    segs = [("tela-preta", 225, 315, "tela"), ("avancar", -45, 45, "dir"), ("iniciar", 45, 135, "telaplay"), ("voltar", 135, 225, "esq")]
    b = [botao(i, segmento(cx, cy, r1, r2, a0, a1, folga=3.5), icone(ic, *centro_seg(cx, cy, r1, r2, a0, a1), 22)) for i, a0, a1, ic in segs]
    b.append(botao("laser", circulo(cx, cy, r1 - 4, "url(#botao)"), icone("laser", cx, cy, 20, cor="#ff4a3a")))
    return svg(260, 352, corpo, b, extras)

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
              '<text x="100" y="70" font-family="Consolas" font-weight="bold" font-size="26" fill="#e8e6e3" fill-opacity="0.88" text-anchor="middle">88</text>'
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


# ------------------------------------------------------------------ peças paramétricas (catálogo de 12/11/2026)
# Corpos por tipo (cunha, gota, reto, cintura, oval, bastão) e botões em par, em gangorra, de lado e em cruz. Cada
# modelo abaixo é um desenho nosso, genérico, sem logo nem nome de marca: só a forma, a cor e a disposição dos botões.

ESC = 'stroke="#000000" stroke-opacity="0.6" stroke-width="1.2"'


def n(v):
    return f"{v:.1f}".replace(".0", "") if abs(v - round(v)) < 0.05 else f"{v:.1f}"


def forma(d, fill="url(#botao)"):
    return f'<path d="{d}" fill="{fill}" {ESC}/>'


def corpo_cunha(cx, t, b, ws, wb, fill="url(#corpo)", ombro=74):
    """Cunha (R400): larga no ombro e afinando até embaixo."""
    d = (f"M{n(cx)} {n(t)} C{n(cx + ws * 0.75)} {n(t)} {n(cx + ws + 2)} {n(t + 28)} {n(cx + ws)} {n(t + ombro)} "
         f"L{n(cx + wb)} {n(b - 60)} C{n(cx + wb - 2)} {n(b - 20)} {n(cx + wb * 0.55)} {n(b)} {n(cx)} {n(b)} "
         f"C{n(cx - wb * 0.55)} {n(b)} {n(cx - wb + 2)} {n(b - 20)} {n(cx - wb)} {n(b - 60)} "
         f"L{n(cx - ws)} {n(t + ombro)} C{n(cx - ws - 2)} {n(t + 28)} {n(cx - ws * 0.75)} {n(t)} {n(cx)} {n(t)} Z")
    e, eb = cx - ws, cx - wb
    br = (f"M{n(e + 20)} {n(t + 22)} C{n(e + 10)} {n(t + 48)} {n(e + 8)} {n(t + 88)} {n(e + 12)} {n(t + 168)} "
          f"L{n(eb + 8)} {n(b - 66)} C{n(eb + 10)} {n(b - 40)} {n(eb + 16)} {n(b - 24)} {n(eb + 24)} {n(b - 16)} "
          f"L{n(eb + 16)} {n(t + 168)} C{n(e + 24)} {n(t + 88)} {n(e + 26)} {n(t + 48)} {n(e + 34)} {n(t + 24)} Z")
    return corpo_path(d, fill) + brilho_lateral(br)


def corpo_gota(cx, t, b, w, fill="url(#corpo)", base=0.45):
    """Gota: cabeça larga e redonda, ponta estreita e redonda embaixo."""
    h = b - t
    wb = w * base
    d = (f"M{n(cx)} {n(t)} C{n(cx + w * 0.7)} {n(t)} {n(cx + w)} {n(t + h * 0.07)} {n(cx + w)} {n(t + h * 0.22)} "
         f"C{n(cx + w)} {n(t + h * 0.45)} {n(cx + wb * 1.15)} {n(t + h * 0.7)} {n(cx + wb)} {n(b - h * 0.08)} "
         f"C{n(cx + wb * 0.9)} {n(b + 4)} {n(cx - wb * 0.9)} {n(b + 4)} {n(cx - wb)} {n(b - h * 0.08)} "
         f"C{n(cx - wb * 1.15)} {n(t + h * 0.7)} {n(cx - w)} {n(t + h * 0.45)} {n(cx - w)} {n(t + h * 0.22)} "
         f"C{n(cx - w)} {n(t + h * 0.07)} {n(cx - w * 0.7)} {n(t)} {n(cx)} {n(t)} Z")
    br = (f"M{n(cx - w + 14)} {n(t + h * 0.1)} C{n(cx - w + 4)} {n(t + h * 0.25)} {n(cx - w + 8)} {n(t + h * 0.45)} "
          f"{n(cx - wb - 2)} {n(t + h * 0.82)} C{n(cx - w + 22)} {n(t + h * 0.45)} {n(cx - w + 15)} {n(t + h * 0.25)} "
          f"{n(cx - w + 24)} {n(t + h * 0.09)} Z")
    return corpo_path(d, fill) + brilho_lateral(br)


def corpo_reto(cx, t, b, wt, wb, r=30, fill="url(#corpo)"):
    """Trapézio de cantos redondos (controle reto, um pouco mais largo em cima)."""
    d = (f"M{n(cx - wt + r)} {n(t)} L{n(cx + wt - r)} {n(t)} Q{n(cx + wt)} {n(t)} {n(cx + wt)} {n(t + r)} "
         f"L{n(cx + wb)} {n(b - r)} Q{n(cx + wb)} {n(b)} {n(cx + wb - r)} {n(b)} L{n(cx - wb + r)} {n(b)} "
         f"Q{n(cx - wb)} {n(b)} {n(cx - wb)} {n(b - r)} L{n(cx - wt)} {n(t + r)} Q{n(cx - wt)} {n(t)} {n(cx - wt + r)} {n(t)} Z")
    br = (f"M{n(cx - wt + 7)} {n(t + r)} L{n(cx - wb + 7)} {n(b - r)} L{n(cx - wb + 15)} {n(b - r - 12)} "
          f"L{n(cx - wt + 16)} {n(t + r + 8)} Z")
    return corpo_path(d, fill) + brilho_lateral(br)


def corpo_cintura(cx, t, b, ws, wm, wb, fill="url(#corpo)"):
    """Ombro largo, cintura fina no meio e o fundo de novo mais cheio (pega de mão)."""
    h = b - t
    d = (f"M{n(cx)} {n(t)} C{n(cx + ws * 0.8)} {n(t)} {n(cx + ws)} {n(t + 30)} {n(cx + ws)} {n(t + 84)} "
         f"C{n(cx + ws)} {n(t + 150)} {n(cx + wm)} {n(t + h * 0.36)} {n(cx + wm)} {n(t + h * 0.5)} "
         f"C{n(cx + wm)} {n(t + h * 0.64)} {n(cx + wb)} {n(b - h * 0.24)} {n(cx + wb)} {n(b - 70)} "
         f"C{n(cx + wb)} {n(b - 22)} {n(cx + wb * 0.55)} {n(b)} {n(cx)} {n(b)} "
         f"C{n(cx - wb * 0.55)} {n(b)} {n(cx - wb)} {n(b - 22)} {n(cx - wb)} {n(b - 70)} "
         f"C{n(cx - wb)} {n(b - h * 0.24)} {n(cx - wm)} {n(t + h * 0.64)} {n(cx - wm)} {n(t + h * 0.5)} "
         f"C{n(cx - wm)} {n(t + h * 0.36)} {n(cx - ws)} {n(t + 150)} {n(cx - ws)} {n(t + 84)} "
         f"C{n(cx - ws)} {n(t + 30)} {n(cx - ws * 0.8)} {n(t)} {n(cx)} {n(t)} Z")
    br = (f"M{n(cx - ws + 14)} {n(t + 40)} C{n(cx - ws + 6)} {n(t + 90)} {n(cx - ws + 10)} {n(t + 140)} "
          f"{n(cx - wm + 8)} {n(t + h * 0.5)} C{n(cx - wm + 10)} {n(t + h * 0.64)} {n(cx - wb + 8)} {n(b - h * 0.24)} "
          f"{n(cx - wb + 14)} {n(b - 60)} C{n(cx - wb + 18)} {n(b - h * 0.24)} {n(cx - wm + 18)} {n(t + h * 0.6)} "
          f"{n(cx - wm + 16)} {n(t + h * 0.5)} C{n(cx - ws + 22)} {n(t + 140)} {n(cx - ws + 18)} {n(t + 90)} {n(cx - ws + 24)} {n(t + 42)} Z")
    return corpo_path(d, fill) + brilho_lateral(br)


def corpo_oval(cx, t, b, w, fill="url(#corpo)"):
    """Ovo deitado em pé: mais largo em cima, fundo redondo."""
    h = b - t
    d = (f"M{n(cx)} {n(t)} C{n(cx + w * 0.58)} {n(t)} {n(cx + w)} {n(t + h * 0.15)} {n(cx + w)} {n(t + h * 0.4)} "
         f"C{n(cx + w)} {n(t + h * 0.72)} {n(cx + w * 0.62)} {n(b)} {n(cx)} {n(b)} "
         f"C{n(cx - w * 0.62)} {n(b)} {n(cx - w)} {n(t + h * 0.72)} {n(cx - w)} {n(t + h * 0.4)} "
         f"C{n(cx - w)} {n(t + h * 0.15)} {n(cx - w * 0.58)} {n(t)} {n(cx)} {n(t)} Z")
    br = (f"M{n(cx - w + 18)} {n(t + h * 0.14)} C{n(cx - w + 4)} {n(t + h * 0.35)} {n(cx - w + 8)} {n(t + h * 0.62)} "
          f"{n(cx - w * 0.55)} {n(t + h * 0.86)} C{n(cx - w + 22)} {n(t + h * 0.62)} {n(cx - w + 16)} {n(t + h * 0.35)} "
          f"{n(cx - w + 28)} {n(t + h * 0.12)} Z")
    return corpo_path(d, fill) + brilho_lateral(br)


def corpo_bastao(cx, t, b, w, fill="url(#corpo)", rx=None):
    """Bastão (caneta ou barra): retângulo de cantos redondos com o reflexo na lateral."""
    rx = w / 2 if rx is None else rx
    x = cx - w / 2
    s = (f'<rect x="{n(x)}" y="{n(t)}" width="{n(w)}" height="{n(b - t)}" rx="{n(rx)}" fill="{fill}" stroke="#000000" stroke-width="1.5"/>'
         f'<rect x="{n(x)}" y="{n(t)}" width="{n(w)}" height="{n(b - t)}" rx="{n(rx)}" fill="none" stroke="#ffffff" stroke-opacity="0.10" stroke-width="1"/>')
    k = min(rx, 30)
    return s + brilho_lateral(f"M{n(x + 6)} {n(t + k + 4)} L{n(x + 6)} {n(b - k - 4)} C{n(x + 7)} {n(b - k + 6)} "
                              f"{n(x + 10)} {n(b - k + 12)} {n(x + 14)} {n(b - k + 16)} L{n(x + 13)} {n(t + k + 2)} Z")


def visor(x, y, w, h, texto, cor="#9fe8b6", fs=20, fundo="url(#visor)"):
    return (f'<rect x="{n(x)}" y="{n(y)}" width="{n(w)}" height="{n(h)}" rx="7" fill="{fundo}" stroke="#3a3a44" stroke-width="1.2"/>'
            f'<text x="{n(x + w / 2)}" y="{n(y + h / 2 + fs * 0.36)}" font-family="Consolas" font-weight="bold" '
            f'font-size="{fs}" fill="{cor}" text-anchor="middle">{texto}</text>')


def janela_laser(cx, y, w=24, cor="#5a0c0c"):
    return f'<rect x="{n(cx - w / 2)}" y="{n(y)}" width="{n(w)}" height="6" rx="3" fill="{cor}"/>'


def receptor(cx, y, w=36):
    return (f'<rect x="{n(cx - w / 2)}" y="{n(y)}" width="{n(w)}" height="8" rx="3" fill="#0a0a0c" stroke="#ffffff" '
            f'stroke-opacity="0.06"/>')


def nervuras(cx, y, w, qtd=5, passo=9):
    s = ""
    for i in range(qtd):
        yy = y + i * passo
        s += (f'<path d="M{n(cx - w / 2)} {n(yy)} L{n(cx + w / 2)} {n(yy)}" stroke="#000000" stroke-opacity="0.55" stroke-width="1.6"/>'
              f'<path d="M{n(cx - w / 2)} {n(yy + 1.5)} L{n(cx + w / 2)} {n(yy + 1.5)}" stroke="#ffffff" stroke-opacity="0.06" stroke-width="1"/>')
    return s


def furos(cx, y, qtd=3, passo=7):
    return "".join(f'<circle cx="{n(cx + (i - (qtd - 1) / 2) * passo)}" cy="{n(y)}" r="1.6" fill="#050506"/>' for i in range(qtd))


def b_circ(id_, cx, cy, r, ic=None, tam=None, fill="url(#botao)", escuro=False, cor=None, extra=""):
    conteudo = (icone(ic, cx, cy, tam or r * 1.05, escuro=escuro, cor=cor) if ic else "") + extra
    return botao(id_, circulo(cx, cy, r, fill), conteudo)


def b_ret(id_, x, y, w, h, rx, ic=None, tam=None, fill="url(#botao)", escuro=False, cor=None, extra=""):
    conteudo = (icone(ic, x + w / 2, y + h / 2, tam or min(w, h) * 0.6, escuro=escuro, cor=cor) if ic else "") + extra
    return botao(id_, retang(n(x), n(y), n(w), n(h), n(rx), fill), conteudo)


def b_laser_oval(cx, y, w=48, h=26, verde=False, fill="url(#botao)", id_="laser"):
    return b_ret(id_, cx - w / 2, y, w, h, h / 2, "laser", 16, fill, cor="#7dff9a" if verde else "#ff6a5a")


def b_lateral(id_, x, y, w=9, h=34, fill="url(#botao)"):
    """Botão ou chave na lateral do corpo (meio para fora)."""
    return botao(id_, retang(n(x), n(y), n(w), n(h), n(min(w, h) / 2 - 0.5), fill),
                 f'<path d="M{n(x + w / 2)} {n(y + 6)} L{n(x + w / 2)} {n(y + h - 6)}" stroke="#000000" stroke-opacity="0.5" stroke-width="1"/>')


def par(cx, y, h, w=44, gap=8, r=12, estilo="r400", ids=("voltar", "avancar"), ics=("esq", "dir"),
        fill="url(#botao)", tam=24, escuro=False, cor=None):
    """Voltar e avançar lado a lado. estilo: r400 (só o canto de fora arredondado embaixo), ret, seta, meia (meio disco)."""
    xl0, xl1, xr0, xr1 = cx - gap / 2 - w, cx - gap / 2, cx + gap / 2, cx + gap / 2 + w
    if estilo == "r400":
        L = (f"M{n(xl0)} {n(y)} L{n(xl1)} {n(y)} L{n(xl1)} {n(y + h)} L{n(xl0 + r)} {n(y + h)} "
             f"Q{n(xl0)} {n(y + h)} {n(xl0)} {n(y + h - r)} Z")
        R = (f"M{n(xr0)} {n(y)} L{n(xr1)} {n(y)} L{n(xr1)} {n(y + h - r)} Q{n(xr1)} {n(y + h)} {n(xr1 - r)} {n(y + h)} "
             f"L{n(xr0)} {n(y + h)} Z")
        cl, cr = (xl0 + xl1) / 2, (xr0 + xr1) / 2
    elif estilo == "ret":
        L = (f"M{n(xl0 + r)} {n(y)} L{n(xl1 - r)} {n(y)} Q{n(xl1)} {n(y)} {n(xl1)} {n(y + r)} L{n(xl1)} {n(y + h - r)} "
             f"Q{n(xl1)} {n(y + h)} {n(xl1 - r)} {n(y + h)} L{n(xl0 + r)} {n(y + h)} Q{n(xl0)} {n(y + h)} {n(xl0)} {n(y + h - r)} "
             f"L{n(xl0)} {n(y + r)} Q{n(xl0)} {n(y)} {n(xl0 + r)} {n(y)} Z")
        R = (f"M{n(xr0 + r)} {n(y)} L{n(xr1 - r)} {n(y)} Q{n(xr1)} {n(y)} {n(xr1)} {n(y + r)} L{n(xr1)} {n(y + h - r)} "
             f"Q{n(xr1)} {n(y + h)} {n(xr1 - r)} {n(y + h)} L{n(xr0 + r)} {n(y + h)} Q{n(xr0)} {n(y + h)} {n(xr0)} {n(y + h - r)} "
             f"L{n(xr0)} {n(y + r)} Q{n(xr0)} {n(y)} {n(xr0 + r)} {n(y)} Z")
        cl, cr = (xl0 + xl1) / 2, (xr0 + xr1) / 2
    elif estilo == "seta":
        p = min(18, w * 0.4)
        L = (f"M{n(xl1 - 6)} {n(y)} Q{n(xl1)} {n(y)} {n(xl1)} {n(y + 6)} L{n(xl1)} {n(y + h - 6)} Q{n(xl1)} {n(y + h)} {n(xl1 - 6)} {n(y + h)} "
             f"L{n(xl0 + p)} {n(y + h)} L{n(xl0)} {n(y + h / 2)} L{n(xl0 + p)} {n(y)} Z")
        R = (f"M{n(xr0 + 6)} {n(y)} Q{n(xr0)} {n(y)} {n(xr0)} {n(y + 6)} L{n(xr0)} {n(y + h - 6)} Q{n(xr0)} {n(y + h)} {n(xr0 + 6)} {n(y + h)} "
             f"L{n(xr1 - p)} {n(y + h)} L{n(xr1)} {n(y + h / 2)} L{n(xr1 - p)} {n(y)} Z")
        cl, cr = (xl0 + xl1) / 2 + 2, (xr0 + xr1) / 2 - 2
    else:  # meia: um disco cortado ao meio
        rr = h / 2
        L = f"M{n(xl1)} {n(y)} A{n(rr)} {n(rr)} 0 0 0 {n(xl1)} {n(y + h)} Z"
        R = f"M{n(xr0)} {n(y)} A{n(rr)} {n(rr)} 0 0 1 {n(xr0)} {n(y + h)} Z"
        cl, cr = xl1 - rr * 0.45, xr0 + rr * 0.45
    return [botao(ids[0], forma(L, fill), icone(ics[0], cl, y + h / 2, tam, escuro=escuro, cor=cor)),
            botao(ids[1], forma(R, fill), icone(ics[1], cr, y + h / 2, tam, escuro=escuro, cor=cor))]


def gangorra(cx, y, w, h, gap=5, ids=("voltar", "avancar"), ics=("cima", "baixo"), fill="url(#botao)", tam=20,
             escuro=False):
    """Voltar em cima e avançar embaixo, numa pílula cortada ao meio (bastões e canetas)."""
    r = w / 2
    ym = y + h / 2
    top = (f"M{n(cx - r)} {n(ym - gap / 2)} L{n(cx - r)} {n(y + r)} A{n(r)} {n(r)} 0 0 1 {n(cx + r)} {n(y + r)} "
           f"L{n(cx + r)} {n(ym - gap / 2)} Z")
    bot = (f"M{n(cx - r)} {n(ym + gap / 2)} L{n(cx + r)} {n(ym + gap / 2)} L{n(cx + r)} {n(y + h - r)} "
           f"A{n(r)} {n(r)} 0 0 1 {n(cx - r)} {n(y + h - r)} Z")
    return [botao(ids[0], forma(top, fill), icone(ics[0], cx, (y + ym) / 2 + 2, tam, escuro=escuro)),
            botao(ids[1], forma(bot, fill), icone(ics[1], cx, (ym + y + h) / 2 - 2, tam, escuro=escuro))]


def cruz(cx, cy, r1, r2, itens, folga=3.0, fill="url(#botao)", tam=20):
    """Botões em fatias de anel: itens = [(id, a0, a1, ícone)]."""
    return [botao(i, segmento(cx, cy, r1, r2, a0, a1, fill, folga), icone(ic, *centro_seg(cx, cy, r1, r2, a0, a1), tam))
            for i, a0, a1, ic in itens]


def circulo_corpo(cx, cy, r, fill="url(#corpo)"):
    d = f"M{n(cx - r)} {n(cy)} A{n(r)} {n(r)} 0 0 1 {n(cx + r)} {n(cy)} A{n(r)} {n(r)} 0 0 1 {n(cx - r)} {n(cy)} Z"
    return corpo_path(d, fill)


# ------------------------------------------------------------------ modelos que dividiam desenho

def spotlight_2():
    corpo = (corpo_bastao(75, 20, 520, 70, "url(#corpoGrafite)")
             + '<rect x="48" y="52" width="54" height="244" rx="27" fill="#0b0b0e"/>')
    extras = led(75, 278, "ledAzul", 5) + '<rect x="63" y="508" width="24" height="5" rx="2" fill="#050506"/>'
    b = [b_circ("foco", 75, 90, 21, "foco", 18), b_circ("avancar", 75, 160, 25, "dir", 22),
         b_circ("voltar", 75, 226, 19, "esq", 16)]
    return svg(150, 540, corpo, b, extras)


def kensington_expert():
    corpo = (corpo_gota(115, 22, 540, 94, "url(#corpoFosco)")
             + '<circle cx="115" cy="172" r="80" fill="url(#cromo)"/><circle cx="115" cy="172" r="74" fill="#0a0a0d"/>'
             + nervuras(115, 352, 70, 6, 10))
    extras = janela_laser(115, 18, cor="#0c4a1c") + led(115, 296, "ledVerde", 6)
    b = cruz(115, 172, 28, 68, [("iniciar", 225, 315, "telaplay"), ("avancar", -45, 45, "dir"),
                                ("tela-preta", 45, 135, "preta"), ("voltar", 135, 225, "esq")])
    b.append(b_circ("laser", 115, 172, 24, "laser", 17, "url(#laserVerde)", cor="#eaffef"))
    return svg(230, 560, corpo, b, extras)


def knup_passador():
    corpo = corpo_cunha(110, 22, 544, 58, 46, ombro=82)
    extras = (janela_laser(110, 18) + '<circle cx="110" cy="84" r="23" fill="#3a0b0b"/>' + led(110, 280, "ledVermelho", 6)
              + '<path d="M78 300 L142 300" stroke="#000000" stroke-opacity="0.5" stroke-width="1.5"/>' + receptor(110, 516, 30))
    b = [b_circ("laser", 110, 84, 18, "laser", 15, cor="#ff6a5a"),
         b_circ("voltar", 82, 146, 24, "esq", 22), b_circ("avancar", 138, 146, 24, "dir", 22),
         b_ret("iniciar", 80, 190, 60, 26, 13, "telaplay", 16), b_ret("tela-preta", 80, 224, 60, 26, 13, "preta", 16)]
    return svg(220, 560, corpo, b, extras)


def genius_ring():
    alca = "M84 232 C58 292 80 352 130 354 C180 352 202 292 176 232"
    corpo = (f'<path d="{alca}" fill="none" stroke="#0e0e12" stroke-width="20" stroke-linecap="round"/>'
             f'<path d="{alca}" fill="none" stroke="#ffffff" stroke-opacity="0.07" stroke-width="2"/>'
             + circulo_corpo(130, 136, 104) + brilho_lateral("M50 80 C34 110 32 160 50 196 C42 160 44 116 62 84 Z")
             + '<circle cx="130" cy="136" r="74" fill="#08080a"/>')
    extras = ('<circle cx="66" cy="136" r="3" fill="#2a2a30"/><circle cx="194" cy="136" r="3" fill="#2a2a30"/>'
              + led(186, 60, "ledVermelho", 6))
    b = cruz(130, 136, 30, 68, [("cima", 205, 335, "cima"), ("baixo", 25, 155, "baixo")], folga=3.5, tam=22)
    b.append(b_circ("sol", 130, 136, 25, "laser", 18, "url(#laser)", cor="#ffd9d4"))
    return svg(260, 374, corpo, b, extras)


# ------------------------------------------------------------------ modelos novos (pesquisa de 12/11/2026)

def logitech_r700():
    corpo = corpo_reto(100, 20, 584, 62, 52, 40, "url(#corpoFosco)")
    extras = (janela_laser(100, 16) + visor(62, 50, 76, 40, "18:00", "#8fd0ff", 20, "url(#visorAzul)")
              + receptor(100, 560, 34))
    b = [b_laser_oval(100, 104, 52, 26)] + par(100, 146, 94, 40, 8, 12, "r400") + [
        b_circ("iniciar", 76, 272, 16, "play", 14), b_circ("tela-preta", 124, 272, 16, "preta", 14),
        b_ret("cronometro", 84, 302, 32, 16, 8, "relogio", 11), b_lateral("liga", 152, 360, 9, 36)]
    return svg(200, 604, corpo, b, extras)


def logitech_cordless():
    corpo = corpo_oval(120, 20, 470, 96, "url(#corpoPrata)")
    extras = visor(78, 52, 84, 36, "05:00", "#1d2a12", 18, "url(#visorVerde)") + led(120, 398, "ledVermelho", 5)
    b = [b_ret("cronometro", 102, 96, 36, 14, 7, None), b_circ("laser", 120, 140, 19, "laser", 15, cor="#ff6a5a")]
    b += par(120, 172, 84, 0, 10, estilo="meia", tam=22)
    b += [b_circ("tela-preta", 92, 292, 15, "preta", 14), b_circ("iniciar", 148, 292, 15, "telaplay", 14),
          b_circ("volume-menos", 98, 336, 12, "volmenos", 12), b_circ("volume-mais", 142, 336, 12, "volmais", 12)]
    return svg(240, 490, corpo, b, extras)


def kensington_k75233():
    corpo = corpo_bastao(75, 30, 540, 72, "url(#corpo)")
    extras = led(75, 304, "ledAzul", 5) + receptor(75, 520, 28)
    b = [b_ret("tela-preta", 53, 16, 44, 20, 9, None)]
    b += gangorra(75, 66, 52, 140, 5, tam=22)
    b.append(b_circ("ponteiro", 75, 252, 22, "foco", 18))
    return svg(150, 560, corpo, b, extras)


def kensington_si600():
    corpo = (corpo_cunha(110, 22, 500, 62, 50, "url(#corpoGrafite)")
             + '<rect x="58" y="62" width="104" height="204" rx="34" fill="url(#cromo)" opacity="0.85"/>'
             + '<rect x="62" y="66" width="96" height="196" rx="30" fill="#141519" opacity="0.55"/>')
    extras = janela_laser(110, 18) + receptor(110, 474, 32)
    b = [b_circ("laser", 110, 98, 18, "laser", 15, cor="#ff6a5a")]
    b += par(110, 128, 74, 40, 8, 12, "ret", tam=22)
    b.append(b_circ("tela-preta", 110, 232, 17, "preta", 15))
    return svg(220, 520, corpo, b, extras)


def kensington_slimblade():
    corpo = (corpo_bastao(120, 20, 380, 180, "url(#corpoFosco)", 74)
             + '<path d="M120 176 L120 360" stroke="#000000" stroke-opacity="0.4" stroke-width="1.2"/>')
    extras = '<rect x="102" y="356" width="36" height="6" rx="3" fill="#0a0a0c"/>'
    cl = "M110 30 L110 172 L40 172 L40 112 C40 62 66 32 110 30 Z"
    cr = "M130 30 L130 172 L200 172 L200 112 C200 62 174 32 130 30 Z"
    b = [botao("voltar", forma(cl, "url(#borracha)"), icone("esq", 78, 110, 26, cor="#6fb3ff")),
         botao("avancar", forma(cr, "url(#borracha)"), icone("dir", 162, 110, 26, cor="#6fb3ff")),
         b_ret("roda", 113, 58, 14, 44, 7, None, fill="url(#botaoCinza)"),
         b_circ("laser", 120, 136, 10, None, fill="url(#laser)")]
    return svg(240, 400, corpo, b, extras)


def kensington_slimblade_media():
    corpo = corpo_bastao(120, 20, 400, 180, "url(#corpoFosco)", 74) + '<circle cx="120" cy="236" r="72" fill="#08080a"/>'
    cl = "M110 30 L110 102 L42 102 C46 58 70 32 110 30 Z"
    cr = "M130 30 L130 102 L198 102 C194 58 170 32 130 30 Z"
    b = [botao("voltar", forma(cl, "url(#borracha)"), icone("esq", 84, 72, 20)),
         botao("avancar", forma(cr, "url(#borracha)"), icone("dir", 156, 72, 20)),
         b_circ("laser", 120, 132, 10, None, fill="url(#laser)")]
    b += cruz(120, 236, 26, 66, [("volume-mais", 225, 315, "volmais"), ("faixa-seguinte", -45, 45, "proxima"),
                                 ("volume-menos", 45, 135, "volmenos"), ("faixa-anterior", 135, 225, "anterior")],
              tam=18)
    b += [b_circ("play", 120, 236, 21, "playpausa", 16), b_ret("modo", 100, 350, 40, 16, 8, None, fill="url(#botaoCinza)")]
    return svg(240, 420, corpo, b)


def targus_amp13():
    corpo = corpo_oval(120, 20, 450, 100) + '<circle cx="120" cy="150" r="84" fill="#09090b"/>'
    extras = '<circle cx="120" cy="150" r="33" fill="none" stroke="#2fd36b" stroke-opacity="0.75" stroke-width="3"/>'
    b = cruz(120, 150, 38, 78, [("voltar", 95, 265, "esq"), ("avancar", -85, 85, "dir")], folga=3.5, tam=24)
    b += [b_circ("liga", 120, 150, 26, "liga", 18),
          b_circ("iniciar", 90, 266, 15, "telaplay", 14), b_circ("tela-preta", 150, 266, 15, "preta", 14),
          b_ret("trava", 104, 296, 32, 16, 8, "cadeado", 11), b_ret("laser", 90, 334, 60, 30, 15, "laser", 16, "url(#laser)", cor="#ffd9d4")]
    return svg(240, 470, corpo, b, extras)


def targus_amp16():
    corpo = corpo_cunha(110, 22, 522, 60, 48)
    extras = janela_laser(110, 18) + led(110, 250, "ledVerde", 5) + receptor(110, 496, 30)
    b = [b_laser_oval(110, 58, 50, 26)] + par(110, 100, 76, 42, 8, 10, "ret", tam=22) + [
        b_circ("iniciar", 84, 212, 16, "telaplay", 14), b_circ("tela-preta", 136, 212, 16, "preta", 14),
        b_circ("conectar", 110, 458, 9, "sinal", 11), b_lateral("liga", 159, 290, 9, 34)]
    return svg(220, 540, corpo, b, extras)


def targus_amp17():
    corpo = corpo_cunha(110, 22, 544, 64, 50)
    extras = janela_laser(110, 18) + receptor(110, 516, 30)
    b = [b_laser_oval(110, 56, 52, 26),
         b_circ("cursor", 110, 128, 29, None, extra='<circle cx="110" cy="128" r="15" fill="url(#borracha)" stroke="#000000" stroke-opacity="0.5"/>'
                + '<circle cx="110" cy="128" r="4" fill="#3d3c45"/>')]
    b += par(110, 172, 80, 44, 8, 12, "r400")
    b += [b_circ("iniciar", 84, 282, 15, "play", 13), b_circ("tela-preta", 136, 282, 15, "preta", 13),
          b_lateral("modo", 49, 320, 9, 34)]
    return svg(220, 564, corpo, b, extras)


def targus_amp067():
    corpo = (corpo_reto(110, 22, 522, 60, 50, 34, "url(#corpoGrafite)")
             + '<rect x="66" y="48" width="88" height="232" rx="28" fill="#0b0c10"/>')
    extras = janela_laser(110, 18) + led(110, 256, "ledAzul", 5) + receptor(110, 498, 30)
    b = [b_laser_oval(110, 62, 48, 26)] + par(110, 102, 72, 36, 6, 10, "ret", tam=20) + [
        b_circ("iniciar", 90, 212, 15, "telaplay", 13), b_circ("tela-preta", 130, 212, 15, "preta", 13)]
    return svg(220, 540, corpo, b, extras)


def canon_pr10g():
    corpo = corpo_cunha(115, 20, 584, 68, 52)
    extras = janela_laser(115, 16, cor="#0c4a1c") + visor(69, 48, 92, 44, "00:00", "#9fe8b6", 22) + receptor(115, 556, 34)
    b = [b_ret("cronometro", 99, 100, 32, 14, 7, None), b_circ("laser", 115, 150, 28, "laser", 22, "url(#laserVerde)", cor="#eaffef")]
    b += par(115, 194, 96, 48, 8, 14, "r400", tam=26)
    b += [b_circ("iniciar", 90, 318, 14, "play", 12), b_circ("trava", 140, 318, 14, "cadeado", 12),
          b_lateral("modo", 168, 370, 9, 36)]
    return svg(230, 600, corpo, b, extras)


def canon_pr100r():
    corpo = corpo_cunha(105, 22, 522, 56, 46, "url(#corpoFosco)", ombro=70)
    extras = janela_laser(105, 18) + led(105, 254, "ledVermelho", 5) + receptor(105, 496, 28)
    b = [b_laser_oval(105, 58, 46, 24)] + par(105, 98, 90, 40, 8, 12, "r400") + [
        b_circ("tela-preta", 82, 214, 15, "preta", 14, "url(#botaoBranco)", escuro=True),
        b_circ("iniciar", 128, 214, 15, "telaplay", 14)]
    return svg(210, 540, corpo, b, extras)


def canon_pr500r():
    corpo = corpo_gota(110, 22, 520, 66, "url(#corpoGrafite)", 0.6)
    extras = janela_laser(110, 18) + led(110, 248, "ledVerde", 5) + receptor(110, 494, 30)
    b = [b_laser_oval(110, 62, 48, 26)] + par(110, 104, 70, 44, 8, estilo="seta", tam=22) + [
        b_circ("iniciar", 86, 210, 15, "telaplay", 13), b_circ("tela-preta", 134, 210, 15, "preta", 13),
        b_lateral("modo", 45, 230, 9, 34)]
    return svg(220, 540, corpo, b, extras)


def canon_pr1100r():
    corpo = corpo_bastao(65, 20, 600, 52)
    extras = (janela_laser(65, 16, 16) + '<path d="M41 520 L89 520" stroke="#000000" stroke-opacity="0.6" stroke-width="1.5"/>'
              + '<path d="M41 522 L89 522" stroke="#ffffff" stroke-opacity="0.07" stroke-width="1"/>')
    b = [b_circ("laser", 65, 76, 14, "laser", 12, cor="#ff6a5a"),
         b_circ("voltar", 65, 126, 17, "cima", 16), b_circ("avancar", 65, 172, 17, "baixo", 16),
         b_circ("iniciar", 65, 216, 12, "play", 10), b_circ("tela-preta", 65, 250, 12, "preta", 11),
         b_lateral("volume-mais", 87, 118, 8, 30), b_lateral("volume-menos", 87, 154, 8, 30)]
    return svg(130, 620, corpo, b, extras)


def norwii_n97s():
    corpo = corpo_bastao(70, 20, 600, 58, "url(#corpoGrafite)", 22)
    extras = '<rect x="58" y="588" width="24" height="5" rx="2" fill="#050506"/>' + led(70, 330, "ledAzul", 4)
    b = [b_circ("laser", 70, 62, 11, "laser", 11, cor="#ff6a5a"), b_circ("mouse", 70, 108, 23, "foco", 18)]
    b += gangorra(70, 146, 44, 112, 5, tam=18)
    b += [b_circ("caneta", 70, 290, 13, "caneta", 11), b_lateral("volume-mais", 37, 196, 8, 30),
          b_lateral("volume-menos", 37, 232, 8, 30)]
    return svg(140, 620, corpo, b, extras)


def norwii_n76():
    corpo = corpo_gota(110, 22, 542, 68, "url(#corpo)", 0.56)
    extras = janela_laser(110, 18) + led(110, 248, "ledVerde", 5) + receptor(110, 516, 30)
    b = [b_laser_oval(110, 58, 48, 26)] + par(110, 98, 84, 42, 8, 12, "r400") + [
        b_circ("tela-cheia", 84, 212, 15, "expandir", 13), b_circ("hyperlink", 136, 212, 15, "link", 13),
        b_lateral("volume-mais", 167, 250, 9, 30), b_lateral("volume-menos", 166, 288, 9, 30)]
    return svg(220, 560, corpo, b, extras)


def norwii_n27():
    d = ("M120 22 C190 22 204 74 198 128 C194 170 176 200 172 240 C168 290 182 330 182 400 C182 470 156 512 120 512 "
         "C84 512 58 470 58 400 C58 330 72 290 68 240 C64 200 46 170 42 128 C36 74 50 22 120 22 Z")
    corpo = corpo_path(d, "url(#corpoFosco)") + brilho_lateral(
        "M62 60 C50 90 50 130 60 170 C66 194 78 214 80 240 C74 214 66 194 62 170 C58 130 58 96 70 64 Z")
    extras = janela_laser(120, 18) + led(120, 340, "ledAzul", 5) + receptor(120, 490, 30)
    b = [b_laser_oval(120, 56, 48, 26)] + par(120, 112, 66, 46, 8, estilo="seta", tam=22) + [
        b_circ("hyperlink", 120, 224, 16, "link", 14),
        b_circ("volume-menos", 98, 286, 13, "volmenos", 12), b_circ("volume-mais", 142, 286, 13, "volmais", 12)]
    return svg(240, 530, corpo, b, extras)


def microsoft_presenter_plus():
    corpo = corpo_bastao(70, 20, 540, 66, "url(#corpoFosco)")
    extras = '<circle cx="70" cy="44" r="3" fill="#ffffff" fill-opacity="0.85"/>' + receptor(70, 518, 22)
    b = [b_circ("teams", 70, 82, 18, "pessoas", 16), b_circ("mudo", 70, 126, 15, "mudo", 13)]
    b += gangorra(70, 160, 50, 136, 5, tam=22)
    b += [b_circ("ponteiro", 70, 328, 18, "foco", 15), b_lateral("parear", 100, 400, 9, 26),
          b_lateral("liga", 31, 400, 9, 26)]
    return svg(140, 560, corpo, b, extras)


def lenovo_x1():
    corpo = (corpo_bastao(80, 20, 540, 96, "url(#corpoFosco)", 30)
             + '<path d="M34 282 L126 282" stroke="#000000" stroke-opacity="0.7" stroke-width="2"/>'
             + '<path d="M34 285 L126 285" stroke="#ffffff" stroke-opacity="0.08" stroke-width="1"/>')
    cl = "M72 26 L72 124 L35 124 L35 52 Q35 26 58 26 Z"
    cr = "M88 26 L88 124 L125 124 L125 52 Q125 26 102 26 Z"
    b = [botao("clique-esquerdo", forma(cl)), botao("clique-direito", forma(cr)),
         b_ret("rolagem", 74, 38, 12, 70, 6, None, fill="#0c0c0e"),
         b_circ("laser-virtual", 80, 190, 20, "foco", 16), b_circ("ponteiro", 80, 236, 16, "telaplay", 14)]
    return svg(160, 560, corpo, b)


def avatto_h100():
    corpo = corpo_bastao(70, 20, 580, 62)
    extras = visor(46, 46, 48, 26, "25", "#8fd0ff", 16, "url(#visorAzul)") + receptor(70, 556, 24)
    b = [b_circ("laser", 70, 96, 11, "laser", 11, cor="#ff6a5a"), b_circ("modo", 70, 140, 22, "foco", 17)]
    b += gangorra(70, 178, 46, 116, 5, tam=19)
    b += [b_circ("iniciar", 54, 322, 12, "telaplay", 11), b_circ("tela-preta", 86, 322, 12, "preta", 11)]
    return svg(140, 600, corpo, b, extras)


def avatto_i10():
    corpo = corpo_bastao(70, 20, 580, 60, "url(#corpoFosco)", 18) + '<circle cx="70" cy="76" r="28" fill="url(#cromo)"/>'
    extras = receptor(70, 556, 24) + led(70, 364, "ledAzul", 4)
    b = [b_circ("laser-digital", 70, 76, 24, "laser", 18, cor="#ffb547")]
    b += gangorra(70, 116, 44, 110, 5, tam=18)
    b += [b_circ("iniciar", 70, 254, 13, "expandir", 11), b_circ("caneta", 70, 290, 13, "caneta", 11),
          b_circ("hyperlink", 70, 326, 13, "link", 11)]
    return svg(140, 600, corpo, b, extras)


def august_lp315():
    corpo = corpo_cunha(110, 22, 552, 64, 50, "url(#corpoGrafite)")
    extras = janela_laser(110, 18) + visor(70, 48, 80, 36, "08:30", "#8fd0ff", 18, "url(#visorAzul)") + receptor(110, 526, 30)
    b = [b_ret("cronometro", 96, 90, 28, 12, 6, None), b_laser_oval(110, 110, 46, 24),
         b_circ("mouse", 110, 166, 23, "mouse", 18)]
    b += par(110, 200, 70, 42, 10, 12, "ret", tam=22)
    b += [b_circ("iniciar", 86, 298, 14, "telaplay", 12), b_circ("tela-preta", 134, 298, 14, "preta", 12)]
    return svg(220, 570, corpo, b, extras)


def doosl_dsit032():
    corpo = corpo_cunha(105, 22, 462, 58, 46, ombro=70)
    extras = janela_laser(105, 18) + led(105, 252, "ledAzul", 6) + receptor(105, 436, 28)
    b = [b_laser_oval(105, 58, 46, 24, fill="url(#botaoCinza)")] + par(
        105, 98, 80, 40, 8, 14, "ret", fill="url(#botaoCinza)", tam=22, escuro=True) + [
        b_circ("hyperlink", 105, 214, 16, "link", 14, "url(#botaoCinza)", escuro=True)]
    return svg(210, 480, corpo, b, extras)


def doosl_dsit014b():
    corpo = corpo_bastao(70, 20, 580, 64)
    extras = janela_laser(70, 16, 18) + receptor(70, 556, 24)
    b = [b_circ("laser", 70, 64, 12, "laser", 11, cor="#ff6a5a"), b_circ("mouse", 70, 108, 20, "mouse", 16)]
    b += gangorra(70, 144, 44, 110, 5, tam=18)
    b += [b_circ("caneta", 70, 282, 13, "caneta", 11), b_circ("volume-menos", 53, 320, 11, "volmenos", 10),
          b_circ("volume-mais", 87, 320, 11, "volmais", 10)]
    return svg(140, 600, corpo, b, extras)


def baseus_orange_dot_ai():
    corpo = corpo_bastao(70, 18, 562, 64, "url(#corpoClaro)")
    extras = furos(70, 312, 3, 8) + '<rect x="58" y="548" width="24" height="5" rx="2" fill="#050506"/>'
    b = [b_circ("laser", 70, 90, 24, None, fill="url(#laranja)")]
    b += gangorra(70, 132, 44, 112, 5, tam=18)
    b.append(b_circ("ia", 70, 276, 16, "microfone", 14))
    return svg(140, 580, corpo, b, extras)


def multilaser_ac285():
    corpo = corpo_bastao(60, 20, 580, 46)
    extras = (janela_laser(60, 16, 14) + '<rect x="80" y="40" width="8" height="160" rx="4" fill="url(#cromo)"/>'
              + '<path d="M37 470 L83 470" stroke="#000000" stroke-opacity="0.6" stroke-width="1.5"/>')
    b = [b_circ("laser", 60, 76, 13, "laser", 11, cor="#ff6a5a"), b_circ("voltar", 60, 122, 14, "cima", 13),
         b_circ("avancar", 60, 164, 14, "baixo", 13)]
    return svg(120, 600, corpo, b, extras)


def multilaser_ac385():
    corpo = corpo_reto(110, 22, 512, 58, 50, 40, "url(#corpoFosco)")
    extras = (janela_laser(110, 18) + '<rect x="62" y="252" width="96" height="3" rx="1.5" fill="#8a1a14" opacity="0.8"/>'
              + receptor(110, 488, 30))
    b = [b_laser_oval(110, 58, 50, 26, fill="url(#borracha)")] + par(110, 98, 74, 42, 10, 16, "ret", fill="url(#borracha)", tam=22) + [
        b_circ("iniciar", 86, 206, 15, "telaplay", 13, "url(#borracha)"),
        b_circ("tela-preta", 134, 206, 15, "preta", 13, "url(#borracha)")]
    return svg(220, 530, corpo, b, extras)


def multilaser_ac164():
    corpo = corpo_cunha(105, 22, 452, 56, 44, ombro=70)
    extras = janela_laser(105, 18) + receptor(105, 426, 28)
    b = [b_laser_oval(105, 58, 46, 24)] + par(105, 100, 80, 0, 8, estilo="meia", tam=22) + [
        b_circ("tela-preta", 105, 214, 16, "preta", 14)]
    return svg(210, 470, corpo, b, extras)


def knup_kp8001():
    corpo = ('<rect x="36" y="18" width="52" height="556" rx="16" fill="url(#corpo)" stroke="#000000" stroke-width="1.5"/>'
             + brilho_lateral("M42 40 C40 80 40 120 40 200 L40 548 C42 558 44 562 48 566 L48 200 C48 120 48 80 50 44 Z")
             + nervuras(62, 400, 36, 8, 9))
    extras = janela_laser(62, 14, 16)
    b = [b_circ("laser", 62, 82, 13, "laser", 11, cor="#ff6a5a"),
         b_ret("voltar", 48, 114, 28, 40, 12, "cima", 14), b_ret("avancar", 48, 160, 28, 40, 12, "baixo", 14)]
    return svg(124, 590, corpo, b, extras)


def exbom_lpt8():
    corpo = ('<rect x="34" y="18" width="56" height="566" rx="20" fill="url(#corpoGrafite)" stroke="#000000" stroke-width="1.5"/>'
             + brilho_lateral("M40 44 C38 80 38 120 38 200 L38 556 C40 566 42 570 46 574 L46 200 C46 120 46 80 48 48 Z")
             + '<rect x="46" y="74" width="32" height="196" rx="14" fill="#0b0b0e"/>')
    extras = janela_laser(62, 14, 16) + led(62, 52, "ledVermelho", 5)
    b = [b_circ("laser", 62, 92, 13, "laser", 11, cor="#ff6a5a"),
         b_ret("voltar", 49, 118, 26, 34, 8, "cima", 13), b_ret("avancar", 49, 158, 26, 34, 8, "baixo", 13),
         b_circ("iniciar", 62, 214, 11, "play", 9), b_circ("tela-preta", 62, 246, 11, "preta", 10)]
    return svg(124, 600, corpo, b, extras)


def c3tech_ap200():
    corpo = corpo_cunha(110, 22, 542, 62, 50)
    extras = janela_laser(110, 18) + led(110, 46, "ledVermelho", 5) + receptor(110, 516, 30)
    b = [b_laser_oval(110, 64, 48, 24)] + par(110, 104, 70, 46, 8, estilo="seta", tam=22) + [
        b_circ("tab", 110, 206, 16, None, extra='<text x="110" y="210" font-family="Segoe UI" font-weight="bold" font-size="10" fill="#e9e7ef" text-anchor="middle">Tab</text>'),
        b_circ("volume-menos", 86, 250, 13, "volmenos", 12), b_circ("volume-mais", 134, 250, 13, "volmais", 12),
        b_lateral("liga", 52, 300, 9, 32)]
    return svg(220, 560, corpo, b, extras)


def genius_mp100():
    corpo = corpo_gota(110, 22, 522, 64, "url(#corpo)", 0.5)
    extras = janela_laser(110, 18) + receptor(110, 498, 28) + nervuras(110, 330, 48, 5, 9)
    b = [b_laser_oval(110, 58, 44, 24)] + par(110, 100, 72, 40, 10, 18, "ret", tam=22) + [
        b_circ("iniciar", 86, 206, 15, "telaplay", 13), b_circ("tela-preta", 134, 206, 15, "preta", 13)]
    return svg(220, 540, corpo, b, extras)


def philips_snp6000():
    corpo = corpo_oval(120, 20, 452, 92, "url(#corpoPrata)") + '<rect x="58" y="128" width="124" height="148" rx="40" fill="#121216"/>'
    extras = led(120, 48, "ledVerde", 6)
    b = [b_circ("iniciar", 90, 94, 15, "play", 13), b_circ("tela-preta", 150, 94, 15, "preta", 13),
         b_circ("laser", 120, 160, 20, "laser", 16, "url(#laser)", cor="#ffd9d4")]
    b += par(120, 194, 70, 50, 10, estilo="seta", tam=24)
    b.append(b_lateral("modo", 207, 250, 9, 34))
    return svg(240, 470, corpo, b, extras)


def hama_xpointer():
    corpo = corpo_cunha(110, 22, 580, 62, 48)
    extras = janela_laser(110, 18) + receptor(110, 554, 30)
    b = [b_laser_oval(110, 56, 48, 24), b_circ("funcao", 110, 110, 17, "mouse", 14)]
    b += par(110, 142, 80, 42, 8, 12, "r400", tam=22)
    b += [b_circ("iniciar", 84, 252, 14, "expandir", 12), b_circ("tela-preta", 136, 252, 14, "preta", 12),
          b_circ("volume-menos", 84, 290, 13, "volmenos", 11), b_circ("volume-mais", 136, 290, 13, "volmais", 11),
          b_circ("caneta", 110, 326, 13, "caneta", 11)]
    return svg(220, 600, corpo, b, extras)


def trust_kazun():
    corpo = corpo_bastao(70, 20, 560, 58, "url(#corpoPrata)")
    extras = janela_laser(70, 16, 16) + led(70, 306, "ledAzul", 4) + '<rect x="58" y="546" width="24" height="5" rx="2" fill="#2a2a30"/>'
    b = [b_circ("laser", 70, 70, 13, "laser", 11, cor="#ff6a5a")]
    b += gangorra(70, 102, 44, 112, 5, tam=18)
    b += [b_circ("iniciar", 70, 242, 12, "telaplay", 10), b_circ("tela-preta", 70, 274, 12, "preta", 10),
          b_lateral("liga", 37, 360, 8, 30, "url(#botaoCinza)")]
    return svg(140, 580, corpo, b, extras)


def satechi_r1():
    corpo = corpo_bastao(70, 20, 540, 60, "url(#corpoGrafite)", 22)
    extras = '<rect x="58" y="528" width="24" height="5" rx="2" fill="#050506"/>' + led(70, 254, "ledAzul", 4)
    b = [b_circ("laser", 70, 68, 15, "laser", 13, cor="#ff6a5a"), b_circ("voltar", 70, 118, 20, "esq", 18),
         b_circ("avancar", 70, 168, 20, "dir", 18), b_circ("tela-preta", 54, 216, 11, "preta", 10),
         b_circ("teclado", 86, 216, 11, "teclado", 11)]
    return svg(140, 560, corpo, b, extras)


def ugreen_60327():
    corpo = corpo_cintura(110, 22, 522, 64, 50, 56, "url(#corpoFosco)")
    extras = janela_laser(110, 18) + receptor(110, 498, 30)
    b = [b_laser_oval(110, 58, 48, 26)] + par(110, 98, 80, 42, 8, 12, "r400", fill="url(#botaoPrata)", escuro=True) + [
        b_circ("hyperlink", 110, 210, 15, "link", 13),
        b_circ("volume-menos", 88, 300, 13, "volmenos", 11), b_circ("volume-mais", 132, 300, 13, "volmais", 11)]
    return svg(220, 540, corpo, b, extras)


def inateck_wp2001():
    corpo = corpo_cunha(105, 22, 412, 56, 44, ombro=70)
    extras = janela_laser(105, 18) + receptor(105, 386, 28) + led(105, 206, "ledVermelho", 5)
    b = [b_circ("laser", 105, 72, 18, "laser", 15, "url(#laser)", cor="#ffd9d4")]
    b += par(105, 106, 66, 42, 8, estilo="seta", tam=22)
    return svg(210, 430, corpo, b, extras)


def amerteer_rcrf006():
    corpo = corpo_cunha(110, 22, 542, 60, 48, "url(#corpoGrafite)")
    extras = (janela_laser(110, 18) + receptor(110, 516, 30)
              + '<circle cx="110" cy="212" r="22" fill="none" stroke="#3d7bff" stroke-opacity="0.7" stroke-width="2.5"/>')
    b = [b_laser_oval(110, 58, 48, 26)] + par(110, 98, 76, 42, 8, 12, "ret", tam=22) + [
        b_circ("hyperlink", 110, 212, 17, "link", 14), b_lateral("volume-mais", 162, 240, 9, 30),
        b_lateral("volume-menos", 161, 278, 9, 30), b_lateral("liga", 50, 300, 9, 30)]
    return svg(220, 560, corpo, b, extras)


def ab_shutter3():
    corpo = ('<circle cx="110" cy="48" r="24" fill="none" stroke="#1d1d22" stroke-width="9"/>'
             '<circle cx="110" cy="48" r="24" fill="none" stroke="#ffffff" stroke-opacity="0.07" stroke-width="1.5"/>'
             '<rect x="98" y="60" width="24" height="20" rx="6" fill="#121116" stroke="#000000"/>'
             + corpo_path("M70 74 L150 74 Q190 74 190 114 L190 254 Q190 294 150 294 L70 294 Q30 294 30 254 L30 114 Q30 74 70 74 Z"))
    extras = led(156, 98, "ledAzul", 6)
    b = [b_circ("ios", 110, 168, 46, "camera", 32), b_circ("android", 110, 248, 17, "ponto", 14),
         b_lateral("liga", 186, 150, 9, 40)]
    return svg(220, 312, corpo, b, extras)


def anel_tiktok():
    alca = "M88 226 C66 276 82 330 130 332 C178 330 194 276 172 226"
    corpo = (f'<path d="{alca}" fill="none" stroke="#0e0e12" stroke-width="14" stroke-linecap="round"/>'
             f'<path d="{alca}" fill="none" stroke="#ffffff" stroke-opacity="0.07" stroke-width="1.5"/>'
             + corpo_path("M130 30 C200 30 226 62 226 136 C226 208 196 244 130 244 C64 244 34 208 34 136 C34 62 60 30 130 30 Z")
             + brilho_lateral("M52 76 C40 104 38 150 48 186 C54 204 62 218 72 228 C60 196 54 156 58 116 C60 96 64 84 70 76 Z"))
    extras = led(198, 62, "ledAzul", 6)
    b = [b_ret("cima", 104, 56, 52, 30, 15, "cima", 18), b_ret("baixo", 104, 186, 52, 30, 15, "baixo", 18),
         b_ret("esquerda", 58, 110, 30, 52, 15, "esq", 18), b_ret("direita", 172, 110, 30, 52, 15, "dir", 18),
         b_circ("ok", 130, 136, 30, "coracao", 22, cor="#ff7a9a")]
    return svg(260, 352, corpo, b, extras)


def airturn_duo200():
    corpo = ('<rect x="16" y="40" width="488" height="236" rx="30" fill="url(#corpoFosco)" stroke="#000000" stroke-width="1.5"/>'
             + brilho_lateral("M40 52 L480 52 L470 62 L50 62 Z")
             + '<rect x="230" y="104" width="60" height="120" rx="14" fill="url(#corpoGrafite)" stroke="#000000" stroke-width="1.2"/>')
    extras = led(260, 200, "ledAzul", 5)
    rid = lambda x: "".join(f'<path d="M{x + 24} {n(110 + i * 22)} L{x + 166} {n(110 + i * 22)}" stroke="#000000" stroke-opacity="0.45" stroke-width="2"/>' for i in range(6))
    b = [botao("pedal-esquerdo", retang(36, 70, 190, 186, 22, "url(#borracha)"), rid(36) + icone("esq", 131, 163, 44)),
         botao("pedal-direito", retang(294, 70, 190, 186, 22, "url(#borracha)"), rid(294) + icone("dir", 389, 163, 44)),
         b_circ("modo", 260, 150, 13, "modo", 14)]
    return svg(520, 300, corpo, b, extras)


MODELOS = {
    "exbom": exbom, "r400": r400, "r800": r800, "r500s": r500s, "spotlight": spotlight, "kensington": kensington,
    "baseus": baseus, "anel-3": anel3, "anel-5": anel5, "r400-generico": r400_generico, "visor-tab": visor_tab,
    "caneta-visor": caneta_visor, "caneta-fina": caneta_fina, "caneta-laranja": caneta_laranja,
    # quem dividia desenho
    "spotlight-2": spotlight_2, "kensington-expert": kensington_expert, "knup-passador": knup_passador,
    "genius-ring": genius_ring,
    # pesquisa de 12/11/2026 (o nome do arquivo é o id do modelo)
    "logitech-r700": logitech_r700, "logitech-cordless-presenter": logitech_cordless,
    "kensington-ultimate-presenter-k75233": kensington_k75233, "kensington-si600": kensington_si600,
    "kensington-slimblade-presenter-mouse": kensington_slimblade,
    "kensington-slimblade-presenter-media-mouse": kensington_slimblade_media,
    "targus-amp13": targus_amp13, "targus-amp16": targus_amp16, "targus-amp17": targus_amp17,
    "targus-control-plus-amp067": targus_amp067, "canon-pr10-g": canon_pr10g, "canon-pr100-r": canon_pr100r,
    "canon-pr500-r": canon_pr500r, "canon-pr1100-r": canon_pr1100r, "norwii-n97s": norwii_n97s,
    "norwii-n76": norwii_n76, "norwii-n27": norwii_n27, "microsoft-presenter-plus": microsoft_presenter_plus,
    "lenovo-thinkpad-x1-presenter-mouse": lenovo_x1, "avatto-h100": avatto_h100, "avatto-i10-pro": avatto_i10,
    "august-lp315": august_lp315, "doosl-dsit032": doosl_dsit032, "doosl-dsit014b": doosl_dsit014b,
    "baseus-orange-dot-ai": baseus_orange_dot_ai, "multilaser-ac285": multilaser_ac285,
    "multilaser-ac385": multilaser_ac385, "multilaser-ac164": multilaser_ac164, "knup-kp-8001": knup_kp8001,
    "exbom-lpt-8": exbom_lpt8, "c3tech-ap-200": c3tech_ap200, "genius-media-pointer-100": genius_mp100,
    "philips-snp6000": philips_snp6000, "hama-x-pointer-6in1": hama_xpointer, "trust-kazun": trust_kazun,
    "satechi-r1": satechi_r1, "ugreen-60327": ugreen_60327, "inateck-wp2001": inateck_wp2001,
    "amerteer-rcrf-006": amerteer_rcrf006, "ab-shutter3": ab_shutter3, "anel-tiktok-5-botoes": anel_tiktok,
    "airturn-duo-200": airturn_duo200,
}

if __name__ == "__main__":
    os.makedirs(PASTA, exist_ok=True)
    for nome, f in MODELOS.items():
        with open(os.path.join(PASTA, nome + ".svg"), "w", encoding="utf-8", newline="\n") as arq:
            arq.write(f())
        print("desenho", nome)

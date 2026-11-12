# -*- coding: utf-8 -*-
"""
Gera assets/passador.ico, assets/passador-256.png, assets/passador-512.png e assets/instalador.png: o passador
inclinado sob o refletor, com o botão de avançar aceso em âmbar. Uso: python tools/gera_icone.py
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter

RAIZ = os.path.join(os.path.dirname(__file__), "..", "assets")
AMBAR = (244, 169, 64)
FUNDO = (27, 22, 33)
CLARO = (246, 240, 250)


def marca(t, fundo=True):
    s = 4  # desenha grande e reduz (antisserrilhado)
    T = t * s
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    if fundo:
        d = ImageDraw.Draw(im)
        d.rounded_rectangle([0, 0, T - 1, T - 1], radius=int(T * 0.24), fill=FUNDO + (255,))
        luz = Image.new("RGBA", (T, T), (0, 0, 0, 0))
        ImageDraw.Draw(luz).ellipse([T * 0.10, T * 0.06, T * 0.90, T * 0.86], fill=AMBAR + (72,))
        luz = luz.filter(ImageFilter.GaussianBlur(T * 0.12))
        mask = Image.new("L", (T, T), 0)
        ImageDraw.Draw(mask).rounded_rectangle([0, 0, T - 1, T - 1], radius=int(T * 0.24), fill=255)
        im = Image.composite(Image.alpha_composite(im, luz), im, mask)

    # O corpo: uma pílula vertical desenhada reta numa camada e girada 32 graus.
    corpo = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    d = ImageDraw.Draw(corpo)
    cx, cy = T * 0.5, T * 0.53
    w, h = T * 0.26, T * 0.70
    x0, y0, x1, y1 = cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2
    d.rounded_rectangle([x0, y0, x1, y1], radius=int(w / 2), fill=CLARO + (255,))
    # botão de avançar: disco âmbar com a seta escura
    bx, by, br = cx, y0 + h * 0.30, w * 0.36
    d.ellipse([bx - br, by - br, bx + br, by + br], fill=AMBAR + (255,))
    # dois botões pequenos abaixo (voltar e tela preta)
    for f in (0.53, 0.67):
        r = w * 0.13
        yy = y0 + h * f
        d.ellipse([cx - r, yy - r, cx + r, yy + r], fill=(206, 199, 214, 255))
    # o laser: ponto aceso no topo
    lr = w * 0.13
    d.ellipse([cx - lr, y0 + h * 0.07 - lr, cx + lr, y0 + h * 0.07 + lr], fill=(255, 214, 150, 255))
    corpo = corpo.rotate(-32, resample=Image.BICUBIC, center=(cx, cy))
    # a seta de avançar vai em pé (apontando para a direita), desenhada depois de girar o corpo
    a = math.radians(32)
    rx, ry = cx + (bx - cx) * math.cos(a) - (by - cy) * math.sin(a), cy + (bx - cx) * math.sin(a) + (by - cy) * math.cos(a)
    d = ImageDraw.Draw(corpo)
    k = br * 0.40
    pts = [(rx - k * 0.40, ry - k), (rx + k * 0.60, ry), (rx - k * 0.40, ry + k)]
    d.line(pts, fill=FUNDO + (255,), width=int(T * 0.03), joint="curve")
    for px, py in pts:
        r = T * 0.015
        d.ellipse([px - r, py - r, px + r, py + r], fill=FUNDO + (255,))
    # sombra suave
    sombra = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    sombra.putalpha(corpo.getchannel("A").point(lambda a: int(a * 0.45)))
    sombra = sombra.filter(ImageFilter.GaussianBlur(T * 0.03))
    im.alpha_composite(sombra, (int(T * 0.015), int(T * 0.03)))
    im.alpha_composite(corpo)
    return im.resize((t, t), Image.LANCZOS)


if __name__ == "__main__":
    os.makedirs(RAIZ, exist_ok=True)
    grande = marca(256)
    grande.save(os.path.join(RAIZ, "passador-256.png"))
    grande.save(os.path.join(RAIZ, "passador.ico"), sizes=[(16, 16), (20, 20), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    marca(512).save(os.path.join(RAIZ, "passador-512.png"))
    # tela do instalador
    w, h = 480, 300
    tela = Image.new("RGBA", (w, h), (20, 17, 24, 255))
    luz = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(luz).ellipse([w * 0.2, -h * 0.2, w * 0.8, h * 0.9], fill=AMBAR + (60,))
    tela = Image.alpha_composite(tela, luz.filter(ImageFilter.GaussianBlur(50)))
    m = marca(120)
    tela.alpha_composite(m, ((w - 120) // 2, (h - 120) // 2 - 10))
    tela.convert("RGB").save(os.path.join(RAIZ, "instalador.png"))
    print("ícones prontos")

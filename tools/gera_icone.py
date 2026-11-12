# -*- coding: utf-8 -*-
"""Gera assets/batuta.ico, assets/batuta-256.png e assets/instalador.png (a batuta sob o refletor). Uso: python tools/gera_icone.py"""
import os
from PIL import Image, ImageDraw, ImageFilter

RAIZ = os.path.join(os.path.dirname(__file__), "..", "assets")
AMBAR = (244, 169, 64)


def marca(t):
    s = 4  # desenha grande e reduz (antisserrilhado)
    T = t * s
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, T - 1, T - 1], radius=int(T * 0.24), fill=(27, 22, 33, 255))
    # luz do refletor
    luz = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    ImageDraw.Draw(luz).ellipse([T * 0.12, T * 0.10, T * 0.88, T * 0.86], fill=AMBAR + (70,))
    luz = luz.filter(ImageFilter.GaussianBlur(T * 0.12))
    mask = Image.new("L", (T, T), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, T - 1, T - 1], radius=int(T * 0.24), fill=255)
    im = Image.composite(Image.alpha_composite(im, luz), im, mask)
    d = ImageDraw.Draw(im)
    # a batuta: traço diagonal que afina, com o cabo
    x0, y0, x1, y1 = T * 0.26, T * 0.76, T * 0.78, T * 0.24
    for i in range(40):
        f = i / 39
        x = x0 + (x1 - x0) * f
        y = y0 + (y1 - y0) * f
        r = T * (0.075 - 0.05 * f)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(246, 240, 250, 255))
    d.ellipse([x0 - T * 0.105, y0 - T * 0.105, x0 + T * 0.105, y0 + T * 0.105], fill=AMBAR + (255,))
    # a ponta acesa (o laser)
    d.ellipse([x1 - T * 0.05, y1 - T * 0.05, x1 + T * 0.05, y1 + T * 0.05], fill=(255, 214, 150, 255))
    return im.resize((t, t), Image.LANCZOS)


if __name__ == "__main__":
    os.makedirs(RAIZ, exist_ok=True)
    grande = marca(256)
    grande.save(os.path.join(RAIZ, "batuta-256.png"))
    grande.save(os.path.join(RAIZ, "batuta.ico"), sizes=[(16, 16), (20, 20), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
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

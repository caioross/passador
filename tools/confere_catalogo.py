# -*- coding: utf-8 -*-
"""
Confere o catálogo (catalogo/modelos.json) contra os desenhos (catalogo/desenhos/*.svg). Uso:  python tools/confere_catalogo.py
Saída 0 = tudo certo; 1 = há problema (cada um numa linha).

- JSON válido; ids de modelo únicos; cada modelo com desenho próprio (sem dois modelos no mesmo desenho);
- todo "desenho" tem o arquivo; os botões do SVG (data-botao) são exatamente os botões do modelo;
- papel conhecido; teclas que o Passador conhece (src/*/Teclas.cs); botão sem tecla diz o porquê (soHardware);
- nenhum SVG traz nome de marca (os desenhos são nossos e genéricos; a marca vai só como texto no catálogo).
"""
import glob
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

RAIZ = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
CATALOGO = os.path.join(RAIZ, "catalogo", "modelos.json")
DESENHOS = os.path.join(RAIZ, "catalogo", "desenhos")

PAPEIS = {"avancar", "voltar", "iniciar", "tela-preta", "laser", "foco", "mouse", "lupa", "caneta", "tab", "mais",
          "menos", "cronometro", "liga", "volume", "midia", "midia-seguinte", "midia-anterior", "cima", "baixo",
          "curtir", "outro"}
CONFIANCAS = {"conferido", "comunidade", "estimado", "aprendido"}
GENERICAS = {"genérico", "generico"}


def teclas_conhecidas():
    arqs = glob.glob(os.path.join(RAIZ, "src", "*", "Teclas.cs"))
    if not arqs:
        sys.exit("não achei src/*/Teclas.cs")
    texto = open(arqs[0], encoding="utf-8").read()
    nomes = set(re.findall(r'\bD\("(\w+)"', texto))
    nomes |= {chr(c) for c in range(ord("A"), ord("Z") + 1)} | {str(d) for d in range(10)} | {f"F{i}" for i in range(1, 25)}
    return {x.lower() for x in nomes}


def tecla_valida(texto, nomes):
    partes = [p for p in re.split(r"\+(?=.)", texto)]
    if not partes or any(not p for p in partes):
        return False
    *mods, tecla = partes
    if any(m.lower() not in ("ctrl", "shift", "alt", "win") for m in mods):
        return False
    return tecla.lower() in nomes or tecla in ("+", "-", ".", ",")


def main():
    erros = []
    try:
        cat = json.load(open(CATALOGO, encoding="utf-8"))
    except Exception as ex:  # noqa: BLE001
        print(f"modelos.json ilegível: {ex}")
        return 1
    nomes = teclas_conhecidas()
    modelos = cat.get("modelos", [])
    vistos, desenhos = set(), {}
    marcas = sorted({m["marca"] for m in modelos if m.get("marca", "").lower() not in GENERICAS}, key=len, reverse=True)

    for m in modelos:
        mid = m.get("id", "?")
        if mid in vistos:
            erros.append(f"{mid}: id repetido")
        vistos.add(mid)
        for campo in ("marca", "nome", "desenho", "conexao", "botoes", "confianca"):
            if not m.get(campo):
                erros.append(f"{mid}: falta {campo}")
        if m.get("confianca") not in CONFIANCAS:
            erros.append(f"{mid}: confianca '{m.get('confianca')}' desconhecida")
        des = m.get("desenho", "")
        if des in desenhos:
            erros.append(f"{mid}: o desenho '{des}' já é do modelo {desenhos[des]} (cada modelo tem o seu)")
        desenhos[des] = mid
        for i in m.get("ids", []):
            if not re.fullmatch(r"[0-9A-F]{4}", i.get("vid", "")) or (i.get("pid") is not None and not re.fullmatch(r"[0-9A-F]{4}", i["pid"])):
                erros.append(f"{mid}: id USB fora do formato ({i})")
        if not m.get("ids") and "VID e PID a confirmar" not in (m.get("notas") or ""):
            erros.append(f"{mid}: sem VID/PID e a nota não diz 'VID e PID a confirmar'")

        botoes = m.get("botoes", [])
        ids_b = [b.get("id") for b in botoes]
        if len(ids_b) != len(set(ids_b)):
            erros.append(f"{mid}: id de botão repetido")
        papeis_cfg = [b["papel"] for b in botoes if b.get("envia") and b.get("papel")]
        for p in {p for p in papeis_cfg if papeis_cfg.count(p) > 1}:
            erros.append(f"{mid}: dois botões configuráveis com o papel '{p}' (o perfil por papel pegaria os dois)")
        for b in botoes:
            if b.get("papel") not in PAPEIS:
                erros.append(f"{mid}/{b.get('id')}: papel '{b.get('papel')}' desconhecido")
            for t in b.get("envia", []):
                if not tecla_valida(t, nomes):
                    erros.append(f"{mid}/{b.get('id')}: tecla '{t}' desconhecida")
            if not b.get("envia") and not b.get("soHardware"):
                erros.append(f"{mid}/{b.get('id')}: não manda tecla e não diz por quê (soHardware)")

        if des.startswith("generico"):
            continue
        arq = os.path.join(DESENHOS, des + ".svg")
        if not os.path.exists(arq):
            erros.append(f"{mid}: falta o desenho {des}.svg")
            continue
        texto = open(arq, encoding="utf-8").read()
        try:
            raiz = ET.fromstring(texto)
        except ET.ParseError as ex:
            erros.append(f"{des}.svg: XML inválido ({ex})")
            continue
        no_svg = {g.get("data-botao") for g in raiz.iter() if g.get("data-botao")}
        if no_svg != set(ids_b):
            erros.append(f"{des}.svg: botões {sorted(no_svg)} != modelo {sorted(ids_b)}")
        if "style=" in texto:
            erros.append(f"{des}.svg: usa style=\"\" (o leitor do app não entende)")
        for marca in marcas:
            if re.search(re.escape(marca), texto, re.IGNORECASE):
                erros.append(f"{des}.svg: contém o nome da marca '{marca}'")

    usados = set(desenhos)
    for arq in glob.glob(os.path.join(DESENHOS, "*.svg")):
        nome = os.path.splitext(os.path.basename(arq))[0]
        if nome not in usados:
            erros.append(f"{nome}.svg: desenho que nenhum modelo usa")

    for e in erros:
        print(e)
    com_id = sum(1 for m in modelos if any(i.get("pid") for i in m.get("ids", [])))
    print(f"{len(modelos)} modelos, {len(desenhos)} desenhos, {com_id} com VID:PID; {len(erros)} problema(s)")
    return 1 if erros else 0


if __name__ == "__main__":
    sys.exit(main())

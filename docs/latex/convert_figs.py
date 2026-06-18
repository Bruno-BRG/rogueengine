"""Converte SVGs de docs/rendered/ para PDF em docs/latex/figs/."""
from __future__ import annotations

import re
import xml.etree.ElementTree as ET
from pathlib import Path

from reportlab.graphics import renderPDF
from svglib.svglib import svg2rlg

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "rendered"
DST = Path(__file__).resolve().parent / "figs"
PADDING = 20


def _pt_to_float(value: str) -> float:
    value = value.strip()
    for suffix in ("pt", "px", "in", "cm", "mm"):
        if value.endswith(suffix):
            number = float(value[: -len(suffix)])
            if suffix == "in":
                return number * 72.0
            if suffix == "cm":
                return number * (72.0 / 2.54)
            if suffix == "mm":
                return number * (72.0 / 25.4)
            return number
    return float(value)


def _svg_target_size(svg_path: Path) -> tuple[float, float]:
    """Le largura/altura declaradas no SVG (graphviz/mermaid)."""
    root = ET.parse(svg_path).getroot()
    width = root.get("width")
    height = root.get("height")
    viewbox = root.get("viewBox")

    if width and height:
        return _pt_to_float(width), _pt_to_float(height)

    if viewbox:
        parts = [float(v) for v in viewbox.split()]
        return parts[2], parts[3]

    raise ValueError(f"SVG sem dimensoes: {svg_path.name}")


def fit_drawing_to_svg_size(drawing, target_w: float, target_h: float, padding: float = PADDING):
    """Escala e centraliza o desenho para caber nas dimensoes do SVG."""
    x1, y1, x2, y2 = drawing.getBounds()
    content_w = x2 - x1
    content_h = y2 - y1
    if content_w <= 0 or content_h <= 0:
        raise ValueError("Desenho sem area valida")

    inner_w = max(target_w - 2 * padding, 1.0)
    inner_h = max(target_h - 2 * padding, 1.0)
    scale = min(inner_w / content_w, inner_h / content_h)
    drawing.scale(scale, scale)

    x1, y1, x2, y2 = drawing.getBounds()
    content_w = x2 - x1
    content_h = y2 - y1
    drawing.width = content_w + 2 * padding
    drawing.height = content_h + 2 * padding
    drawing.shift(padding - x1, padding - y1)
    return drawing


def main() -> None:
    DST.mkdir(parents=True, exist_ok=True)
    for svg in sorted(SRC.glob("*.svg")):
        drawing = svg2rlg(str(svg))
        if drawing is None:
            raise RuntimeError(f"Falha ao carregar {svg.name}")

        target_w, target_h = _svg_target_size(svg)
        fit_drawing_to_svg_size(drawing, target_w, target_h)

        out = DST / f"{svg.stem}.pdf"
        renderPDF.drawToFile(drawing, str(out))
        print(f"OK {out.name} ({drawing.width:.0f}x{drawing.height:.0f})")


if __name__ == "__main__":
    main()

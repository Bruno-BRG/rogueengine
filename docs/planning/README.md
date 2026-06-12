# Architecture specification (planning)

Full development document for RogueEngine (draft name **MomoRogue** in the original PDF).

| File | Description |
|------|-------------|
| [`documento_desenvolvimento_momorogue.pdf`](documento_desenvolvimento_momorogue.pdf) | Compiled PDF |
| [`documento_desenvolvimento_momorogue.tex`](documento_desenvolvimento_momorogue.tex) | LaTeX source |

Diagram sources live in [`../diagrams/`](../diagrams/); rendered figures in [`../rendered/`](../rendered/).

## Rebuild the PDF

From this folder (`docs/planning/`):

```bash
pdflatex documento_desenvolvimento_momorogue.tex
pdflatex documento_desenvolvimento_momorogue.tex
```

Diagrams are already rendered; re-run Mermaid CLI only if you edit `../diagrams/*.mmd`.

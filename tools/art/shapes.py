"""Writes the shape SVGs of every art style (D-020, D-022) into art/src/<style>/.

    tools/.venv/bin/python tools/art/shapes.py     (render.py runs it first)

Styles share sprite NAMES, so the plugin's code never knows which style is loaded; each
style declares its own sizes, 9-slice borders and content insets in its SPRITES table.
"""
import os

import carved_shapes
import gold_shapes

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
STYLES = {"carved": carved_shapes, "gold": gold_shapes}


def main():
    for style, module in STYLES.items():
        src = os.path.join(ROOT, "art", "src", style)
        os.makedirs(src, exist_ok=True)
        for name, make in module.SHAPES.items():
            with open(os.path.join(src, name + ".svg"), "w") as f:
                f.write(make())
        print(f"{style}: {len(module.SHAPES)} shapes written to art/src/{style}")


if __name__ == "__main__":
    main()

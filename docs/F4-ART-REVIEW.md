# F4 art review — concept cutouts

The first crops in `art/src/concept/` preserve the exact ornaments Diego asked for, but
their color threshold also retained scenery and local lighting. Upscaling makes those pixels
larger; it does not repair geometry or recover a clean layer that the concept never had.

`tools/art/refine_concept.py` builds **review candidates** in `dist/art-review/`.
The second pass incorporates Diego's feedback on the first (square corners, flat gold,
wrong central stamina composition and stretched vertical bars):

- Bodies are dark **and translucent** inside their rounded or pointed silhouette. Pixels
  outside the silhouette are transparent. The generator checks all four extreme corners of
  the card, hotbar plate, boss plate and both slots.
- The metallic borders retain brightness variations from the concept. Straight rails gain
  bronze shadow, a gold body and a thin cream reflection. The selected slot keeps its
  original bright metal edge without mirrored ornaments or a flat recolor.
- The stamina composition matches ConceptArt (5): `VIGOR` above the frame, diamond marks,
  knot caps, two rails and a lit fill inside the channel. Label and fill are demo layers;
  the production sprite remains a frame with a separate opening mask.
- Health has a 46×226 design size and stamina/eitr 38×196. Each size gets its own frame and
  opening mask: the end caps scale uniformly and only a straight shaft row repeats. This
  preserves the knot proportions at both widths. The liquid reaches into the pointed top
  and lower channel. Integration requires the vitals view to select the matching variant
  and render it without horizontal 9-slice stretching.
- The minimap ring is geometrically circular; its left knot remains deliberately asymmetric.
- It writes `preview.html` with each current crop beside the candidate, with a switchable
  background. This page is self-contained and can be opened directly in a browser.
- It does **not** overwrite `art/src/concept/` or `art/out/`. Integrate a candidate only
  after visual review, then update its 9-slice borders to cover the full fixed cap.

Run:

```sh
tools/.venv/bin/python tools/art/refine_concept.py
```

The pass covers the card, hotbar plate, boss plate, stamina frame, two vital-bar frame sizes,
minimap pill and ring, normal slot and selected slot. `sprint_demo.png` and
`vitals_demo.png` show their assembled appearance beside concept crops in the review page.
The 4K concept batch may continue in the background; this pass only processes the small
committed cutouts. Food, window frames and remaining concept pieces still need individual
extraction and review.

# F4 art review — concept cutouts

The first crops in `art/src/concept/` preserve the exact ornaments Diego asked for, but
their color threshold also retained scenery and local lighting. Upscaling makes those pixels
larger; it does not repair geometry or recover a clean layer that the concept never had.

`tools/art/refine_concept.py` now builds a **review candidate** in `dist/art-review/`:

- It takes one good corner or cap for each symmetric frame and mirrors it. The long straight
  runs are reconstructed, so they no longer contain scenery or varying line thickness.
- It normalizes the gold and dark fill while retaining the small knot strokes. The minimap
  ring is geometrically circular; its left knot remains deliberately asymmetric.
- It writes `preview.html` with each current crop beside the candidate, with a switchable
  background. This page is self-contained and can be opened directly in a browser.
- It does **not** overwrite `art/src/concept/` or `art/out/`. Integrate a candidate only
  after visual review, then update its 9-slice borders to cover the full fixed cap.

Run:

```sh
tools/.venv/bin/python tools/art/refine_concept.py
```

The first pass covers the card, hotbar plate, boss plate, stamina frame, vital-bar frame,
minimap pill and ring, normal slot and selected slot. The 4K concept batch may continue in
the background; this pass only processes the small committed cutouts. Food, window frames
and remaining concept pieces still need individual extraction and review.

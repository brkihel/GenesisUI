"""Upscales Diego's concept images to 4K (3840x2160) with Real-ESRGAN x4plus, on the CPU, in tiles.

    tools/.venv-upscale/bin/python tools/art/upscale.py <input.png> [...] --out <dir>

The concept images are Diego's own (ChatGPT + his Photoshop edits, D-026). They and their
upscaled copies stay outside the repository (default output: ~/GenesisUI-Concept/4k); only the
pieces cut from them by extract.py are committed. This is a heavy task: run one at a time
(AGENTS.md §4); memory stays bounded by the tile size.

Setup once (CPU-only PyTorch, kept apart from the art venv):
  python3 -m venv tools/.venv-upscale
  tools/.venv-upscale/bin/pip install --index-url https://download.pytorch.org/whl/cpu torch==2.4.1
  tools/.venv-upscale/bin/pip install pillow numpy
Weights (official release, BSD-3-Clause, not redistributed):
  https://github.com/xinntao/Real-ESRGAN/releases/download/v0.1.0/RealESRGAN_x4plus.pth
  -> ~/.cache/genesisui/RealESRGAN_x4plus.pth, sha256 4fa0d38905f75ac06eb49a7951b426670021be3018265fd191d2125df9d682f1

The network below follows the published RRDB architecture (ESRGAN paper) so the official
weights load into it; it is written here, not copied from the Real-ESRGAN code base.
"""
import argparse
import hashlib
import os
import sys
import time

import numpy as np
import torch
from PIL import Image
from torch import nn
from torch.nn import functional as F

WEIGHTS = os.path.expanduser("~/.cache/genesisui/RealESRGAN_x4plus.pth")
WEIGHTS_SHA256 = "4fa0d38905f75ac06eb49a7951b426670021be3018265fd191d2125df9d682f1"
TILE = 192        # input pixels per tile side; bounds memory
PAD = 16          # overlap so tile seams disappear
TARGET = (3840, 2160)


class DenseBlock(nn.Module):
    def __init__(self, f=64, g=32):
        super().__init__()
        self.conv1 = nn.Conv2d(f, g, 3, 1, 1)
        self.conv2 = nn.Conv2d(f + g, g, 3, 1, 1)
        self.conv3 = nn.Conv2d(f + 2 * g, g, 3, 1, 1)
        self.conv4 = nn.Conv2d(f + 3 * g, g, 3, 1, 1)
        self.conv5 = nn.Conv2d(f + 4 * g, f, 3, 1, 1)
        self.act = nn.LeakyReLU(0.2, inplace=True)

    def forward(self, x):
        x1 = self.act(self.conv1(x))
        x2 = self.act(self.conv2(torch.cat((x, x1), 1)))
        x3 = self.act(self.conv3(torch.cat((x, x1, x2), 1)))
        x4 = self.act(self.conv4(torch.cat((x, x1, x2, x3), 1)))
        x5 = self.conv5(torch.cat((x, x1, x2, x3, x4), 1))
        return x5 * 0.2 + x


class RRDB(nn.Module):
    def __init__(self, f=64):
        super().__init__()
        self.rdb1, self.rdb2, self.rdb3 = DenseBlock(f), DenseBlock(f), DenseBlock(f)

    def forward(self, x):
        return self.rdb3(self.rdb2(self.rdb1(x))) * 0.2 + x


class RRDBNet(nn.Module):
    def __init__(self, blocks=23, f=64):
        super().__init__()
        self.conv_first = nn.Conv2d(3, f, 3, 1, 1)
        self.body = nn.Sequential(*[RRDB(f) for _ in range(blocks)])
        self.conv_body = nn.Conv2d(f, f, 3, 1, 1)
        self.conv_up1 = nn.Conv2d(f, f, 3, 1, 1)
        self.conv_up2 = nn.Conv2d(f, f, 3, 1, 1)
        self.conv_hr = nn.Conv2d(f, f, 3, 1, 1)
        self.conv_last = nn.Conv2d(f, 3, 3, 1, 1)
        self.act = nn.LeakyReLU(0.2, inplace=True)

    def forward(self, x):
        feat = self.conv_first(x)
        feat = feat + self.conv_body(self.body(feat))
        feat = self.act(self.conv_up1(F.interpolate(feat, scale_factor=2, mode="nearest")))
        feat = self.act(self.conv_up2(F.interpolate(feat, scale_factor=2, mode="nearest")))
        return self.conv_last(self.act(self.conv_hr(feat)))


def load_model():
    with open(WEIGHTS, "rb") as f:
        if hashlib.sha256(f.read()).hexdigest() != WEIGHTS_SHA256:
            sys.exit("weights file does not match the official sha256; refusing to load it")
    state = torch.load(WEIGHTS, map_location="cpu", weights_only=True)
    state = state.get("params_ema", state.get("params", state))
    model = RRDBNet()
    model.load_state_dict(state, strict=True)
    return model.eval()


@torch.no_grad()
def upscale(model, img):
    a = np.asarray(img.convert("RGB"), np.float32) / 255.0
    h, w = a.shape[:2]
    out = np.zeros((h * 4, w * 4, 3), np.float32)
    x = torch.from_numpy(a).permute(2, 0, 1).unsqueeze(0)
    tiles = [(ty, tx) for ty in range(0, h, TILE) for tx in range(0, w, TILE)]
    for n, (ty, tx) in enumerate(tiles, 1):
        y0, x0 = max(ty - PAD, 0), max(tx - PAD, 0)
        y1, x1 = min(ty + TILE + PAD, h), min(tx + TILE + PAD, w)
        o = model(x[:, :, y0:y1, x0:x1]).clamp(0, 1)[0].permute(1, 2, 0).numpy()
        cy, cx = (ty - y0) * 4, (tx - x0) * 4
        th, tw = min(TILE, h - ty) * 4, min(TILE, w - tx) * 4
        out[ty * 4:ty * 4 + th, tx * 4:tx * 4 + tw] = o[cy:cy + th, cx:cx + tw]
        print(f"  tile {n}/{len(tiles)}", flush=True)
    return Image.fromarray((out * 255 + 0.5).astype(np.uint8))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("inputs", nargs="+")
    ap.add_argument("--out", default=os.path.expanduser("~/GenesisUI-Concept/4k"))
    ap.add_argument("--threads", type=int, default=4)
    args = ap.parse_args()
    torch.set_num_threads(args.threads)
    os.makedirs(args.out, exist_ok=True)
    model = load_model()
    for path in args.inputs:
        dst = os.path.join(args.out, os.path.splitext(os.path.basename(path))[0] + ".png")
        if os.path.exists(dst):
            print("skip (done): " + dst)
            continue
        start = time.time()
        print("upscaling " + path, flush=True)
        big = upscale(model, Image.open(path))
        big.resize(TARGET, Image.LANCZOS).save(dst)
        print(f"  -> {dst} in {time.time() - start:.0f}s", flush=True)


if __name__ == "__main__":
    main()

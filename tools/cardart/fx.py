"""ตัวช่วยวาดเอฟเฟกต์ภาพประกอบ (เรืองแสง วงหมุน รอยฟัน เศษหิน ฯลฯ) ใช้ร่วมกันทุกการ์ด"""
import math
import random

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

W, H = 1280, 960


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def mul(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c)


def mix(c, other, t):
    return lerp(c, other, t)


def gradient(top, bottom):
    t = np.linspace(0, 1, H)[:, None, None]
    top = np.array(top, dtype=np.float32)[None, None, :]
    bottom = np.array(bottom, dtype=np.float32)[None, None, :]
    arr = np.repeat(top + (bottom - top) * t, W, axis=1)
    return Image.fromarray(arr.astype(np.uint8), "RGB")


def glow(img, center, radius, color, strength=1.0):
    ys, xs = np.mgrid[0:H, 0:W]
    d = np.sqrt((xs - center[0]) ** 2 + (ys - center[1]) ** 2) / radius
    a = np.clip(1.0 - d, 0, 1) ** 2 * strength
    base = np.asarray(img).astype(np.float32)
    add = a[:, :, None] * np.array(color, dtype=np.float32)[None, None, :]
    return Image.fromarray(np.clip(base + add, 0, 255).astype(np.uint8), "RGB")


def new_layer():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def paste_rgba(img, layer):
    base = img.convert("RGBA")
    base.alpha_composite(layer)
    return base.convert("RGB")


def add_layer(img, layer):
    base = np.asarray(img).astype(np.float32)
    lay = np.asarray(layer).astype(np.float32)
    add = lay[:, :, :3] * (lay[:, :, 3:4] / 255.0)
    return Image.fromarray(np.clip(base + add, 0, 255).astype(np.uint8), "RGB")


def blur_layer(draw_fn, radius):
    layer = new_layer()
    draw_fn(ImageDraw.Draw(layer))
    return layer.filter(ImageFilter.GaussianBlur(radius))


def clip_layer(layer, mask):
    """ตัด layer ให้เหลือเฉพาะส่วนที่อยู่ใน mask (โหมด L)"""
    r, g, b, a = layer.split()
    a = Image.fromarray(
        (np.asarray(a).astype(np.float32) * np.asarray(mask).astype(np.float32) / 255.0).astype(np.uint8), "L")
    return Image.merge("RGBA", (r, g, b, a))


def sparkles(img, rng, color, count=70, region=(0, 0, W, H), size=(2, 7)):
    layer = new_layer()
    d = ImageDraw.Draw(layer)
    for _ in range(count):
        x = rng.uniform(region[0], region[2])
        y = rng.uniform(region[1], region[3])
        r = rng.uniform(*size)
        a = rng.randint(120, 255)
        d.ellipse([x - r, y - r, x + r, y + r], fill=color + (a,))
        if r > size[1] * 0.7:  # ประกายสี่แฉก
            d.line([(x - r * 3, y), (x + r * 3, y)], fill=color + (a // 2,), width=2)
            d.line([(x, y - r * 3), (x, y + r * 3)], fill=color + (a // 2,), width=2)
    return add_layer(img, layer.filter(ImageFilter.GaussianBlur(1.2)))


def bokeh(img, rng, colors, count=26, region=(0, 0, W, H)):
    layer = new_layer()
    d = ImageDraw.Draw(layer)
    for _ in range(count):
        x = rng.uniform(region[0], region[2])
        y = rng.uniform(region[1], region[3])
        r = rng.uniform(14, 70)
        c = rng.choice(colors)
        d.ellipse([x - r, y - r, x + r, y + r], fill=c + (rng.randint(25, 70),))
    return add_layer(img, layer.filter(ImageFilter.GaussianBlur(6)))


def vignette(img, k=0.55):
    ys, xs = np.mgrid[0:H, 0:W]
    d = np.sqrt(((xs - W / 2) / (W / 2)) ** 2 + ((ys - H / 2) / (H / 2)) ** 2)
    v = 1.0 - np.clip(d - 0.4, 0, 1) * k
    arr = np.asarray(img).astype(np.float32) * v[:, :, None]
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB")


def rays(img, center, color, count=14, strength=0.35, rng=None):
    rng = rng or random.Random(1)
    layer = new_layer()
    d = ImageDraw.Draw(layer)
    for i in range(count):
        a = (i / count) * math.tau + rng.uniform(-0.1, 0.1)
        w = rng.uniform(0.03, 0.09)
        L = 1800
        p1 = (center[0] + math.cos(a - w) * L, center[1] + math.sin(a - w) * L)
        p2 = (center[0] + math.cos(a + w) * L, center[1] + math.sin(a + w) * L)
        d.polygon([center, p1, p2], fill=color + (int(255 * strength),))
    return add_layer(img, layer.filter(ImageFilter.GaussianBlur(10)))


def speed_lines(img, center, color, rng, count=60, inner=260, outer=1500, alpha=120):
    layer = new_layer()
    d = ImageDraw.Draw(layer)
    for _ in range(count):
        a = rng.uniform(0, math.tau)
        r0 = rng.uniform(inner, inner + 500)
        r1 = rng.uniform(r0 + 150, outer)
        w = rng.uniform(1.5, 4.5)
        p0 = (center[0] + math.cos(a) * r0, center[1] + math.sin(a) * r0)
        p1 = (center[0] + math.cos(a) * r1, center[1] + math.sin(a) * r1)
        d.line([p0, p1], fill=color + (rng.randint(alpha // 2, alpha),), width=int(w))
    return add_layer(img, layer.filter(ImageFilter.GaussianBlur(0.8)))


# ---------------------------------------------------------------- รอยปาด / วงหมุน

def _arc_points(center, radius, a0, a1, squash, steps, spiral):
    pts = []
    for i in range(steps + 1):
        t = i / steps
        a = a0 + (a1 - a0) * t
        r = radius + spiral * t
        pts.append((center[0] + math.cos(a) * r, center[1] + math.sin(a) * r * squash, t))
    return pts


def swoosh_polygon(center, radius, a0, a1, width, squash=0.8, spiral=0.0, steps=40, skew=0.5):
    """รอยปาดโค้งปลายแหลมสองด้าน (เหมือนรอยพู่กัน)"""
    base = _arc_points(center, radius, a0, a1, squash, steps, spiral)
    outer, inner = [], []
    for i, (x, y, t) in enumerate(base):
        prof = math.sin(math.pi * (t ** (0.6 + skew * 0.8))) ** 0.9
        w = width * prof
        a = a0 + (a1 - a0) * t
        nx, ny = math.cos(a), math.sin(a) * squash
        outer.append((x + nx * w / 2, y + ny * w / 2))
        inner.append((x - nx * w / 2, y - ny * w / 2))
    return outer + inner[::-1]


def draw_swoosh(img, poly, color, core=None, glow_r=18, outline=None, alpha=235):
    """วาดรอยปาด: เรืองแสง + สีหลัก + แกนสว่าง (+ เส้นขอบเข้ม)"""
    gl = new_layer()
    ImageDraw.Draw(gl).polygon(poly, fill=color + (200,))
    img = add_layer(img, gl.filter(ImageFilter.GaussianBlur(glow_r)))
    l = new_layer()
    d = ImageDraw.Draw(l)
    if outline:
        d.polygon(poly, fill=outline + (255,), outline=outline + (255,), width=7)
    d.polygon(poly, fill=color + (alpha,))
    img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(1.2)))
    if core:
        cx = sum(p[0] for p in poly) / len(poly)
        cy = sum(p[1] for p in poly) / len(poly)
        shrunk = [(cx + (x - cx) * 0.78 + 0, cy + (y - cy) * 0.78) for x, y in poly]
        l2 = new_layer()
        ImageDraw.Draw(l2).polygon(shrunk, fill=core + (210,))
        img = paste_rgba(img, l2.filter(ImageFilter.GaussianBlur(2.5)))
    return img


def vortex(img, center, palette, rng, count=14, rmin=140, rmax=620, squash=0.78,
           width=(40, 130), core=(255, 255, 255), outline=None, glow_r=20, spin=1.0):
    """วงหมุนของรอยปาดหลายสี เหมือนพายุพลังเวทในภาพประกอบอนิเมะ"""
    for i in range(count):
        r = rng.uniform(rmin, rmax)
        a0 = rng.uniform(0, math.tau)
        span = rng.uniform(1.4, 3.6) * spin
        w = rng.uniform(*width) * (0.5 + r / rmax * 0.8)
        poly = swoosh_polygon(center, r, a0, a0 + span, w, squash, spiral=rng.uniform(-80, 80))
        col = palette[i % len(palette)]
        img = draw_swoosh(img, poly, col, core=mix(col, core, 0.5), glow_r=glow_r, outline=outline)
    return img


def shards(img, center, rng, color, count=12, rmin=120, rmax=520, size=(40, 130), edge=(255, 230, 255),
           outline=(20, 5, 30)):
    """เศษแตกกระจายออกจากจุดศูนย์กลาง ขอบสว่าง เส้นขอบเข้ม"""
    for _ in range(count):
        a = rng.uniform(0, math.tau)
        r = rng.uniform(rmin, rmax)
        cx = center[0] + math.cos(a) * r
        cy = center[1] + math.sin(a) * r * 0.8
        s = rng.uniform(*size)
        n = rng.randint(5, 7)
        pts = []
        for k in range(n):
            ang = k / n * math.tau + rng.uniform(-0.3, 0.3)
            rr = s * rng.uniform(0.55, 1.1)
            pts.append((cx + math.cos(ang) * rr, cy + math.sin(ang) * rr * 0.85))
        l = new_layer()
        d = ImageDraw.Draw(l)
        d.polygon(pts, fill=outline + (255,), outline=outline + (255,), width=8)
        d.polygon(pts, fill=mul(color, rng.uniform(0.5, 0.95)) + (255,))
        # ไฮไลต์ขอบด้านที่หันเข้าหาแสง
        d.line([pts[0], pts[1], pts[2]], fill=edge + (255,), width=5)
        img = paste_rgba(img, l)
    return img


def petals(img, rng, color, count=40, region=(0, 0, W, H)):
    l = new_layer()
    d = ImageDraw.Draw(l)
    for _ in range(count):
        x = rng.uniform(region[0], region[2])
        y = rng.uniform(region[1], region[3])
        s = rng.uniform(10, 26)
        a = rng.uniform(0, math.tau)
        pts = [(x + math.cos(a) * s * 2, y + math.sin(a) * s * 2),
               (x + math.cos(a + 1.6) * s, y + math.sin(a + 1.6) * s),
               (x - math.cos(a) * s * 2, y - math.sin(a) * s * 2),
               (x + math.cos(a - 1.6) * s, y + math.sin(a - 1.6) * s)]
        d.polygon(pts, fill=color + (rng.randint(150, 240),))
    return paste_rgba(img, l.filter(ImageFilter.GaussianBlur(0.8)))


# ---------------------------------------------------------------- สายฟ้า / เปลวไฟ

def lightning_path(rng, start, end, jag=60, segments=14):
    pts = [start]
    for i in range(1, segments):
        t = i / segments
        x = start[0] + (end[0] - start[0]) * t + rng.uniform(-jag, jag)
        y = start[1] + (end[1] - start[1]) * t + rng.uniform(-jag * 0.3, jag * 0.3)
        pts.append((x, y))
    pts.append(end)
    return pts


def glow_line(img, pts, color, width=18, blur=16, core=(255, 255, 255)):
    gl = new_layer()
    ImageDraw.Draw(gl).line(pts, fill=color + (255,), width=width, joint="curve")
    img = add_layer(img, gl.filter(ImageFilter.GaussianBlur(blur)))
    l = new_layer()
    ImageDraw.Draw(l).line(pts, fill=color + (255,), width=max(3, int(width * 0.6)), joint="curve")
    img = paste_rgba(img, l)
    l = new_layer()
    ImageDraw.Draw(l).line(pts, fill=core + (255,), width=max(2, width // 3), joint="curve")
    return paste_rgba(img, l)


def flame_poly(x, base_y, height, width, rng, sway=0.25, steps=26):
    ph = rng.uniform(0, 6.28)
    left, right = [], []
    for i in range(steps + 1):
        t = i / steps
        prof = (1 - t) ** 0.85 * (1 + 0.18 * math.sin(t * 9 + ph))
        cx = x + math.sin(t * 3.2 + ph) * width * sway * t * 2
        y = base_y - height * t
        w = width * prof
        left.append((cx - w, y))
        right.append((cx + w, y))
    return left + right[::-1]


def flame_stack(img, x, base_y, height, width, rng,
                colors=((255, 90, 10), (255, 170, 40), (255, 245, 190)), outline=None):
    for k, col in enumerate(colors):
        poly = flame_poly(x, base_y, height * (1 - 0.12 * k), width * (1 - 0.28 * k), rng)
        if k == 0:
            g = new_layer()
            ImageDraw.Draw(g).polygon(poly, fill=col + (255,))
            img = add_layer(img, g.filter(ImageFilter.GaussianBlur(22)))
        l = new_layer()
        d = ImageDraw.Draw(l)
        if outline and k == 0:
            d.polygon(poly, fill=outline + (255,), outline=outline + (255,), width=8)
        d.polygon(poly, fill=col + (255,))
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(2.5)))
    return img

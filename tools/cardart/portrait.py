"""ภาพครึ่งตัวสไตล์อนิเมะ: ตาโต เงาเซล ผม/อุปกรณ์ต่อตัวละคร บนฉากวงหมุนพลังเวท"""
import math
import random

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

from .fx import (W, H, add_layer, bokeh, draw_swoosh, glow, mix, mul, new_layer, paste_rgba, sparkles,
                 speed_lines, swoosh_polygon, vignette, vortex)
from .magic import blade_halves, draw_blade, draw_hilt, magic_bg, xform

INK = (28, 18, 36)  # สีเส้นขอบ


# ---------------------------------------------------------------- รูปทรง + เงาเซล

def poly_mask(points):
    m = Image.new("L", (W, H), 0)
    ImageDraw.Draw(m).polygon(points, fill=255)
    return m


def comp(img, color, mask, alpha=255):
    layer = Image.new("RGBA", (W, H), color + (255,))
    if alpha != 255:
        mask = mask.point(lambda v: int(v * alpha / 255))
    layer.putalpha(mask)
    base = img.convert("RGBA")
    base.alpha_composite(layer)
    return base.convert("RGB")


def shape(img, pts, color, shade=None, light=None, outline=True, dx=34, dy=26, shade_alpha=150, light_alpha=90,
          ow=6):
    """วาดรูปทรงพร้อมเส้นขอบ เงาเซล (ด้านขวาล่าง) และแสง (ด้านซ้ายบน)"""
    mask = poly_mask(pts)
    if outline:
        img = comp(img, INK, mask.filter(ImageFilter.MaxFilter(ow * 2 + 1)))
    img = comp(img, color, mask)
    if shade is not None:
        sh = ImageChops.subtract(mask, ImageChops.offset(mask, -dx, -dy)).filter(ImageFilter.GaussianBlur(1.4))
        img = comp(img, shade, sh, shade_alpha)
    if light is not None:
        lt = ImageChops.subtract(mask, ImageChops.offset(mask, dx, dy)).filter(ImageFilter.GaussianBlur(1.4))
        img = comp(img, light, lt, light_alpha)
    return img, mask


def curve(p0, p1, p2, steps=22):
    pts = []
    for i in range(steps + 1):
        t = i / steps
        x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0]
        y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]
        pts.append((x, y))
    return pts


def lock(base, ctrl, tip, width, steps=22, taper=0.8):
    """ปอยผมโค้ง โคนกว้าง ปลายแหลม"""
    c = curve(base, ctrl, tip, steps)
    left, right = [], []
    for i, (x, y) in enumerate(c):
        t = i / steps
        if i < steps:
            nx, ny = c[i + 1][0] - x, c[i + 1][1] - y
        else:
            nx, ny = x - c[i - 1][0], y - c[i - 1][1]
        ln = math.hypot(nx, ny) or 1
        nx, ny = -ny / ln, nx / ln
        w = width * (1 - t) ** taper * (0.55 + 0.45 * math.sin(min(1, t * 3.2) * math.pi / 2))
        left.append((x + nx * w / 2, y + ny * w / 2))
        right.append((x - nx * w / 2, y - ny * w / 2))
    return left + right[::-1]


def tr(pts, dx, dy):
    return [(x + dx, y + dy) for x, y in pts]


def mirror(pts):
    return [(-x, y) for x, y in pts]


def ellipse_pts(cx, cy, a, b, a0=0, a1=360, n=48):
    return [(cx + math.cos(math.radians(a0 + (a1 - a0) * i / n)) * a,
             cy + math.sin(math.radians(a0 + (a1 - a0) * i / n)) * b) for i in range(n + 1)]


# ---------------------------------------------------------------- ใบหน้า

FW, FTOP, FCHIN = 135, 165, 205


def face_pts():
    pts = ellipse_pts(0, 0, FW, FTOP, 180, 360, 40)
    for i in range(1, 25):
        s = i / 24
        pts.append((FW * (1 - s ** 2.4) ** 0.55, FCHIN * s * 0.98))
    left = []
    for i in range(24, 0, -1):
        s = i / 24
        left.append((-FW * (1 - s ** 2.4) ** 0.55, FCHIN * s * 0.98))
    return pts + left


def eye(img, ex, ey, cfg, side):
    iris = cfg.get("eye", (90, 60, 40))
    ew, eh = 40, 50
    d_layer = new_layer()
    d = ImageDraw.Draw(d_layer)
    # ตาขาว
    d.ellipse([ex - ew, ey - eh, ex + ew, ey + eh], fill=(250, 250, 255, 255))
    base = paste_rgba(img, d_layer)
    # ม่านตา (ไล่โทน) + รูม่านตา
    il = new_layer()
    di = ImageDraw.Draw(il)
    ir_w, ir_h = ew * 0.82, eh * 0.95
    di.ellipse([ex - ir_w, ey - ir_h, ex + ir_w, ey + ir_h], fill=mul(iris, 0.45) + (255,))
    di.ellipse([ex - ir_w * 0.9, ey - ir_h * 0.15, ex + ir_w * 0.9, ey + ir_h * 0.98], fill=iris + (255,))
    di.ellipse([ex - ir_w * 0.7, ey + ir_h * 0.2, ex + ir_w * 0.7, ey + ir_h * 0.95], fill=mix(iris, (255, 255, 255), 0.35) + (255,))
    di.ellipse([ex - ir_w * 0.34, ey - ir_h * 0.3, ex + ir_w * 0.34, ey + ir_h * 0.42], fill=(20, 12, 24, 255))
    # ตัดให้อยู่ในตาขาว
    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).ellipse([ex - ew, ey - eh, ex + ew, ey + eh], fill=255)
    r, g, b, a = il.split()
    a = ImageChops.multiply(a, mask)
    il = Image.merge("RGBA", (r, g, b, a))
    base = paste_rgba(base, il)
    # ไฮไลต์
    hl = new_layer()
    dh = ImageDraw.Draw(hl)
    dh.ellipse([ex - ew * 0.55, ey - eh * 0.62, ex - ew * 0.05, ey - eh * 0.1], fill=(255, 255, 255, 255))
    dh.ellipse([ex + ew * 0.15, ey + eh * 0.2, ex + ew * 0.45, ey + eh * 0.48], fill=(255, 255, 255, 230))
    base = paste_rgba(base, hl)
    # ขอบตา + ขนตา
    ln = new_layer()
    dl = ImageDraw.Draw(ln)
    top = [(ex + math.cos(math.radians(a)) * (ew + 3), ey + math.sin(math.radians(a)) * (eh + 3))
           for a in range(190, 351, 8)]
    dl.line(top, fill=INK + (255,), width=13, joint="curve")
    outer = top[-1] if side > 0 else top[0]
    flick = (outer[0] + side * 22, outer[1] + 10)
    dl.line([outer, flick], fill=INK + (255,), width=9)
    low = [(ex + math.cos(math.radians(a)) * (ew + 1), ey + math.sin(math.radians(a)) * (eh + 1))
           for a in range(10, 171, 10)]
    dl.line(low, fill=mul(INK, 1.8) + (160,), width=4, joint="curve")
    return paste_rgba(base, ln)


def brow(img, ex, ey, cfg, side):
    col = mul(cfg.get("hair", (60, 40, 30)), 0.7)
    thick = cfg.get("brow", 1.0)
    tilt = {"stern": 0.35, "worried": -0.3, "grin": 0.0, "smile": 0.0, "calm": 0.0}.get(cfg.get("mood", "smile"), 0.0)
    y = ey - 78
    inner_y = y + tilt * 22
    outer_y = y - tilt * 22
    pts = [(ex - 40 * side, inner_y), (ex, (inner_y + outer_y) / 2 - 7), (ex + 42 * side, outer_y + 4)]
    l = new_layer()
    ImageDraw.Draw(l).line(pts, fill=col + (255,), width=int(9 * thick), joint="curve")
    return paste_rgba(img, l)


def mouth(img, cfg, cx, cy):
    mood = cfg.get("mood", "smile")
    l = new_layer()
    d = ImageDraw.Draw(l)
    ink = (120, 50, 60, 255)
    if mood == "grin":
        d.chord([cx - 38, cy - 18, cx + 38, cy + 46], 0, 180, fill=(150, 40, 60, 255), outline=ink, width=5)
        d.pieslice([cx - 30, cy - 14, cx + 30, cy + 22], 0, 180, fill=(255, 255, 255, 255))
        d.chord([cx - 38, cy - 18, cx + 38, cy + 46], 0, 180, outline=ink, width=5)
    elif mood == "stern":
        d.line([(cx - 24, cy + 6), (cx + 24, cy + 6)], fill=ink, width=6)
    elif mood == "worried":
        pts = [(cx - 26 + i * 6.5, cy + 8 + math.sin(i * 1.6) * 5) for i in range(9)]
        d.line(pts, fill=ink, width=6, joint="curve")
    elif mood == "calm":
        d.arc([cx - 22, cy - 10, cx + 22, cy + 14], 20, 160, fill=ink, width=6)
    else:
        d.arc([cx - 32, cy - 16, cx + 32, cy + 22], 15, 165, fill=ink, width=7)
    if cfg.get("fangs"):
        for s in (-1, 1):
            d.polygon([(cx + s * 12, cy + 4), (cx + s * 25, cy + 4), (cx + s * 19, cy + 30)], fill=(255, 255, 255, 255),
                      outline=ink)
    return paste_rgba(img, l)


# ---------------------------------------------------------------- ผม / หมวก

def hair_style_front(style):
    """คืนลิสต์ของรูปทรงผมด้านหน้า (พิกัดสัมพัทธ์กับกลางหัว)"""
    a = FW + 24
    cap = ellipse_pts(0, -12, a, FTOP + 42, 180, 360, 40) + [(a * 0.78, -66), (a * 0.34, -100), (0, -108),
                                                              (-a * 0.34, -100), (-a * 0.78, -66)]
    shapes = [cap]
    if style == "part":
        for s in (1, -1):
            f = (lambda p: p) if s == 1 else mirror
            shapes.append(f(lock((8, -118), (70, -30), (118, 46), 92)))
            shapes.append(f(lock((50, -128), (130, -70), (162, 52), 78)))
            shapes.append(f(lock((FW - 2, -80), (FW + 28, 70), (FW + 6, 215), 64)))
    elif style == "spiky":
        for k, ang in enumerate(range(200, 345, 24)):
            r = math.radians(ang)
            base = (math.cos(r) * 95, math.sin(r) * 100 - 30)
            tip = (math.cos(r) * (FW + 95), math.sin(r) * (FTOP + 90) - 20)
            shapes.append(lock(base, ((base[0] + tip[0]) / 2 + 20, (base[1] + tip[1]) / 2), tip, 78, taper=0.6))
        for x in (-80, 0, 85):
            shapes.append(lock((x, -112), (x + 8, -70), (x + 24 * (1 if x >= 0 else -1), -10), 62, taper=0.7))
    elif style == "bowl":
        shapes.append([(-a, -40), (a, -40), (a, 40), (FW * 0.95, 80), (FW * 0.82, -34), (FW * 0.55, -52),
                       (FW * 0.28, -42), (0, -54), (-FW * 0.28, -42), (-FW * 0.55, -52), (-FW * 0.82, -34),
                       (-FW * 0.95, 80), (-a, 40)])
    elif style == "bob":
        shapes.append(lock((8, -118), (70, -40), (130, 24), 90))
        shapes.append(mirror(lock((8, -118), (70, -40), (130, 24), 90)))
        for s in (1, -1):
            f = (lambda p: p) if s == 1 else mirror
            shapes.append(f(lock((FW - 2, -70), (FW + 55, 60), (FW - 30, 232), 96, taper=0.5)))
    elif style == "short":
        for x, ty in [(-95, 44), (-40, 20), (30, 30), (92, 50)]:
            shapes.append(lock((x, -112), (x + (6 if x > 0 else -6), -50), (x + (14 if x > 0 else -14), ty), 70))
        for s in (1, -1):
            f = (lambda p: p) if s == 1 else mirror
            shapes.append(f(lock((FW, -60), (FW + 14, 20), (FW - 4, 100), 50)))
    elif style == "pompadour":
        shapes = [ellipse_pts(0, -30, a + 4, FTOP + 70, 180, 360, 40) +
                  [(a * 0.78, -66), (a * 0.34, -100), (0, -108), (-a * 0.34, -100), (-a * 0.78, -66)]]
        for x, tx, ty in [(-90, -150, -400), (-30, -50, -440), (35, 60, -450), (95, 170, -410)]:
            shapes.append(lock((x, -150), (x + (tx - x) * 0.3, -300), (tx, ty), 84, taper=0.7))
        for s in (1, -1):
            f = (lambda p: p) if s == 1 else mirror
            shapes.append(f(lock((FW, -70), (FW + 20, 40), (FW - 12, 130), 56)))
    elif style == "bald":
        shapes = []
    return shapes


def hair_style_back(style):
    shapes = []
    if style == "long":
        shapes.append(ellipse_pts(0, -20, FW + 85, FTOP + 70, 180, 360, 40) +
                      [(FW + 105, 120), (320, 420), (270, 700), (-270, 700), (-320, 420), (-FW - 105, 120)])
    elif style == "twin":
        for s in (1, -1):
            f = (lambda p: p) if s == 1 else mirror
            shapes.append(f(lock((FW - 20, -70), (FW + 200, 60), (FW + 80, 560), 150, taper=0.4)))
    elif style == "ponytail":
        shapes.append(lock((FW - 30, -90), (FW + 260, -80), (FW + 160, 420), 140, taper=0.45))
    elif style == "bob":
        shapes.append(ellipse_pts(0, 40, FW + 40, 215, 0, 360, 48))
    return shapes


def draw_hair(img, shapes, color, shade, light, rng):
    union = Image.new("L", (W, H), 0)
    for pts in shapes:
        img, m = shape(img, pts, color, shade, light, outline=True, dx=26, dy=20, shade_alpha=170, light_alpha=70,
                       ow=5)
        union = ImageChops.lighter(union, m)
    return img, union


def hair_highlight(img, union, color, top_y=-120):
    l = new_layer()
    d = ImageDraw.Draw(l)
    # วงแสง (angel ring) + เส้นเงาตามเส้นผม
    ring = ellipse_pts(0, -40, FW + 6, 118, 205, 335, 24) + ellipse_pts(0, -40, FW - 38, 82, 335, 205, 24)
    d.polygon(ring, fill=color + (200,))
    for k in range(5):
        x = -90 + k * 45
        d.line(curve((x, -150), (x + 20, -90), (x + 8, -30), 12), fill=color + (110,), width=8)
    layer = l.filter(ImageFilter.GaussianBlur(2))
    r, g, b, a = layer.split()
    a = ImageChops.multiply(a, union)
    return paste_rgba(img, Image.merge("RGBA", (r, g, b, a)))


# ---------------------------------------------------------------- ชุด

def body(img, cfg, ox, oy, rng):
    skin = cfg.get("skin", (255, 226, 205))
    skin_sh = mix(mul(skin, 0.82), (190, 150, 190), 0.25)
    outfit = cfg.get("outfit", (60, 70, 120))
    accent = cfg.get("accent", (240, 220, 160))
    o_sh = mix(mul(outfit, 0.62), (30, 20, 70), 0.35)
    o_lt = mix(outfit, (255, 255, 255), 0.35)
    collar = cfg.get("collar", "v")
    # คอ
    neck = [(-52, 150), (52, 150), (62, 300), (-62, 300)]
    shoulders = [(x * 0.88, y) for x, y in [(-90, 238), (90, 238), (230, 262), (370, 330), (470, 480), (520, 700),
                                            (-520, 700), (-470, 480), (-370, 330), (-230, 262)]]
    img, _ = shape(img, tr(neck, ox, oy), skin, skin_sh, None, ow=5)
    if cfg.get("cape"):
        img, _ = shape(img, tr([(-560, 700), (-430, 330), (430, 330), (560, 700)], ox, oy), cfg["cape"],
                       mul(cfg["cape"], 0.6), mix(cfg["cape"], (255, 255, 255), 0.3))
    img, m = shape(img, tr(shoulders, ox, oy), outfit, o_sh, o_lt, ow=6)
    if collar == "v":
        v = [(-92, 238), (92, 238), (0, 380)]
        img, _ = shape(img, tr(v, ox, oy), skin, skin_sh, None, ow=5)
        img, _ = shape(img, tr([(-92, 238), (-70, 238), (4, 372), (-14, 392)], ox, oy), accent, None, None, ow=3)
        img, _ = shape(img, tr([(92, 238), (70, 238), (-4, 372), (14, 392)], ox, oy), accent, None, None, ow=3)
    elif collar == "high":
        img, _ = shape(img, tr([(-96, 205), (96, 205), (110, 290), (-110, 290)], ox, oy), accent,
                       mul(accent, 0.7), None, ow=5)
    elif collar == "jacket":
        img, _ = shape(img, tr([(-80, 238), (80, 238), (50, 420), (-50, 420)], ox, oy), cfg.get("inner", (240, 240, 245)),
                       mul(cfg.get("inner", (240, 240, 245)), 0.75), None, ow=5)
        img, _ = shape(img, tr([(-96, 238), (-30, 238), (-52, 430), (-130, 330)], ox, oy), accent, mul(accent, 0.7), None)
        img, _ = shape(img, tr([(96, 238), (30, 238), (52, 430), (130, 330)], ox, oy), accent, mul(accent, 0.7), None)
    elif collar == "armor":
        for s in (-1, 1):
            img, _ = shape(img, tr(ellipse_pts(s * 330, 345, 150, 105, 0, 360, 36), ox, oy), accent,
                           mul(accent, 0.62), mix(accent, (255, 255, 255), 0.5), ow=6)
        img, _ = shape(img, tr([(-130, 215), (130, 215), (160, 330), (-160, 330)], ox, oy), accent,
                       mul(accent, 0.62), mix(accent, (255, 255, 255), 0.45), ow=6)
    elif collar == "scarf":
        sc = cfg.get("scarf_color", (60, 110, 220))
        img, _ = shape(img, tr([(-170, 200), (170, 200), (190, 300), (-190, 300)], ox, oy), sc, mul(sc, 0.65),
                       mix(sc, (255, 255, 255), 0.35), ow=6)
        img, _ = shape(img, tr(lock((120, 290), (240, 420), (150, 640), 110, taper=0.3), ox, oy), sc, mul(sc, 0.65),
                       None, ow=6)
    # แถบลายเสื้อ
    if cfg.get("stripe"):
        img, _ = shape(img, tr([(-480, 560), (480, 560), (500, 620), (-500, 620)], ox, oy), cfg["stripe"], None, None,
                       outline=False)
    return img


def helmet(img, cfg, ox, oy):
    col = cfg.get("helmet", (150, 150, 170))
    sh = mul(col, 0.58)
    lt = mix(col, (255, 255, 255), 0.5)
    dome = ellipse_pts(0, -20, FW + 30, FTOP + 55, 180, 360, 40) + [(FW + 30, 120), (FW - 20, 215), (0, 250),
                                                                    (-FW + 20, 215), (-FW - 30, 120)]
    img, m = shape(img, tr(dome, ox, oy), col, sh, lt, ow=7, dx=40, dy=30)
    # ช่องมองตา
    vis = [(-FW + 6, -2), (FW - 6, -2), (FW - 24, 62), (-FW + 24, 62)]
    img, _ = shape(img, tr(vis, ox, oy), (18, 12, 24), None, None, outline=True, ow=5)
    eyec = cfg.get("glow_eye", (255, 90, 90))
    l = new_layer()
    d = ImageDraw.Draw(l)
    for s in (-1, 1):
        d.polygon([(ox + s * 28, oy + 14), (ox + s * 90, oy + 10), (ox + s * 84, oy + 38), (ox + s * 32, oy + 40)],
                  fill=eyec + (255,))
    img = add_layer(img, l.filter(ImageFilter.GaussianBlur(10)))
    img = paste_rgba(img, l)
    # สันหมวก
    img, _ = shape(img, tr([(-18, -FTOP - 60), (18, -FTOP - 60), (22, 250), (-22, 250)], ox, oy), mul(col, 0.8),
                   None, mix(col, (255, 255, 255), 0.4), ow=4)
    return img


def accessories(img, cfg, ox, oy, stage, rng):
    acc = cfg.get("acc", [])
    for a in acc:
        name = a[0]
        col = a[1] if len(a) > 1 else (240, 240, 240)
        if stage == "front":
            if name == "goggles":
                l = new_layer()
                d = ImageDraw.Draw(l)
                d.rectangle([ox - FW - 14, oy - 140, ox + FW + 14, oy - 112], fill=mul(col, 0.5) + (255,), outline=INK + (255,),
                            width=5)
                for s in (-1, 1):
                    d.ellipse([ox + s * 52 - 44, oy - 190, ox + s * 52 + 44, oy - 100], fill=INK + (255,))
                    d.ellipse([ox + s * 52 - 36, oy - 182, ox + s * 52 + 36, oy - 108], fill=col + (255,))
                    d.ellipse([ox + s * 52 - 20, oy - 170, ox + s * 52 + 4, oy - 146], fill=(255, 255, 255, 200))
                img = paste_rgba(img, l)
            elif name == "headband":
                img, _ = shape(img, tr([(-FW - 20, -100), (FW + 20, -100), (FW + 20, -62), (-FW - 20, -62)], ox, oy), col,
                               mul(col, 0.7), mix(col, (255, 255, 255), 0.4), ow=5)
                img, _ = shape(img, tr([(-62, -104), (62, -104), (62, -58), (-62, -58)], ox, oy), (190, 195, 210),
                               (120, 125, 140), (240, 240, 250), ow=5)
            elif name == "glasses":
                l = new_layer()
                d = ImageDraw.Draw(l)
                for s in (-1, 1):
                    d.ellipse([ox + s * 68 - 58, oy + 30 - 62, ox + s * 68 + 58, oy + 30 + 62], outline=col + (255,), width=9)
                    d.ellipse([ox + s * 68 - 54, oy + 30 - 58, ox + s * 68 + 54, oy + 30 + 58], fill=(210, 235, 255, 55))
                d.line([(ox - 10, oy + 22), (ox + 10, oy + 22)], fill=col + (255,), width=8)
                img = paste_rgba(img, l)
            elif name == "dots":
                l = new_layer()
                d = ImageDraw.Draw(l)
                for x, y in [(-30, -105), (30, -105), (-62, -75), (62, -75), (-14, -70), (14, -70)]:
                    d.ellipse([ox + x - 9, oy + y - 9, ox + x + 9, oy + y + 9], fill=(120, 70, 70, 255))
                img = paste_rgba(img, l)
            elif name == "mark":
                l = new_layer()
                d = ImageDraw.Draw(l)
                d.rectangle([ox - 30, oy - 118, ox + 4, oy - 82], fill=(210, 40, 50, 255))
                d.line([(ox - 22, oy - 108), (ox - 4, oy - 92)], fill=(255, 220, 220, 255), width=4)
                img = paste_rgba(img, l)
            elif name == "rings":
                l = new_layer()
                d = ImageDraw.Draw(l)
                for s in (-1, 1):
                    d.ellipse([ox + s * 68 - 56, oy + 30 - 66, ox + s * 68 + 56, oy + 30 + 66], outline=(60, 30, 50, 120),
                              width=9)
                img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(3)))
            elif name == "hat":
                img, _ = shape(img, tr(ellipse_pts(0, -150, 175, 135, 180, 360, 36) + [(175, -150), (-175, -150)], ox, oy),
                               col, mul(col, 0.65), mix(col, (255, 255, 255), 0.4), ow=6)
                img, _ = shape(img, tr(ellipse_pts(0, -142, 205, 34, 0, 360, 36), ox, oy), mul(col, 0.9), mul(col, 0.6),
                               None, ow=5)
                l = new_layer()
                d = ImageDraw.Draw(l)
                d.line([(ox - 40, oy - 245), (ox + 40, oy - 175)], fill=(255, 255, 255, 255), width=14)
                d.line([(ox + 40, oy - 245), (ox - 40, oy - 175)], fill=(255, 255, 255, 255), width=14)
                img = paste_rgba(img, l)
            elif name == "pink_nose":
                l = new_layer()
                d = ImageDraw.Draw(l)
                d.ellipse([ox - 18, oy + 78, ox + 18, oy + 104], fill=col + (255,), outline=INK + (255,), width=4)
                img = paste_rgba(img, l)
            elif name == "long_nose":
                img, _ = shape(img, tr([(-26, 20), (26, 20), (36, 150), (0, 176), (-36, 150)], ox, oy), (240, 200, 170),
                               (205, 160, 140), (255, 235, 215), ow=5)
            elif name == "horns":
                for s in (1, -1):
                    f = (lambda p: p) if s == 1 else mirror
                    img, _ = shape(img, tr(f(lock((FW - 40, -135), (FW + 80, -230), (FW + 150, -330), 70, taper=0.55)), ox, oy),
                                   col, mul(col, 0.62), mix(col, (255, 255, 255), 0.5), ow=5)
            elif name == "antlers":
                for s in (1, -1):
                    f = (lambda p: p) if s == 1 else mirror
                    img, _ = shape(img, tr(f(lock((FW - 20, -170), (FW + 60, -280), (FW + 40, -380), 46, taper=0.5)), ox, oy),
                                   col, mul(col, 0.65), mix(col, (255, 255, 255), 0.4), ow=5)
                    img, _ = shape(img, tr(f(lock((FW + 50, -270), (FW + 110, -300), (FW + 150, -350), 36, taper=0.5)), ox, oy),
                                   col, mul(col, 0.65), None, ow=4)
            elif name == "earring":
                l = new_layer()
                d = ImageDraw.Draw(l)
                for s in (-1, 1):
                    d.ellipse([ox + s * (FW - 2) - 11, oy + 110, ox + s * (FW - 2) + 11, oy + 132], fill=col + (255,),
                              outline=INK + (255,), width=3)
                img = paste_rgba(img, l)
            elif name == "ribbon":
                for s in (1, -1):
                    f = (lambda p: p) if s == 1 else mirror
                    img, _ = shape(img, tr(f([(FW - 20, -110), (FW + 70, -150), (FW + 74, -70)]), ox, oy), col, mul(col, 0.65),
                                   mix(col, (255, 255, 255), 0.4), ow=4)
            elif name == "flame_hair":
                l = new_layer()
                d = ImageDraw.Draw(l)
                for i, x in enumerate(range(-120, 121, 40)):
                    h = 70 + (i % 2) * 40
                    d.polygon([(ox + x - 24, oy - 150), (ox + x, oy - 150 - h - 30), (ox + x + 24, oy - 150)],
                              fill=col + (255,), outline=INK + (255,))
                img = paste_rgba(img, l)
            elif name == "fangs":
                pass
        else:  # stage back: ปีกหลังตัวละคร
            if name == "wings":
                for s in (1, -1):
                    f = (lambda p: p) if s == 1 else mirror
                    for k in range(4):
                        img, _ = shape(img, tr(f(lock((260, 300 + k * 30), (520 + k * 40, 120 + k * 80),
                                                       (560 + k * 70, 420 + k * 90), 120, taper=0.35)), ox, oy), col,
                                       mul(col, 0.55), mix(col, (255, 255, 255), 0.3), ow=5)
    return img


def draw_props(img, cfg, ox, oy, rng):
    for p in cfg.get("props", []):
        t = p["t"]
        col = p.get("col", (225, 230, 245))
        if t == "blade":
            ang = math.radians(p.get("ang", -62))
            origin = (ox + p.get("x", 430), oy + p.get("y", 600))
            img = draw_blade(img, ang, origin, p.get("len", 760), p.get("w", 56), p.get("bend", 30), 100, col,
                             mul(col, 0.62), glow_color=p.get("glow"))
            img = draw_hilt(img, ang, origin, 46, 112, 220, 38, p.get("metal", (210, 170, 90)), p.get("wrap", (40, 40, 70)))
        elif t == "orb":
            c = (ox + p.get("x", -380), oy + p.get("y", 470))
            img = glow(img, c, 230, col, 0.8)
            l = new_layer()
            d = ImageDraw.Draw(l)
            r = p.get("r", 66)
            d.ellipse([c[0] - r, c[1] - r, c[0] + r, c[1] + r], fill=col + (255,), outline=INK + (255,), width=6)
            d.ellipse([c[0] - r * 0.6, c[1] - r * 0.6, c[0] + r * 0.2, c[1] + r * 0.2], fill=(255, 255, 255, 220))
            img = paste_rgba(img, l)
        elif t == "shield":
            c = (ox + p.get("x", -400), oy + p.get("y", 620))
            l = new_layer()
            d = ImageDraw.Draw(l)
            pts = [(c[0] - 150, c[1] - 190), (c[0] + 150, c[1] - 190), (c[0] + 160, c[1] + 30), (c[0], c[1] + 220),
                   (c[0] - 160, c[1] + 30)]
            d.polygon(pts, fill=INK + (255,), outline=INK + (255,), width=12)
            d.polygon([(x * 0.93 + c[0] * 0.07, y * 0.93 + c[1] * 0.07) for x, y in pts], fill=col + (255,))
            d.polygon([(c[0], c[1] - 190), (c[0] + 150, c[1] - 190), (c[0] + 160, c[1] + 30), (c[0], c[1] + 220)],
                      fill=mul(col, 0.7) + (255,))
            d.ellipse([c[0] - 46, c[1] - 70, c[0] + 46, c[1] + 22], fill=p.get("emblem", (255, 220, 110)) + (255,),
                      outline=INK + (255,), width=5)
            img = paste_rgba(img, l)
        elif t == "staff":
            x = ox + p.get("x", 440)
            l = new_layer()
            d = ImageDraw.Draw(l)
            d.line([(x, 60), (x - 30, 960)], fill=INK + (255,), width=34)
            d.line([(x, 60), (x - 30, 960)], fill=col + (255,), width=20)
            img = paste_rgba(img, l)
    return img


# ---------------------------------------------------------------- ประกอบทั้งภาพ

def render_portrait(card_id, cfg):
    rng = random.Random(card_id)
    pal = cfg["palette"]
    top, bottom, light = cfg["bg"]
    img = magic_bg(top, bottom, light, rng, [light] + list(pal))
    hx, hy = W // 2 + cfg.get("dx", 0), 410 + cfg.get("dy", 0)

    # วงหมุนด้านหลัง
    img = vortex(img, (hx, hy + 60), list(pal), rng, count=13, rmin=230, rmax=650, outline=mul(top, 0.4),
                 width=(50, 150), glow_r=22)
    img = speed_lines(img, (hx, hy), mix(light, (255, 255, 255), 0.5), rng, 40, inner=330)

    # ปีก/ของหลังตัว
    img = accessories(img, cfg, hx, hy, "back", rng)

    # ผมหลัง
    hair = cfg.get("hair", (70, 50, 40))
    hair_sh = mix(mul(hair, 0.55), (30, 20, 80), 0.3)
    hair_lt = mix(hair, (255, 255, 255), 0.45)
    img, _ = draw_hair(img, [tr(p, hx, hy) for p in hair_style_back(cfg.get("back", "none"))], hair, hair_sh, hair_lt,
                       rng)

    # ตัว
    img = body(img, cfg, hx, hy, rng)

    # ของถือ (อยู่หน้าไหล่)
    img = draw_props(img, cfg, hx, hy, rng)

    skin = cfg.get("skin", (255, 226, 205))
    skin_sh = mix(mul(skin, 0.82), (190, 150, 190), 0.25)

    if cfg.get("helm"):
        img = helmet(img, cfg, hx, hy)
        img = accessories(img, {"acc": [x for x in cfg.get("acc", []) if x[0] in ("horns", "flame_hair")]}, hx, hy,
                          "front", rng)
    else:
        # หน้า
        img, fmask = shape(img, tr(face_pts(), hx, hy), skin, skin_sh, mix(skin, (255, 255, 255), 0.5), ow=6, dx=30,
                           dy=22, shade_alpha=140, light_alpha=60)
        # แก้ม
        l = new_layer()
        d = ImageDraw.Draw(l)
        for s in (-1, 1):
            d.ellipse([hx + s * 82 - 36, hy + 96, hx + s * 82 + 36, hy + 128], fill=(255, 130, 140, 85))
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(8)))
        # จมูก
        l = new_layer()
        ImageDraw.Draw(l).line([(hx - 5, hy + 88), (hx + 3, hy + 104)], fill=mul(skin, 0.6) + (255,), width=5)
        img = paste_rgba(img, l)
        # ตา คิ้ว ปาก
        for s in (-1, 1):
            img = eye(img, hx + s * 68, hy + 28, cfg, s)
        for s_ in (-1, 1):
            img = brow(img, hx + s_ * 68, hy + 28, cfg, s_)
        img = mouth(img, cfg, hx, hy + 130)
        # ผมหน้า
        front = [tr(p, hx, hy) for p in hair_style_front(cfg.get("front", "part"))]
        img, union = draw_hair(img, front, hair, hair_sh, hair_lt, rng)
        if front:
            img = hair_highlight(img, union, mix(hair, (255, 255, 255), 0.55))
        img = accessories(img, cfg, hx, hy, "front", rng)

    # รอยปาดด้านหน้าบางส่วนให้ภาพมีมิติ
    for k in range(3):
        side = -1 if k % 2 == 0 else 1
        poly = swoosh_polygon((hx + side * 330, hy + 380 + k * 70), 360 + k * 60, math.pi * (0.1 + 0.3 * k),
                              math.pi * (0.8 + 0.2 * k), 70, 0.7)
        c = pal[k % len(pal)]
        img = draw_swoosh(img, poly, c, core=mix(c, (255, 255, 255), 0.5), glow_r=16, outline=mul(top, 0.4), alpha=170)

    img = sparkles(img, rng, mix(light, (255, 255, 255), 0.6), 60, size=(2, 7))
    img = glow(img, (hx, hy - 60), 360, light, 0.18)
    return vignette(img, 0.5)

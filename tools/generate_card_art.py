#!/usr/bin/env python3
"""
สร้างรูปการ์ดแบบ procedural (ภาพประกอบสไตล์ซิลูเอตต์ + แสงเรือง) ให้การ์ด SUP_xxx / MAG_xxx
รันใหม่ได้ทุกเมื่อ: python3 tools/generate_card_art.py
ผลลัพธ์: Assets/_Project/Resources/CardArt/{CardID}.png (640x480)

ใส่รูปจริงทับได้เลย: แค่วางไฟล์ PNG ชื่อเดียวกับ CardID ในโฟลเดอร์เดียวกัน
(สคริปต์จะไม่เขียนทับไฟล์ที่ไม่ได้อยู่ในรายการนี้ และข้ามไฟล์ที่มีอยู่ถ้าใส่ --keep)
"""
import math
import os
import random
import sys

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

W, H = 1280, 960          # วาดใหญ่แล้วย่อลงเพื่อให้เส้นเนียน
OUT_W, OUT_H = 640, 480

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Resources", "CardArt")


# ---------------------------------------------------------------- ตัวช่วยวาด

def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def mul(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c)


def gradient(top, bottom):
    t = np.linspace(0, 1, H)[:, None, None]
    top = np.array(top, dtype=np.float32)[None, None, :]
    bottom = np.array(bottom, dtype=np.float32)[None, None, :]
    arr = top + (bottom - top) * t
    arr = np.repeat(arr, W, axis=1)
    return Image.fromarray(arr.astype(np.uint8), "RGB")


def glow(img, center, radius, color, strength=1.0):
    """เพิ่มแสงวงกลมนุ่มๆ (additive)"""
    ys, xs = np.mgrid[0:H, 0:W]
    d = np.sqrt((xs - center[0]) ** 2 + (ys - center[1]) ** 2) / radius
    a = np.clip(1.0 - d, 0, 1) ** 2 * strength
    base = np.asarray(img).astype(np.float32)
    add = a[:, :, None] * np.array(color, dtype=np.float32)[None, None, :]
    return Image.fromarray(np.clip(base + add, 0, 255).astype(np.uint8), "RGB")


def blur_layer(draw_fn, radius, size=(W, H)):
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    draw_fn(d)
    return layer.filter(ImageFilter.GaussianBlur(radius))


def paste_rgba(img, layer):
    base = img.convert("RGBA")
    base.alpha_composite(layer)
    return base.convert("RGB")


def add_layer(img, layer):
    """บวกแสงจาก layer RGBA แบบ additive"""
    base = np.asarray(img).astype(np.float32)
    lay = np.asarray(layer).astype(np.float32)
    add = lay[:, :, :3] * (lay[:, :, 3:4] / 255.0)
    return Image.fromarray(np.clip(base + add, 0, 255).astype(np.uint8), "RGB")


def sparkles(img, rng, color, count=70, region=(0, 0, W, H), size=(2, 7)):
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for _ in range(count):
        x = rng.uniform(region[0], region[2])
        y = rng.uniform(region[1], region[3])
        r = rng.uniform(*size)
        a = rng.randint(120, 255)
        d.ellipse([x - r, y - r, x + r, y + r], fill=color + (a,))
    layer = layer.filter(ImageFilter.GaussianBlur(1.2))
    return add_layer(img, layer)


def vignette(img, k=0.55):
    ys, xs = np.mgrid[0:H, 0:W]
    d = np.sqrt(((xs - W / 2) / (W / 2)) ** 2 + ((ys - H / 2) / (H / 2)) ** 2)
    v = 1.0 - np.clip(d - 0.35, 0, 1) * k
    arr = np.asarray(img).astype(np.float32) * v[:, :, None]
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB")


def rays(img, center, color, count=14, strength=0.35, rng=None):
    rng = rng or random.Random(1)
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for i in range(count):
        a = (i / count) * math.tau + rng.uniform(-0.1, 0.1)
        w = rng.uniform(0.03, 0.09)
        L = 1800
        p1 = (center[0] + math.cos(a - w) * L, center[1] + math.sin(a - w) * L)
        p2 = (center[0] + math.cos(a + w) * L, center[1] + math.sin(a + w) * L)
        d.polygon([center, p1, p2], fill=color + (int(255 * strength),))
    layer = layer.filter(ImageFilter.GaussianBlur(10))
    return add_layer(img, layer)


# ---------------------------------------------------------------- ฉากหลัง

SCENES = {
    # ชื่อ: (สีบน, สีล่าง, สีแสง, สีพื้น)
    "dawn":    ((255, 190, 120), (90, 60, 130), (255, 220, 150), (30, 25, 50)),
    "sunset":  ((255, 120, 70), (70, 30, 90), (255, 190, 90), (25, 15, 35)),
    "night":   ((20, 25, 70), (60, 50, 120), (170, 190, 255), (10, 10, 25)),
    "sea":     ((70, 170, 240), (20, 70, 140), (255, 240, 200), (15, 40, 80)),
    "storm":   ((50, 60, 90), (110, 120, 170), (255, 245, 150), (20, 22, 40)),
    "forest":  ((120, 200, 130), (30, 90, 70), (255, 255, 170), (15, 45, 35)),
    "ember":   ((255, 140, 50), (120, 25, 25), (255, 210, 90), (35, 10, 10)),
    "ice":     ((190, 235, 255), (70, 120, 190), (255, 255, 255), (25, 45, 85)),
    "abyss":   ((40, 20, 70), (10, 5, 25), (180, 90, 255), (5, 3, 12)),
    "blood":   ((120, 20, 50), (20, 5, 20), (255, 70, 90), (8, 2, 8)),
    "temple":  ((235, 210, 150), (110, 80, 70), (255, 245, 200), (40, 28, 25)),
    "desert":  ((255, 210, 140), (190, 120, 80), (255, 245, 190), (60, 38, 25)),
    "tech":    ((60, 40, 120), (15, 15, 50), (120, 220, 255), (8, 8, 24)),
    "ruins":   ((130, 140, 150), (50, 55, 70), (255, 235, 190), (18, 20, 28)),
    "cosmos":  ((30, 10, 70), (5, 5, 25), (200, 140, 255), (3, 3, 12)),
}


def background(scene, rng):
    top, bottom, light, ground = SCENES[scene]
    img = gradient(top, bottom)
    img = glow(img, (W * rng.uniform(0.3, 0.7), H * 0.38), 620, light, 0.75)
    img = rays(img, (W * 0.5, H * 0.35), light, 12, 0.10, rng)

    # ภูเขา / เมืองไกลๆ
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for k, (base_y, amp, shade) in enumerate([(640, 160, 0.55), (730, 120, 0.38), (810, 80, 0.26)]):
        pts = [(0, H)]
        x = 0
        while x <= W + 60:
            y = base_y - rng.uniform(0, amp) * (0.5 + 0.5 * math.sin(x / 190.0 + k * 1.7 + rng.random()))
            pts.append((x, y))
            x += 60
        pts.append((W, H))
        col = lerp(bottom, ground, 0.55 + 0.15 * k)
        d.polygon(pts, fill=mul(col, shade * 2.2) + (255,))
    img = paste_rgba(img, layer.filter(ImageFilter.GaussianBlur(2)))

    # พื้น
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.rectangle([0, 860, W, H], fill=ground + (255,))
    layer = layer.filter(ImageFilter.GaussianBlur(6))
    img = paste_rgba(img, layer)
    return img


# ---------------------------------------------------------------- ตัวละคร (ซิลูเอตต์)

def poly_thick(d, p1, p2, width, fill):
    """เส้นหนาเป็นแคปซูล"""
    d.line([p1, p2], fill=fill, width=int(width))
    r = width / 2
    for p in (p1, p2):
        d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=fill)


POSES = {
    # (มือซ้ายของภาพ, มือขวาของภาพ) เป็นออฟเซ็ตจากไหล่ + มุมขา
    "stand":  {"armL": (-130, 170), "armR": (130, 170), "legs": 42},
    "attack": {"armL": (-190, -150), "armR": (150, 140), "legs": 85},
    "cast":   {"armL": (-170, -170), "armR": (170, -170), "legs": 40},
    "guard":  {"armL": (-90, 100), "armR": (110, 60), "legs": 70},
    "ready":  {"armL": (-160, 40), "armR": (160, -120), "legs": 60},
    "crouch": {"armL": (-140, 120), "armR": (150, 90), "legs": 110},
}


def draw_hair(d, kind, cx, cy, r, color):
    """ทรงผม/ของบนหัว ตามลักษณะตัวละคร"""
    if kind == "spiky":
        pts = []
        n = 9
        for i in range(n + 1):
            a = math.pi + (math.pi * i / n)
            rr = r * (1.55 if i % 2 else 1.15)
            pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr * 1.05))
        d.polygon(pts + [(cx + r, cy), (cx - r, cy)], fill=color)
    elif kind == "bowl":
        d.pieslice([cx - r * 1.15, cy - r * 1.2, cx + r * 1.15, cy + r * 1.05], 180, 360, fill=color)
        d.rectangle([cx - r * 1.15, cy - r * 0.2, cx + r * 1.15, cy + r * 0.25], fill=color)
    elif kind == "long":
        d.pieslice([cx - r * 1.2, cy - r * 1.25, cx + r * 1.2, cy + r * 1.05], 180, 360, fill=color)
        d.polygon([(cx - r * 1.2, cy), (cx - r * 1.5, cy + r * 3.4), (cx - r * 0.5, cy + r * 2.6),
                   (cx - r * 0.6, cy)], fill=color)
        d.polygon([(cx + r * 1.2, cy), (cx + r * 1.5, cy + r * 3.4), (cx + r * 0.5, cy + r * 2.6),
                   (cx + r * 0.6, cy)], fill=color)
    elif kind == "hat":
        d.ellipse([cx - r * 1.8, cy - r * 0.75, cx + r * 1.8, cy - r * 0.2], fill=color)
        d.rectangle([cx - r * 0.95, cy - r * 1.4, cx + r * 0.95, cy - r * 0.5], fill=color)
    elif kind == "bighat":
        d.polygon([(cx - r * 1.25, cy - r * 0.2), (cx, cy - r * 2.5), (cx + r * 1.25, cy - r * 0.2)], fill=color)
        d.ellipse([cx - r * 1.6, cy - r * 0.55, cx + r * 1.6, cy - r * 0.05], fill=color)
    elif kind == "horns":
        d.pieslice([cx - r * 1.1, cy - r * 1.15, cx + r * 1.1, cy + r], 180, 360, fill=mul(color, 0.5))
        d.polygon([(cx - r * 0.9, cy - r * 0.6), (cx - r * 1.7, cy - r * 1.9), (cx - r * 0.35, cy - r * 1.05)], fill=color)
        d.polygon([(cx + r * 0.9, cy - r * 0.6), (cx + r * 1.7, cy - r * 1.9), (cx + r * 0.35, cy - r * 1.05)], fill=color)
    elif kind == "ponytail":
        d.pieslice([cx - r * 1.15, cy - r * 1.2, cx + r * 1.15, cy + r * 1.0], 180, 360, fill=color)
        d.polygon([(cx + r * 0.9, cy - r * 0.5), (cx + r * 2.3, cy + r * 0.6),
                   (cx + r * 1.9, cy + r * 2.2), (cx + r * 1.2, cy + r * 0.4)], fill=color)
    elif kind == "twin":
        d.pieslice([cx - r * 1.15, cy - r * 1.2, cx + r * 1.15, cy + r * 1.0], 180, 360, fill=color)
        for s in (-1, 1):
            d.ellipse([cx + s * r * 1.5 - r * 0.55, cy - r * 0.2, cx + s * r * 1.5 + r * 0.55, cy + r * 1.7], fill=color)
    elif kind == "hood":
        d.pieslice([cx - r * 1.45, cy - r * 1.55, cx + r * 1.45, cy + r * 1.4], 180, 360, fill=color)
        d.polygon([(cx - r * 1.45, cy), (cx - r * 1.0, cy + r * 1.7), (cx + r * 1.0, cy + r * 1.7),
                   (cx + r * 1.45, cy)], fill=color)
    elif kind == "helmet":
        d.pieslice([cx - r * 1.2, cy - r * 1.3, cx + r * 1.2, cy + r * 1.1], 180, 360, fill=color)
        d.rectangle([cx - r * 1.2, cy - r * 0.35, cx + r * 1.2, cy - r * 0.12], fill=color)
        d.rectangle([cx - r * 0.1, cy - r * 0.35, cx + r * 0.1, cy + r * 0.45], fill=mul(color, 0.7))
    elif kind == "mohawk":
        d.polygon([(cx - r * 0.3, cy - r * 0.9), (cx, cy - r * 2.0), (cx + r * 0.3, cy - r * 0.9)], fill=color)
        d.pieslice([cx - r * 1.1, cy - r * 1.1, cx + r * 1.1, cy + r], 200, 340, fill=color)
    elif kind == "short":
        d.pieslice([cx - r * 1.12, cy - r * 1.2, cx + r * 1.12, cy + r * 1.0], 180, 360, fill=color)
    elif kind == "sidepart":
        d.pieslice([cx - r * 1.15, cy - r * 1.25, cx + r * 1.15, cy + r * 1.0], 180, 360, fill=color)
        d.polygon([(cx - r * 1.15, cy - r * 0.2), (cx - r * 1.3, cy + r * 1.2), (cx - r * 0.7, cy + r * 0.3)], fill=color)
    # bald = ไม่วาดอะไร


def draw_prop(d, kind, hand, color, scale=1.0, flip=1):
    x, y = hand
    s = scale
    if kind == "sword":
        d.polygon([(x - 9 * s, y), (x + 9 * s, y), (x + 5 * s, y - 330 * s), (x, y - 360 * s), (x - 5 * s, y - 330 * s)],
                  fill=color)
        d.rectangle([x - 36 * s, y - 6 * s, x + 36 * s, y + 8 * s], fill=mul(color, 0.6))
    elif kind == "katana":
        d.polygon([(x - 6 * s, y), (x + 6 * s, y), (x + 14 * s * flip, y - 300 * s), (x + 24 * s * flip, y - 340 * s),
                   (x + 2 * s, y - 300 * s)], fill=color)
        d.ellipse([x - 20 * s, y - 6 * s, x + 20 * s, y + 10 * s], fill=mul(color, 0.5))
    elif kind == "greatsword":
        d.polygon([(x - 20 * s, y), (x + 20 * s, y), (x + 22 * s, y - 330 * s), (x, y - 400 * s), (x - 22 * s, y - 330 * s)],
                  fill=color)
        d.rectangle([x - 48 * s, y - 8 * s, x + 48 * s, y + 12 * s], fill=mul(color, 0.55))
    elif kind == "staff":
        d.rectangle([x - 6 * s, y - 330 * s, x + 6 * s, y + 140 * s], fill=mul(color, 0.7))
        d.ellipse([x - 32 * s, y - 380 * s, x + 32 * s, y - 316 * s], fill=color)
    elif kind == "bow":
        d.arc([x - 40 * s, y - 230 * s, x + 200 * s * flip, y + 230 * s] if flip > 0
              else [x - 200 * s, y - 230 * s, x + 40 * s, y + 230 * s], 270, 90 if flip > 0 else 270,
              fill=color, width=int(10 * s))
        d.line([(x + 80 * s * flip, y - 225 * s), (x + 80 * s * flip, y + 225 * s)], fill=mul(color, 1.2), width=int(3 * s))
    elif kind == "slingshot":
        d.line([(x, y), (x, y - 100 * s)], fill=color, width=int(12 * s))
        d.line([(x, y - 100 * s), (x - 40 * s, y - 170 * s)], fill=color, width=int(12 * s))
        d.line([(x, y - 100 * s), (x + 40 * s, y - 170 * s)], fill=color, width=int(12 * s))
        d.line([(x - 40 * s, y - 170 * s), (x + 40 * s, y - 170 * s)], fill=mul(color, 0.6), width=int(4 * s))
    elif kind == "gun":
        d.rectangle([x - 12 * s, y - 20 * s, x + 130 * s * flip, y + 6 * s] if flip > 0
                    else [x - 130 * s, y - 20 * s, x + 12 * s, y + 6 * s], fill=color)
        d.rectangle([x - 12 * s, y, x + 8 * s, y + 70 * s], fill=mul(color, 0.7))
    elif kind == "shield":
        d.ellipse([x - 110 * s, y - 130 * s, x + 110 * s, y + 130 * s], fill=mul(color, 0.8))
        d.ellipse([x - 82 * s, y - 100 * s, x + 82 * s, y + 100 * s], fill=color)
        d.ellipse([x - 24 * s, y - 24 * s, x + 24 * s, y + 24 * s], fill=mul(color, 0.5))
    elif kind == "tower_shield":
        d.polygon([(x - 90 * s, y - 180 * s), (x + 90 * s, y - 180 * s), (x + 90 * s, y + 60 * s),
                   (x, y + 190 * s), (x - 90 * s, y + 60 * s)], fill=mul(color, 0.85))
        d.polygon([(x - 62 * s, y - 150 * s), (x + 62 * s, y - 150 * s), (x + 62 * s, y + 50 * s),
                   (x, y + 150 * s), (x - 62 * s, y + 50 * s)], fill=color)
        d.line([(x, y - 150 * s), (x, y + 150 * s)], fill=mul(color, 0.5), width=int(8 * s))
    elif kind == "spear":
        d.rectangle([x - 5 * s, y - 360 * s, x + 5 * s, y + 160 * s], fill=mul(color, 0.7))
        d.polygon([(x - 22 * s, y - 360 * s), (x, y - 450 * s), (x + 22 * s, y - 360 * s)], fill=color)
    elif kind == "book":
        d.polygon([(x - 70 * s, y - 40 * s), (x, y - 62 * s), (x + 70 * s, y - 40 * s),
                   (x + 70 * s, y + 70 * s), (x, y + 90 * s), (x - 70 * s, y + 70 * s)], fill=color)
        d.line([(x, y - 62 * s), (x, y + 90 * s)], fill=mul(color, 0.4), width=int(5 * s))
    elif kind == "fan":
        for i in range(-3, 4):
            a = math.radians(-90 + i * 18)
            d.line([(x, y), (x + math.cos(a) * 190 * s, y + math.sin(a) * 190 * s)], fill=color, width=int(14 * s))
    elif kind == "chainsaw":
        d.rectangle([x - 20 * s, y - 80 * s, x + 20 * s, y + 40 * s], fill=mul(color, 0.7))
        d.rectangle([x - 14 * s, y - 330 * s, x + 14 * s, y - 70 * s], fill=color)
        for i in range(9):
            d.polygon([(x - 14 * s, y - 330 * s + i * 28 * s), (x - 32 * s, y - 316 * s + i * 28 * s),
                       (x - 14 * s, y - 302 * s + i * 28 * s)], fill=color)
    elif kind == "claws":
        for i in (-1, 0, 1):
            d.polygon([(x + i * 22 * s - 6 * s, y), (x + i * 22 * s + 6 * s, y), (x + i * 28 * s, y - 120 * s)], fill=color)
    elif kind == "orb":
        d.ellipse([x - 50 * s, y - 50 * s, x + 50 * s, y + 50 * s], fill=color)
    elif kind == "lance":
        d.rectangle([x - 6 * s, y - 520 * s, x + 6 * s, y + 80 * s], fill=mul(color, 0.7))
        d.polygon([(x - 26 * s, y - 520 * s), (x, y - 620 * s), (x + 26 * s, y - 520 * s)], fill=color)
    elif kind == "whip":
        pts = [(x + i * 28 * s * flip, y - 40 * s * math.sin(i / 1.2) - i * 6 * s) for i in range(12)]
        d.line(pts, fill=color, width=int(8 * s))
    elif kind == "dagger":
        d.polygon([(x - 8 * s, y), (x + 8 * s, y), (x, y - 120 * s)], fill=color)


def draw_figure(img, cfg, scene_colors, rng):
    """วาดตัวละครซิลูเอตต์ตรงกลางภาพ พร้อมแสงขอบ"""
    cx = cfg.get("cx", W // 2)
    pose = POSES[cfg.get("pose", "stand")]
    scale = cfg.get("scale", 1.0) * 1.22
    body = cfg.get("body", (40, 40, 60))
    accent = cfg.get("accent", (230, 230, 240))
    skin = cfg.get("skin", (235, 190, 160))
    hair = cfg.get("hair", (40, 30, 30))
    hair_kind = cfg.get("hairstyle", "short")
    prop = cfg.get("prop")
    prop_col = cfg.get("prop_color", (210, 220, 235))
    prop_hand = cfg.get("prop_hand", "R")
    cape = cfg.get("cape")
    width = cfg.get("width", 1.0)
    light = scene_colors[2]

    feet_y = 905
    hip_y = feet_y - 270 * scale
    sh_y = hip_y - 190 * scale
    head_r = 80 * scale
    head_y = sh_y - head_r * 1.35
    sw = 118 * scale * width       # ครึ่งความกว้างไหล่
    hw = 78 * scale * width        # ครึ่งความกว้างเอว

    def build(color_body, color_skin, color_hair, color_accent, color_prop, off=(0, 0), shade=False):
        layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        d = ImageDraw.Draw(layer)
        ox, oy = off

        # เสื้อคลุม
        if cape:
            ccol = color_body if shade else cape
            d.polygon([(cx - sw + ox, sh_y + oy), (cx + sw + ox, sh_y + oy),
                       (cx + sw * 1.9 + ox, feet_y - 20 + oy), (cx - sw * 1.9 + ox, feet_y - 20 + oy)], fill=ccol)

        # ขา
        spread = pose["legs"] * scale
        leg_w = 60 * scale * width
        poly_thick(d, (cx - hw * 0.5 + ox, hip_y + oy), (cx - hw * 0.5 - spread + ox, feet_y + oy), leg_w, color_body)
        poly_thick(d, (cx + hw * 0.5 + ox, hip_y + oy), (cx + hw * 0.5 + spread + ox, feet_y + oy), leg_w, color_body)

        # ลำตัว
        d.polygon([(cx - sw + ox, sh_y + oy), (cx + sw + ox, sh_y + oy), (cx + hw + ox, hip_y + 20 + oy),
                   (cx - hw + ox, hip_y + 20 + oy)], fill=color_body)
        if not shade:
            # ลายเสื้อ
            d.polygon([(cx - 18 * scale + ox, sh_y + oy), (cx + 18 * scale + ox, sh_y + oy),
                       (cx + 12 * scale + ox, hip_y + oy), (cx - 12 * scale + ox, hip_y + oy)], fill=color_accent)
            d.rectangle([cx - hw + ox, hip_y - 14 + oy, cx + hw + ox, hip_y + 18 + oy], fill=color_accent)

        # แขน
        shoulderL = (cx - sw + 14 + ox, sh_y + 24 + oy)
        shoulderR = (cx + sw - 14 + ox, sh_y + 24 + oy)
        handL = (shoulderL[0] + pose["armL"][0] * scale, shoulderL[1] + pose["armL"][1] * scale)
        handR = (shoulderR[0] + pose["armR"][0] * scale, shoulderR[1] + pose["armR"][1] * scale)
        arm_w = 46 * scale * width
        poly_thick(d, shoulderL, handL, arm_w, color_body)
        poly_thick(d, shoulderR, handR, arm_w, color_body)
        for h in (handL, handR):
            r = 26 * scale
            d.ellipse([h[0] - r, h[1] - r, h[0] + r, h[1] + r], fill=color_skin)

        # ของถือ
        if prop:
            hand = handR if prop_hand == "R" else handL
            draw_prop(d, prop, hand, color_prop, scale, 1 if prop_hand == "R" else -1)
        if cfg.get("prop2"):
            hand2 = handL if prop_hand == "R" else handR
            draw_prop(d, cfg["prop2"], hand2, cfg.get("prop2_color", color_prop), scale, -1 if prop_hand == "R" else 1)

        # คอ + หัว
        d.rectangle([cx - 26 * scale + ox, sh_y - 30 * scale + oy, cx + 26 * scale + ox, sh_y + 10 * scale + oy],
                    fill=color_skin)
        d.ellipse([cx - head_r + ox, head_y - head_r * 1.12 + oy, cx + head_r + ox, head_y + head_r * 1.12 + oy],
                  fill=color_skin)
        draw_hair(d, hair_kind, cx + ox, head_y + oy - head_r * 0.12, head_r, color_hair)

        # ส่วนเสริม
        extra = cfg.get("extra")
        if extra:
            extra(d, cx + ox, head_y + oy, head_r, sh_y + oy, scale, color_accent)
        return layer

    # แสงขอบ (rim light) -> เงา -> สีจริง
    rim = build(mul(light, 0.9), mul(light, 0.9), mul(light, 0.9), mul(light, 0.9), mul(light, 0.9), off=(-9, -4), shade=True)
    img = paste_rgba(img, rim.filter(ImageFilter.GaussianBlur(3)))

    shadow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ds = ImageDraw.Draw(shadow)
    ds.ellipse([cx - 260 * scale, feet_y - 30, cx + 260 * scale, feet_y + 40], fill=(0, 0, 0, 150))
    img = paste_rgba(img, shadow.filter(ImageFilter.GaussianBlur(14)))

    main = build(body, skin, hair, accent, prop_col)
    img = paste_rgba(img, main)

    # เติมเงาไล่โทนด้านล่างตัว + ตา/หน้าเล็กน้อย
    face = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    df = ImageDraw.Draw(face)
    eye_col = cfg.get("eye", (30, 25, 30))
    for sx in (-1, 1):
        ex = cx + sx * head_r * 0.38
        ey = head_y + head_r * 0.05
        df.ellipse([ex - 8 * scale, ey - 12 * scale, ex + 8 * scale, ey + 12 * scale], fill=eye_col + (255,))
    mouth = cfg.get("mouth", "smile")
    if mouth == "smile":
        df.arc([cx - 22 * scale, head_y + head_r * 0.35, cx + 22 * scale, head_y + head_r * 0.75], 15, 165,
               fill=(120, 60, 60, 230), width=max(2, int(5 * scale)))
    elif mouth == "grin":
        df.chord([cx - 28 * scale, head_y + head_r * 0.28, cx + 28 * scale, head_y + head_r * 0.82], 0, 180,
                 fill=(150, 50, 60, 240))
    elif mouth == "stern":
        df.line([(cx - 20 * scale, head_y + head_r * 0.55), (cx + 20 * scale, head_y + head_r * 0.55)],
                fill=(110, 60, 60, 230), width=max(2, int(5 * scale)))
    img = paste_rgba(img, face)
    return img


# ---------------------------------------------------------------- เอฟเฟกต์เวทมนตร์

def lightning_path(rng, start, end, jag=60, segments=14):
    pts = [start]
    for i in range(1, segments):
        t = i / segments
        x = start[0] + (end[0] - start[0]) * t + rng.uniform(-jag, jag)
        y = start[1] + (end[1] - start[1]) * t + rng.uniform(-jag * 0.3, jag * 0.3)
        pts.append((x, y))
    pts.append(end)
    return pts


def glowing_poly(img, points_fn, color, width=14, blur=14, closed=False):
    def fn(d):
        points_fn(d, color + (255,), width)

    big = blur_layer(fn, blur)
    img = add_layer(img, big)
    sharp = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ds = ImageDraw.Draw(sharp)
    points_fn(ds, (255, 255, 255, 255), max(2, width // 3))
    return paste_rgba(img, sharp)


def flame_poly(x, base_y, height, width, rng, sway=0.25, steps=26):
    """รูปเปลวไฟโค้งอ่อน ปลายแหลม"""
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


def flame_stack(img, x, base_y, height, width, rng, colors=((255, 90, 10), (255, 170, 40), (255, 245, 190))):
    for k, col in enumerate(colors):
        k_scale = 1 - 0.28 * k
        poly = flame_poly(x, base_y, height * (1 - 0.12 * k), width * k_scale, rng)
        if k == 0:
            img = add_layer(img, blur_layer(lambda d, p=poly, c=col: d.polygon(p, fill=c + (255,)), 22))
        l2 = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        ImageDraw.Draw(l2).polygon(poly, fill=col + (255,))
        img = paste_rgba(img, l2.filter(ImageFilter.GaussianBlur(3)))
    return img


def spell_art(kind, rng):
    cfgs = {
        "spark": "storm", "flame": "ember", "frost": "ice", "rain": "sea", "gale": "forest",
        "wall": "desert", "notes": "temple", "chain": "storm", "bind": "abyss", "dawn": "dawn",
        "break": "tech", "ward": "tech", "inferno": "ember", "rewind": "cosmos", "end": "blood",
    }
    img = background(cfgs[kind], rng)
    light = SCENES[cfgs[kind]][2]
    cx, cy = W // 2, 470

    if kind == "spark":
        for k in range(4):
            start = (cx + rng.uniform(-200, 200), 40)
            end = (cx + rng.uniform(-140, 140), 800)
            pts = lightning_path(rng, start, end, 70, 12)
            img = glowing_poly(img, lambda d, c, w, p=pts: d.line(p, fill=c, width=w, joint="curve"),
                               (255, 240, 120), 34 if k == 0 else 20, 18)
        img = glow(img, (cx, 800), 380, (255, 240, 140), 0.9)
    elif kind == "flame":
        for k, (ox, h, wd) in enumerate([(cx - 230, 420, 120), (cx + 230, 460, 125), (cx, 640, 190)]):
            img = flame_stack(img, ox, 860, h, wd, rng)
        img = glow(img, (cx, 560), 520, (255, 140, 40), 0.9)
        img = sparkles(img, rng, (255, 220, 120), 90, region=(250, 150, 1030, 800), size=(2, 6))
    elif kind == "frost":
        for k in range(7):
            a = math.radians(-90 + (k - 3) * 17)
            L = 420 + rng.uniform(-60, 90)
            bx, by = cx + (k - 3) * 70, 880
            tip = (bx + math.cos(a) * L * 0.35, by - L)
            def cr(d, c, w, bx=bx, by=by, tip=tip):
                d.polygon([(bx - 58, by), (bx + 58, by), tip], fill=c)
            img = add_layer(img, blur_layer(lambda d, f=cr: f(d, (150, 220, 255, 200), 0), 16))
            l2 = Image.new("RGBA", (W, H), (0, 0, 0, 0))
            cr(ImageDraw.Draw(l2), (210, 240, 255, 255), 0)
            img = paste_rgba(img, l2.filter(ImageFilter.GaussianBlur(1.5)))
        img = sparkles(img, rng, (230, 250, 255), 110, size=(2, 6))
    elif kind == "rain":
        for _ in range(130):
            x = rng.uniform(0, W)
            y = rng.uniform(0, 860)
            l = rng.uniform(40, 110)
            def dr(d, c, w, x=x, y=y, l=l):
                d.line([(x, y), (x - l * 0.25, y + l)], fill=c, width=w)
            img = glowing_poly(img, dr, (150, 220, 255), 6, 3)
        img = glow(img, (cx, 760), 520, (120, 200, 255), 0.7)
        # ใบไม้/แสงรักษา
        img = sparkles(img, rng, (190, 255, 210), 70, size=(3, 8))
    elif kind == "gale":
        for k in range(5):
            y0 = 260 + k * 110
            def sl(d, c, w, y0=y0, k=k):
                pts = [(100 + k * 40 + i * 60, y0 + 70 * math.sin(i / 2.3 + k)) for i in range(18)]
                d.line(pts, fill=c, width=w, joint="curve")
            img = glowing_poly(img, sl, (190, 255, 210), 18, 12)
        img = sparkles(img, rng, (220, 255, 230), 60)
    elif kind == "wall":
        for k in range(5):
            x0 = 240 + k * 160
            h = 380 + rng.uniform(-60, 70)
            l = Image.new("RGBA", (W, H), (0, 0, 0, 0))
            dl = ImageDraw.Draw(l)
            dl.polygon([(x0, 880), (x0 + 30, 880 - h), (x0 + 140, 880 - h - 30), (x0 + 160, 880)],
                       fill=(140 + k * 8, 105, 70, 255))
            dl.polygon([(x0 + 30, 880 - h), (x0 + 140, 880 - h - 30), (x0 + 120, 880 - h + 40)],
                       fill=(190, 150, 100, 255))
            img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(1.5)))
        img = glow(img, (cx, 880), 500, (255, 200, 120), 0.5)
        img = sparkles(img, rng, (255, 225, 170), 60, region=(150, 500, 1100, 880))
    elif kind == "notes":
        l = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        dl = ImageDraw.Draw(l)
        # ปกหนังสือ + หน้ากระดาษสองข้างแบบเปิดอ่าน
        dl.polygon([(cx - 400, 560), (cx, 640), (cx + 400, 560), (cx + 400, 600), (cx, 690), (cx - 400, 600)],
                   fill=(110, 60, 45, 255))
        dl.polygon([(cx - 380, 330), (cx - 10, 400), (cx - 10, 640), (cx - 380, 560)], fill=(250, 238, 205, 255))
        dl.polygon([(cx + 380, 330), (cx + 10, 400), (cx + 10, 640), (cx + 380, 560)], fill=(245, 230, 195, 255))
        for i in range(5):
            dl.line([(cx - 330, 380 + i * 36), (cx - 60, 430 + i * 36)], fill=(150, 110, 80, 255), width=6)
            dl.line([(cx + 330, 380 + i * 36), (cx + 60, 430 + i * 36)], fill=(150, 110, 80, 255), width=6)
        img = paste_rgba(img, l)
        img = glow(img, (cx, 470), 520, (255, 240, 170), 0.9)
        img = sparkles(img, rng, (255, 245, 190), 90, region=(200, 100, 1100, 650), size=(3, 9))
    elif kind == "chain":
        for k in range(3):
            start = (260 + k * 380, 60)
            end = (330 + k * 300, 760)
            pts = lightning_path(rng, start, end, 80, 11)
            img = glowing_poly(img, lambda d, c, w, p=pts: d.line(p, fill=c, width=w, joint="curve"),
                               (160, 190, 255), 28, 18)
        for (x, y) in [(330, 760), (630, 760), (930, 760)]:
            img = glow(img, (x, y), 250, (150, 190, 255), 0.9)
    elif kind == "bind":
        for k in range(7):
            def ch(d, c, w, k=k):
                a0 = 40 + k * 22
                d.arc([cx - 360 + k * 12, 160 + k * 12, cx + 360 - k * 12, 840 - k * 12], a0, a0 + 170, fill=c, width=w)
            img = glowing_poly(img, ch, (190, 100, 255), 24, 14)
        l = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        dl = ImageDraw.Draw(l)
        dl.ellipse([cx - 90, 380, cx + 90, 560], fill=(8, 3, 14, 255))
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(3)))
        img = glow(img, (cx, 470), 330, (160, 80, 255), 0.8)
    elif kind == "dawn":
        img = rays(img, (cx, 470), (255, 230, 150), 22, 0.28, rng)
        img = glow(img, (cx, 470), 460, (255, 230, 150), 1.0)
        for r in (180, 260, 340):
            def rg(d, c, w, r=r):
                d.ellipse([cx - r, 470 - r, cx + r, 470 + r], outline=c, width=w)
            img = glowing_poly(img, rg, (255, 235, 160), 10, 8)
        img = sparkles(img, rng, (255, 250, 210), 100, size=(2, 8))
    elif kind == "break":
        for r in (120, 220, 320):
            def rg(d, c, w, r=r):
                d.ellipse([cx - r, 470 - r, cx + r, 470 + r], outline=c, width=w)
            img = glowing_poly(img, rg, (110, 220, 255), 10, 9)
        for k in range(8):
            a = k * math.tau / 8
            def spk(d, c, w, a=a):
                d.line([(cx + math.cos(a) * 120, 470 + math.sin(a) * 120), (cx + math.cos(a) * 320, 470 + math.sin(a) * 320)],
                       fill=c, width=w)
            img = glowing_poly(img, spk, (110, 220, 255), 8, 7)
        def xx(d, c, w):
            d.line([(cx - 90, 380), (cx + 90, 560)], fill=c, width=w)
            d.line([(cx + 90, 380), (cx - 90, 560)], fill=c, width=w)
        img = glowing_poly(img, xx, (255, 120, 120), 24, 12)
    elif kind == "ward":
        l = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        dl = ImageDraw.Draw(l)
        dl.ellipse([cx - 280, 190, cx + 280, 750], fill=(120, 220, 255, 70), outline=(190, 245, 255, 255), width=10)
        dl.ellipse([cx - 220, 250, cx + 220, 690], outline=(190, 245, 255, 150), width=5)
        for k in range(6):
            a = math.radians(30 + k * 60)
            pts = [(cx + math.cos(a + j * 0.35) * 250, 470 + math.sin(a + j * 0.35) * 250) for j in range(2)]
            dl.line(pts, fill=(255, 255, 255, 220), width=6)
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(2)))
        img = glow(img, (cx, 470), 420, (120, 220, 255), 0.7)
        img = sparkles(img, rng, (200, 245, 255), 70, region=(300, 220, 980, 720))
    elif kind == "inferno":
        for k in range(8):
            ox = 110 + k * 150 + rng.uniform(-30, 30)
            h = 420 + rng.uniform(0, 330)
            img = flame_stack(img, ox, 900, h, 120 + rng.uniform(0, 50), rng)
        img = glow(img, (cx, 620), 700, (255, 120, 30), 1.0)
        img = sparkles(img, rng, (255, 220, 120), 150, region=(0, 100, W, 700), size=(2, 7))
    elif kind == "rewind":
        for r in (300, 220):
            def rg(d, c, w, r=r):
                d.ellipse([cx - r, 470 - r, cx + r, 470 + r], outline=c, width=w)
            img = glowing_poly(img, rg, (200, 150, 255), 16, 12)
        for k in range(12):
            a = k * math.tau / 12
            def tick(d, c, w, a=a):
                d.line([(cx + math.cos(a) * 262, 470 + math.sin(a) * 262), (cx + math.cos(a) * 292, 470 + math.sin(a) * 292)],
                       fill=c, width=w)
            img = glowing_poly(img, tick, (230, 200, 255), 12, 6)
        def hands(d, c, w):
            d.line([(cx, 470), (cx + 10, 470 - 190)], fill=c, width=w)
            d.line([(cx, 470), (cx - 130, 470 + 40)], fill=c, width=w)
        img = glowing_poly(img, hands, (255, 230, 255), 22, 10)
        def arrow(d, c, w):
            d.arc([cx - 360, 110, cx + 360, 830], 200, 330, fill=c, width=w)
            d.polygon([(cx - 330, 330), (cx - 395, 255), (cx - 250, 275)], fill=c)
        img = glowing_poly(img, arrow, (210, 170, 255), 22, 12)
        img = sparkles(img, rng, (240, 220, 255), 100)
    elif kind == "end":
        l = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        dl = ImageDraw.Draw(l)
        dl.ellipse([cx - 260, 210, cx + 260, 730], fill=(10, 0, 8, 255))
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(2)))
        img = glow(img, (cx, 470), 520, (255, 50, 70), 1.0)
        for k in range(10):
            a = k * math.tau / 10 + 0.2
            pts = lightning_path(rng, (cx + math.cos(a) * 250, 470 + math.sin(a) * 250),
                                 (cx + math.cos(a) * 560, 470 + math.sin(a) * 440), 40, 6)
            img = glowing_poly(img, lambda d, c, w, p=pts: d.line(p, fill=c, width=w, joint="curve"),
                               (255, 70, 90), 12, 12)
        img = sparkles(img, rng, (255, 170, 170), 120)
    return img


# ---------------------------------------------------------------- รายการการ์ด

def _horn_extra(d, hx, hy, r, sh_y, s, acc):
    pass


def _glasses(d, hx, hy, r, sh_y, s, acc):
    for sx in (-1, 1):
        ex = hx + sx * r * 0.38
        d.ellipse([ex - 20 * s, hy - 18 * s, ex + 20 * s, hy + 22 * s], outline=(30, 30, 50, 255), width=max(2, int(5 * s)))
    d.line([(hx - 20 * s, hy + 2 * s), (hx + 20 * s, hy + 2 * s)], fill=(30, 30, 50, 255), width=max(2, int(4 * s)))


def _long_nose(d, hx, hy, r, sh_y, s, acc):
    d.polygon([(hx, hy), (hx + r * 1.75, hy + r * 0.28), (hx, hy + r * 0.38)], fill=(235, 190, 160, 255))
    d.line([(hx - r * 0.9, hy - r * 0.75), (hx + r * 0.9, hy - r * 0.75)], fill=(240, 230, 120, 255), width=max(2, int(8 * s)))


def _dots(d, hx, hy, r, sh_y, s, acc):
    for sx in (-1, 1):
        d.ellipse([hx + sx * r * 0.2 - 6 * s, hy - r * 0.55 - 6 * s, hx + sx * r * 0.2 + 6 * s, hy - r * 0.55 + 6 * s],
                  fill=(60, 40, 40, 255))


def _scarf(d, hx, hy, r, sh_y, s, acc):
    d.polygon([(hx - r * 1.4, sh_y - 30 * s), (hx + r * 1.4, sh_y - 30 * s), (hx + r * 1.2, sh_y + 40 * s),
               (hx - r * 1.2, sh_y + 40 * s)], fill=acc + (255,))


def _headband(d, hx, hy, r, sh_y, s, acc):
    d.rectangle([hx - r * 1.1, hy - r * 0.5, hx + r * 1.1, hy - r * 0.2], fill=acc + (255,))


def _fangs(d, hx, hy, r, sh_y, s, acc):
    for sx in (-1, 1):
        d.polygon([(hx + sx * 14 * s, hy + r * 0.45), (hx + sx * 24 * s, hy + r * 0.45), (hx + sx * 19 * s, hy + r * 0.8)],
                  fill=(255, 255, 255, 255))


def _crown(d, hx, hy, r, sh_y, s, acc):
    d.polygon([(hx - r * 0.9, hy - r * 1.0), (hx - r * 0.7, hy - r * 1.8), (hx - r * 0.25, hy - r * 1.3),
               (hx, hy - r * 1.95), (hx + r * 0.25, hy - r * 1.3), (hx + r * 0.7, hy - r * 1.8),
               (hx + r * 0.9, hy - r * 1.0)], fill=acc + (255,))


def _tattoo(d, hx, hy, r, sh_y, s, acc):
    for sx in (-1, 1):
        d.line([(hx + sx * r * 0.6, hy - r * 0.2), (hx + sx * r * 0.6, hy + r * 0.5)], fill=(190, 40, 40, 255), width=max(2, int(5 * s)))


def _visor(d, hx, hy, r, sh_y, s, acc):
    d.rectangle([hx - r * 1.0, hy - r * 0.25, hx + r * 1.0, hy + r * 0.15], fill=(120, 230, 255, 235))


def _gourd(d, hx, hy, r, sh_y, s, acc):
    d.ellipse([hx + r * 0.9, sh_y - 10 * s, hx + r * 1.9, sh_y + 190 * s], fill=(200, 160, 90, 255))
    d.ellipse([hx + r * 1.15, sh_y - 55 * s, hx + r * 1.65, sh_y + 15 * s], fill=(200, 160, 90, 255))


def _halo(d, hx, hy, r, sh_y, s, acc):
    d.ellipse([hx - r * 1.2, hy - r * 2.0, hx + r * 1.2, hy - r * 1.55], outline=acc + (255,), width=max(3, int(8 * s)))


def _flame_hair(d, hx, hy, r, sh_y, s, acc):
    for i in range(-2, 3):
        d.polygon([(hx + i * r * 0.4 - r * 0.25, hy - r * 0.9), (hx + i * r * 0.45, hy - r * (1.7 + 0.2 * (2 - abs(i)))),
                   (hx + i * r * 0.4 + r * 0.25, hy - r * 0.9)], fill=(255, 150, 40, 255))


UNITS = {
    # id: dict(scene, pose, body, accent, hair, hairstyle, prop, ...)
    "SUP_001": dict(scene="dawn", pose="attack", body=(70, 90, 140), accent=(240, 140, 40), hair=(35, 30, 30),
                    hairstyle="spiky", prop="dagger", prop_color=(210, 215, 225), scale=0.82, extra=_scarf, mouth="grin"),
    "SUP_002": dict(scene="sunset", pose="attack", body=(40, 160, 90), accent=(250, 160, 40), hair=(15, 15, 18),
                    hairstyle="bowl", scale=0.9, mouth="grin"),
    "SUP_003": dict(scene="sea", pose="ready", body=(190, 150, 70), accent=(90, 60, 40), hair=(25, 20, 25),
                    hairstyle="short", prop="slingshot", prop_color=(120, 80, 50), scale=0.92, extra=_long_nose),
    "SUP_004": dict(scene="forest", pose="cast", body=(210, 70, 80), accent=(255, 235, 235), hair=(120, 80, 50),
                    hairstyle="hat", skin=(200, 150, 110), scale=0.68, width=1.25, mouth="grin"),
    "SUP_005": dict(scene="temple", pose="cast", body=(230, 120, 40), accent=(30, 60, 140), hair=(0, 0, 0),
                    hairstyle="bald", extra=_dots, scale=0.9, mouth="grin"),
    "SUP_006": dict(scene="ruins", pose="attack", body=(120, 105, 80), accent=(70, 110, 70), hair=(70, 40, 30),
                    hairstyle="short", prop="katana", prop_color=(210, 215, 225), scale=0.86),
    "SUP_007": dict(scene="forest", pose="ready", body=(105, 95, 75), accent=(70, 110, 70), hair=(110, 70, 40),
                    hairstyle="ponytail", prop="bow", prop_color=(190, 150, 90), scale=0.9, mouth="grin"),
    "SUP_008": dict(scene="ice", pose="guard", body=(255, 120, 190), accent=(255, 230, 245), hair=(255, 160, 210),
                    hairstyle="twin", prop="shield", prop_color=(180, 230, 255), prop_hand="L", scale=0.88, mouth="grin"),
    "SUP_009": dict(scene="ember", pose="ready", body=(200, 50, 50), accent=(250, 220, 120), hair=(120, 30, 30),
                    hairstyle="ponytail", prop="gun", prop_color=(220, 220, 230), scale=0.92),
    "SUP_010": dict(scene="night", pose="guard", body=(35, 40, 70), accent=(235, 235, 240), hair=(30, 25, 35),
                    hairstyle="short", scale=0.84, mouth="stern"),
    "SUP_011": dict(scene="ruins", pose="guard", body=(110, 100, 90), accent=(180, 160, 120), hair=(125, 120, 130),
                    hairstyle="helmet", prop="tower_shield", prop_color=(170, 150, 120), prop_hand="L",
                    skin=(150, 140, 130), scale=0.95, width=1.1, mouth="stern"),
    "SUP_012": dict(scene="storm", pose="cast", body=(40, 130, 190), accent=(250, 245, 230), hair=(255, 140, 40),
                    hairstyle="long", prop="orb", prop_color=(255, 245, 140), scale=0.92),
    "SUP_013": dict(scene="night", pose="ready", body=(235, 240, 250), accent=(60, 120, 220), hair=(25, 30, 55),
                    hairstyle="sidepart", prop="bow", prop_color=(120, 210, 255), extra=_glasses, scale=0.92, mouth="stern"),
    "SUP_014": dict(scene="sunset", pose="attack", body=(70, 60, 110), accent=(240, 160, 70), hair=(150, 70, 40),
                    hairstyle="short", prop="dagger", prop_color=(210, 215, 225), prop2="dagger", scale=0.92, mouth="grin"),
    "SUP_015": dict(scene="cosmos", pose="guard", body=(70, 70, 130), accent=(240, 200, 255), hair=(200, 150, 220),
                    hairstyle="sidepart", prop="tower_shield", prop_color=(220, 235, 255), prop_hand="L", scale=0.95),
    "SUP_016": dict(scene="storm", pose="crouch", body=(250, 200, 50), accent=(230, 120, 40), hair=(255, 210, 60),
                    hairstyle="short", prop="katana", prop_color=(250, 250, 255), scale=0.9),
    "SUP_017": dict(scene="blood", pose="attack", body=(60, 40, 80), accent=(220, 70, 90), hair=(255, 235, 170),
                    hairstyle="horns", prop="claws", prop_color=(240, 240, 250), scale=0.94, extra=_fangs, mouth="grin"),
    "SUP_018": dict(scene="ember", pose="cast", body=(35, 35, 50), accent=(220, 60, 60), hair=(35, 25, 40),
                    hairstyle="short", prop="gun", prop_color=(230, 230, 240), prop2="gun", scale=0.98, mouth="stern"),
    "SUP_019": dict(scene="temple", pose="ready", body=(190, 190, 205), accent=(240, 200, 80), hair=(200, 200, 215),
                    hairstyle="helmet", prop="sword", prop_color=(245, 230, 170), prop2="shield", prop2_color=(240, 200, 80),
                    scale=0.98, width=1.1, cape=(240, 220, 150)),
    "SUP_020": dict(scene="night", pose="attack", body=(235, 235, 245), accent=(90, 140, 230), hair=(240, 200, 120),
                    hairstyle="ponytail", prop="sword", prop_color=(210, 230, 255), scale=0.96),
    "SUP_021": dict(scene="sunset", pose="ready", body=(110, 120, 150), accent=(200, 200, 220), hair=(235, 235, 245),
                    hairstyle="mohawk", prop="sword", prop_color=(230, 235, 245), scale=0.96, cape=(70, 70, 120)),
    "SUP_022": dict(scene="abyss", pose="attack", body=(130, 20, 50), accent=(255, 70, 90), hair=(50, 12, 30),
                    hairstyle="helmet", prop="greatsword", prop_color=(255, 90, 100), skin=(70, 20, 40), scale=1.05,
                    width=1.2, cape=(90, 10, 40), eye=(255, 200, 200)),
    "SUP_023": dict(scene="abyss", pose="guard", body=(245, 245, 255), accent=(200, 160, 255), hair=(15, 10, 25),
                    hairstyle="horns", prop="shield", prop_color=(255, 235, 170), prop_hand="L", skin=(240, 225, 230),
                    scale=1.0, mouth="smile", extra=_halo),
    "SUP_024": dict(scene="blood", pose="cast", body=(70, 20, 50), accent=(255, 80, 120), hair=(240, 240, 250),
                    hairstyle="twin", prop="lance", prop_color=(255, 120, 150), skin=(245, 225, 230), scale=1.0,
                    cape=(100, 10, 50), mouth="grin", extra=_fangs, eye=(255, 60, 90)),
    "SUP_025": dict(scene="ember", pose="attack", body=(250, 250, 245), accent=(240, 120, 40), hair=(255, 200, 60),
                    hairstyle="spiky", prop="katana", prop_color=(255, 190, 90), scale=1.05, width=1.15,
                    cape=(255, 120, 40), mouth="grin", extra=_flame_hair),
    "SUP_026": dict(scene="desert", pose="cast", body=(180, 160, 130), accent=(120, 90, 60), hair=(190, 60, 50),
                    hairstyle="short", scale=1.02, extra=_gourd, mouth="stern", prop="orb", prop_color=(220, 190, 120)),
}

SPELLS = {
    "MAG_001": "spark", "MAG_002": "frost", "MAG_003": "rain", "MAG_004": "gale", "MAG_005": "wall",
    "MAG_006": "notes", "MAG_007": "chain", "MAG_008": "bind", "MAG_009": "dawn", "MAG_010": "break",
    "MAG_011": "ward", "MAG_012": "inferno", "MAG_013": "rewind", "MAG_014": "end", "MAG_015": "flame",
}


def render_unit(card_id, cfg):
    rng = random.Random(card_id)
    img = background(cfg["scene"], rng)
    scene_colors = SCENES[cfg["scene"]]
    img = draw_figure(img, cfg, scene_colors, rng)
    img = sparkles(img, rng, scene_colors[2], 55, size=(2, 6))
    return vignette(img)


def render_spell(card_id, kind):
    rng = random.Random(card_id)
    img = spell_art(kind, rng)
    img = sparkles(img, rng, (255, 255, 255), 40, size=(2, 5))
    return vignette(img, 0.5)


def save(img, card_id):
    os.makedirs(OUT_DIR, exist_ok=True)
    out = img.resize((OUT_W, OUT_H), Image.LANCZOS)
    out.save(os.path.join(OUT_DIR, card_id + ".png"), optimize=True)


def main():
    only = [a for a in sys.argv[1:] if not a.startswith("--")]
    keep = "--keep" in sys.argv

    for cid, cfg in UNITS.items():
        if only and cid not in only:
            continue
        if keep and os.path.exists(os.path.join(OUT_DIR, cid + ".png")):
            continue
        save(render_unit(cid, cfg), cid)
        print("unit", cid)

    for cid, kind in SPELLS.items():
        if only and cid not in only:
            continue
        if keep and os.path.exists(os.path.join(OUT_DIR, cid + ".png")):
            continue
        save(render_spell(cid, kind), cid)
        print("spell", cid)


if __name__ == "__main__":
    main()

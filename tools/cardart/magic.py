"""เวทมนตร์และอุปกรณ์: ภาพประกอบพลังเวทแบบวงหมุน/รอยปาด คล้ายภาพการ์ดอนิเมะ"""
import math
import random

from PIL import Image, ImageDraw, ImageFilter

from .fx import (W, H, add_layer, blur_layer, bokeh, draw_swoosh, flame_stack, glow, glow_line, gradient,
                 lightning_path, mix, mul, new_layer, paste_rgba, petals, rays, shards, sparkles, speed_lines,
                 swoosh_polygon, vignette, vortex)

CX, CY = W // 2, 470


def magic_bg(top, bottom, light, rng, bokeh_colors=None):
    img = gradient(top, bottom)
    img = glow(img, (CX, CY), 700, light, 0.55)
    img = bokeh(img, rng, bokeh_colors or [light, mix(light, (255, 255, 255), 0.5)], 22)
    return img


def ring(img, center, r, color, width=10, blur=8, squash=1.0):
    def fn(d):
        d.ellipse([center[0] - r, center[1] - r * squash, center[0] + r, center[1] + r * squash],
                  outline=color + (255,), width=width)
    img = add_layer(img, blur_layer(fn, blur))
    l = new_layer()
    fn(ImageDraw.Draw(l))
    return paste_rgba(img, l)


def rune_circle(img, center, r, color, rng, ticks=24):
    img = ring(img, center, r, color, 8, 6)
    img = ring(img, center, r * 0.82, color, 4, 4)
    l = new_layer()
    d = ImageDraw.Draw(l)
    for i in range(ticks):
        a = i / ticks * math.tau
        r0, r1 = r * 0.84, r * 0.97
        d.line([(center[0] + math.cos(a) * r0, center[1] + math.sin(a) * r0),
                (center[0] + math.cos(a) * r1, center[1] + math.sin(a) * r1)], fill=color + (255,), width=5)
    # รูปสามเหลี่ยมซ้อน (ลายเวท)
    for off in (0, math.pi / 3):
        pts = [(center[0] + math.cos(off + k * math.tau / 3 - math.pi / 2) * r * 0.8,
                center[1] + math.sin(off + k * math.tau / 3 - math.pi / 2) * r * 0.8) for k in range(3)]
        d.polygon(pts, outline=color + (255,), width=5)
    return paste_rgba(add_layer(img, l.filter(ImageFilter.GaussianBlur(6))), l)


def orb(img, center, r, color, core=(255, 255, 255)):
    img = glow(img, center, r * 3.2, color, 0.9)
    l = new_layer()
    d = ImageDraw.Draw(l)
    d.ellipse([center[0] - r, center[1] - r, center[0] + r, center[1] + r], fill=color + (255,))
    d.ellipse([center[0] - r * 0.66, center[1] - r * 0.66, center[0] + r * 0.66, center[1] + r * 0.66],
              fill=mix(color, core, 0.6) + (255,))
    d.ellipse([center[0] - r * 0.32, center[1] - r * 0.32, center[0] + r * 0.32, center[1] + r * 0.32],
              fill=core + (255,))
    return paste_rgba(img, l.filter(ImageFilter.GaussianBlur(2)))


def hexagon_grid(img, center, r, color, alpha=120):
    l = new_layer()
    d = ImageDraw.Draw(l)
    hs = 70
    for row in range(-6, 7):
        for col in range(-8, 9):
            x = center[0] + col * hs * 1.5
            y = center[1] + row * hs * 1.732 + (hs * 0.866 if col % 2 else 0)
            if (x - center[0]) ** 2 + (y - center[1]) ** 2 > r * r:
                continue
            pts = [(x + math.cos(k * math.pi / 3) * hs * 0.92, y + math.sin(k * math.pi / 3) * hs * 0.92)
                   for k in range(6)]
            d.polygon(pts, outline=color + (alpha,), width=4, fill=color + (alpha // 5,))
    return paste_rgba(img, l)


# ---------------------------------------------------------------- เวทมนตร์

def spell_art(kind, rng):
    if kind == "spark":
        img = magic_bg((14, 18, 56), (70, 60, 150), (150, 170, 255), rng)
        img = vortex(img, (CX, CY), [(255, 236, 110), (120, 200, 255), (200, 170, 255)], rng, count=12,
                     rmin=160, rmax=600, outline=(10, 10, 40))
        for k in range(4):
            start = (CX + rng.uniform(-220, 220), 20)
            end = (CX + rng.uniform(-120, 120), 780)
            img = glow_line(img, lightning_path(rng, start, end, 80, 12), (255, 240, 120),
                            34 if k == 0 else 20, 20)
        img = glow(img, (CX, 780), 420, (255, 240, 140), 0.9)
        img = speed_lines(img, (CX, 780), (255, 245, 170), rng, 50)
    elif kind == "flame":
        img = magic_bg((70, 15, 15), (160, 45, 25), (255, 160, 60), rng)
        img = vortex(img, (CX, CY + 40), [(255, 120, 30), (255, 200, 70), (255, 70, 40)], rng, count=14,
                     rmin=170, rmax=620, outline=(60, 5, 5), width=(50, 150))
        img = flame_stack(img, CX, 1010, 770, 210, rng, outline=(70, 8, 5))
        img = flame_stack(img, CX - 260, 1010, 520, 120, rng, outline=(70, 8, 5))
        img = flame_stack(img, CX + 270, 1010, 560, 125, rng, outline=(70, 8, 5))
        img = sparkles(img, rng, (255, 225, 130), 100, region=(200, 120, 1080, 800), size=(2, 7))
    elif kind == "frost":
        img = magic_bg((10, 30, 90), (60, 130, 220), (190, 235, 255), rng)
        img = vortex(img, (CX, CY), [(180, 235, 255), (90, 160, 255), (255, 255, 255)], rng, count=15,
                     rmin=150, rmax=640, outline=(10, 30, 80))
        img = shards(img, (CX, CY + 40), rng, (200, 240, 255), 11, 80, 430, (50, 120), edge=(255, 255, 255),
                     outline=(10, 40, 100))
        img = sparkles(img, rng, (235, 250, 255), 120, size=(2, 7))
    elif kind == "rain":
        img = magic_bg((8, 40, 90), (30, 120, 190), (140, 220, 255), rng)
        img = vortex(img, (CX, CY), [(110, 200, 255), (60, 140, 235), (200, 245, 255)], rng, count=13,
                     rmin=160, rmax=600, outline=(5, 35, 80))
        l = new_layer()
        d = ImageDraw.Draw(l)
        for _ in range(110):
            x = rng.uniform(0, W)
            y = rng.uniform(0, 850)
            ln = rng.uniform(50, 130)
            d.line([(x, y), (x - ln * 0.25, y + ln)], fill=(190, 235, 255, rng.randint(110, 220)), width=5)
        img = add_layer(img, l.filter(ImageFilter.GaussianBlur(1.5)))
        img = orb(img, (CX, CY), 85, (120, 235, 200))
        img = petals(img, rng, (170, 255, 210), 36)
    elif kind == "gale":
        img = magic_bg((10, 60, 55), (60, 150, 110), (190, 255, 215), rng)
        img = vortex(img, (CX, CY), [(200, 255, 225), (110, 220, 160), (255, 255, 255)], rng, count=17,
                     rmin=130, rmax=660, width=(30, 100), outline=(10, 70, 50), spin=1.3)
        img = petals(img, rng, (150, 235, 150), 55)
        img = speed_lines(img, (CX, CY), (230, 255, 235), rng, 60, inner=200)
    elif kind == "wall":
        img = magic_bg((50, 25, 40), (140, 80, 60), (255, 190, 110), rng)
        img = vortex(img, (CX, CY), [(255, 200, 120), (190, 120, 70)], rng, count=7, rmin=250, rmax=600,
                     outline=(40, 15, 10))
        img = shards(img, (CX, CY + 60), rng, (170, 110, 70), 13, 60, 380, (80, 190), edge=(255, 220, 160),
                     outline=(35, 15, 10))
        img = glow(img, (CX, CY + 160), 480, (255, 190, 110), 0.55)
        img = sparkles(img, rng, (255, 230, 170), 70, size=(2, 6))
    elif kind == "notes":
        img = magic_bg((25, 20, 70), (90, 60, 130), (255, 225, 140), rng)
        img = rune_circle(img, (CX, 470), 400, (255, 225, 140), rng)
        img = vortex(img, (CX, 470), [(255, 225, 140), (255, 255, 235)], rng, count=8, rmin=250, rmax=560,
                     outline=(40, 25, 70), width=(30, 90))
        l = new_layer()
        d = ImageDraw.Draw(l)
        d.polygon([(CX - 400, 560), (CX, 650), (CX + 400, 560), (CX + 400, 605), (CX, 700), (CX - 400, 605)],
                  fill=(110, 60, 45, 255), outline=(40, 20, 20, 255), width=6)
        d.polygon([(CX - 385, 330), (CX - 10, 405), (CX - 10, 650), (CX - 385, 565)], fill=(252, 240, 210, 255),
                  outline=(60, 30, 20, 255), width=6)
        d.polygon([(CX + 385, 330), (CX + 10, 405), (CX + 10, 650), (CX + 385, 565)], fill=(246, 232, 198, 255),
                  outline=(60, 30, 20, 255), width=6)
        for i in range(5):
            d.line([(CX - 335, 385 + i * 36), (CX - 60, 438 + i * 36)], fill=(150, 110, 80, 255), width=6)
            d.line([(CX + 335, 385 + i * 36), (CX + 60, 438 + i * 36)], fill=(150, 110, 80, 255), width=6)
        img = paste_rgba(img, l)
        img = glow(img, (CX, 470), 520, (255, 235, 160), 0.8)
        img = sparkles(img, rng, (255, 245, 190), 100, region=(200, 100, 1100, 650), size=(3, 9))
    elif kind == "chain":
        img = magic_bg((15, 18, 50), (50, 60, 140), (150, 190, 255), rng)
        img = vortex(img, (CX, CY), [(150, 190, 255), (255, 240, 130), (255, 255, 255)], rng, count=12,
                     rmin=180, rmax=620, outline=(10, 10, 40))
        pts_list = [((260, 40), (400, 780)), ((640, 20), (640, 800)), ((1020, 40), (880, 780))]
        for s, e in pts_list:
            img = glow_line(img, lightning_path(rng, s, e, 70, 11), (160, 195, 255), 30, 18)
        for (x, y) in [(400, 780), (640, 800), (880, 780)]:
            img = glow(img, (x, y), 260, (150, 190, 255), 0.9)
        img = sparkles(img, rng, (230, 240, 255), 80)
    elif kind == "bind":
        img = magic_bg((18, 6, 40), (60, 20, 100), (170, 90, 255), rng)
        img = vortex(img, (CX, CY), [(170, 90, 255), (90, 220, 230), (40, 10, 80)], rng, count=16,
                     rmin=140, rmax=640, outline=(8, 2, 20), spin=1.4)
        img = glow(img, (CX, CY), 340, (150, 70, 255), 0.9)
        l = new_layer()
        ImageDraw.Draw(l).ellipse([CX - 92, CY - 92, CX + 92, CY + 92], fill=(6, 2, 12, 255))
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(2)))
        img = ring(img, (CX, CY), 108, (225, 200, 255), 7, 6)
        img = ring(img, (CX, CY), 150, (170, 120, 255), 4, 6)
        img = sparkles(img, rng, (230, 200, 255), 80)
    elif kind == "dawn":
        img = magic_bg((255, 200, 120), (120, 70, 140), (255, 235, 170), rng)
        img = rays(img, (CX, CY), (255, 240, 180), 24, 0.3, rng)
        img = vortex(img, (CX, CY), [(255, 235, 160), (255, 190, 100), (255, 255, 255)], rng, count=11,
                     rmin=170, rmax=600, outline=(90, 40, 80))
        img = glow(img, (CX, CY), 480, (255, 240, 180), 1.0)
        img = petals(img, rng, (255, 225, 140), 50)
        img = sparkles(img, rng, (255, 250, 210), 110, size=(2, 8))
    elif kind == "break":
        img = magic_bg((10, 20, 60), (40, 60, 130), (110, 220, 255), rng)
        img = rune_circle(img, (CX, CY), 400, (110, 220, 255), rng, 30)
        img = shards(img, (CX, CY), rng, (90, 170, 230), 12, 140, 520, (40, 100), edge=(230, 250, 255),
                     outline=(5, 15, 50))
        l = new_layer()
        d = ImageDraw.Draw(l)
        d.line([(CX - 100, CY - 100), (CX + 100, CY + 100)], fill=(255, 110, 110, 255), width=26)
        d.line([(CX + 100, CY - 100), (CX - 100, CY + 100)], fill=(255, 110, 110, 255), width=26)
        img = add_layer(img, l.filter(ImageFilter.GaussianBlur(14)))
        img = paste_rgba(img, l)
        img = sparkles(img, rng, (200, 240, 255), 80)
    elif kind == "ward":
        img = magic_bg((8, 40, 70), (30, 110, 150), (120, 230, 255), rng)
        img = vortex(img, (CX, CY), [(130, 230, 255), (255, 255, 255), (80, 170, 240)], rng, count=12,
                     rmin=260, rmax=640, outline=(5, 30, 60), width=(30, 90))
        l = new_layer()
        ImageDraw.Draw(l).ellipse([CX - 330, CY - 330, CX + 330, CY + 330], fill=(120, 230, 255, 70))
        img = paste_rgba(img, l)
        img = hexagon_grid(img, (CX, CY), 330, (200, 245, 255))
        img = ring(img, (CX, CY), 330, (230, 250, 255), 12, 8)
        img = sparkles(img, rng, (210, 245, 255), 90, region=(250, 140, 1030, 800))
    elif kind == "inferno":
        img = magic_bg((50, 8, 8), (150, 35, 20), (255, 140, 40), rng)
        img = vortex(img, (CX, CY + 60), [(255, 110, 30), (255, 200, 70), (200, 40, 30)], rng, count=16,
                     rmin=180, rmax=700, outline=(55, 5, 5), width=(60, 170))
        for k in range(9):
            ox = 110 + k * 150 + rng.uniform(-30, 30)
            img = flame_stack(img, ox, 1020, 520 + rng.uniform(0, 330), 125 + rng.uniform(0, 50), rng,
                              outline=(60, 6, 4))
        img = glow(img, (CX, 640), 720, (255, 120, 30), 0.9)
        img = sparkles(img, rng, (255, 225, 130), 150, region=(0, 100, W, 700), size=(2, 7))
    elif kind == "rewind":
        img = magic_bg((25, 10, 70), (80, 40, 150), (210, 160, 255), rng)
        img = vortex(img, (CX, CY), [(210, 170, 255), (130, 100, 255), (255, 235, 255)], rng, count=11,
                     rmin=330, rmax=640, outline=(20, 5, 50), width=(30, 80), spin=1.6)
        img = rune_circle(img, (CX, CY), 330, (225, 195, 255), rng, 12)
        l = new_layer()
        d = ImageDraw.Draw(l)
        d.line([(CX, CY), (CX + 12, CY - 220)], fill=(255, 240, 255, 255), width=20)
        d.line([(CX, CY), (CX - 150, CY + 55)], fill=(255, 240, 255, 255), width=20)
        d.ellipse([CX - 26, CY - 26, CX + 26, CY + 26], fill=(255, 255, 255, 255))
        img = add_layer(img, l.filter(ImageFilter.GaussianBlur(12)))
        img = paste_rgba(img, l)
        img = sparkles(img, rng, (240, 220, 255), 110, size=(2, 7))
    elif kind == "end":
        img = magic_bg((70, 8, 30), (14, 2, 18), (255, 60, 90), rng)
        img = glow(img, (CX, CY), 520, (255, 60, 90), 1.0)
        l = new_layer()
        d = ImageDraw.Draw(l)
        d.ellipse([CX - 270, CY - 270, CX + 270, CY + 270], fill=(40, 4, 14, 255), outline=(255, 90, 110, 255),
                  width=10)
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(1.5)))
        img = vortex(img, (CX, CY), [(20, 4, 20), (255, 70, 100), (40, 8, 60)], rng, count=15, rmin=240,
                     rmax=700, outline=(10, 0, 8), width=(60, 150), core=(255, 160, 170), spin=1.5)
        for k in range(10):
            a = k * math.tau / 10 + 0.2
            pts = lightning_path(rng, (CX + math.cos(a) * 280, CY + math.sin(a) * 280),
                                 (CX + math.cos(a) * 640, CY + math.sin(a) * 500), 50, 7)
            img = glow_line(img, pts, (255, 70, 100), 16, 12)
        img = shards(img, (CX, CY), rng, (120, 20, 50), 8, 300, 560, (30, 80), edge=(255, 150, 170),
                     outline=(10, 0, 8))
        img = sparkles(img, rng, (255, 180, 190), 100)
    else:
        raise ValueError(kind)
    return vignette(img, 0.45)


# ---------------------------------------------------------------- อุปกรณ์

def xform(points, angle, origin):
    ca, sa = math.cos(angle), math.sin(angle)
    return [(origin[0] + x * ca - y * sa, origin[1] + x * sa + y * ca) for x, y in points]


def blade_halves(length, width, bend, tip, steps=34, start=0.0):
    top, bot = [], []
    for i in range(steps + 1):
        x = start + (length - start) * i / steps
        f = 1.0 if x < length - tip else max(0.0, (length - x) / tip) ** 0.8
        by = -bend * (x / length) ** 2
        top.append((x, by - width / 2 * f))
        bot.append((x, by + width / 2 * f))
    return top, bot


def draw_blade(img, angle, origin, length, width, bend, tip, light, dark, edge=(255, 255, 255), outline=(10, 8, 20),
               glow_color=None, start=0.0):
    top, bot = blade_halves(length, width, bend, tip, start=start)
    mid = [((a[0] + b[0]) / 2, (a[1] + b[1]) / 2) for a, b in zip(top, bot)]
    full = xform(top + bot[::-1], angle, origin)
    if glow_color:
        g = new_layer()
        ImageDraw.Draw(g).polygon(full, fill=glow_color + (255,))
        img = add_layer(img, g.filter(ImageFilter.GaussianBlur(26)))
    l = new_layer()
    d = ImageDraw.Draw(l)
    d.polygon(full, fill=outline + (255,), outline=outline + (255,), width=10)
    d.polygon(xform(top + mid[::-1], angle, origin), fill=light + (255,))
    d.polygon(xform(mid + bot[::-1], angle, origin), fill=dark + (255,))
    d.line(xform(top, angle, origin), fill=edge + (255,), width=5)
    d.line(xform(mid, angle, origin), fill=mix(light, edge, 0.5) + (255,), width=3)
    return paste_rgba(img, l.filter(ImageFilter.GaussianBlur(0.8)))


def draw_hilt(img, angle, origin, guard_w, guard_h, handle_len, handle_w, metal, wrap, gem=None, round_guard=True,
              outline=(10, 8, 20)):
    l = new_layer()
    d = ImageDraw.Draw(l)
    handle = xform([(-handle_len, -handle_w / 2), (0, -handle_w / 2), (0, handle_w / 2), (-handle_len, handle_w / 2)],
                   angle, origin)
    d.polygon(handle, fill=outline + (255,), outline=outline + (255,), width=8)
    d.polygon(handle, fill=wrap + (255,))
    for i in range(7):
        x = -handle_len + handle_len * (i + 0.5) / 7
        d.line(xform([(x - 10, -handle_w / 2), (x + 10, handle_w / 2)], angle, origin), fill=mul(wrap, 0.45) + (255,),
               width=5)
    if round_guard:
        c = xform([(0, 0)], angle, origin)[0]
        gp = xform([(-guard_w / 2, -guard_h / 2), (guard_w / 2, -guard_h / 2), (guard_w / 2, guard_h / 2),
                    (-guard_w / 2, guard_h / 2)], angle, origin)
        d.polygon(gp, fill=outline + (255,), outline=outline + (255,), width=8)
        d.polygon(gp, fill=metal + (255,))
    else:
        gp = xform([(-guard_w / 2, 0), (0, -guard_h / 2), (guard_w / 2, 0), (0, guard_h / 2)], angle, origin)
        d.polygon(gp, fill=outline + (255,), outline=outline + (255,), width=8)
        d.polygon(gp, fill=metal + (255,))
    pommel = xform([(-handle_len - 16, 0)], angle, origin)[0]
    d.ellipse([pommel[0] - 24, pommel[1] - 24, pommel[0] + 24, pommel[1] + 24], fill=metal + (255,),
              outline=outline + (255,), width=6)
    if gem:
        c = xform([(-8, 0)], angle, origin)[0]
        d.ellipse([c[0] - 14, c[1] - 14, c[0] + 14, c[1] + 14], fill=gem + (255,), outline=(255, 255, 255, 255), width=3)
    return paste_rgba(img, l.filter(ImageFilter.GaussianBlur(0.8)))


def equipment_art(kind, rng):
    ang = -math.radians(38)
    if kind == "katana":
        img = magic_bg((25, 30, 55), (90, 100, 150), (210, 225, 255), rng)
        img = vortex(img, (CX, CY), [(200, 220, 255), (255, 255, 255), (120, 150, 220)], rng, count=13, rmin=200,
                     rmax=640, outline=(10, 14, 40), width=(30, 90))
        img = draw_blade(img, ang, (330, 760), 1000, 70, 70, 120, (235, 240, 250), (150, 165, 190),
                         glow_color=(170, 200, 255))
        img = draw_hilt(img, ang, (330, 760), 60, 150, 330, 46, (190, 160, 90), (40, 40, 70))
        img = speed_lines(img, (CX, CY), (230, 240, 255), rng, 45, inner=350)
    elif kind == "plate":
        img = magic_bg((50, 40, 20), (140, 110, 60), (255, 225, 140), rng)
        img = vortex(img, (CX, CY), [(255, 225, 140), (255, 255, 235), (200, 150, 70)], rng, count=11, rmin=300,
                     rmax=640, outline=(50, 30, 10), width=(30, 90))
        l = new_layer()
        d = ImageDraw.Draw(l)
        pts = [(CX - 250, 170), (CX + 250, 170), (CX + 270, 520), (CX, 800), (CX - 270, 520)]
        d.polygon(pts, fill=(15, 12, 24, 255), outline=(15, 12, 24, 255), width=14)
        inner = [(CX - 225, 195), (CX + 225, 195), (CX + 243, 510), (CX, 765), (CX - 243, 510)]
        d.polygon(inner, fill=(210, 215, 230, 255))
        d.polygon([(CX, 195), (CX + 225, 195), (CX + 243, 510), (CX, 765)], fill=(150, 160, 185, 255))
        d.line(inner + [inner[0]], fill=(255, 235, 150, 255), width=12)
        # ตรากลาง (ดวงอาทิตย์)
        d.ellipse([CX - 90, 360, CX + 90, 540], fill=(255, 215, 110, 255), outline=(15, 12, 24, 255), width=8)
        for i in range(12):
            a = i / 12 * math.tau
            d.line([(CX + math.cos(a) * 100, 450 + math.sin(a) * 100), (CX + math.cos(a) * 140, 450 + math.sin(a) * 140)],
                   fill=(255, 215, 110, 255), width=9)
        img = add_layer(img, l.filter(ImageFilter.GaussianBlur(24)))
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(0.8)))
        img = sparkles(img, rng, (255, 240, 190), 90, size=(2, 7))
    elif kind == "boots":
        img = magic_bg((15, 60, 55), (60, 150, 120), (190, 255, 220), rng)
        img = vortex(img, (CX, CY), [(200, 255, 225), (255, 255, 255), (110, 220, 160)], rng, count=15, rmin=170,
                     rmax=660, outline=(8, 60, 45), width=(30, 100), spin=1.3)
        l = new_layer()
        d = ImageDraw.Draw(l)
        boot = [(CX - 120, 200), (CX + 60, 200), (CX + 80, 520), (CX + 330, 600), (CX + 340, 720), (CX - 160, 720),
                (CX - 150, 520)]
        d.polygon(boot, fill=(10, 20, 20, 255), outline=(10, 20, 20, 255), width=14)
        inner = [(CX - 100, 220), (CX + 45, 220), (CX + 62, 535), (CX + 312, 612), (CX + 320, 700), (CX - 140, 700),
                 (CX - 132, 535)]
        d.polygon(inner, fill=(215, 175, 100, 255))
        d.polygon([(CX - 140, 640), (CX + 320, 640), (CX + 320, 700), (CX - 140, 700)], fill=(120, 80, 45, 255))
        d.polygon([(CX - 100, 220), (CX + 45, 220), (CX + 50, 290), (CX - 104, 290)], fill=(240, 235, 215, 255))
        for i in range(3):  # ปีก
            y = 380 + i * 52
            d.polygon([(CX - 150, y), (CX - 380 - i * 40, y - 80 + i * 20), (CX - 330 - i * 30, y + 10),
                       (CX - 150, y + 40)], fill=(245, 250, 245, 255), outline=(10, 40, 30, 255), width=6)
        img = add_layer(img, l.filter(ImageFilter.GaussianBlur(24)))
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(0.8)))
        img = speed_lines(img, (CX, CY), (230, 255, 235), rng, 55, inner=300)
    elif kind == "fang":
        img = magic_bg((70, 8, 28), (150, 20, 50), (255, 90, 110), rng)
        img = vortex(img, (CX, CY), [(255, 80, 100), (40, 4, 18), (255, 160, 170)], rng, count=14, rmin=180,
                     rmax=660, outline=(20, 0, 8), width=(40, 120))
        img = draw_blade(img, -math.radians(52), (380, 790), 780, 120, -260, 260, (255, 215, 220), (190, 40, 70),
                         edge=(255, 255, 255), glow_color=(255, 70, 100))
        img = draw_hilt(img, -math.radians(52), (380, 790), 60, 130, 250, 44, (200, 190, 200), (60, 8, 24),
                        gem=(255, 60, 90))
        img = sparkles(img, rng, (255, 190, 200), 90)
    elif kind == "moon":
        img = magic_bg((20, 20, 80), (60, 40, 130), (160, 150, 255), rng)
        l = new_layer()
        d = ImageDraw.Draw(l)
        d.ellipse([CX - 300, CY - 300, CX + 300, CY + 300], fill=(210, 220, 255, 255), outline=(20, 20, 70, 255),
                  width=10)
        d.ellipse([CX - 190, CY - 330, CX + 330, CY + 190], fill=(25, 20, 80, 255))
        img = add_layer(img, l.filter(ImageFilter.GaussianBlur(40)))
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(1.2)))
        img = vortex(img, (CX, CY), [(180, 180, 255), (255, 255, 255), (100, 90, 230)], rng, count=11, rmin=250,
                     rmax=680, outline=(10, 8, 50), width=(30, 100))
        img = draw_blade(img, -math.radians(36), (260, 800), 1120, 90, 160, 160, (240, 245, 255), (130, 140, 220),
                         glow_color=(150, 160, 255))
        img = draw_hilt(img, -math.radians(36), (260, 800), 64, 190, 300, 46, (210, 190, 120), (30, 30, 90),
                        gem=(120, 190, 255), round_guard=False)
        img = sparkles(img, rng, (225, 230, 255), 100)
    elif kind == "charm":
        img = magic_bg((10, 50, 40), (50, 140, 100), (170, 255, 200), rng)
        img = vortex(img, (CX, CY), [(170, 255, 200), (255, 255, 255), (90, 210, 150)], rng, count=12, rmin=250,
                     rmax=640, outline=(5, 45, 30), width=(30, 90))
        l = new_layer()
        d = ImageDraw.Draw(l)
        for i in range(-6, 7):  # สายสร้อย
            x0 = CX + i * 52
            y0 = 40 + abs(i) ** 1.7 * 11
            d.ellipse([x0 - 17, y0 - 11, x0 + 17, y0 + 11], outline=(230, 210, 140, 255), width=7)
        d.polygon([(CX, 250), (CX + 190, 420), (CX, 700), (CX - 190, 420)], fill=(15, 18, 20, 255),
                  outline=(15, 18, 20, 255), width=14)
        d.polygon([(CX, 270), (CX + 168, 420), (CX, 672), (CX - 168, 420)], fill=(235, 205, 120, 255))
        d.polygon([(CX, 270), (CX + 168, 420), (CX, 672)], fill=(190, 155, 80, 255))
        d.polygon([(CX, 340), (CX + 92, 430), (CX, 570), (CX - 92, 430)], fill=(70, 235, 150, 255),
                  outline=(255, 255, 255, 255), width=5)
        d.polygon([(CX, 340), (CX + 92, 430), (CX, 430)], fill=(190, 255, 220, 255))
        img = add_layer(img, l.filter(ImageFilter.GaussianBlur(26)))
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(0.8)))
        img = orb(img, (CX, 430), 30, (190, 255, 220))
        img = petals(img, rng, (170, 255, 200), 45)
        img = sparkles(img, rng, (220, 255, 235), 90)
    elif kind == "monocle":
        img = magic_bg((20, 20, 70), (70, 60, 140), (255, 225, 140), rng)
        img = rune_circle(img, (CX, CY), 420, (255, 225, 140), rng, 28)
        img = vortex(img, (CX, CY), [(255, 225, 140), (150, 190, 255)], rng, count=9, rmin=280, rmax=640,
                     outline=(20, 15, 60), width=(30, 80))
        l = new_layer()
        d = ImageDraw.Draw(l)
        d.ellipse([CX - 215, CY - 215, CX + 215, CY + 215], fill=(15, 12, 24, 255))
        d.ellipse([CX - 195, CY - 195, CX + 195, CY + 195], fill=(240, 205, 120, 255))
        d.ellipse([CX - 160, CY - 160, CX + 160, CY + 160], fill=(130, 190, 255, 215), outline=(15, 12, 24, 255),
                  width=6)
        d.ellipse([CX - 120, CY - 130, CX - 30, CY - 60], fill=(255, 255, 255, 190))
        for i in range(9):
            x0 = CX + 180 + i * 46
            y0 = CY + 170 + math.sin(i * 0.9) * 40 + i * 20
            d.ellipse([x0 - 17, y0 - 11, x0 + 17, y0 + 11], outline=(240, 205, 120, 255), width=7)
        img = add_layer(img, l.filter(ImageFilter.GaussianBlur(26)))
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(0.8)))
        img = sparkles(img, rng, (255, 240, 190), 90, size=(2, 8))
    elif kind == "brazier":
        img = magic_bg((30, 10, 60), (110, 40, 120), (255, 170, 90), rng)
        img = vortex(img, (CX, CY - 40), [(255, 150, 60), (190, 100, 255), (255, 220, 120)], rng, count=14, rmin=200,
                     rmax=660, outline=(25, 5, 45), width=(40, 120))
        img = flame_stack(img, CX, 560, 520, 190, rng, colors=((190, 90, 255), (255, 140, 80), (255, 245, 200)),
                          outline=(30, 5, 50))
        l = new_layer()
        d = ImageDraw.Draw(l)
        d.polygon([(CX - 280, 540), (CX + 280, 540), (CX + 170, 700), (CX - 170, 700)], fill=(12, 10, 20, 255),
                  outline=(12, 10, 20, 255), width=14)
        d.polygon([(CX - 262, 552), (CX + 262, 552), (CX + 158, 688), (CX - 158, 688)], fill=(95, 80, 125, 255))
        d.polygon([(CX, 552), (CX + 262, 552), (CX + 158, 688), (CX, 688)], fill=(60, 50, 90, 255))
        d.rectangle([CX - 300, 520, CX + 300, 556], fill=(235, 195, 110, 255), outline=(12, 10, 20, 255), width=7)
        for sx in (-1, 1):
            d.polygon([(CX + sx * 120, 690), (CX + sx * 250, 880), (CX + sx * 200, 880), (CX + sx * 80, 700)],
                      fill=(12, 10, 20, 255))
        d.polygon([(CX - 30, 690), (CX - 70, 880), (CX + 70, 880), (CX + 30, 690)], fill=(12, 10, 20, 255))
        img = paste_rgba(img, l.filter(ImageFilter.GaussianBlur(0.8)))
        img = sparkles(img, rng, (255, 210, 150), 120, region=(250, 80, 1030, 700), size=(2, 7))
    else:
        raise ValueError(kind)
    return vignette(img, 0.45)

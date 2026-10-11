#!/usr/bin/env python3
"""
สร้างรูปการ์ด SUP_xxx / MAG_xxx / EQP_xxx แบบ procedural (สไตล์อนิเมะ: ตัวละครครึ่งตัว + วงหมุนรอยปาดพลังเวท)
โมดูลวาดอยู่ใน tools/cardart/ (fx = เครื่องมือแสง/รอยปาด, magic = เวท/อุปกรณ์, portrait = ตัวละคร,
characters = ค่าตั้งตัวละคร)

รันใหม่ได้ทุกเมื่อ:  python3 tools/generate_card_art.py            (ทุกใบ)
                    python3 tools/generate_card_art.py SUP_001   (เฉพาะ ID)
                    python3 tools/generate_card_art.py --keep    (ข้ามไฟล์ที่มีอยู่แล้ว)
ผลลัพธ์: Assets/_Project/Resources/CardArt/{CardID}.png (640x480)

ใส่รูปจริงทับได้เลย: วางไฟล์ PNG ชื่อเดียวกับ CardID ในโฟลเดอร์เดียวกัน แล้วรันด้วย --keep
(หรือไม่ต้องรันสคริปต์อีกเลย) เกมจะโหลดรูปจริงแทนอัตโนมัติ
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image

from cardart.characters import CHARACTERS
from cardart.fx import sparkles, vignette
from cardart.magic import equipment_art, spell_art
from cardart.portrait import render_portrait

OUT_W, OUT_H = 640, 480

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Resources", "CardArt")

SPELLS = {
    "MAG_001": "spark", "MAG_002": "frost", "MAG_003": "rain", "MAG_004": "gale", "MAG_005": "wall",
    "MAG_006": "notes", "MAG_007": "chain", "MAG_008": "bind", "MAG_009": "dawn", "MAG_010": "break",
    "MAG_011": "ward", "MAG_012": "inferno", "MAG_013": "rewind", "MAG_014": "end", "MAG_015": "flame",
}

EQUIPMENT = {
    "EQP_001": "katana", "EQP_002": "plate", "EQP_003": "boots", "EQP_004": "fang", "EQP_005": "moon",
    "EQP_006": "charm", "EQP_007": "monocle", "EQP_008": "brazier",
}


def render_spell(card_id, kind):
    rng = random.Random(card_id)
    img = spell_art(kind, rng)
    img = sparkles(img, rng, (255, 255, 255), 40, size=(2, 5))
    return vignette(img, 0.5)


def render_equipment(card_id, kind):
    rng = random.Random(card_id)
    img = equipment_art(kind, rng)
    img = sparkles(img, rng, (255, 255, 255), 40, size=(2, 5))
    return vignette(img, 0.5)


def save(img, card_id):
    os.makedirs(OUT_DIR, exist_ok=True)
    out = img.resize((OUT_W, OUT_H), Image.LANCZOS)
    out.save(os.path.join(OUT_DIR, card_id + ".png"), optimize=True)


def main():
    only = [a for a in sys.argv[1:] if not a.startswith("--")]
    keep = "--keep" in sys.argv

    jobs = []
    for cid, cfg in CHARACTERS.items():
        jobs.append((cid, "unit", lambda c=cid, g=cfg: render_portrait(c, g)))
    for cid, kind in SPELLS.items():
        jobs.append((cid, "spell", lambda c=cid, k=kind: render_spell(c, k)))
    for cid, kind in EQUIPMENT.items():
        jobs.append((cid, "equip", lambda c=cid, k=kind: render_equipment(c, k)))

    for cid, label, fn in jobs:
        if only and cid not in only:
            continue
        if keep and os.path.exists(os.path.join(OUT_DIR, cid + ".png")):
            continue
        save(fn(), cid)
        print(label, cid, flush=True)


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""
เจนรูปการ์ดด้วย OpenAI Images API (ใช้แค่ไลบรารีมาตรฐานของ Python ไม่ต้อง pip install)

ตั้ง API key เป็น environment variable บนเครื่องคุณเอง (อย่าใส่ในไฟล์/แชท/commit):
    macOS/Linux : export OPENAI_API_KEY="sk-..."
    Windows PS  : $env:OPENAI_API_KEY = "sk-..."

ตัวอย่าง:
    python tools/ai_art/generate_ai_art.py --dry-run            # ดู prompt + ประมาณจำนวนรูป ไม่เสียเงิน
    python tools/ai_art/generate_ai_art.py --only SUP_001       # เจนใบเดียวเพื่อทดสอบสไตล์
    python tools/ai_art/generate_ai_art.py                      # เจนทุกใบที่ยังไม่เคยเจนด้วย AI
    python tools/ai_art/generate_ai_art.py --only MAG_001 --force   # เจนซ้ำ (ทับรูปเดิม)
    python tools/ai_art/generate_ai_art.py --check              # ตรวจว่าการ์ดทุกใบในเกมมี prompt

ผลลัพธ์: Assets/_Project/Resources/CardArt/{CardID}.png (ทับรูปโค้ดเดิม; รูปเดิมกู้คืนได้จาก git)
ประวัติว่าใบไหนเจนด้วย AI แล้ว: tools/ai_art/generated.json
"""
import argparse
import base64
import json
import os
import re
import sys
import time
import urllib.error
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Resources", "CardArt")
MANIFEST = os.path.join(HERE, "generated.json")
CARDS_JSON = os.path.join(HERE, "cards.json")
ROSTER_CS = os.path.join(ROOT, "Assets", "_Project", "Scripts", "Data", "StarterCardLibrary.Roster.cs")


def load_cards():
    with open(CARDS_JSON, encoding="utf-8") as f:
        return json.load(f)


def build_prompt(data, card, use_names):
    style = data["style"]
    parts = [style["common"], style[card["kind"]]]
    subject = card["desc"]
    if card["kind"] == "unit":
        if use_names and card.get("ref"):
            subject = "{} in the likeness of {}".format(subject, card["ref"])
        parts.append("Subject: " + subject + ".")
    else:
        parts.append("Subject: " + subject + ".")
    return " ".join(parts)


def load_manifest():
    if os.path.exists(MANIFEST):
        with open(MANIFEST, encoding="utf-8") as f:
            return json.load(f)
    return {}


def save_manifest(m):
    with open(MANIFEST, "w", encoding="utf-8") as f:
        json.dump(m, f, indent=2, ensure_ascii=False, sort_keys=True)


def call_api(base, key, model, prompt, size, quality, retries=4):
    body = json.dumps({
        "model": model,
        "prompt": prompt,
        "size": size,
        "quality": quality,
        "n": 1,
    }).encode("utf-8")
    req = urllib.request.Request(
        base.rstrip("/") + "/v1/images/generations",
        data=body,
        headers={"Authorization": "Bearer " + key, "Content-Type": "application/json"},
        method="POST",
    )
    delay = 5
    for attempt in range(retries + 1):
        try:
            with urllib.request.urlopen(req, timeout=300) as resp:
                payload = json.loads(resp.read().decode("utf-8"))
            return base64.b64decode(payload["data"][0]["b64_json"])
        except urllib.error.HTTPError as e:
            text = e.read().decode("utf-8", "replace")
            # 429 / 5xx = ลองใหม่; อย่างอื่น (เช่น ถูกปฏิเสธ prompt, key ผิด) = หยุดใบนี้
            if e.code in (429, 500, 502, 503, 504) and attempt < retries:
                time.sleep(delay)
                delay *= 2
                continue
            try:
                msg = json.loads(text)["error"]["message"]
            except Exception:
                msg = text[:300]
            raise RuntimeError("HTTP {}: {}".format(e.code, msg))
        except urllib.error.URLError as e:
            if attempt < retries:
                time.sleep(delay)
                delay *= 2
                continue
            raise RuntimeError("network error: {}".format(e.reason))


def check(data):
    ids = {c["id"] for c in data["cards"]}
    with open(ROSTER_CS, encoding="utf-8") as f:
        src = f.read()
    wanted = set(re.findall(r'"((?:SUP|MAG|EQP)_\d{3})"', src))
    missing = sorted(wanted - ids)
    extra = sorted(ids - wanted)
    if missing:
        print("การ์ดที่ยังไม่มี prompt ใน cards.json:", ", ".join(missing))
    if extra:
        print("prompt ที่ไม่มีการ์ดในเกม:", ", ".join(extra))
    if not missing and not extra:
        print("OK: การ์ด {} ใบมี prompt ครบ".format(len(ids)))
    return 1 if missing else 0


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--only", nargs="*", default=[], help="เจนเฉพาะ CardID เหล่านี้")
    ap.add_argument("--force", action="store_true", help="เจนซ้ำแม้เคยเจนด้วย AI แล้ว")
    ap.add_argument("--dry-run", action="store_true", help="แสดง prompt เฉยๆ ไม่เรียก API")
    ap.add_argument("--check", action="store_true", help="ตรวจว่าการ์ดทุกใบมี prompt")
    ap.add_argument("--model", default=os.environ.get("CARD_ART_MODEL", "gpt-image-2"))
    ap.add_argument("--quality", default="medium", choices=["low", "medium", "high"])
    ap.add_argument("--size", default="1280x960", help="กว้างxสูง ต้องหาร 16 ลงตัว (ค่าเริ่มต้น 4:3)")
    ap.add_argument("--use-names", action="store_true",
                    help="ใส่ชื่อตัวละครต้นฉบับใน prompt (อาจโดนปฏิเสธบ่อยขึ้น)")
    ap.add_argument("--out-dir", default=OUT_DIR, help="โฟลเดอร์ปลายทาง (ค่าเริ่มต้น Resources/CardArt)")
    ap.add_argument("--api-base", default=os.environ.get("OPENAI_BASE_URL", "https://api.openai.com"))
    args = ap.parse_args()

    data = load_cards()

    if args.check:
        sys.exit(check(data))

    cards = [c for c in data["cards"] if not args.only or c["id"] in args.only]
    unknown = [i for i in args.only if i not in {c["id"] for c in data["cards"]}]
    if unknown:
        sys.exit("ไม่พบ CardID: " + ", ".join(unknown))

    manifest = load_manifest()
    todo = [c for c in cards if args.force or c["id"] not in manifest]
    print("จะเจน {} รูป (ข้าม {} ใบที่เคยเจนแล้ว) โมเดล={} คุณภาพ={} ขนาด={}".format(
        len(todo), len(cards) - len(todo), args.model, args.quality, args.size))

    if args.dry_run:
        for c in todo:
            print("\n[{}] {}\n{}".format(c["id"], c["name"], build_prompt(data, c, args.use_names)))
        return

    key = os.environ.get("OPENAI_API_KEY", "").strip()
    if not key:
        sys.exit("ยังไม่ได้ตั้ง OPENAI_API_KEY (ดูวิธีตั้งที่หัวไฟล์นี้)")

    os.makedirs(args.out_dir, exist_ok=True)
    failed = []

    for i, c in enumerate(todo, 1):
        print("[{}/{}] {} {} ...".format(i, len(todo), c["id"], c["name"]), end=" ", flush=True)
        try:
            png = call_api(args.api_base, key, args.model, build_prompt(data, c, args.use_names),
                           args.size, args.quality)
        except RuntimeError as e:
            print("ล้มเหลว:", e)
            failed.append(c["id"])
            if "HTTP 401" in str(e) or "HTTP 403" in str(e):
                print("key ใช้ไม่ได้หรือยังไม่ได้ยืนยันองค์กร (Organization Verification) หยุดการทำงาน")
                break
            continue

        path = os.path.join(args.out_dir, c["id"] + ".png")
        with open(path, "wb") as f:
            f.write(png)
        manifest[c["id"]] = {"model": args.model, "quality": args.quality, "size": args.size,
                             "at": time.strftime("%Y-%m-%d %H:%M:%S")}
        save_manifest(manifest)  # เซฟทุกใบ เผื่อรันค้างกลางทาง
        print("เสร็จ ({} KB)".format(len(png) // 1024))

    print("\nสำเร็จ {} / ล้มเหลว {}".format(len(todo) - len(failed), len(failed)))
    if failed:
        print("ล้มเหลว:", " ".join(failed))
        print("ลองใหม่เฉพาะใบเหล่านี้: python tools/ai_art/generate_ai_art.py --only " + " ".join(failed))


if __name__ == "__main__":
    main()

# เจนรูปการ์ดด้วย OpenAI (gpt-image-2)

1. สร้าง API key ที่ platform.openai.com แล้วตั้งเป็น environment variable บนเครื่องคุณ (อย่า commit)
   - Windows PowerShell: `$env:OPENAI_API_KEY = "sk-..."`
   - macOS/Linux: `export OPENAI_API_KEY="sk-..."`
   - โมเดล GPT Image อาจต้องทำ Organization Verification ในหน้า console ก่อน
2. ดู prompt โดยไม่เสียเงิน: `python tools/ai_art/generate_ai_art.py --dry-run`
3. ลองใบเดียว: `python tools/ai_art/generate_ai_art.py --only SUP_001`
4. ถูกใจแล้วเจนทั้งหมด: `python tools/ai_art/generate_ai_art.py` (ข้ามใบที่เคยเจนแล้ว; ~49 รูป ระดับ medium ราว $2–3)
5. แก้ prompt ได้ที่ `cards.json` แล้วเจนซ้ำด้วย `--force --only <ID>`

ตัวเลือก: `--quality low|medium|high`, `--model gpt-image-2.5-flare` (รุ่นใหม่ ตรวจราคาก่อน), `--size 1536x1152`, `--use-names` (ใส่ชื่อตัวละครต้นฉบับใน prompt, อาจโดนปฏิเสธบ่อยขึ้น)
รูปเดิมที่วาดด้วยโค้ดกู้คืนได้ด้วย `git checkout -- Assets/_Project/Resources/CardArt`

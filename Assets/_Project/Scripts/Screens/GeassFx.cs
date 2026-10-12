using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Core;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// เอฟเฟกต์ Geass ของ Lelouch: ตราปีกนกสีแดงเรืองที่ตาของ Lelouch ฟ้าผ่าเส้นพลังไปหาเป้าหมาย
    /// ตราหมุนซ้อนทับบนเป้าหมายแล้วล็อก (ตัวหนังสือ GEASS / OBEY) เป้าหมายถูกสตัน
    /// ถ้าถูก Ward กัน ตราจะแตกกระจายและไม่ล็อก
    /// ทุกอย่างสร้างจากโค้ด ใช้เวลาแบบ unscaled
    /// </summary>
    public static class GeassFx
    {
        private static readonly Color Red = new Color(1f, 0.12f, 0.2f, 1f);
        private static readonly Color Magenta = new Color(0.85f, 0.15f, 0.7f, 1f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.4f, 1f);

        public static void Play(
            Transform fx, RectTransform shakeTarget,
            Vector3 from, Vector3 to, bool blocked)
        {
            if (fx == null)
                return;

            // 1) ตาของ Lelouch ลุกวาบ: ตราปีกนกบนการ์ดต้นทาง
            SecretFx.Flash(fx, Red, 0.22f, 0.4f);

            GeassSigil.Make(fx, from, 150f, Red, Gold, 0.9f, true);

            UiFx.Ring(fx, from, Red, 30f, 380f, 0.5f);
            UiFx.Burst(fx, from, Magenta, 16, 260f, 18f, 0.7f, -80f);

            Sfx.Play(SoundId.Skill);

            // 2) เส้นพลังพุ่งไปหาเป้าหมาย แล้ว 3) ตราล็อกที่เป้าหมาย
            SecretFx.Timeline(fx,
                new KeyValuePair<float, System.Action>(0.22f, () =>
                {
                    GeassLink.Make(fx, from, to, Red, 0.55f);
                    GeassLink.Make(fx, from, to, Gold, 0.4f, 0.04f);
                }),
                new KeyValuePair<float, System.Action>(0.5f, () =>
                {
                    if (blocked)
                        Shatter(fx, shakeTarget, to);
                    else
                        Lock(fx, shakeTarget, to);
                }));
        }

        // ตราล็อกเป้าหมาย: ตราใหญ่หมุนซ้อน เสาแสงแดง ข้อความ OBEY
        private static void Lock(Transform fx, RectTransform shakeTarget, Vector3 to)
        {
            SecretFx.Shake(shakeTarget, 10f, 0.35f);
            SecretFx.HitStop(fx, 0.15f, 0.12f);

            GeassSigil.Make(fx, to, 230f, Red, Gold, 1.5f, false);
            GeassSigil.Make(fx, to, 140f, Magenta, Color.white, 1.3f, true);

            UiFx.Beam(fx, to, new Color(Red.r, Red.g, Red.b, 0.8f), 150f, 900f, 0.8f);
            UiFx.Ring(fx, to, Red, 40f, 520f, 0.6f);
            UiFx.Ring(fx, to, Color.white, 30f, 320f, 0.4f);
            UiFx.Burst(fx, to, Red, 34, 420f, 24f, 1.0f, -90f);

            for (int i = 0; i < 4; i++)
                UiFx.Slash(fx, to, i % 2 == 0 ? Red : Gold, 380f, 8f, 0.35f);

            SecretFx.Title(fx, to + new Vector3(0f, 170f, 0f), "OBEY", Red, 54);

            Sfx.Play(SoundId.SpellDark);
        }

        // ถูก Ward กัน: ตราแตกกระจาย
        private static void Shatter(Transform fx, RectTransform shakeTarget, Vector3 to)
        {
            SecretFx.Shake(shakeTarget, 6f, 0.25f);

            UiFx.Ring(fx, to, new Color(0.6f, 0.9f, 1f, 1f), 40f, 420f, 0.5f);
            UiFx.Burst(fx, to, new Color(0.6f, 0.9f, 1f, 1f), 26, 380f, 18f, 0.9f, 140f);
            UiFx.Burst(fx, to, Red, 20, 300f, 14f, 0.9f, 200f);

            SecretFx.Title(fx, to + new Vector3(0f, 150f, 0f), "RESISTED", new Color(0.6f, 0.9f, 1f, 1f), 44);

            Sfx.Play(SoundId.Clang);
        }
    }

    /// <summary>
    /// ตรา Geass (ปีกนก): วงแหวน + แกนปีก 2 ข้างและขนปีกเป็นเส้นเรียงพัด หมุนช้าๆ ขยายออกแล้วจาง
    /// </summary>
    public class GeassSigil : MonoBehaviour
    {
        private RectTransform rect;
        private Image[] images;
        private Color[] colors;
        private float life;
        private float age;
        private float spin;
        private float size;

        public static GeassSigil Make(
            Transform parent, Vector3 world, float size,
            Color main, Color accent, float life, bool clockwise)
        {
            Image root = UIFactory.CreatePanel(parent, "GeassSigil", new Color(1f, 1f, 1f, 0f));

            root.raycastTarget = false;

            RectTransform rect = root.rectTransform;

            rect.position = world;
            rect.sizeDelta = Vector2.one * size;

            GeassSigil sigil = root.gameObject.AddComponent<GeassSigil>();

            sigil.rect = rect;
            sigil.life = Mathf.Max(0.2f, life);
            sigil.spin = clockwise ? -55f : 55f;
            sigil.size = size;

            List<Image> list = new List<Image>();
            List<Color> tints = new List<Color>();

            // วงแหวนนอก + ไฟเรือง
            Image glow = UIFactory.CreatePanel(rect, "Glow", new Color(main.r, main.g, main.b, 0.5f));

            glow.sprite = UIShapes.Glow;
            glow.type = Image.Type.Simple;
            glow.raycastTarget = false;

            UIFactory.Stretch(glow.rectTransform);

            glow.rectTransform.offsetMin = Vector2.one * (-size * 0.25f);
            glow.rectTransform.offsetMax = Vector2.one * (size * 0.25f);

            list.Add(glow);
            tints.Add(glow.color);

            Image ring = UIFactory.CreatePanel(rect, "Ring", main);

            ring.sprite = UIShapes.RingCircle;
            ring.type = Image.Type.Simple;
            ring.raycastTarget = false;

            UIFactory.Stretch(ring.rectTransform);

            list.Add(ring);
            tints.Add(main);

            // ปีกนก: ขนปีกเป็นเส้นเรียงพัดด้านซ้ายและขวา (7 เส้นต่อข้าง) เหมือนตรา Geass
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 7; i++)
                {
                    float t = i / 6f;
                    float angle = Mathf.Lerp(8f, 62f, t);
                    float length = size * Mathf.Lerp(0.5f, 0.36f, t);

                    Image feather = UIFactory.CreatePanel(
                        rect, "Feather", i % 2 == 0 ? accent : main);

                    feather.raycastTarget = false;

                    RectTransform fr = feather.rectTransform;

                    fr.anchorMin = new Vector2(0.5f, 0.5f);
                    fr.anchorMax = new Vector2(0.5f, 0.5f);
                    fr.pivot = new Vector2(0f, 0.5f);
                    fr.sizeDelta = new Vector2(length, Mathf.Max(3f, size * 0.022f));
                    fr.anchoredPosition = Vector2.zero;
                    fr.localRotation = Quaternion.Euler(0f, 0f, side > 0 ? angle : 180f - angle);

                    list.Add(feather);
                    tints.Add(feather.color);
                }
            }

            // แกนกลาง (ตา): จุดสว่าง
            Image eye = UIFactory.CreatePanel(rect, "Eye", Color.white);

            eye.sprite = UIShapes.Circle;
            eye.type = Image.Type.Simple;
            eye.raycastTarget = false;

            eye.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            eye.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            eye.rectTransform.sizeDelta = Vector2.one * (size * 0.14f);
            eye.rectTransform.anchoredPosition = Vector2.zero;

            list.Add(eye);
            tints.Add(new Color(1f, 0.85f, 0.9f, 1f));

            sigil.images = list.ToArray();
            sigil.colors = tints.ToArray();

            rect.localScale = Vector3.one * 0.3f;

            return sigil;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            float g = Mathf.Clamp01(age / 0.28f);
            float ease = 1f - (1f - g) * (1f - g) * (1f - g);

            rect.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, ease) * (1f + 0.08f * (age / life));
            rect.Rotate(0f, 0f, spin * Time.unscaledDeltaTime);

            float a = Mathf.Clamp01(age / 0.1f);
            float remain = life - age;

            if (remain < 0.35f)
                a *= Mathf.Clamp01(remain / 0.35f);

            for (int i = 0; i < images.Length; i++)
            {
                Color c = colors[i];

                c.a *= a;

                images[i].color = c;
            }

            if (age >= life)
                Destroy(gameObject);
        }
    }

    /// <summary>เส้นพลัง Geass จากจุดหนึ่งพุ่งไปอีกจุด (หัวเส้นวิ่งไปถึงเป้าหมายแล้วจาง)</summary>
    public class GeassLink : MonoBehaviour
    {
        private RectTransform rect;
        private Image image;
        private Color color;
        private float length;
        private float thickness;
        private float life;
        private float age;
        private float delay;

        public static GeassLink Make(
            Transform parent, Vector3 from, Vector3 to, Color color, float life, float delay = 0f)
        {
            Image img = UIFactory.CreatePanel(parent, "GeassLink", new Color(color.r, color.g, color.b, 0f));

            img.raycastTarget = false;

            RectTransform rect = img.rectTransform;

            Vector3 a = parent.InverseTransformPoint(from);
            Vector3 b = parent.InverseTransformPoint(to);
            Vector3 delta = b - a;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.localPosition = a;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            GeassLink link = img.gameObject.AddComponent<GeassLink>();

            link.rect = rect;
            link.image = img;
            link.color = color;
            link.length = delta.magnitude;
            link.thickness = delay > 0f ? 5f : 11f;
            link.life = Mathf.Max(0.1f, life);
            link.delay = delay;

            rect.sizeDelta = new Vector2(0f, link.thickness);

            return link;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01((age - delay) / life);

            if (age < delay)
                return;

            // หัวเส้นพุ่งออกเร็วในช่วงแรก แล้วเส้นค้างและจาง
            float reach = Mathf.Clamp01(t / 0.4f);

            reach = 1f - (1f - reach) * (1f - reach);

            rect.sizeDelta = new Vector2(length * reach, thickness * (1f - 0.5f * t));

            Color c = color;

            c.a = t < 0.5f ? 1f : 1f - (t - 0.5f) / 0.5f;

            image.color = c;

            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}

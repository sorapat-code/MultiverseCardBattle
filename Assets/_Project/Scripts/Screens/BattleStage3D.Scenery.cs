using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ส่วนตกแต่งและชีวิตชีวาของโต๊ะ 3D: เสาหินคริสตัลเรืองแสง, วงรูนกลางสนาม (หมุนช้าๆ), กรอบช่องวางยูนิต,
    /// ลำแสงผ่านใบไม้, หมอกคลุมพื้น, ละอองแสง, โพสต์เอฟเฟกต์ (Bloom/ปรับสี/วิกเน็ต),
    /// กล้องขยับตามเมาส์เล็กน้อย (parallax) และกล้องสั่นตอนโดนตี
    /// ทุกอย่างสร้างด้วยโค้ด (เชดเดอร์เรืองแสงอยู่ใน Resources/MCBGlow)
    /// </summary>
    public partial class BattleStage3D
    {
        private const int SlotsPerSide = 7;
        private const float SlotSpacing = 2.05f;

        private class Pulse
        {
            public Material Material;
            public Color Color;
            public float Speed;
            public float Amount;
            public float Phase;
            public int Side; // 0 กลาง, 1 ฝั่งศัตรู, 2 ฝั่งเรา
        }

        private readonly List<Pulse> pulses = new List<Pulse>();

        private Material enemySlotMaterial;
        private Material playerSlotMaterial;
        private Transform runePivot;
        private StagePostFx postFx;

        private float enemyBoost = 0.55f;
        private float playerBoost = 0.55f;
        private float enemyBoostTarget = 0.55f;
        private float playerBoostTarget = 0.55f;

        private Vector3 fitPosition;
        private Vector2 sway;
        private float shakeMagnitude;
        private float shakeLeft;
        private float shakeDuration = 1f;

        private static Shader glowShader;

        // ---------- วัสดุเรืองแสง ----------

        /// <summary>วัสดุเรืองแสง ไม่รับแสง (สี HDR ได้ — ค่าเกิน 1 จะติด Bloom); additive = บวกสี, ไม่ใช่ = ทึบ</summary>
        private static Material GlowMaterial(Color color, Texture texture, bool additive)
        {
            if (glowShader == null)
                glowShader = Resources.Load<Shader>("MCBGlow");

            if (glowShader == null)
                return Unlit(color, texture);

            Material material = new Material(glowShader);

            material.color = color;

            if (texture != null)
                material.mainTexture = texture;

            if (additive)
            {
                material.SetFloat("_SrcBlend", 5f); // SrcAlpha
                material.SetFloat("_DstBlend", 1f); // One
                material.SetFloat("_ZWrite", 0f);
                material.renderQueue = 3000;
            }
            else
            {
                material.SetFloat("_SrcBlend", 1f); // One
                material.SetFloat("_DstBlend", 0f); // Zero
                material.SetFloat("_ZWrite", 1f);
                material.renderQueue = 2000;
            }

            return material;
        }

        private static Color Hdr(Color color, float intensity, float alpha)
        {
            return new Color(color.r * intensity, color.g * intensity, color.b * intensity, alpha);
        }

        private void AddPulse(Material material, float speed, float amount, int side)
        {
            pulses.Add(new Pulse
            {
                Material = material,
                Color = material.color,
                Speed = speed,
                Amount = amount,
                Phase = Random.value * 6.28f,
                Side = side
            });
        }

        // ---------- ภาพที่สร้างด้วยโค้ด ----------

        private static Texture2D NewTex(int width, int height, TextureWrapMode wrap)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = wrap;
            texture.hideFlags = HideFlags.HideAndDontSave;

            return texture;
        }

        private static float Line(float distance, float width)
        {
            return 1f - Mathf.SmoothStep(0f, width, distance);
        }

        private static float SegDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));

            return (p - (a + ab * t)).magnitude;
        }

        // วงรูนมายากล: วงแหวนซ้อน เส้นขีดรอบนอก และดาวหกแฉก
        private static Texture2D BuildRuneTexture()
        {
            const int size = 256;

            Texture2D texture = NewTex(size, size, TextureWrapMode.Clamp);

            Color32[] pixels = new Color32[size * size];

            Vector2[] starA = new Vector2[3];
            Vector2[] starB = new Vector2[3];

            for (int i = 0; i < 3; i++)
            {
                float angleA = (90f + i * 120f) * Mathf.Deg2Rad;
                float angleB = (30f + i * 120f) * Mathf.Deg2Rad;

                starA[i] = new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * 0.8f;
                starB[i] = new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * 0.8f;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);

                    float r = p.magnitude;
                    float angle = Mathf.Atan2(p.y, p.x);

                    float v = 0.1f * Mathf.Clamp01(1f - r);

                    v = Mathf.Max(v, Line(Mathf.Abs(r - 0.96f), 0.016f));
                    v = Mathf.Max(v, 0.8f * Line(Mathf.Abs(r - 0.82f), 0.012f));
                    v = Mathf.Max(v, 0.7f * Line(Mathf.Abs(r - 0.52f), 0.012f));
                    v = Mathf.Max(v, 0.6f * Line(Mathf.Abs(r - 0.2f), 0.012f));

                    // ขีดรอบวง
                    if (r > 0.86f && r < 0.94f)
                    {
                        float cell = Mathf.Repeat(angle / (Mathf.PI * 2f) * 48f, 1f);

                        v = Mathf.Max(v, 0.8f * Line(Mathf.Abs(cell - 0.5f), 0.12f));
                    }

                    // ดาวหกแฉก
                    float star = 9f;

                    for (int i = 0; i < 3; i++)
                    {
                        star = Mathf.Min(star, SegDistance(p, starA[i], starA[(i + 1) % 3]));
                        star = Mathf.Min(star, SegDistance(p, starB[i], starB[(i + 1) % 3]));
                    }

                    v = Mathf.Max(v, 0.9f * Line(star, 0.016f));

                    v *= 1f - Mathf.SmoothStep(0.97f, 1f, r);

                    byte a = (byte)(Mathf.Clamp01(v) * 255f);

                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true);

            return texture;
        }

        // กรอบช่องวางการ์ด: เส้นขอบมุมมน + เติมจางๆ
        private static Texture2D BuildSlotTexture()
        {
            const int w = 64;
            const int h = 96;

            Texture2D texture = NewTex(w, h, TextureWrapMode.Clamp);

            Color32[] pixels = new Color32[w * h];

            float hx = w * 0.5f - 3f;
            float hy = h * 0.5f - 3f;
            float radius = 10f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - w * 0.5f) - (hx - radius);
                    float py = Mathf.Abs(y + 0.5f - h * 0.5f) - (hy - radius);

                    float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
                    float inside = Mathf.Min(Mathf.Max(px, py), 0f);

                    float sd = outside + inside - radius;

                    float v = 1f - Mathf.SmoothStep(1f, 2.6f, Mathf.Abs(sd));

                    if (sd < 0f)
                        v = Mathf.Max(v, 0.09f);

                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(v) * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true);

            return texture;
        }

        // ลำแสง: สว่างที่ต้นทาง (v = 1) จางลงสู่พื้น และนุ่มที่สองข้าง
        private static Texture2D BuildBeamTexture()
        {
            const int w = 16;
            const int h = 128;

            Texture2D texture = NewTex(w, h, TextureWrapMode.Clamp);

            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                float v = y / (h - 1f);

                for (int x = 0; x < w; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);

                    float side = Mathf.Pow(1f - u, 2f);
                    float along = Mathf.Pow(v, 1.2f) * (1f - Mathf.SmoothStep(0.9f, 1f, v));

                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(side * along) * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true);

            return texture;
        }

        // ---------- สร้างฉากตกแต่ง ----------

        private void BuildScenery()
        {
            Transform folder = new GameObject("Scenery").transform;

            folder.SetParent(transform, false);

            BuildPillars(folder);
            BuildRuneCircle(folder);
            BuildBeams(folder);
            BuildAmbientParticles();
        }

        private void BuildPostFx()
        {
            if (Cam == null)
                return;

            Cam.allowHDR = true;

            postFx = Cam.GetComponent<StagePostFx>();

            if (postFx == null)
                postFx = Cam.gameObject.AddComponent<StagePostFx>();
        }

        private GameObject Halo(Transform parent, Vector3 position, float size, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);

            go.name = "Halo";

            Collider collider = go.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size, size, 1f);

            Material material = GlowMaterial(color, UIShapes.Glow.texture, true);

            Renderer renderer = go.GetComponent<Renderer>();

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            StageBillboard billboard = go.AddComponent<StageBillboard>();

            billboard.Target = Cam != null ? Cam.transform : null;

            return go;
        }

        // เสาหินสี่มุม มีแหวนทองและคริสตัลเรืองแสง (ชมพูฝั่งศัตรู ฟ้าฝั่งเรา)
        private void BuildPillars(Transform folder)
        {
            Material stone = Lit(new Color(0.5f, 0.45f, 0.4f, 1f), 0.12f, 0f, BoardArt.StoneTexture);
            Material gold = Lit(new Color(0.95f, 0.72f, 0.28f, 1f), 0.6f, 0.3f, null);

            float px = BoardHalfWidth + 1.15f;
            float pz = BoardHalfDepth + 1.0f;

            for (int side = 0; side < 2; side++)
            {
                bool enemy = side == 0;

                Color tint = enemy ? new Color(1f, 0.3f, 0.55f, 1f) : new Color(0.3f, 0.85f, 1f, 1f);
                float scale = enemy ? 1f : 0.78f;

                for (int dir = -1; dir <= 1; dir += 2)
                {
                    float x = px * dir;
                    float z = enemy ? pz : -pz;

                    Transform pillar = new GameObject("Pillar").transform;

                    pillar.SetParent(folder, false);
                    pillar.localPosition = new Vector3(x, 0f, z);
                    pillar.localScale = Vector3.one * scale;

                    Prim(PrimitiveType.Cube, "Base", pillar, new Vector3(0f, -0.05f, 0f), new Vector3(1.15f, 0.5f, 1.15f), stone, true);
                    Prim(PrimitiveType.Cylinder, "Shaft", pillar, new Vector3(0f, 1.05f, 0f), new Vector3(0.8f, 0.85f, 0.8f), stone, true);
                    Prim(PrimitiveType.Cylinder, "Ring", pillar, new Vector3(0f, 0.55f, 0f), new Vector3(0.92f, 0.04f, 0.92f), gold, false);
                    Prim(PrimitiveType.Cylinder, "RingTop", pillar, new Vector3(0f, 1.85f, 0f), new Vector3(0.92f, 0.04f, 0.92f), gold, false);
                    Prim(PrimitiveType.Cube, "Cap", pillar, new Vector3(0f, 2.05f, 0f), new Vector3(1.0f, 0.22f, 1.0f), stone, true);

                    // คริสตัลลอยเหนือหัวเสา
                    Material crystalMaterial = GlowMaterial(Hdr(tint, 2.2f, 1f), null, false);

                    GameObject crystal = Prim(PrimitiveType.Cube, "Crystal", pillar,
                        new Vector3(0f, 2.75f, 0f), new Vector3(0.5f, 0.5f, 0.5f), crystalMaterial, false);

                    crystal.transform.localRotation = Quaternion.Euler(35f, 45f, 35f);
                    crystal.AddComponent<StageSpin>();

                    AddPulse(crystalMaterial, 2.2f, 0.22f, enemy ? 1 : 2);

                    GameObject halo = Halo(pillar, new Vector3(0f, 2.75f, 0f), 3.2f, Hdr(tint, 1.4f, 0.5f));

                    AddPulse(halo.GetComponent<Renderer>().sharedMaterial, 2.2f, 0.25f, enemy ? 1 : 2);
                }
            }
        }

        private void BuildRuneCircle(Transform folder)
        {
            runePivot = new GameObject("RunePivot").transform;

            runePivot.SetParent(folder, false);
            runePivot.localPosition = new Vector3(0f, 0.03f, 0f);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

            quad.name = "Rune";

            Collider collider = quad.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            quad.transform.SetParent(runePivot, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(4.4f, 4.4f, 1f);

            Material material = GlowMaterial(Hdr(new Color(1f, 0.78f, 0.35f, 1f), 1.7f, 0.5f), BuildRuneTexture(), true);

            Renderer renderer = quad.GetComponent<Renderer>();

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            AddPulse(material, 1.3f, 0.25f, 0);
        }

        // กรอบช่องวางยูนิตบนโต๊ะ (ชมพูฝั่งศัตรู ฟ้าฝั่งเรา) สว่างขึ้นตอนเป็นเทิร์นฝั่งนั้น
        private void BuildSlots(Transform folder)
        {
            Texture2D texture = BuildSlotTexture();

            enemySlotMaterial = GlowMaterial(Hdr(new Color(1f, 0.35f, 0.6f, 1f), 1.4f, 0.3f), texture, true);
            playerSlotMaterial = GlowMaterial(Hdr(new Color(0.35f, 0.85f, 1f, 1f), 1.4f, 0.3f), texture, true);

            for (int side = 0; side < 2; side++)
            {
                bool enemy = side == 0;

                for (int i = 0; i < SlotsPerSide; i++)
                {
                    GameObject slot = GameObject.CreatePrimitive(PrimitiveType.Quad);

                    slot.name = "Slot";

                    Collider collider = slot.GetComponent<Collider>();

                    if (collider != null)
                        Destroy(collider);

                    slot.transform.SetParent(folder, false);
                    slot.transform.localPosition = new Vector3(
                        (i - (SlotsPerSide - 1) * 0.5f) * SlotSpacing, 0.026f, enemy ? FieldRowZ : -FieldRowZ);
                    slot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    slot.transform.localScale = new Vector3(1.62f, 2.2f, 1f);

                    Renderer renderer = slot.GetComponent<Renderer>();

                    renderer.sharedMaterial = enemy ? enemySlotMaterial : playerSlotMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
        }

        // ลำแสงอุ่นลอดใบไม้ ตามทิศของ Sun
        private void BuildBeams(Transform folder)
        {
            Texture2D beam = BuildBeamTexture();

            Vector3 toSun = -(Quaternion.Euler(52f, -28f, 0f) * Vector3.forward);

            Vector3[] targets =
            {
                new Vector3(-5.5f, 0f, 2.5f), new Vector3(0.5f, 0f, 4.5f), new Vector3(5.5f, 0f, 1.2f)
            };

            float[] widths = { 2.8f, 2.2f, 3.2f };

            for (int i = 0; i < targets.Length; i++)
            {
                float length = 12f;

                Vector3 center = targets[i] + toSun * (length * 0.5f);
                Vector3 toCamera = (camBase - center).normalized;

                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

                quad.name = "Beam";

                Collider collider = quad.GetComponent<Collider>();

                if (collider != null)
                    Destroy(collider);

                quad.transform.SetParent(folder, false);
                quad.transform.localPosition = center;
                quad.transform.localRotation = Quaternion.LookRotation(toCamera, toSun);
                quad.transform.localScale = new Vector3(widths[i], length, 1f);

                Material material = GlowMaterial(Hdr(new Color(1f, 0.86f, 0.55f, 1f), 1.2f, 0.09f), beam, true);

                Renderer renderer = quad.GetComponent<Renderer>();

                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                AddPulse(material, 0.5f + i * 0.17f, 0.4f, 0);
            }
        }

        private ParticleSystem MakeParticles(
            string name, Vector3 position, Vector3 box, int max, float rate,
            Vector2 lifetime, Vector2 speed, Vector2 size, Material material,
            Color colorA, Color colorB, float noise, float gravity,
            ParticleSystemRenderMode mode)
        {
            GameObject go = new GameObject(name);

            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;

            ParticleSystem system = go.AddComponent<ParticleSystem>();

            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;

            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = max;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);

            ParticleSystem.EmissionModule emission = system.emission;

            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = system.shape;

            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;

            if (noise > 0f)
            {
                ParticleSystem.NoiseModule noiseModule = system.noise;

                noiseModule.enabled = true;
                noiseModule.strength = noise;
                noiseModule.frequency = 0.3f;
            }

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;

            fade.enabled = true;

            Gradient gradient = new Gradient();

            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.3f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });

            fade.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();

            renderer.renderMode = mode;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            system.Play();

            return system;
        }

        // หมอกบางคลุมพื้น + ละอองแสงลอยขึ้น
        private void BuildAmbientParticles()
        {
            Texture glow = UIShapes.Glow.texture;

            MakeParticles(
                "Mist", new Vector3(0f, 0.45f, 0f),
                new Vector3(BoardHalfWidth * 2.2f, 0.2f, BoardHalfDepth * 2.2f),
                16, 1.1f, new Vector2(12f, 16f), new Vector2(0.05f, 0.2f), new Vector2(5f, 8f),
                GlowMaterial(Hdr(new Color(0.5f, 0.78f, 0.85f, 1f), 1f, 0.07f), glow, true),
                new Color(0.7f, 0.9f, 1f, 1f), new Color(0.6f, 1f, 0.85f, 1f),
                0.3f, 0f, ParticleSystemRenderMode.HorizontalBillboard);

            MakeParticles(
                "Motes", new Vector3(0f, 0.6f, 0f),
                new Vector3(BoardHalfWidth * 2.2f, 3f, BoardHalfDepth * 2.2f),
                70, 7f, new Vector2(5f, 9f), new Vector2(0.03f, 0.14f), new Vector2(0.05f, 0.11f),
                GlowMaterial(Hdr(new Color(1f, 0.92f, 0.7f, 1f), 3f, 1f), glow, true),
                new Color(1f, 0.95f, 0.75f, 1f), new Color(0.8f, 1f, 0.9f, 1f),
                0.35f, -0.01f, ParticleSystemRenderMode.Billboard);
        }

        // ---------- มีชีวิต ----------

        public void Shake(float magnitude, float duration)
        {
            if (magnitude <= shakeMagnitude * (shakeLeft / shakeDuration))
                return;

            shakeMagnitude = magnitude;
            shakeDuration = Mathf.Max(0.05f, duration);
            shakeLeft = shakeDuration;
        }

        private static Vector2 PointerNormalized()
        {
            Pointer pointer = Pointer.current;

            if (pointer == null || Screen.width < 1 || Screen.height < 1)
                return Vector2.zero;

            Vector2 position = pointer.position.ReadValue();

            return new Vector2(
                Mathf.Clamp(position.x / Screen.width * 2f - 1f, -1f, 1f),
                Mathf.Clamp(position.y / Screen.height * 2f - 1f, -1f, 1f));
        }

        private void LateUpdate()
        {
            if (Cam == null)
                return;

            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;

            // ความสว่างของแต่ละฝั่งไล่ตามเทิร์น
            float follow = 1f - Mathf.Exp(-4f * dt);

            enemyBoost = Mathf.Lerp(enemyBoost, enemyBoostTarget, follow);
            playerBoost = Mathf.Lerp(playerBoost, playerBoostTarget, follow);

            foreach (Pulse pulse in pulses)
            {
                if (pulse.Material == null)
                    continue;

                float side = pulse.Side == 1 ? enemyBoost : (pulse.Side == 2 ? playerBoost : 1f);
                float wave = 1f + pulse.Amount * Mathf.Sin(time * pulse.Speed + pulse.Phase);
                float k = side * wave;

                pulse.Material.color = new Color(
                    pulse.Color.r * k, pulse.Color.g * k, pulse.Color.b * k, pulse.Color.a);
            }

            if (runePivot != null)
                runePivot.Rotate(0f, 5f * dt, 0f, Space.Self);

            // กล้อง: ขยับตามเมาส์เล็กน้อย + สั่น
            sway = Vector2.Lerp(sway, PointerNormalized(), 1f - Mathf.Exp(-3f * dt));

            Quaternion baseRotation = Quaternion.LookRotation(camLook - fitPosition, Vector3.up);
            Vector3 right = baseRotation * Vector3.right;
            Vector3 up = baseRotation * Vector3.up;

            Vector3 offset = right * (sway.x * 0.45f) + up * (sway.y * 0.25f);

            if (shakeLeft > 0f)
            {
                shakeLeft -= dt;

                float k = Mathf.Clamp01(shakeLeft / shakeDuration);

                offset += Random.insideUnitSphere * (shakeMagnitude * k * k);
            }

            Cam.transform.position = fitPosition + offset;
            Cam.transform.LookAt(camLook + right * (sway.x * 0.25f));
        }

        private void OnDestroy()
        {
            if (postFx != null)
                Destroy(postFx);
        }
    }

    /// <summary>หันหน้าเข้าหากล้องเสมอ (ใช้กับแสงฟุ้ง)</summary>
    public class StageBillboard : MonoBehaviour
    {
        public Transform Target;

        private void LateUpdate()
        {
            if (Target != null)
                transform.rotation = Target.rotation;
        }
    }

    /// <summary>หมุนช้าๆ + ลอยขึ้นลง (คริสตัล)</summary>
    public class StageSpin : MonoBehaviour
    {
        private Vector3 basePosition;
        private float phase;

        private void Start()
        {
            basePosition = transform.localPosition;
            phase = Random.value * 6.28f;
        }

        private void Update()
        {
            transform.Rotate(0f, 40f * Time.unscaledDeltaTime, 0f, Space.World);

            transform.localPosition = basePosition +
                Vector3.up * (Mathf.Sin(Time.unscaledTime * 1.6f + phase) * 0.08f);
        }
    }
}

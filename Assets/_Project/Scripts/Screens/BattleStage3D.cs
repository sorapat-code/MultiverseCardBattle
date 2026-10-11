using System.Collections.Generic;
using UnityEngine;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ฉาก 3D ของหน้าต่อสู้ สร้างด้วยโค้ดทั้งหมด (ไม่มีโมเดลหรือไฟล์รูป):
    /// โต๊ะหิน, สนามหญ้า, กรอบหินพร้อมอัญมณีทอง, พุ่มไม้/ต้นไม้ฉากหลัง, แสงเงา, หมอก,
    /// หิ่งห้อยและใบไม้ปลิว (ParticleSystem) และกล้อง Perspective เอียงมองลงมา
    /// การ์ดทุกใบเป็นวัตถุ 3D (CardHolder3D) ที่วางบนโต๊ะ: ช่องยูนิตสองแถวและมือที่คลี่เป็นพัด
    ///
    /// ระบบ: Built-in Render Pipeline (Standard shader; ไม่มีก็ถอยไปใช้ Diffuse / Sprites/Default)
    /// พิกัดโลก: x = ขวา, y = ขึ้น, z = ไปทางฝั่งศัตรู กลางสนามคือ (0,0,0) ฝั่งผู้เล่นอยู่ z ติดลบ
    /// </summary>
    public partial class BattleStage3D : MonoBehaviour
    {
        public const float BoardHalfWidth = 7.6f;
        public const float BoardHalfDepth = 4.3f;

        // 1 พิกเซลของ Canvas การ์ด = 0.01 หน่วยโลก (การ์ด 150x210 px = 1.5 x 2.1)
        public const float CardUnit = 0.01f;

        public const float FieldTilt = 70f;
        public const float HandTilt = 58f;

        private const float FieldRowZ = 2.4f;

        public Camera Cam { get; private set; }

        private readonly Vector3 camLook = new Vector3(0f, 0f, -0.8f);
        private readonly Vector3 camBase = new Vector3(0f, 14f, -10.5f);

        private StageTheme theme;

        private int fitWidth;
        private int fitHeight;

        private Material enemyGlowMaterial;
        private Material enemyLineMaterial;
        private Material playerGlowMaterial;
        private Material playerLineMaterial;
        private Light enemyLight;
        private Light playerLight;

        public static BattleStage3D Create()
        {
            GameObject go = new GameObject("Stage3D");

            BattleStage3D stage = go.AddComponent<BattleStage3D>();

            stage.Build();

            return stage;
        }

        // ---------- วัสดุ ----------

        private static Material Lit(Color color, float smoothness, float metallic, Texture texture)
        {
            Shader shader = Shader.Find("Standard");

            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Diffuse");

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material material = new Material(shader);

            material.color = color;

            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            if (texture != null)
                material.mainTexture = texture;

            return material;
        }

        // ไม่รับแสง โปร่งใสได้ (ใช้กับแสงเรืองขอบสนามและอนุภาค)
        private static Material Unlit(Color color, Texture texture)
        {
            Material material = new Material(Shader.Find("Sprites/Default"));

            material.color = color;

            if (texture != null)
                material.mainTexture = texture;

            return material;
        }

        private GameObject Prim(
            PrimitiveType type, string name, Transform parent,
            Vector3 position, Vector3 scale, Material material, bool castShadow)
        {
            GameObject go = GameObject.CreatePrimitive(type);

            go.name = name;

            Collider collider = go.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            Renderer renderer = go.GetComponent<Renderer>();

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadow
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;

            return go;
        }

        // ---------- สร้างฉาก ----------

        private void Build()
        {
            theme = StageTheme.Current;

            BuildCamera();
            BuildPostFx();
            BuildLights();
            BuildTable();
            BuildFrame();
            BuildGlowStrips();
            if (!BuildNature())
                BuildFoliage();

            BuildParticles();
            BuildScenery();

            Fit();
        }

        private void BuildCamera()
        {
            Cam = Camera.main;

            if (Cam == null)
            {
                GameObject cameraObject = new GameObject("StageCamera");

                cameraObject.tag = "MainCamera";

                Cam = cameraObject.AddComponent<Camera>();

                if (Object.FindAnyObjectByType<AudioListener>() == null)
                    cameraObject.AddComponent<AudioListener>();
            }

            Cam.orthographic = false;
            Cam.fieldOfView = 36f;
            Cam.nearClipPlane = 0.3f;
            Cam.farClipPlane = 90f;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = theme.Fog;
            Cam.depth = -1f;
            Cam.allowMSAA = true;
        }

        private void BuildLights()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = theme.Ambient;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = theme.FogDensity;
            RenderSettings.fogColor = theme.Fog;

            // แสงหลัก: อุ่น เอียงจากบนซ้ายหน้า เงานุ่ม
            GameObject sun = new GameObject("Sun");

            sun.transform.SetParent(transform, false);
            sun.transform.rotation = Quaternion.Euler(theme.SunEuler);

            Light light = sun.AddComponent<Light>();

            light.type = LightType.Directional;
            light.color = theme.SunColor;
            light.intensity = theme.SunIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;

            // แสงสีรายฝั่ง: ชมพูฝั่งศัตรู ฟ้าฝั่งเรา สว่างขึ้นตอนเป็นเทิร์นของฝั่งนั้น
            enemyLight = PointLight("EnemyLight", new Vector3(0f, 2.6f, 3.4f), new Color(1f, 0.3f, 0.5f, 1f));
            playerLight = PointLight("PlayerLight", new Vector3(0f, 2.6f, -3.4f), new Color(0.3f, 0.8f, 1f, 1f));
        }

        private Light PointLight(string name, Vector3 position, Color color)
        {
            GameObject go = new GameObject(name);

            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;

            Light light = go.AddComponent<Light>();

            light.type = LightType.Point;
            light.color = color;
            light.range = 10f;
            light.intensity = 0.8f;
            light.shadows = LightShadows.None;

            return light;
        }

        private void BuildTable()
        {
            Material stone = Lit(theme.TableTint, 0.1f, 0f, BoardArt.StoneTexture);

            stone.mainTextureScale = new Vector2(7f, 5f);

            // โต๊ะใหญ่ใต้สนาม
            Prim(PrimitiveType.Cube, "Table", transform,
                new Vector3(0f, -0.6f, 0f), new Vector3(34f, 0.6f, 24f), stone, false);

            // สนามหญ้ายกขึ้นจากโต๊ะเล็กน้อย
            Material grass = Lit(theme.BoardTint, 0.02f, 0f, BoardArt.Grass.texture);

            Prim(PrimitiveType.Cube, "Board", transform,
                new Vector3(0f, -0.15f, 0f),
                new Vector3(BoardHalfWidth * 2f, 0.3f, BoardHalfDepth * 2f), grass, false);
        }

        private void BuildFrame()
        {
            Material stone = Lit(new Color(0.55f, 0.46f, 0.38f, 1f), 0.15f, 0f, BoardArt.StoneTexture);

            stone.mainTextureScale = new Vector2(4f, 1f);

            Material gold = Lit(new Color(0.95f, 0.72f, 0.28f, 1f), 0.65f, 0.55f, null);

            float w = BoardHalfWidth;
            float d = BoardHalfDepth;
            float t = 0.6f;

            // กรอบหินสี่ด้าน
            Prim(PrimitiveType.Cube, "FrameFar", transform,
                new Vector3(0f, 0.15f, d + t * 0.5f), new Vector3(w * 2f + t * 2f, 0.55f, t), stone, true);
            Prim(PrimitiveType.Cube, "FrameNear", transform,
                new Vector3(0f, 0.15f, -d - t * 0.5f), new Vector3(w * 2f + t * 2f, 0.55f, t), stone, true);
            Prim(PrimitiveType.Cube, "FrameLeft", transform,
                new Vector3(-w - t * 0.5f, 0.15f, 0f), new Vector3(t, 0.55f, d * 2f), stone, true);
            Prim(PrimitiveType.Cube, "FrameRight", transform,
                new Vector3(w + t * 0.5f, 0.15f, 0f), new Vector3(t, 0.55f, d * 2f), stone, true);

            // เส้นทองฝังบนกรอบ
            Prim(PrimitiveType.Cube, "InlayFar", transform,
                new Vector3(0f, 0.43f, d + t * 0.5f), new Vector3(w * 2f + t * 2f - 0.2f, 0.03f, 0.07f), gold, false);
            Prim(PrimitiveType.Cube, "InlayNear", transform,
                new Vector3(0f, 0.43f, -d - t * 0.5f), new Vector3(w * 2f + t * 2f - 0.2f, 0.03f, 0.07f), gold, false);
            Prim(PrimitiveType.Cube, "InlayLeft", transform,
                new Vector3(-w - t * 0.5f, 0.43f, 0f), new Vector3(0.07f, 0.03f, d * 2f), gold, false);
            Prim(PrimitiveType.Cube, "InlayRight", transform,
                new Vector3(w + t * 0.5f, 0.43f, 0f), new Vector3(0.07f, 0.03f, d * 2f), gold, false);

            // อัญมณีทองที่มุมทั้งสี่และกลางขอบซ้ายขวา
            Vector3 gemScale = new Vector3(0.5f, 0.7f, 0.5f);

            float gx = w + t * 0.5f;
            float gz = d + t * 0.5f;

            Prim(PrimitiveType.Sphere, "GemFL", transform, new Vector3(-gx, 0.7f, gz), gemScale, gold, true);
            Prim(PrimitiveType.Sphere, "GemFR", transform, new Vector3(gx, 0.7f, gz), gemScale, gold, true);
            Prim(PrimitiveType.Sphere, "GemNL", transform, new Vector3(-gx, 0.7f, -gz), gemScale, gold, true);
            Prim(PrimitiveType.Sphere, "GemNR", transform, new Vector3(gx, 0.7f, -gz), gemScale, gold, true);
            Prim(PrimitiveType.Sphere, "GemML", transform, new Vector3(-gx, 0.6f, 0f), gemScale * 0.8f, gold, true);
            Prim(PrimitiveType.Sphere, "GemMR", transform, new Vector3(gx, 0.6f, 0f), gemScale * 0.8f, gold, true);

            // เส้นกลางสนาม
            Prim(PrimitiveType.Cube, "CenterLine", transform,
                new Vector3(0f, 0.02f, 0f), new Vector3(w * 2f, 0.03f, 0.08f), gold, false);
        }

        // แสงเรืองแนบขอบสนามสองฝั่ง (ชมพู = ศัตรู ฟ้า = เรา)
        private void BuildGlowStrips()
        {
            float w = BoardHalfWidth;
            float d = BoardHalfDepth;

            enemyGlowMaterial = GlowMaterial(new Color(1.4f, 0.4f, 0.7f, 0.2f), BoardArt.FadeDown.texture, true);
            playerGlowMaterial = GlowMaterial(new Color(0.4f, 1.1f, 1.5f, 0.2f), BoardArt.FadeUp.texture, true);
            enemyLineMaterial = GlowMaterial(new Color(2.2f, 0.5f, 1f, 0.5f), null, true);
            playerLineMaterial = GlowMaterial(new Color(0.5f, 1.6f, 2.2f, 0.5f), null, true);

            Quaternion flat = Quaternion.Euler(90f, 0f, 0f);

            GameObject enemyGlow = Prim(PrimitiveType.Quad, "EnemyGlow", transform,
                new Vector3(0f, 0.03f, d - 0.8f), new Vector3(w * 2f, 1.6f, 1f), enemyGlowMaterial, false);

            enemyGlow.transform.localRotation = flat;

            GameObject playerGlow = Prim(PrimitiveType.Quad, "PlayerGlow", transform,
                new Vector3(0f, 0.03f, -d + 0.8f), new Vector3(w * 2f, 1.6f, 1f), playerGlowMaterial, false);

            playerGlow.transform.localRotation = flat;

            GameObject enemyLine = Prim(PrimitiveType.Quad, "EnemyLine", transform,
                new Vector3(0f, 0.035f, d - 0.05f), new Vector3(w * 2f, 0.1f, 1f), enemyLineMaterial, false);

            enemyLine.transform.localRotation = flat;

            GameObject playerLine = Prim(PrimitiveType.Quad, "PlayerLine", transform,
                new Vector3(0f, 0.035f, -d + 0.05f), new Vector3(w * 2f, 0.1f, 1f), playerLineMaterial, false);

            playerLine.transform.localRotation = flat;
        }

        // ฝั่งที่เป็นเทิร์นสว่างกว่า
        public void SetTurnGlow(bool enemyTurn, bool playerTurn)
        {
            if (enemyGlowMaterial == null)
                return;

            SetAlpha(enemyGlowMaterial, enemyTurn ? 0.85f : 0.2f);
            SetAlpha(enemyLineMaterial, enemyTurn ? 1f : 0.4f);
            SetAlpha(playerGlowMaterial, playerTurn ? 0.85f : 0.2f);
            SetAlpha(playerLineMaterial, playerTurn ? 1f : 0.4f);

            enemyBoostTarget = enemyTurn ? 1.25f : 0.55f;
            playerBoostTarget = playerTurn ? 1.25f : 0.55f;

            if (enemySlotMaterial != null)
                SetAlpha(enemySlotMaterial, enemyTurn ? 0.75f : 0.28f);

            if (playerSlotMaterial != null)
                SetAlpha(playerSlotMaterial, playerTurn ? 0.75f : 0.28f);

            if (enemyLight != null)
                enemyLight.intensity = enemyTurn ? 1.6f : 0.5f;

            if (playerLight != null)
                playerLight.intensity = playerTurn ? 1.6f : 0.5f;
        }

        private static void SetAlpha(Material material, float alpha)
        {
            Color color = material.color;

            color.a = alpha;

            material.color = color;
        }

        // พุ่มไม้ ก้อนหิน และต้นไม้ฉากหลัง
        private void BuildFoliage()
        {
            Random.State saved = Random.state;

            Random.InitState(20261011);

            Transform folder = new GameObject("Foliage").transform;

            folder.SetParent(transform, false);

            Material[] leaves =
            {
                Lit(new Color(0.07f, 0.24f, 0.13f, 1f), 0.05f, 0f, null),
                Lit(new Color(0.1f, 0.32f, 0.16f, 1f), 0.05f, 0f, null),
                Lit(new Color(0.05f, 0.18f, 0.12f, 1f), 0.05f, 0f, null)
            };

            Material rock = Lit(new Color(0.3f, 0.29f, 0.27f, 1f), 0.2f, 0f, BoardArt.StoneTexture);

            float w = BoardHalfWidth + 0.9f;
            float d = BoardHalfDepth + 0.9f;

            // พุ่มด้านหลังและด้านข้าง (ด้านหน้าไม่วาง เพราะมือผู้เล่นอยู่ตรงนั้น)
            for (int i = 0; i < 26; i++)
            {
                float x = Mathf.Lerp(-w - 1.5f, w + 1.5f, i / 25f) + Random.Range(-0.4f, 0.4f);

                float scale = Random.Range(0.9f, 1.8f);

                Prim(PrimitiveType.Sphere, "BushBack", folder,
                    new Vector3(x, 0.35f + scale * 0.3f, d + Random.Range(0.2f, 1.4f)),
                    new Vector3(scale * 1.3f, scale, scale * 1.1f),
                    leaves[Random.Range(0, leaves.Length)], true);
            }

            for (int i = 0; i < 8; i++)
            {
                float z = Mathf.Lerp(-d + 0.5f, d, i / 7f) + Random.Range(-0.3f, 0.3f);

                float scale = Random.Range(0.9f, 1.7f);

                Prim(PrimitiveType.Sphere, "BushLeft", folder,
                    new Vector3(-w - Random.Range(0.3f, 1.4f), 0.35f + scale * 0.3f, z),
                    new Vector3(scale * 1.1f, scale, scale * 1.2f),
                    leaves[Random.Range(0, leaves.Length)], true);

                Prim(PrimitiveType.Sphere, "BushRight", folder,
                    new Vector3(w + Random.Range(0.3f, 1.4f), 0.35f + scale * 0.3f, z),
                    new Vector3(scale * 1.1f, scale, scale * 1.2f),
                    leaves[Random.Range(0, leaves.Length)], true);
            }

            // ก้อนหินตามมุม
            Prim(PrimitiveType.Sphere, "RockFL", folder, new Vector3(-w - 0.4f, 0.2f, d + 0.2f), new Vector3(1.4f, 0.8f, 1.1f), rock, true);
            Prim(PrimitiveType.Sphere, "RockFR", folder, new Vector3(w + 0.5f, 0.2f, d + 0.1f), new Vector3(1.2f, 0.7f, 1.3f), rock, true);
            Prim(PrimitiveType.Sphere, "RockNL", folder, new Vector3(-w - 0.5f, 0.15f, -d - 0.1f), new Vector3(1.1f, 0.6f, 1f), rock, true);
            Prim(PrimitiveType.Sphere, "RockNR", folder, new Vector3(w + 0.5f, 0.15f, -d - 0.1f), new Vector3(1.3f, 0.7f, 1.1f), rock, true);

            // ต้นไม้ฉากหลัง
            Material trunk = Lit(new Color(0.16f, 0.1f, 0.07f, 1f), 0.05f, 0f, null);

            for (int i = 0; i < 9; i++)
            {
                float x = -14f + i * 3.5f + Random.Range(-0.8f, 0.8f);
                float z = 9f + Random.Range(0f, 3f);
                float height = Random.Range(3.2f, 4.6f);

                Prim(PrimitiveType.Cylinder, "Trunk", folder,
                    new Vector3(x, height * 0.5f - 0.3f, z), new Vector3(0.6f, height * 0.5f, 0.6f), trunk, true);

                float crown = Random.Range(3.2f, 4.4f);

                Prim(PrimitiveType.Sphere, "Crown", folder,
                    new Vector3(x, height + 0.4f, z), new Vector3(crown, crown * 0.8f, crown),
                    leaves[Random.Range(0, leaves.Length)], true);

                Prim(PrimitiveType.Sphere, "CrownSmall", folder,
                    new Vector3(x + Random.Range(-1f, 1f), height + 1.4f, z - 0.5f),
                    new Vector3(crown * 0.6f, crown * 0.5f, crown * 0.6f),
                    leaves[Random.Range(0, leaves.Length)], true);
            }

            Random.state = saved;
        }

        // หิ่งห้อยลอยเหนือสนามและใบไม้ปลิวลงมา
        private void BuildParticles()
        {
            Texture glow = UIShapes.Glow.texture;

            BuildFireflies(glow);

            if (theme.Leaves)
                BuildLeaves(UIShapes.Circle.texture);
        }

        private void BuildFireflies(Texture glow)
        {
            GameObject go = new GameObject("Fireflies");

            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            ParticleSystem system = go.AddComponent<ParticleSystem>();

            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;

            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = 45;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(theme.MoteA, theme.MoteB);
            main.gravityModifier = theme.MoteGravity;

            ParticleSystem.EmissionModule emission = system.emission;

            emission.rateOverTime = theme.MoteRate;

            ParticleSystem.ShapeModule shape = system.shape;

            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(BoardHalfWidth * 2f, 3f, BoardHalfDepth * 2f);

            ParticleSystem.NoiseModule noise = system.noise;

            noise.enabled = true;
            noise.strength = 0.7f;
            noise.frequency = 0.35f;

            // เกิด/จางหายนุ่มๆ
            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;

            fade.enabled = true;

            Gradient gradient = new Gradient();

            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });

            fade.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();

            renderer.sharedMaterial = GlowMaterial(Hdr(Color.white, 2.4f, 1f), glow, true);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            system.Play();
        }

        private void BuildLeaves(Texture circle)
        {
            GameObject go = new GameObject("Leaves");

            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 7f, 0f);

            ParticleSystem system = go.AddComponent<ParticleSystem>();

            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;

            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = 24;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 13f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.24f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.gravityModifier = 0.025f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(theme.LeafA, theme.LeafB);

            ParticleSystem.EmissionModule emission = system.emission;

            emission.rateOverTime = 1.6f;

            ParticleSystem.ShapeModule shape = system.shape;

            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(BoardHalfWidth * 2.4f, 0.2f, BoardHalfDepth * 2f);

            ParticleSystem.NoiseModule noise = system.noise;

            noise.enabled = true;
            noise.strength = 0.9f;
            noise.frequency = 0.25f;

            ParticleSystem.RotationOverLifetimeModule spin = system.rotationOverLifetime;

            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();

            renderer.sharedMaterial = Unlit(Color.white, circle);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            system.Play();
        }

        // ---------- กล้อง ----------

        // จอแคบกว่า 16:9 ให้ถอยกล้องออก สนามจะได้ไม่ล้นจอ
        public void Fit()
        {
            if (Cam == null)
                return;

            fitWidth = Screen.width;
            fitHeight = Screen.height;

            float aspect = fitHeight > 0 ? (float)fitWidth / fitHeight : 16f / 9f;

            float scale = Mathf.Max(1f, (16f / 9f) / aspect);

            fitPosition = camLook + (camBase - camLook) * scale;

            Cam.transform.position = fitPosition;
            Cam.transform.LookAt(camLook);
        }

        /// <summary>ขนาดหน้าจอเปลี่ยน (ย่อ/ขยายหน้าต่าง) ต้องจัดกล้องใหม่</summary>
        public bool NeedsRefit
        {
            get { return Screen.width != fitWidth || Screen.height != fitHeight; }
        }

        /// <summary>กรอบบนหน้าจอ (พิกเซล) ที่ครอบพื้นที่บนโต๊ะช่วง x0..x1, z0..z1</summary>
        public Rect ScreenBounds(float x0, float x1, float z0, float z1)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            Vector3[] corners =
            {
                new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z0),
                new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1)
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 screen = Cam.WorldToScreenPoint(corner);

                min = Vector2.Min(min, new Vector2(screen.x, screen.y));
                max = Vector2.Max(max, new Vector2(screen.x, screen.y));
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>จุดบนหน้าจออยู่เหนือสนามหญ้า (ใช้ตอนลากการ์ดจากมือมาปล่อย)</summary>
        public bool IsOverBoard(Vector2 screenPoint)
        {
            if (Cam == null)
                return false;

            Ray ray = Cam.ScreenPointToRay(screenPoint);

            if (Mathf.Abs(ray.direction.y) < 0.0001f)
                return false;

            float t = -ray.origin.y / ray.direction.y;

            if (t < 0f)
                return false;

            Vector3 hit = ray.origin + ray.direction * t;

            return Mathf.Abs(hit.x) <= BoardHalfWidth + 0.3f &&
                   Mathf.Abs(hit.z) <= BoardHalfDepth + 0.3f;
        }

        // ---------- ตำแหน่งการ์ด ----------

        public CardHolder3D CreateFieldHolder(bool enemySide, int index, int count, Vector2 pixelSize)
        {
            float spacing = Mathf.Min(2.05f, 14.4f / Mathf.Max(1, count));

            float x = (index - (count - 1) * 0.5f) * spacing;
            float z = enemySide ? FieldRowZ : -FieldRowZ;

            float height = 0.04f + pixelSize.y * CardUnit * 0.5f * Mathf.Cos(FieldTilt * Mathf.Deg2Rad);

            CardHolder3D holder = CardHolder3D.Create(
                transform, Cam, pixelSize,
                new Vector3(x, height, z),
                Quaternion.Euler(FieldTilt, 0f, 0f),
                new Vector3(0f, 0.3f, -0.12f));

            holder.BobPhase = index * 1.3f + (enemySide ? 2f : 0f);

            return holder;
        }

        public CardHolder3D CreateHandHolder(int index, int count, Vector2 pixelSize)
        {
            float spacing = Mathf.Min(1.55f, 10.8f / Mathf.Max(1, count));

            float offset = index - (count - 1) * 0.5f;

            // พัด: กลางสูง ปลายต่ำและเอียงออก ใบหลังๆ ซ้อนทับใบก่อนหน้า
            Vector3 position = new Vector3(
                offset * spacing,
                1.3f - 0.03f * offset * offset + index * 0.01f,
                -5.4f - 0.02f * offset * offset - index * 0.01f);

            return CardHolder3D.Create(
                transform, Cam, pixelSize,
                position,
                Quaternion.Euler(HandTilt, 0f, -offset * 2.4f),
                new Vector3(0f, 0.7f, -0.7f));
        }
    }

    /// <summary>
    /// การ์ดหนึ่งใบบนโต๊ะ 3D: Canvas แบบ World Space (ขนาดตามพิกเซล x 0.01) สำหรับ CardView
    /// + กล่องบางๆ ด้านหลังเป็นตัวการ์ด (มีความหนาและเงา) เอาเมาส์ชี้แล้วลอยขึ้นเข้าหากล้อง
    /// ปุ่ม/ลาก/ชี้ของ CardView ทำงานผ่าน GraphicRaycaster ของ Canvas นี้ (ใช้ EventSystem เดิม)
    /// </summary>
    public class CardHolder3D : MonoBehaviour
    {
        private static Material bodyMaterial;

        public RectTransform Rect { get; private set; }

        private GameObject body;
        private CanvasGroup face;

        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Vector3 hoverOffset;
        private float hover;
        private bool hovered;
        private Vector2 tilt;
        private Camera viewCamera;

        /// <summary>ลอยขึ้นลงเบาๆ ตอนอยู่นิ่ง (เฟสต่างกันตามช่อง) ไม่มี = นิ่ง</summary>
        public float BobPhase = -1f;

        public static CardHolder3D Create(
            Transform parent, Camera camera, Vector2 pixelSize,
            Vector3 position, Quaternion rotation, Vector3 hoverOffset)
        {
            GameObject go = new GameObject("CardHolder");

            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;

            CardHolder3D holder = go.AddComponent<CardHolder3D>();

            holder.basePosition = position;
            holder.baseRotation = rotation;
            holder.hoverOffset = hoverOffset;
            holder.viewCamera = camera;

            // Canvas ของหน้าการ์ด (มองเห็นจากด้าน -z ของ holder)
            GameObject canvasObject = new GameObject("CardCanvas", typeof(RectTransform));

            canvasObject.transform.SetParent(go.transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            UnityEngine.UI.CanvasScaler scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();

            scaler.dynamicPixelsPerUnit = 1.5f;

            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            RectTransform rect = canvasObject.GetComponent<RectTransform>();

            rect.sizeDelta = pixelSize;
            rect.localScale = Vector3.one * BattleStage3D.CardUnit;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;

            holder.Rect = rect;

            // ตัวการ์ด: กล่องบางด้านหลังหน้าการ์ด
            if (bodyMaterial == null)
            {
                Shader shader = Shader.Find("Standard");

                if (shader == null)
                    shader = Shader.Find("Legacy Shaders/Diffuse");

                bodyMaterial = new Material(shader);
                bodyMaterial.color = new Color(0.78f, 0.58f, 0.24f, 1f);

                if (bodyMaterial.HasProperty("_Glossiness"))
                    bodyMaterial.SetFloat("_Glossiness", 0.55f);

                if (bodyMaterial.HasProperty("_Metallic"))
                    bodyMaterial.SetFloat("_Metallic", 0.35f);
            }

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);

            body.name = "CardBody";

            Collider collider = body.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(
                pixelSize.x * BattleStage3D.CardUnit - 0.05f,
                pixelSize.y * BattleStage3D.CardUnit - 0.05f,
                0.09f);
            body.transform.localPosition = new Vector3(0f, 0f, 0.05f);

            Renderer renderer = body.GetComponent<Renderer>();

            renderer.sharedMaterial = bodyMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;

            holder.body = body;

            return holder;
        }

        /// <summary>ผูกกับ CardView: ใช้ซ่อนตัวการ์ดตอนหน้าการ์ดโปร่งใส และยกการ์ดลอยเมื่อชี้</summary>
        public void Bind(CardView view)
        {
            if (view == null)
                return;

            face = view.GetComponent<CanvasGroup>();

            view.HoverEntered += OnHoverEntered;
            view.HoverExited += OnHoverExited;
        }

        private void OnHoverEntered(CardView view)
        {
            hovered = true;

            MultiverseCardBattle.Core.Sfx.Play(MultiverseCardBattle.Core.SoundId.Hover);
        }

        private void OnHoverExited(CardView view)
        {
            hovered = false;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            float target = hovered ? 1f : 0f;

            hover = Mathf.Lerp(hover, target, 1f - Mathf.Exp(-16f * dt));

            // ยกขึ้นตามแกนของโลก (เข้าหากล้องและสูงขึ้น)
            Vector3 position = basePosition + hoverOffset * hover;

            if (BobPhase >= 0f)
                position.y += Mathf.Sin(Time.unscaledTime * 1.5f + BobPhase) * 0.025f * (1f - hover);

            transform.localPosition = position;

            // ชี้แล้วการ์ดเอียงเข้าหาเมาส์: ขอบด้านเมาส์จมลง
            Vector2 wanted = Vector2.zero;

            if (hovered && viewCamera != null && UnityEngine.InputSystem.Pointer.current != null)
            {
                Vector2 pointer = UnityEngine.InputSystem.Pointer.current.position.ReadValue();

                Vector3 center = viewCamera.WorldToScreenPoint(transform.position);
                Vector3 edge = viewCamera.WorldToScreenPoint(transform.position + transform.right * 0.75f);
                Vector3 top = viewCamera.WorldToScreenPoint(transform.position + transform.up * 1.05f);

                float halfWidth = Mathf.Max(8f, Mathf.Abs(edge.x - center.x));
                float halfHeight = Mathf.Max(8f, Mathf.Abs(top.y - center.y));

                wanted = new Vector2(
                    Mathf.Clamp((pointer.x - center.x) / halfWidth, -1f, 1f),
                    Mathf.Clamp((pointer.y - center.y) / halfHeight, -1f, 1f));
            }

            tilt = Vector2.Lerp(tilt, wanted, 1f - Mathf.Exp(-14f * dt));

            transform.localRotation = baseRotation * Quaternion.Euler(tilt.y * 9f, -tilt.x * 11f, 0f);
        }

        private void LateUpdate()
        {
            if (body != null && face != null)
            {
                bool visible = face.alpha > 0.02f;

                if (body.activeSelf != visible)
                    body.SetActive(visible);
            }
        }

        /// <summary>เลิกใช้: ปิดทันที (กันรับเหตุการณ์ซ้ำ) แล้วทำลาย</summary>
        public static void Dispose(List<CardHolder3D> holders)
        {
            foreach (CardHolder3D holder in holders)
            {
                if (holder == null)
                    continue;

                holder.gameObject.SetActive(false);
                holder.transform.SetParent(null, false);

                Destroy(holder.gameObject);
            }

            holders.Clear();
        }
    }
}

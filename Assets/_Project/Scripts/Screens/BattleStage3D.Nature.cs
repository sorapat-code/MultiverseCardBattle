using System.Collections.Generic;
using UnityEngine;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ของตกแต่งสองข้างและด้านหลังโต๊ะจากโมเดลจริง: Quaternius "Ultimate Stylized Nature Pack" (CC0 / สาธารณสมบัติ)
    /// อยู่ใน Resources/Nature (FBX + texture ย่อเหลือ 512px) โหลดด้วย Resources.Load และสวม Material
    /// เชดเดอร์ MCB/FoliageCutout ตามชื่อ material ในโมเดล (เช่น PineTree_Leaves -> Textures/PineTree_Leaves)
    /// ถ้าหาโมเดลไม่เจอ ให้ตัวสร้างพุ่มไม้แบบเดิม (ทรงกลม) ทำงานแทน
    /// </summary>
    public partial class BattleStage3D
    {
        private const float GroundY = -0.32f;

        private static Shader foliageShader;
        private readonly Dictionary<string, Material> natureMaterials = new Dictionary<string, Material>();

        private struct Spot
        {
            public float X;
            public float Z;
            public float Radius;
        }

        private readonly List<Spot> spots = new List<Spot>();

        private Material NatureMaterial(Material source)
        {
            string key = source != null ? source.name.Replace(" (Instance)", "").Trim() : "";

            Material cached;

            if (natureMaterials.TryGetValue(key, out cached))
                return cached;

            if (foliageShader == null)
                foliageShader = Resources.Load<Shader>("MCBFoliage");

            if (foliageShader == null)
                return source;

            string textureName = key == "Rock" ? "Rocks" : key;

            Texture2D texture = Resources.Load<Texture2D>("Nature/Textures/" + textureName);

            Material material = new Material(foliageShader);

            bool leafy = key.Contains("Leaves") || key == "Flowers" || key == "Grass";

            if (texture != null)
            {
                material.mainTexture = texture;
                material.color = leafy ? new Color(0.62f, 0.78f, 0.68f, 1f) : new Color(0.7f, 0.7f, 0.76f, 1f);
            }
            else
            {
                material.color = source != null ? source.color : new Color(0.3f, 0.5f, 0.3f, 1f);
            }

            material.SetFloat("_UseAlpha", leafy && texture != null ? 1f : 0f);
            material.SetFloat("_Cutoff", 0.45f);

            natureMaterials[key] = material;

            return material;
        }

        // วางโมเดลหนึ่งชิ้น: ปรับให้สูงตามที่ต้องการ ฐานแตะพื้น สวม material เอง
        private GameObject Spawn(string path, Transform parent, float x, float z, float height, float yaw, float sink)
        {
            GameObject prefab = Resources.Load<GameObject>(path);

            // โมเดล Quaternius ต้องสวม material ของเราเอง; ของจากแพ็คอื่น (NatureAsset) ใช้ material เดิมของมัน
            bool swapMaterials = path.StartsWith("Nature/");

            string model = path.Substring(path.LastIndexOf('/') + 1);

            if (prefab == null)
                return null;

            GameObject go = Instantiate(prefab, parent);

            go.name = model;
            go.transform.localPosition = new Vector3(x, 0f, z);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

            // ขนาดจริงของโมเดล (หน่วยของ prefab) วัดจาก bounds ของ mesh ในพิกัดของราก
            // ไม่ใช้ Renderer.bounds เพราะอาจยังไม่อัปเดตทันทีหลัง Instantiate
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            bool found = false;

            Matrix4x4 toRoot = go.transform.worldToLocalMatrix;

            foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                    continue;

                Matrix4x4 matrix = toRoot * filter.transform.localToWorldMatrix;

                Bounds meshBounds = filter.sharedMesh.bounds;

                for (int cx = -1; cx <= 1; cx += 2)
                {
                    for (int cy = -1; cy <= 1; cy += 2)
                    {
                        for (int cz = -1; cz <= 1; cz += 2)
                        {
                            Vector3 corner = matrix.MultiplyPoint3x4(
                                meshBounds.center + Vector3.Scale(meshBounds.extents, new Vector3(cx, cy, cz)));

                            min = Vector3.Min(min, corner);
                            max = Vector3.Max(max, corner);
                            found = true;
                        }
                    }
                }
            }

            if (!found || renderers.Length == 0 || max.y - min.y < 0.00001f && max.x - min.x < 0.00001f)
            {
                Destroy(go);
                return null;
            }

            // ชิ้นที่นอนราบ (ท่อนไม้ กิ่งไม้) สูงนิดเดียว ถ้าวัดแค่ความสูงจะถูกขยายมหาศาล
            // จึงใช้ค่าอ้างอิงที่ไม่ต่ำกว่า 60% ของด้านที่ยาวที่สุด
            Vector3 size = max - min;

            float reference = Mathf.Max(size.y, 0.6f * Mathf.Max(size.x, size.z));

            float scale = height / reference;

            go.transform.localScale = Vector3.one * scale;
            go.transform.localPosition = new Vector3(x, GroundY - sink - min.y * scale, z);

            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;

                if (swapMaterials)
                {
                    for (int i = 0; i < materials.Length; i++)
                        materials[i] = NatureMaterial(materials[i]);

                    renderer.sharedMaterials = materials;
                }

                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            return go;
        }

        private bool Free(float x, float z, float radius)
        {
            // เสาหินสี่มุม
            float px = BoardHalfWidth + 1.15f;
            float pz = BoardHalfDepth + 1.0f;

            foreach (float sx in new[] { -px, px })
            {
                foreach (float sz in new[] { -pz, pz })
                {
                    if ((x - sx) * (x - sx) + (z - sz) * (z - sz) < (radius + 1.1f) * (radius + 1.1f))
                        return false;
                }
            }

            foreach (Spot spot in spots)
            {
                float dx = x - spot.X;
                float dz = z - spot.Z;
                float r = radius + spot.Radius;

                if (dx * dx + dz * dz < r * r)
                    return false;
            }

            return true;
        }

        // สุ่มวางจนกว่าจะได้ที่ว่าง (สูงสุด 12 ครั้ง)
        private bool Place(
            string[] models, Transform parent, float xMin, float xMax, float zMin, float zMax,
            float heightMin, float heightMax, float radius, float sink)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float x = Random.Range(xMin, xMax);
                float z = Random.Range(zMin, zMax);

                if (!Free(x, z, radius))
                    continue;

                string model = models[Random.Range(0, models.Length)];

                GameObject go = Spawn(
                    model, parent, x, z, Random.Range(heightMin, heightMax), Random.Range(0f, 360f), sink);

                if (go == null)
                    return false;

                spots.Add(new Spot { X = x, Z = z, Radius = radius });

                return true;
            }

            return true;
        }

        private static string[] Q(params string[] names)
        {
            string[] paths = new string[names.Length];

            for (int i = 0; i < names.Length; i++)
                paths[i] = "Nature/" + names[i];

            return paths;
        }

        // ชื่อ prefab ทั้งหมดในโฟลเดอร์ Resources/NatureAsset/<category> (ที่เครื่องมือ Collect สร้างให้)
        private static string[] PackPaths(string category)
        {
            GameObject[] prefabs = Resources.LoadAll<GameObject>("NatureAsset/" + category);

            string[] paths = new string[prefabs.Length];

            for (int i = 0; i < prefabs.Length; i++)
                paths[i] = "NatureAsset/" + category + "/" + prefabs[i].name;

            return paths;
        }

        /// <summary>คืน false ถ้าไม่มีโมเดล (ให้ใช้ฉากพุ่มไม้ทรงกลมแทน)</summary>
        private bool BuildNature()
        {
            string[] pines;
            string[] leafy;
            string[] dead;
            string[] bushes;
            string[] rocks;
            string[] flowers;

            string[] packTrees = PackPaths("Trees");

            if (packTrees.Length > 0)
            {
                // แพ็คที่นำเข้ามา (เช่น Ultimate Nature Starter) — ใช้ต้นไม้ทั้งหมดผสมกัน
                pines = packTrees;
                leafy = packTrees;
                dead = packTrees;

                string[] packBushes = PackPaths("Bushes");
                string[] packRocks = PackPaths("Rocks");
                string[] packSmall = PackPaths("Small");

                bushes = packBushes.Length > 0 ? packBushes : packTrees;
                rocks = packRocks.Length > 0 ? packRocks : bushes;
                flowers = packSmall.Length > 0 ? packSmall : bushes;
            }
            else
            {
                if (Resources.Load<GameObject>("Nature/PineTree_1") == null)
                    return false;

                pines = Q("PineTree_1", "PineTree_2", "PineTree_3");
                leafy = Q("NormalTree_1", "NormalTree_2", "NormalTree_3", "BirchTree_1", "BirchTree_2", "MapleTree_1");
                dead = Q("DeadTree_1", "DeadTree_2", "DeadTree_3");
                bushes = Q("Bush", "Bush_Large", "Bush_Large_Flowers", "Bush_Flowers", "Bush_Small");
                rocks = Q("Rock_1", "Rock_2", "Rock_3", "Rock_4", "Rock_5");
                flowers = Q("Flower_1_Clump", "Flower_2_Clump", "Flower_3_Clump", "Plant_1", "Grass_Small");
            }

            Random.State saved = Random.state;

            Random.InitState(20261012);

            Transform folder = new GameObject("Nature").transform;

            folder.SetParent(transform, false);

            float edge = BoardHalfWidth + 0.95f;

            // แถวหลังสุด: แนวต้นไม้ใหญ่ปิดฉาก (สนสลับต้นไม้ มีต้นไม้แห้งแซม)
            for (int i = 0; i < 16; i++)
            {
                float x = Mathf.Lerp(-16f, 16f, i / 15f) + Random.Range(-0.8f, 0.8f);

                string[] set = Random.value < 0.55f ? pines : (Random.value < 0.8f ? leafy : dead);

                Place(set, folder, x - 0.01f, x + 0.01f, 8.4f, 11.5f, 6.5f, 10f, 1.5f, 0f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                // ต้นไม้ใหญ่ข้างโต๊ะ (ไกลออกไป)
                for (int i = 0; i < 9; i++)
                {
                    float z = Mathf.Lerp(-6f, 8f, i / 8f);

                    string[] set = Random.value < 0.5f ? pines : (Random.value < 0.85f ? leafy : dead);

                    Place(set, folder, side * (edge + 2.6f), side * (edge + 5.6f), z - 0.4f, z + 0.4f, 5f, 8.5f, 1.4f, 0f);
                }

                // พุ่มไม้ชิดกรอบ
                for (int i = 0; i < 9; i++)
                {
                    float z = Mathf.Lerp(-4.5f, 6f, i / 8f);

                    Place(bushes, folder, side * (edge + 0.2f), side * (edge + 1.8f), z - 0.3f, z + 0.3f, 1.0f, 1.9f, 0.9f, 0.05f);
                }

                // ก้อนหิน
                for (int i = 0; i < 5; i++)
                {
                    Place(rocks, folder, side * (edge + 0.1f), side * (edge + 2.6f), -5.5f, 7.5f, 0.7f, 1.8f, 0.8f, 0.1f);
                }

                // ดอกไม้/หญ้ากอเล็ก
                for (int i = 0; i < 12; i++)
                {
                    Place(flowers, folder, side * (edge + 0.1f), side * (edge + 1.5f), -5f, 6.5f, 0.35f, 0.7f, 0.35f, 0f);
                }
            }

            // แนวพุ่ม/หิน/ดอกไม้หลังโต๊ะ
            for (int i = 0; i < 14; i++)
            {
                float x = Mathf.Lerp(-edge, edge, i / 13f);

                Place(bushes, folder, x - 0.2f, x + 0.2f, BoardHalfDepth + 1.7f, BoardHalfDepth + 3.2f, 1.0f, 1.9f, 0.9f, 0.05f);
            }

            for (int i = 0; i < 4; i++)
                Place(rocks, folder, -edge, edge, BoardHalfDepth + 1.7f, BoardHalfDepth + 3f, 0.8f, 1.8f, 0.8f, 0.1f);

            for (int i = 0; i < 14; i++)
                Place(flowers, folder, -edge, edge, BoardHalfDepth + 1.3f, BoardHalfDepth + 2.2f, 0.35f, 0.7f, 0.35f, 0f);

            Random.state = saved;

            return true;
        }
    }
}

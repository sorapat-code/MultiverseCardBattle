using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MultiverseCardBattle.Editor
{
    /// <summary>
    /// รวบรวม prefab ธรรมชาติจากแพ็คที่นำเข้ามา (เช่น Ultimate Nature Starter) เข้า Resources/NatureAsset
    /// เพื่อให้โต๊ะ 3D โหลดไปวางสองข้างได้ แยกหมวดจากชื่อ prefab:
    ///   Trees  = tree, pine, spruce, birch, oak, palm, fir
    ///   Rocks  = rock, stone, boulder, cliff
    ///   Bushes = bush, shrub, fern, plant
    ///   Small  = flower, grass, mushroom, stump, log, branch, clover, pebble
    /// ใช้: เลือกโฟลเดอร์ของแพ็คใน Project แล้วเลือกเมนู Multiverse Card Battle/Assets/Collect Nature Prefabs (Selected Folder)
    /// (ข้ามโฟลเดอร์ Demo; prefab ที่คัดลอกยังอ้าง mesh/material ในแพ็คเดิม)
    /// </summary>
    public static class CollectNaturePrefabs
    {
        private const string Target = "Assets/_Project/Resources/NatureAsset";

        private static readonly string[][] Categories =
        {
            new[] { "Trees", "tree", "pine", "spruce", "birch", "oak", "palm", "fir" },
            new[] { "Rocks", "rock", "stone", "boulder", "cliff" },
            new[] { "Bushes", "bush", "shrub", "fern", "plant" },
            new[] { "Small", "flower", "grass", "mushroom", "stump", "log", "branch", "clover", "pebble" }
        };

        [MenuItem("Multiverse Card Battle/Assets/Collect Nature Prefabs (Selected Folder)")]
        public static void CollectSelected()
        {
            Object selected = Selection.activeObject;

            string folder = selected != null ? AssetDatabase.GetAssetPath(selected) : "";

            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog(
                    "Collect Nature Prefabs", "เลือกโฟลเดอร์ของแพ็คในหน้าต่าง Project ก่อน", "OK");
                return;
            }

            // ล้างของเก่า
            if (AssetDatabase.IsValidFolder(Target))
                AssetDatabase.DeleteAsset(Target);

            Directory.CreateDirectory(Target);

            AssetDatabase.Refresh();

            Dictionary<string, int> counts = new Dictionary<string, int>();
            List<string> skipped = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                string lowerPath = path.ToLowerInvariant();

                if (lowerPath.Contains("/demo/") || lowerPath.Contains("/documentation/"))
                    continue;

                string name = Path.GetFileNameWithoutExtension(path);
                string lower = name.ToLowerInvariant();

                string category = null;

                // เช็ก Small ก่อน (stump/log/branch ไม่ใช่ต้นไม้) แล้วค่อย Rocks, Trees, Bushes
                foreach (int index in new[] { 3, 1, 0, 2 })
                {
                    string[] entry = Categories[index];

                    for (int k = 1; k < entry.Length && category == null; k++)
                    {
                        if (lower.Contains(entry[k]))
                            category = entry[0];
                    }

                    if (category != null)
                        break;
                }

                if (category == null)
                {
                    skipped.Add(name);
                    continue;
                }

                string folderPath = Target + "/" + category;

                if (!AssetDatabase.IsValidFolder(folderPath))
                    AssetDatabase.CreateFolder(Target, category);

                if (AssetDatabase.CopyAsset(path, folderPath + "/" + name + ".prefab"))
                {
                    int count;

                    counts.TryGetValue(category, out count);
                    counts[category] = count + 1;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary = "";

            foreach (KeyValuePair<string, int> pair in counts)
                summary += pair.Key + ": " + pair.Value + "  ";

            Debug.Log("[CollectNaturePrefabs] " + (summary.Length > 0 ? summary : "ไม่พบ prefab ที่ตรงชื่อ") +
                (skipped.Count > 0 ? "\nข้าม (ชื่อไม่ตรงหมวดใด): " + string.Join(", ", skipped) : ""));
        }
    }
}

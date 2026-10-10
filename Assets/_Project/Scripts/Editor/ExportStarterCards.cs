using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Effects;

namespace MultiverseCardBattle.Editor
{
    /// <summary>
    /// ส่งออกการ์ดตั้งต้นจากโค้ด (StarterCardLibrary) เป็น CardData asset
    /// ไว้ที่ Assets/_Project/Resources/Cards เพื่อให้แก้ใน Inspector ได้
    /// CardRegistry ให้ asset มีสิทธิ์เหนือการ์ดจากโค้ดที่ ID ตรงกัน
    /// ข้ามการ์ดที่มี asset อยู่แล้ว จึงเรียกซ้ำได้ปลอดภัย
    /// </summary>
    public static class ExportStarterCards
    {
        private const string Folder = "Assets/_Project/Resources/Cards";

        [MenuItem("Multiverse Card Battle/Export Starter Cards")]
        public static void Export()
        {
            Directory.CreateDirectory(Folder);

            Dictionary<string, CardData> created =
                new Dictionary<string, CardData>();

            int skipped = 0;

            foreach (CardData source in StarterCardLibrary.All)
            {
                string path = Folder + "/" + source.CardID + ".asset";

                if (File.Exists(path))
                {
                    skipped++;
                    continue;
                }

                CardData copy = Object.Instantiate(source);

                copy.name = source.CardID;

                AssetDatabase.CreateAsset(copy, path);

                created[source.CardID] = copy;
            }

            // Effect ที่อ้างถึงการ์ดอื่น (CreateCard / Transform) ต้องชี้ไปที่ asset
            foreach (CardData asset in created.Values)
            {
                RemapReferences(asset);

                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            CardRegistry.Reload();

            Debug.Log(
                "Export Starter Cards: created " + created.Count +
                ", skipped " + skipped + " (already exist)");
        }

        private static void RemapReferences(CardData asset)
        {
            // Evolve ชี้ไปที่ร่างใหม่ ซึ่งต้องเป็น asset ด้วย
            if (asset.EvolvesInto != null)
            {
                CardData evolved = AssetDatabase.LoadAssetAtPath<CardData>(
                    Folder + "/" + asset.EvolvesInto.CardID + ".asset");

                if (evolved != null)
                    asset.EvolvesInto = evolved;
            }

            foreach (CardActionData action in asset.Actions)
            {
                foreach (EffectData effect in action.Effects)
                {
                    if (effect == null || effect.Card == null)
                        continue;

                    string path =
                        Folder + "/" + effect.Card.CardID + ".asset";

                    CardData target =
                        AssetDatabase.LoadAssetAtPath<CardData>(path);

                    if (target != null)
                        effect.Card = target;
                }
            }
        }
    }
}

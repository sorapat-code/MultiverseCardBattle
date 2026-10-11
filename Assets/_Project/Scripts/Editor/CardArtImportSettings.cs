using UnityEditor;
using UnityEngine;

namespace MultiverseCardBattle.Editor
{
    /// <summary>
    /// ตั้งค่า Import รูปใน Resources/CardArt ให้เป็น Texture ธรรมดา (ไม่ใช่ Sprite Sheet)
    /// เพราะ CardArtLoader สร้าง Sprite เองตอนรันอยู่แล้ว
    /// แก้คำเตือน "Sprite 'XXX_0' not generated because the rect lies (partially) outside of texture"
    /// ที่เกิดเมื่อเปลี่ยนรูปเป็นขนาดใหม่แต่ .meta ยังจำ rect ของรูปเก่า
    /// </summary>
    public class CardArtImportSettings : AssetPostprocessor
    {
        private const string Folder = "Assets/_Project/Resources/CardArt/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder))
                return;

            TextureImporter importer = (TextureImporter)assetImporter;

            importer.textureType = TextureImporterType.Default;
            importer.spriteImportMode = SpriteImportMode.None;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
        }

        [MenuItem("Multiverse Card Battle/Card Art/Reimport All Card Art")]
        public static void ReimportAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder.TrimEnd('/') });

            for (int i = 0; i < guids.Length; i++)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guids[i]),
                    ImportAssetOptions.ForceUpdate);
            }

            Debug.Log("Reimported " + guids.Length + " card art textures.");
        }
    }
}

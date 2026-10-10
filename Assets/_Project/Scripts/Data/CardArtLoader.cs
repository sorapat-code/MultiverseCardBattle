using System.Collections.Generic;
using UnityEngine;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// โหลดรูปการ์ดจาก Assets/_Project/Resources/CardArt/{CardID}.png
    /// อ่านเป็น Texture แล้วสร้าง Sprite เอง จึงไม่ต้องตั้งค่า Import เป็น Sprite
    /// ไม่มีไฟล์ = ไม่มีรูป (การ์ดแสดงตัวอักษรย่อตามเดิม)
    /// </summary>
    public static class CardArtLoader
    {
        private const string Folder = "CardArt/";

        private static readonly Dictionary<string, Sprite> cache =
            new Dictionary<string, Sprite>();

        public static Sprite Load(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return null;

            Sprite sprite;

            if (cache.TryGetValue(cardId, out sprite) && sprite != null)
                return sprite;

            sprite = null;

            Texture2D texture = Resources.Load<Texture2D>(Folder + cardId);

            if (texture != null)
            {
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);

                sprite.name = cardId;
                sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }

            cache[cardId] = sprite;

            return sprite;
        }
    }
}

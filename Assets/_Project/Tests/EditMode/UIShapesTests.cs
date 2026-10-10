using NUnit.Framework;
using UnityEngine;
using MultiverseCardBattle.Screens;

namespace MultiverseCardBattle.Tests
{
    public class UIShapesTests
    {
        [Test]
        public void Sprites_AreBuilt()
        {
            Assert.IsNotNull(UIShapes.Circle);
            Assert.IsNotNull(UIShapes.Rounded);

            Assert.AreEqual(16f, UIShapes.Rounded.border.x);
        }

        [Test]
        public void Circle_IsTransparentAtCornersAndOpaqueAtCenter()
        {
            Texture2D texture = UIShapes.Circle.texture;

            Assert.AreEqual(0f, texture.GetPixel(0, 0).a, 0.01f);
            Assert.AreEqual(1f, texture.GetPixel(64, 64).a, 0.01f);
        }

        [Test]
        public void Rounded_IsTransparentAtCornersAndOpaqueAtCenter()
        {
            Texture2D texture = UIShapes.Rounded.texture;

            Assert.AreEqual(0f, texture.GetPixel(0, 0).a, 0.01f);
            Assert.AreEqual(1f, texture.GetPixel(32, 32).a, 0.01f);
        }

        [Test]
        public void FantasySprites_AreBuilt()
        {
            Assert.IsNotNull(UIShapes.Bevel);
            Assert.IsNotNull(UIShapes.Ring);
            Assert.IsNotNull(UIShapes.MenuBackdrop);
            Assert.IsNotNull(UIShapes.BattleBackdrop);
            Assert.IsNotNull(UIShapes.Divider);
        }

        [Test]
        public void PortraitName_StripsSymbolsAndLowercases()
        {
            Assert.AreEqual("portrait_darkking", UIArt.PortraitName("Dark King"));
            Assert.AreEqual("portrait_", UIArt.PortraitName(""));
        }

        [Test]
        public void UIArt_MissingFileReturnsNull()
        {
            Assert.IsNull(UIArt.Get("this_file_does_not_exist"));
        }

        [Test]
        public void EffectSprites_AreBuiltWithExpectedAlpha()
        {
            Assert.IsNotNull(UIShapes.Glow);
            Assert.IsNotNull(UIShapes.Vignette);
            Assert.IsNotNull(UIShapes.RingCircle);

            Texture2D glow = UIShapes.Glow.texture;

            Assert.AreEqual(0f, glow.GetPixel(0, 0).a, 0.01f);
            Assert.Greater(glow.GetPixel(32, 32).a, 0.9f);

            Texture2D vignette = UIShapes.Vignette.texture;

            Assert.AreEqual(0f, vignette.GetPixel(128, 72).a, 0.01f);
            Assert.Greater(vignette.GetPixel(0, 0).a, 0.3f);
        }
    }
}

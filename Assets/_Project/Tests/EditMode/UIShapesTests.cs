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
    }
}

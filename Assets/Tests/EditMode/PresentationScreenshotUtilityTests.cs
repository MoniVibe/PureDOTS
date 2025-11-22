using NUnit.Framework;
using PureDOTS.Runtime.Presentation;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Tests.Editor
{
    public class PresentationScreenshotUtilityTests
    {
        [Test]
        public void ComputeHash_ChangesWhenPixelsChange()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.black });
            texture.Apply();

            Unity.Entities.Hash128 firstHash = PresentationScreenshotUtility.ComputeHash(texture);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            Unity.Entities.Hash128 secondHash = PresentationScreenshotUtility.ComputeHash(texture);
            Assert.AreNotEqual(firstHash, secondHash);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Compare_AllowsStyleChangeWhenRequested()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.red);
            texture.Apply();

            Unity.Entities.Hash128 baseline = PresentationScreenshotUtility.ComputeHash(texture);
            texture.SetPixel(0, 0, Color.blue);
            texture.Apply();

            var result = PresentationScreenshotUtility.Compare(texture, baseline, allowStyleChange: true);
            Assert.IsFalse(result.MatchesBaseline);
            Assert.IsTrue(result.AllowedDifference);

            Object.DestroyImmediate(texture);
        }
    }
}

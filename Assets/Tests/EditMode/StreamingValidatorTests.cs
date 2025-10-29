#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using PureDOTS.Authoring;
using PureDOTS.Editor.Streaming;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

namespace PureDOTS.Tests.Editor
{
    public sealed class StreamingValidatorTests
    {
        [Test]
        public void GatherIssues_FlagsMissingGuidOverlapAndRadiusOnce()
        {
            var subSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene.unity");
            Assert.IsNotNull(subSceneAsset, "Expected SampleScene.unity to exist for validator test.");

            var subSceneGO = new GameObject("ValidatorTest_SubScene");
            var subScene = subSceneGO.AddComponent<SubScene>();
            subScene.SceneAsset = subSceneAsset;

            var sectionAObject = new GameObject("ValidatorSectionA");
            var sectionA = sectionAObject.AddComponent<StreamingSectionAuthoring>();
            sectionA.useTransformCenter = false;
            sectionA.manualCenter = Vector3.zero;
            sectionA.enterRadius = 0f; // triggers non-positive radius
            sectionA.exitRadius = 1f;
            sectionA.subScene = null; // triggers missing GUID

            var sectionBObject = new GameObject("ValidatorSectionB");
            var sectionB = sectionBObject.AddComponent<StreamingSectionAuthoring>();
            sectionB.useTransformCenter = false;
            sectionB.manualCenter = new Vector3(0.4f, 0f, 0f);
            sectionB.enterRadius = 1f;
            sectionB.exitRadius = 1f;
            sectionB.subScene = subScene;

            try
            {
                var issues = StreamingValidator.GatherIssues();

                Assert.AreEqual(3, issues.Count, "Expected exactly three validation issues.");
                Assert.AreEqual(1, issues.Count(message => message.Contains("missing a SubScene reference")), "Missing GUID reported multiple times.");
                Assert.AreEqual(1, issues.Count(message => message.Contains("non-positive Enter Radius")), "Zero radius reported multiple times.");
                Assert.AreEqual(1, issues.Count(message => message.Contains("overlap")), "Overlap reported multiple times.");
            }
            finally
            {
                Object.DestroyImmediate(sectionAObject);
                Object.DestroyImmediate(sectionBObject);
                Object.DestroyImmediate(subSceneGO);
            }
        }
    }
}
#endif

using System;
using System.IO;
using NUnit.Framework;
using PureDOTS.Runtime.Config;

namespace PureDOTS.Tests
{
    public class RuntimeConfigTests
    {
        private string _configPath;

        [SetUp]
        public void SetUp()
        {
            RuntimeConfigRegistry.ResetForTests();
            _configPath = Path.Combine(Path.GetTempPath(), $"puredots_config_test_{Guid.NewGuid():N}.cfg");
            RuntimeConfigRegistry.StoragePath = _configPath;
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_configPath))
            {
                File.Delete(_configPath);
            }
        }

        [Test]
        public void Initialize_RegistersManualPhaseVars()
        {
            RuntimeConfigRegistry.Initialize();
            Assert.IsTrue(RuntimeConfigRegistry.TryGetVar("phase.camera.enabled", out var cameraVar));
            Assert.AreEqual("1", cameraVar.Value);
            Assert.IsTrue(RuntimeConfigRegistry.TryGetVar("camera.ecs.enabled", out var ecsVar));
            Assert.AreEqual("0", ecsVar.Value);
        }

        [Test]
        public void SaveAndLoad_PersistsValues()
        {
            RuntimeConfigRegistry.Initialize();
            RuntimeConfigRegistry.SetValue("phase.camera.enabled", "0", out _);
            RuntimeConfigRegistry.Save();

            RuntimeConfigRegistry.ResetForTests();
            RuntimeConfigRegistry.StoragePath = _configPath;
            RuntimeConfigRegistry.Initialize();

            Assert.IsTrue(RuntimeConfigRegistry.TryGetVar("phase.camera.enabled", out var cameraVar));
            Assert.AreEqual("0", cameraVar.Value);
        }
    }
}



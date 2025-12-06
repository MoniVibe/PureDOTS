using PureDOTS.Environment;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Bootstraps planet physical profile singleton from world configuration.
    /// Computes baseline coefficients once at world creation.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PlanetPhysicalBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // This system runs once during initialization
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Only run if profile doesn't exist
            if (SystemAPI.HasSingleton<PlanetPhysicalProfile>())
            {
                state.Enabled = false;
                return;
            }

            // Create default Earth-like profile
            // In a real implementation, this would read from world configuration
            var profile = PlanetPhysicalProfileBlob.Compute(
                mass: 5.972e24f,              // Earth mass (kg)
                radius: 6.371e6f,              // Earth radius (m)
                distanceToStar: 1.496e11f,     // 1 AU (m)
                starLuminosity: 3.828e26f,     // Solar luminosity (W)
                rotationRate: 7.292e-5f,       // Earth rotation (rad/s)
                axialTilt: 0.409f,             // ~23.4° (rad)
                compositionOxygen: 0.21f,      // 21% O₂
                compositionCO2: 0.0004f,       // 0.04% CO₂
                compositionNitrogen: 0.78f     // 78% N₂
            );

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<PlanetPhysicalProfileBlob>();
            root = profile;
            var blobAsset = builder.CreateBlobAssetReference<PlanetPhysicalProfileBlob>(Allocator.Persistent);
            builder.Dispose();

            var profileEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(profileEntity, new PlanetPhysicalProfile
            {
                Blob = blobAsset
            });

            state.Enabled = false;
        }
    }
}


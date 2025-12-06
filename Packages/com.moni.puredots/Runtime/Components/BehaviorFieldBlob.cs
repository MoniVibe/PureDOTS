using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// BlobAsset structure for behavior field coefficients.
    /// Race behavior profiles define "field coefficients" (social bias, aggression, fear).
    /// </summary>
    public struct BehaviorFieldCoefficientsBlob
    {
        public BlobString RaceId;            // Race identifier
        public float SocialBiasCoefficient;  // Social bias (attraction to same race)
        public float AggressionCoefficient;   // Aggression (repulsion strength)
        public float FearCoefficient;         // Fear (repulsion from threats)
        public float GoalAttractionCoefficient; // Goal attraction strength
        public float InfluenceDecayRate;      // How quickly influence decays with distance
        public float MaxInfluenceRadius;      // Maximum influence radius
    }

    /// <summary>
    /// Catalog of behavior field coefficient profiles.
    /// </summary>
    public struct BehaviorFieldCoefficientsCatalogBlob
    {
        public BlobArray<BehaviorFieldCoefficientsBlob> Profiles;
    }
}


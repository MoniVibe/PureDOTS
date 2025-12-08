using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using PureDOTS.Runtime.Economy.Ownership;

namespace PureDOTS.Authoring.Economy.Ownership
{
    /// <summary>
    /// ScriptableObject for authoring AssetSpec data.
    /// Fields: CapitalCost, Upkeep, OutputRate, OutputType (ResourceTypeId), WorkforceNeed.
    /// </summary>
    [CreateAssetMenu(fileName = "AssetSpec", menuName = "PureDOTS/Economy/Ownership/Asset Spec", order = 1)]
    public sealed class AssetSpecAuthoring : ScriptableObject
    {
        [Header("Asset Properties")]
        [SerializeField] private AssetType _assetType = AssetType.Mine;
        [SerializeField] private float _capitalCost = 1000f;
        [SerializeField] private float _upkeep = 10f;
        [SerializeField] private float _outputRate = 1f;
        [SerializeField] private string _outputType = "iron_ore";
        [SerializeField] private float _workforceNeed = 1f;

        public AssetType AssetType => _assetType;
        public float CapitalCost => _capitalCost;
        public float Upkeep => _upkeep;
        public float OutputRate => _outputRate;
        public string OutputType => _outputType;
        public float WorkforceNeed => _workforceNeed;

        /// <summary>
        /// Converts authoring data to AssetSpecBlob.
        /// </summary>
        public AssetSpecBlob ToBlob()
        {
            return new AssetSpecBlob
            {
                Type = _assetType,
                CapitalCost = _capitalCost,
                Upkeep = _upkeep,
                OutputRate = _outputRate,
                OutputType = new FixedString64Bytes(_outputType),
                WorkforceNeed = _workforceNeed
            };
        }
    }
}


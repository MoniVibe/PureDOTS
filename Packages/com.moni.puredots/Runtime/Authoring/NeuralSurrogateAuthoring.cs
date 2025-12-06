using System;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Authoring
{
    /// <summary>
    /// MonoBehaviour authoring for neural surrogate models.
    /// Allows designers to configure neural models in Unity editor.
    /// </summary>
    [CreateAssetMenu(menuName = "PureDOTS/Neural Surrogate Model", fileName = "NeuralSurrogateModel")]
    public class NeuralSurrogateAuthoring : ScriptableObject
    {
        public string modelId;
        public NeuralModelType type;
        public float[] weights; // Model weights (flattened)
        public int inputSize;
        public int outputSize;
        public int[] hiddenLayerSizes;

        public class NeuralSurrogateBaker : Baker<NeuralSurrogateAuthoring>
        {
            public override void Bake(NeuralSurrogateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<NeuralModelWeightsBlob>();

                builder.AllocateString(ref root.ModelId, authoring.modelId);
                root.Type = authoring.type;
                root.InputSize = authoring.inputSize;
                root.OutputSize = authoring.outputSize;
                root.HiddenLayerCount = authoring.hiddenLayerSizes?.Length ?? 0;

                // Allocate weights array
                var weightsArray = builder.Allocate(ref root.Weights, authoring.weights?.Length ?? 0);
                if (authoring.weights != null)
                {
                    for (int i = 0; i < authoring.weights.Length; i++)
                    {
                        weightsArray[i] = authoring.weights[i];
                    }
                }

                // Allocate hidden layer sizes array
                var hiddenSizesArray = builder.Allocate(ref root.HiddenLayerSizes, authoring.hiddenLayerSizes?.Length ?? 0);
                if (authoring.hiddenLayerSizes != null)
                {
                    for (int i = 0; i < authoring.hiddenLayerSizes.Length; i++)
                    {
                        hiddenSizesArray[i] = authoring.hiddenLayerSizes[i];
                    }
                }

                var blobAsset = builder.CreateBlobAssetReference<NeuralModelWeightsBlob>(Allocator.Persistent);
                AddBlobAsset(ref blobAsset, out _);

                AddComponent(entity, new NeuralModelWeightsLookup { Value = blobAsset });

                builder.Dispose();
            }
        }
    }
}


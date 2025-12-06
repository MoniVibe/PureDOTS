using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Neural surrogate model reference and weights.
    /// Stores reference to trained model for inference.
    /// </summary>
    public struct NeuralSurrogateModel : IComponentData
    {
        public int ModelId;                 // Model identifier
        public NeuralModelType Type;        // Type of model (SensorNoise, WeatherDiffusion, MoralePrediction)
        public bool IsActive;                // Whether model is active
        public float InferenceCost;          // Cost of inference (ms)
    }

    /// <summary>
    /// Types of neural surrogate models.
    /// </summary>
    public enum NeuralModelType : byte
    {
        SensorNoise = 0,
        WeatherDiffusion = 1,
        MoralePrediction = 2
    }

    /// <summary>
    /// Neural model weights stored as BlobAsset for Burst access.
    /// </summary>
    public struct NeuralModelWeightsBlob
    {
        public BlobString ModelId;          // Model identifier
        public NeuralModelType Type;        // Model type
        public BlobArray<float> Weights;    // Model weights (flattened)
        public int InputSize;               // Input size
        public int OutputSize;              // Output size
        public int HiddenLayerCount;        // Number of hidden layers
        public BlobArray<int> HiddenLayerSizes; // Size of each hidden layer
    }

    /// <summary>
    /// Component linking entity to neural model weights.
    /// </summary>
    public struct NeuralModelWeightsLookup : IComponentData
    {
        public BlobAssetReference<NeuralModelWeightsBlob> Value;
    }
}


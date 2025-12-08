using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Checkpoint tier classification.
    /// </summary>
    public enum CheckpointTier : byte
    {
        /// <summary>Tier 0: 0-10s, RAM ring buffer, instant access.</summary>
        Tier0_RAM = 0,
        /// <summary>Tier 1: 10-120s, compressed RAM, < 50ms access.</summary>
        Tier1_CompressedRAM = 1,
        /// <summary>Tier 2: > 120s, disk/SSD, streamed access.</summary>
        Tier2_Disk = 2
    }

    /// <summary>
    /// Checkpoint data stored in multi-tier system.
    /// </summary>
    public struct CheckpointData : System.IDisposable
    {
        public uint Tick;
        public CheckpointTier Tier;
        public NativeArray<byte> Data;
        public NativeArray<byte> CompressedData;
        public bool IsCompressed;

        public bool IsCreated => Data.IsCreated || CompressedData.IsCreated;

        public CheckpointData(uint tick, CheckpointTier tier, Allocator allocator)
        {
            Tick = tick;
            Tier = tier;
            Data = default;
            CompressedData = default;
            IsCompressed = false;
        }

        public void Dispose()
        {
            if (Data.IsCreated)
            {
                Data.Dispose();
            }
            if (CompressedData.IsCreated)
            {
                CompressedData.Dispose();
            }
        }
    }

    /// <summary>
    /// Multi-tier checkpoint storage system.
    /// Tier 0: 0-10s → RAM ring buffer (instant)
    /// Tier 1: 10-120s → compressed RAM (< 50ms)
    /// Tier 2: > 120s → disk or SSD (streamed)
    /// </summary>
    public struct MultiTierCheckpoint : System.IDisposable
    {
        // Tier 0: RAM ring buffer (0-10s)
        private NativeList<CheckpointData> _tier0Checkpoints;
        private const int Tier0Capacity = 600; // 10s @ 60 TPS

        // Tier 1: Compressed RAM (10-120s)
        private NativeList<CheckpointData> _tier1Checkpoints;
        private const int Tier1Capacity = 6600; // 110s @ 60 TPS

        // Tier 2: Disk/SSD (> 120s) - placeholder for future implementation
        // For now, we just track metadata
        private NativeHashMap<uint, CheckpointTier> _tier2Metadata;

        private float _ticksPerSecond;
        private uint _currentTick;

        public bool IsCreated => _tier0Checkpoints.IsCreated;

        public MultiTierCheckpoint(float ticksPerSecond, Allocator allocator)
        {
            _tier0Checkpoints = new NativeList<CheckpointData>(Tier0Capacity, allocator);
            _tier1Checkpoints = new NativeList<CheckpointData>(Tier1Capacity, allocator);
            _tier2Metadata = new NativeHashMap<uint, CheckpointTier>(1000, allocator);
            _ticksPerSecond = ticksPerSecond;
            _currentTick = 0u;
        }

        public void Dispose()
        {
            if (_tier0Checkpoints.IsCreated)
            {
                for (int i = 0; i < _tier0Checkpoints.Length; i++)
                {
                    _tier0Checkpoints[i].Dispose();
                }
                _tier0Checkpoints.Dispose();
            }
            if (_tier1Checkpoints.IsCreated)
            {
                for (int i = 0; i < _tier1Checkpoints.Length; i++)
                {
                    _tier1Checkpoints[i].Dispose();
                }
                _tier1Checkpoints.Dispose();
            }
            if (_tier2Metadata.IsCreated)
            {
                _tier2Metadata.Dispose();
            }
        }

        /// <summary>
        /// Determine which tier a tick belongs to.
        /// </summary>
        public CheckpointTier GetTierForTick(uint tick, uint currentTick)
        {
            float ageSeconds = (currentTick - tick) / _ticksPerSecond;

            if (ageSeconds <= 10f)
            {
                return CheckpointTier.Tier0_RAM;
            }
            else if (ageSeconds <= 120f)
            {
                return CheckpointTier.Tier1_CompressedRAM;
            }
            else
            {
                return CheckpointTier.Tier2_Disk;
            }
        }

        /// <summary>
        /// Store checkpoint data at specified tick.
        /// </summary>
        public void StoreCheckpoint(uint tick, NativeArray<byte> data, Allocator allocator)
        {
            _currentTick = math.max(_currentTick, tick);
            CheckpointTier tier = GetTierForTick(tick, _currentTick);

            var checkpoint = new CheckpointData(tick, tier, allocator);

            if (tier == CheckpointTier.Tier0_RAM)
            {
                // Store uncompressed in RAM
                checkpoint.Data = new NativeArray<byte>(data.Length, allocator, NativeArrayOptions.UninitializedMemory);
                NativeArray<byte>.Copy(data, checkpoint.Data);
                checkpoint.IsCompressed = false;

                // Prune old Tier 0 checkpoints
                PruneTier0(tick);
                _tier0Checkpoints.Add(checkpoint);
            }
            else if (tier == CheckpointTier.Tier1_CompressedRAM)
            {
                // Compress and store
                var previousData = GetPreviousCheckpointData(tick);
                CheckpointCompression.Compress(in data, in previousData, allocator, out checkpoint.CompressedData);
                checkpoint.IsCompressed = true;

                if (previousData.IsCreated)
                {
                    previousData.Dispose();
                }

                // Prune old Tier 1 checkpoints
                PruneTier1(tick);
                _tier1Checkpoints.Add(checkpoint);
            }
            else
            {
                // Tier 2: Store metadata only (disk implementation deferred)
                _tier2Metadata[tick] = tier;
                data.Dispose(); // Free memory - disk storage not implemented yet
            }
        }

        /// <summary>
        /// Retrieve checkpoint data for specified tick.
        /// </summary>
        public bool TryGetCheckpoint(uint tick, out CheckpointData checkpoint, Allocator allocator)
        {
            checkpoint = default;

            // Try Tier 0 first
            for (int i = _tier0Checkpoints.Length - 1; i >= 0; i--)
            {
                if (_tier0Checkpoints[i].Tick == tick)
                {
                    checkpoint = _tier0Checkpoints[i];
                    return true;
                }
            }

            // Try Tier 1
            for (int i = _tier1Checkpoints.Length - 1; i >= 0; i--)
            {
                if (_tier1Checkpoints[i].Tick == tick)
                {
                    checkpoint = _tier1Checkpoints[i];

                    // Decompress if needed
                    if (checkpoint.IsCompressed && !checkpoint.Data.IsCreated)
                    {
                        var previousData = GetPreviousCheckpointData(tick);
                        // Estimate decompressed size (in practice, store this metadata)
                        int estimatedSize = checkpoint.CompressedData.Length * 2;
                        CheckpointCompression.Decompress(
                            in checkpoint.CompressedData,
                            in previousData,
                            estimatedSize,
                            allocator,
                            out checkpoint.Data);

                        if (previousData.IsCreated)
                        {
                            previousData.Dispose();
                        }
                    }

                    return true;
                }
            }

            // Tier 2: Not implemented yet
            if (_tier2Metadata.ContainsKey(tick))
            {
                return false; // Disk storage not implemented
            }

            return false;
        }

        /// <summary>
        /// Get previous checkpoint data for delta compression.
        /// </summary>
        private NativeArray<byte> GetPreviousCheckpointData(uint tick)
        {
            // Find nearest checkpoint before this tick
            uint bestTick = 0u;
            CheckpointData bestCheckpoint = default;

            for (int i = 0; i < _tier0Checkpoints.Length; i++)
            {
                if (_tier0Checkpoints[i].Tick < tick && _tier0Checkpoints[i].Tick > bestTick)
                {
                    bestTick = _tier0Checkpoints[i].Tick;
                    bestCheckpoint = _tier0Checkpoints[i];
                }
            }

            for (int i = 0; i < _tier1Checkpoints.Length; i++)
            {
                if (_tier1Checkpoints[i].Tick < tick && _tier1Checkpoints[i].Tick > bestTick)
                {
                    bestTick = _tier1Checkpoints[i].Tick;
                    bestCheckpoint = _tier1Checkpoints[i];
                }
            }

            if (bestCheckpoint.IsCreated)
            {
                if (bestCheckpoint.IsCompressed && !bestCheckpoint.Data.IsCreated)
                {
                    // Would need to decompress - for now return empty
                    return default;
                }
                return bestCheckpoint.Data;
            }

            return default;
        }

        /// <summary>
        /// Prune Tier 0 checkpoints older than threshold.
        /// </summary>
        private void PruneTier0(uint currentTick)
        {
            uint thresholdTick = currentTick > (uint)(10f * _ticksPerSecond)
                ? currentTick - (uint)(10f * _ticksPerSecond)
                : 0u;

            for (int i = _tier0Checkpoints.Length - 1; i >= 0; i--)
            {
                if (_tier0Checkpoints[i].Tick < thresholdTick)
                {
                    _tier0Checkpoints[i].Dispose();
                    _tier0Checkpoints.RemoveAtSwapBack(i);
                }
            }
        }

        /// <summary>
        /// Prune Tier 1 checkpoints older than threshold.
        /// </summary>
        private void PruneTier1(uint currentTick)
        {
            uint thresholdTick = currentTick > (uint)(120f * _ticksPerSecond)
                ? currentTick - (uint)(120f * _ticksPerSecond)
                : 0u;

            for (int i = _tier1Checkpoints.Length - 1; i >= 0; i--)
            {
                if (_tier1Checkpoints[i].Tick < thresholdTick)
                {
                    _tier1Checkpoints[i].Dispose();
                    _tier1Checkpoints.RemoveAtSwapBack(i);
                }
            }
        }

        /// <summary>
        /// Migrate checkpoint from Tier 0 to Tier 1 when it ages.
        /// </summary>
        public void MigrateToTier1(uint tick, Allocator allocator)
        {
            for (int i = 0; i < _tier0Checkpoints.Length; i++)
            {
                if (_tier0Checkpoints[i].Tick == tick)
                {
                    var checkpoint = _tier0Checkpoints[i];
                    var previousData = GetPreviousCheckpointData(tick);

                    // Compress
                    CheckpointCompression.Compress(
                        in checkpoint.Data,
                        in previousData,
                        allocator,
                        out checkpoint.CompressedData);
                    checkpoint.IsCompressed = true;
                    checkpoint.Tier = CheckpointTier.Tier1_CompressedRAM;

                    // Free uncompressed data
                    checkpoint.Data.Dispose();
                    checkpoint.Data = default;

                    // Move to Tier 1
                    _tier1Checkpoints.Add(checkpoint);
                    _tier0Checkpoints.RemoveAtSwapBack(i);

                    if (previousData.IsCreated)
                    {
                        previousData.Dispose();
                    }

                    break;
                }
            }
        }
    }
}


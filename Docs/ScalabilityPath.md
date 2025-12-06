# Long-Term Scalability Path

## Overview

PureDOTS is designed with scalability in mind, supporting future distributed simulation and persistent universes. This document outlines the architecture decisions that enable this path.

## Binary Serialization

All file I/O, replay, and network serialization uses `BinarySerialization` for:
- **Endian-fixed**: Little-endian format ensures cross-platform compatibility
- **Struct-only**: Blittable types only, no managed references
- **Deterministic**: Identical serialization across all platforms

## World Partitioning

Large worlds are partitioned into sub-worlds by cell authority:
- Each partition has an `AuthorityId` (0-255)
- Cell coordinates determine authority via hash-based partitioning
- Local vs remote partitions are tracked via `WorldPartition` component

## Distributed Simulation Interface

Future interface (`IDistributedSimulationInterface`) for:
- Offloading ECS worlds to separate processes/machines
- Synchronizing world partitions across distributed processes
- Sending/receiving world state via binary serialization

## Current State

- ✅ Binary serialization implemented
- ✅ World partitioning components implemented
- ⏳ Distributed simulation interface (stub only, not yet implemented)

## Future Work

1. Implement full distributed simulation interface
2. Add network transport layer for inter-process communication
3. Implement partition synchronization protocols
4. Add persistent universe support (save/load world partitions)


# Architecture Concepts - Multi-ECS Implementation

**Last Updated:** 2025-12-07  
**Purpose:** Index of architecture concept documents for the three-pillar ECS system

---

## Overview

This folder contains implementation-friendly concept captures for the multi-ECS architecture described in `TRI_PROJECT_BRIEFING.md`. All concepts are tailored for developers implementing Body/Mind/Aggregate world systems.

---

## Core Architecture Documents

### 1. Three Pillar ECS Worlds
**File:** `Three_Pillar_ECS_Worlds.md`  
**Focus:** World responsibilities, tick rates, data ownership, system groups

**Key Topics:**
- Body ECS (60 Hz, deterministic, Burst-compiled)
- Mind ECS (1 Hz, cognitive, managed allowed)
- Aggregate ECS (0.2 Hz, group decisions, managed)
- Communication rules and anti-patterns
- Performance targets

**Use When:**
- Understanding world separation
- Deciding which world a system belongs to
- Setting up cross-world communication
- Performance budgeting

---

### 2. AgentSyncBus Communication
**File:** `AgentSyncBus_Communication.md`  
**Focus:** Message bus API, access patterns, delta compression

**Key Topics:**
- Message types and queues
- Burst-safe access patterns (collect in Burst, enqueue in managed)
- Delta compression and batching
- Extension rules for new message types
- Troubleshooting guide

**Use When:**
- Implementing bridge systems
- Adding new message types
- Debugging sync issues
- Optimizing sync performance

---

### 3. World Bootstrap Implementation
**File:** `World_Bootstrap_Implementation.md`  
**Focus:** Bootstrap setup, system registration, tick rate configuration

**Key Topics:**
- `ICustomBootstrap` implementation
- Creating three worlds
- System group definitions
- Bridge system setup
- Tick rate configuration

**Use When:**
- Setting up project bootstrap
- Registering systems in correct worlds
- Configuring tick rates
- Testing world initialization

---

## Quick Reference

### World Selection Guide

| System Type | World | Tick Rate | Threading |
|-------------|-------|-----------|-----------|
| Physics, movement, combat | Body | 60 Hz | Burst jobs |
| Goals, planning, learning | Mind | 1 Hz | Main thread |
| Groups, fleets, empires | Aggregate | 0.2 Hz | Main thread |

### Communication Patterns

| Direction | Message Type | Interval |
|-----------|--------------|----------|
| Body → Mind | `BodyToMindMessage`, `Percept` | ~100 ms |
| Mind → Body | `MindToBodyMessage`, `LimbCommand` | ~250 ms |
| Aggregate → Mind | `AggregateIntentMessage` | Per Aggregate tick |
| Any → Aggregate | `ConsensusVoteMessage` | Per Aggregate tick |

### Implementation Checklist

- [ ] Create three worlds in bootstrap
- [ ] Define system groups for each world
- [ ] Create `AgentSyncBridgeCoordinator` in Body world
- [ ] Implement bridge systems in correct order
- [ ] Configure tick rates (60 Hz / 1 Hz / 0.2 Hz)
- [ ] Use GUID identity for cross-world references
- [ ] Profile sync cost (< 3 ms/frame target)

---

## Related Documentation

**Architecture Specs:**
- `Docs/Architecture/ThreePillarECS_Architecture.md` - Canonical architecture overview
- `Docs/Architecture/AgentSyncBus_Specification.md` - Complete bus API reference

**Integration Guides:**
- `Docs/Guides/MultiECS_Integration_Guide.md` - Integration cookbook
- `TRI_PROJECT_BRIEFING.md` - Tri-project architecture overview

**Foundation:**
- `Docs/FoundationGuidelines.md` - Coding patterns (P0-P25)
- `Docs/Concepts/Entity_Agnostic_Design.md` - Entity-agnostic design principles

---

## Document Status

| Document | Status | Last Updated |
|----------|--------|--------------|
| Three_Pillar_ECS_Worlds.md | Design Document | 2025-12-07 |
| AgentSyncBus_Communication.md | Design Document | 2025-12-07 |
| World_Bootstrap_Implementation.md | Design Document | 2025-12-07 |

---

**For Implementers:** These documents provide implementation-ready guidance for the multi-ECS architecture. Start with `Three_Pillar_ECS_Worlds.md` for overview, then `World_Bootstrap_Implementation.md` for setup, and `AgentSyncBus_Communication.md` for cross-world communication.

**For Designers:** These concepts capture the architectural decisions behind the three-pillar ECS system. They explain why worlds are separated, how they communicate, and what patterns to follow.

---

**Last Updated:** 2025-12-07  
**Maintainer:** PureDOTS Architecture Team


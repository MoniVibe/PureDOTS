# Agent: Family & Dynasty

## Scope
Implement family relationship tracking and dynasty lineage systems for inheritance, succession, and bloodline mechanics.

## Stub Files to Implement

### Family System (3 files)
- `Runtime/Stubs/FamilyServiceStub.cs` → `Runtime/Family/FamilyService.cs`
- `Runtime/Stubs/FamilyStubComponents.cs` → `Runtime/Family/FamilyComponents.cs`
- `Runtime/Stubs/FamilyStubSystems.cs` → `Systems/Family/FamilySystems.cs`

**Requirements:**
- Family identity: family unit with founder
- Family members: entities belonging to family with roles (Founder, Parent, Child, Sibling, Spouse)
- Family relations: relationship types (Parent, Child, Sibling, Spouse, Grandparent, etc.)
- Family tree: buffer tracking parent-child relationships
- Relationship calculation: compute relationship between two family members
- Inheritance tracking: inheritance flows through family

### Dynasty System (3 files)
- `Runtime/Stubs/DynastyServiceStub.cs` → `Runtime/Dynasty/DynastyService.cs`
- `Runtime/Stubs/DynastyStubComponents.cs` → `Runtime/Dynasty/DynastyComponents.cs`
- `Runtime/Stubs/DynastyStubSystems.cs` → `Systems/Dynasty/DynastySystems.cs`

**Requirements:**
- Dynasty identity: extended family lineage controlling aggregates
- Dynasty members: entities with ranks (Founder, Heir, Noble, Member)
- Dynasty lineage: bloodline tracking with generations
- Dynasty prestige: reputation of the dynasty
- Dynasty succession: leadership succession through dynasty
- Dynasty reputation: affects succession and unlocks

## Reference Documentation
- `Docs/Concepts/Core/Genealogy_Mixing_System.md` - Genealogy system
- `Docs/Concepts/Core/Entity_Lifecycle.md` - Entity lifecycle (birth, death, inheritance)
- `Docs/Concepts/Politics/Leadership_And_Succession.md` - Leadership succession

## Implementation Notes
- Family is small-scale (immediate family unit)
- Dynasty is large-scale (extended lineage controlling aggregates)
- Family members share reputation
- Dynasty prestige affects leadership elections
- Inheritance flows through family/dynasty
- Succession rules based on dynasty rank

## Dependencies
- `GenealogyComponents` - Genealogy affects family/dynasty
- `EntityLifecycleComponents` - Birth/death events
- `LeadershipComponents` - Leadership succession
- `ReputationComponents` - Reputation sharing

## Integration Points
- Genealogy system: genealogy affects family/dynasty membership
- Lifecycle system: birth/death events create family/dynasty members
- Leadership system: dynasty affects leadership succession
- Reputation system: family/dynasty members share reputation


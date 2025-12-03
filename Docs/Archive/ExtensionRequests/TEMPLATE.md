# Extension Request: {Brief Description}

**Status**: `[PENDING]`  
**Submitted**: YYYY-MM-DD  
**Game Project**: {Space4X / Godgame / Both}  
**Priority**: {P0 / P1 / P2}  
**Assigned To**: {TBD}

---

## Use Case

Describe the game feature or functionality that requires this PureDOTS extension.

**Example:**
Space4X needs to detect custom entity types (trading posts, space stations) via the AI sensor system so that vessels can navigate to and interact with these locations.

---

## Proposed Solution

Describe the specific extension point(s) needed in PureDOTS.

**Extension Type**: {New Tag / New Enum Value / New Config Field / New System / Other}

**Details:**
- What component/system/enum needs to be added or modified?
- Where in the codebase should this go?
- What is the proposed API/interface?

**Example:**
Extend `AISensorCategory` enum with `Custom0` through `Custom15` (values 240-255) reserved for game-specific categories. This allows games to define their own entity detection categories without modifying PureDOTS core.

---

## Impact Assessment

**Files/Systems Affected:**
- List specific files that would need changes
- List systems that would be affected

**Breaking Changes:**
- Will this break existing code? (Yes/No)
- If yes, describe migration path

**Example:**
- `Packages/com.moni.puredots/Runtime/Runtime/AI/AIComponents.cs` - Add enum values
- `Packages/com.moni.puredots/Runtime/Systems/AI/AISystems.cs` - Update MatchesCategory logic
- No breaking changes - new enum values are additive

---

## Example Usage

Show how game code would use this extension.

```csharp
// Example: How Space4X would use custom sensor categories

// In Space4X authoring
var sensorConfig = new AISensorConfig {
    PrimaryCategory = AISensorCategory.Custom0, // Space4X trading post
    Range = 100f,
    MaxResults = 5
};

// In Space4X system
if (reading.Category == AISensorCategory.Custom0)
{
    // Handle trading post detection
}
```

---

## Alternative Approaches Considered

Describe any alternative solutions you considered and why they don't work.

**Example:**
- **Alternative 1**: Create game-specific sensor system
  - **Rejected**: Would duplicate PureDOTS infrastructure
- **Alternative 2**: Use existing categories with component tags
  - **Rejected**: Not flexible enough for multiple custom entity types

---

## Implementation Notes

Any additional notes for implementers:
- Related issues or requests
- Dependencies on other features
- Performance considerations
- Testing requirements

---

## Review Notes

*(This section is for PureDOTS team use)*

**Reviewer**: {Name}  
**Review Date**: YYYY-MM-DD  
**Decision**: {APPROVED / REJECTED / DEFERRED}  
**Notes**: {Review comments}


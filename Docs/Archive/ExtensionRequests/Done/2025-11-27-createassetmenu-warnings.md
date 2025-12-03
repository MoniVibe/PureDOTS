# Extension Request: Fix CreateAssetMenu Attribute Warnings

**Status**: `[RESOLVED]`  
**Submitted**: 2025-11-27  
**Game Project**: Space4X  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Space4X console shows multiple warnings from PureDOTS authoring classes that have `[CreateAssetMenu]` attribute but don't derive from `ScriptableObject`. These warnings clutter the console and indicate potential design issues.

---

## Proposed Solution

**Extension Type**: Bug Fix / Cleanup

**Details:**

For each affected class, either:

**Option A** - If intended as asset: Change to derive from `ScriptableObject`
```csharp
// Change from:
public class MyCatalogAuthoring : MonoBehaviour

// To:
public class MyCatalogAuthoring : ScriptableObject
```

**Option B** - If intended as authoring component: Remove the attribute
```csharp
// Remove this line:
// [CreateAssetMenu(fileName = "...", menuName = "...")]
public class MyCatalogAuthoring : MonoBehaviour
```

---

## Impact Assessment

**Files/Systems Affected:**

| Class | Recommended Fix |
|-------|----------------|
| `CultureStoryCatalogAuthoring` | Remove attribute or convert to SO |
| `LessonCatalogAuthoring` | Remove attribute or convert to SO |
| `SpellCatalogAuthoring` | Remove attribute or convert to SO |
| `ItemPartCatalogAuthoring` | Remove attribute or convert to SO |
| `EnlightenmentProfileAuthoring` | Remove attribute or convert to SO |
| `BuffCatalogAuthoring` | Remove attribute or convert to SO |
| `SchoolComplexityCatalogAuthoring` | Remove attribute or convert to SO |
| `QualityFormulaAuthoring` | Remove attribute or convert to SO |
| `SpellSignatureCatalogAuthoring` | Remove attribute or convert to SO |
| `QualityCurveAuthoring` | Remove attribute or convert to SO |

**Breaking Changes:**
- If converting to ScriptableObject: May require migration of existing components
- If just removing attribute: No breaking changes

---

## Example Usage

```csharp
// If catalog should be a ScriptableObject asset:
[CreateAssetMenu(fileName = "NewSpellCatalog", menuName = "PureDOTS/Catalogs/Spell Catalog")]
public class SpellCatalogData : ScriptableObject
{
    public List<SpellEntry> Spells;
}

// Reference from authoring component:
public class SpellCatalogAuthoring : MonoBehaviour
{
    public SpellCatalogData CatalogData;  // Reference the SO
    
    public class Baker : Baker<SpellCatalogAuthoring> { ... }
}
```

---

## Alternative Approaches Considered

- **Alternative 1**: Ignore the warnings
  - **Rejected**: Clutters console, indicates design issues

---

## Implementation Notes

**Warning Text:**
```
CreateAssetMenu attribute on PureDOTS.Authoring.Spells.SpellCatalogAuthoring will be 
ignored as PureDOTS.Authoring.Spells.SpellCatalogAuthoring is not derived from ScriptableObject.
```

This is a low-priority cleanup task. The warnings don't block functionality but do indicate a mismatch between intended and actual usage.

---

## Review Notes

**Reviewer**: Automated  
**Review Date**: 2025-11-27  
**Decision**: Already Resolved  
**Notes**: 

Verified that the listed classes (`CultureStoryCatalogAuthoring`, `LessonCatalogAuthoring`, `SpellCatalogAuthoring`, `ItemPartCatalogAuthoring`, `EnlightenmentProfileAuthoring`, `BuffCatalogAuthoring`, `SchoolComplexityCatalogAuthoring`, `QualityFormulaAuthoring`, `SpellSignatureCatalogAuthoring`, `QualityCurveAuthoring`) **do not have** `[CreateAssetMenu]` attributes.

These fixes were completed by Agent5 and Agent6 prompts in an earlier session. See `Docs/TODO/Done/Agent5_Prompt.md` and `Docs/TODO/Done/Agent6_Prompt.md` for details.

No further action required. 


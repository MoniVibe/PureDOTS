# Extension Request: Environmental Lore & Quote Triggers System

**Status**: `[COMPLETED]`  
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P3  
**Assigned To**: TBD

---

## Use Case

Both games need location-triggered narrative content:

**Space4X:**
- Crew quotes when entering dangerous regions
- Location-specific lore from discovered artifacts
- Environmental storytelling from debris fields
- Event-triggered narrative sequences
- Log discovery system

**Godgame:**
- NPC quips when entering landmarks
- Seasonal flavor text
- Discovery quotes for new areas
- Event celebration/mourning lines
- Legend/myth delivery

Shared needs:
- Location-triggered text delivery
- Event-triggered quotes
- Discovery log tracking
- Quote cooldown/frequency
- Context-aware selection

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components

```csharp
/// <summary>
/// Type of lore trigger.
/// </summary>
public enum LoreTriggerType : byte
{
    Location = 0,       // Enter/exit location
    Discovery = 1,      // First-time discovery
    Event = 2,          // Game event occurs
    Time = 3,           // Time/season based
    Entity = 4,         // Specific entity interaction
    Proximity = 5,      // Near specific object
    Achievement = 6     // Accomplishment trigger
}

/// <summary>
/// Lore entry that can be triggered.
/// </summary>
public struct LoreEntry : IComponentData
{
    public FixedString128Bytes Text;
    public FixedString32Bytes Category;
    public FixedString32Bytes SpeakerRole; // "captain", "elder", "narrator"
    public LoreTriggerType TriggerType;
    public float Priority;             // Higher = more important
    public uint MinCooldownTicks;      // Min time before replay
    public uint LastTriggeredTick;
    public byte HasBeenSeen;
    public byte IsOneTime;             // Only show once ever
}

/// <summary>
/// Location-based lore trigger zone.
/// </summary>
public struct LoreTriggerZone : IComponentData
{
    public float3 Center;
    public float Radius;
    public byte TriggerOnEnter;
    public byte TriggerOnExit;
    public byte TriggerOnStay;
    public uint StayDurationRequired;  // Ticks to stay before triggering
}

/// <summary>
/// Lore entries attached to a trigger zone.
/// </summary>
[InternalBufferCapacity(4)]
public struct ZoneLoreEntry : IBufferElementData
{
    public FixedString128Bytes Text;
    public FixedString32Bytes SpeakerRole;
    public float Weight;               // Selection weight
    public uint LastTriggeredTick;
    public byte IsDiscoveryLore;       // First time only
}

/// <summary>
/// Discovery log for tracked discoveries.
/// </summary>
[InternalBufferCapacity(32)]
public struct DiscoveryLogEntry : IBufferElementData
{
    public FixedString64Bytes DiscoveryId;
    public FixedString32Bytes Category;
    public uint DiscoveredTick;
    public float Significance;         // Importance score
    public byte WasShared;             // Player saw the notification
}

/// <summary>
/// Quote queue for pending delivery.
/// </summary>
[InternalBufferCapacity(4)]
public struct PendingQuote : IBufferElementData
{
    public FixedString128Bytes Text;
    public FixedString32Bytes SpeakerRole;
    public float Priority;
    public uint QueuedTick;
    public uint ExpiresAt;             // Don't show if too old
}

/// <summary>
/// Lore delivery preferences.
/// </summary>
public struct LoreDeliverySettings : IComponentData
{
    public float MinQuoteInterval;     // Min ticks between quotes
    public float MaxQueueSize;         // Max pending quotes
    public byte PrioritizeDiscoveries; // Discoveries skip queue
    public byte AllowDuplicates;       // Same quote can repeat
    public uint LastQuoteDeliveredTick;
}

/// <summary>
/// Contextual filter for lore selection.
/// </summary>
public struct LoreContext : IComponentData
{
    public FixedString32Bytes CurrentRegion;
    public FixedString32Bytes CurrentSeason;
    public byte ThreatLevel;           // 0-10
    public byte MoodLevel;             // 0=sad, 5=neutral, 10=happy
    public byte InCombat;
    public byte InDiscovery;
}
```

### Static Helpers

```csharp
public static class LoreHelpers
{
    /// <summary>
    /// Checks if position triggers zone lore.
    /// </summary>
    public static bool CheckZoneTrigger(
        float3 position,
        float3 previousPosition,
        in LoreTriggerZone zone)
    {
        bool wasInside = math.length(previousPosition - zone.Center) <= zone.Radius;
        bool isInside = math.length(position - zone.Center) <= zone.Radius;
        
        if (zone.TriggerOnEnter != 0 && !wasInside && isInside)
            return true;
        
        if (zone.TriggerOnExit != 0 && wasInside && !isInside)
            return true;
        
        if (zone.TriggerOnStay != 0 && isInside)
            return true;
        
        return false;
    }

    /// <summary>
    /// Selects best lore entry from available options.
    /// </summary>
    public static int SelectLoreEntry(
        in DynamicBuffer<ZoneLoreEntry> entries,
        in LoreContext context,
        uint currentTick,
        uint minCooldown,
        uint seed)
    {
        float totalWeight = 0;
        int validCount = 0;
        
        // Calculate valid entries and weights
        for (int i = 0; i < entries.Length; i++)
        {
            if (!IsEntryValid(entries[i], currentTick, minCooldown))
                continue;
            
            totalWeight += entries[i].Weight;
            validCount++;
        }
        
        if (validCount == 0) return -1;
        
        // Weighted random selection
        var rng = new Random(seed);
        float roll = rng.NextFloat(0, totalWeight);
        float accumulated = 0;
        
        for (int i = 0; i < entries.Length; i++)
        {
            if (!IsEntryValid(entries[i], currentTick, minCooldown))
                continue;
            
            accumulated += entries[i].Weight;
            if (roll <= accumulated)
                return i;
        }
        
        return entries.Length > 0 ? 0 : -1;
    }

    private static bool IsEntryValid(
        in ZoneLoreEntry entry,
        uint currentTick,
        uint minCooldown)
    {
        // Check cooldown
        if (currentTick - entry.LastTriggeredTick < minCooldown)
            return false;
        
        return true;
    }

    /// <summary>
    /// Adds quote to delivery queue.
    /// </summary>
    public static bool QueueQuote(
        ref DynamicBuffer<PendingQuote> queue,
        in LoreDeliverySettings settings,
        FixedString128Bytes text,
        FixedString32Bytes speaker,
        float priority,
        uint currentTick,
        uint expirationDuration)
    {
        // Check queue capacity
        if (queue.Length >= settings.MaxQueueSize)
        {
            // Try to replace lowest priority
            int lowestIdx = -1;
            float lowestPriority = priority;
            for (int i = 0; i < queue.Length; i++)
            {
                if (queue[i].Priority < lowestPriority)
                {
                    lowestPriority = queue[i].Priority;
                    lowestIdx = i;
                }
            }
            
            if (lowestIdx >= 0)
                queue.RemoveAt(lowestIdx);
            else
                return false; // Queue full with higher priority
        }
        
        // Check for duplicates
        if (settings.AllowDuplicates == 0)
        {
            for (int i = 0; i < queue.Length; i++)
            {
                if (queue[i].Text.Equals(text))
                    return false;
            }
        }
        
        queue.Add(new PendingQuote
        {
            Text = text,
            SpeakerRole = speaker,
            Priority = priority,
            QueuedTick = currentTick,
            ExpiresAt = currentTick + expirationDuration
        });
        
        return true;
    }

    /// <summary>
    /// Gets next quote to deliver.
    /// </summary>
    public static bool TryDeliverQuote(
        ref DynamicBuffer<PendingQuote> queue,
        ref LoreDeliverySettings settings,
        uint currentTick,
        out PendingQuote quote)
    {
        quote = default;
        
        // Check delivery cooldown
        if (currentTick - settings.LastQuoteDeliveredTick < settings.MinQuoteInterval)
            return false;
        
        if (queue.Length == 0)
            return false;
        
        // Remove expired quotes
        for (int i = queue.Length - 1; i >= 0; i--)
        {
            if (queue[i].ExpiresAt < currentTick)
                queue.RemoveAt(i);
        }
        
        if (queue.Length == 0)
            return false;
        
        // Find highest priority
        int bestIdx = 0;
        float bestPriority = queue[0].Priority;
        for (int i = 1; i < queue.Length; i++)
        {
            if (queue[i].Priority > bestPriority)
            {
                bestPriority = queue[i].Priority;
                bestIdx = i;
            }
        }
        
        quote = queue[bestIdx];
        queue.RemoveAt(bestIdx);
        settings.LastQuoteDeliveredTick = currentTick;
        
        return true;
    }

    /// <summary>
    /// Records a discovery.
    /// </summary>
    public static void RecordDiscovery(
        ref DynamicBuffer<DiscoveryLogEntry> log,
        FixedString64Bytes discoveryId,
        FixedString32Bytes category,
        float significance,
        uint currentTick)
    {
        // Check if already discovered
        for (int i = 0; i < log.Length; i++)
        {
            if (log[i].DiscoveryId.Equals(discoveryId))
                return; // Already discovered
        }
        
        log.Add(new DiscoveryLogEntry
        {
            DiscoveryId = discoveryId,
            Category = category,
            DiscoveredTick = currentTick,
            Significance = significance,
            WasShared = 0
        });
    }

    /// <summary>
    /// Checks if discovery is new.
    /// </summary>
    public static bool IsNewDiscovery(
        in DynamicBuffer<DiscoveryLogEntry> log,
        FixedString64Bytes discoveryId)
    {
        for (int i = 0; i < log.Length; i++)
        {
            if (log[i].DiscoveryId.Equals(discoveryId))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Gets discovery count by category.
    /// </summary>
    public static int GetDiscoveryCount(
        in DynamicBuffer<DiscoveryLogEntry> log,
        FixedString32Bytes category)
    {
        int count = 0;
        for (int i = 0; i < log.Length; i++)
        {
            if (log[i].Category.Equals(category))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Filters lore by context.
    /// </summary>
    public static bool MatchesContext(
        in LoreEntry entry,
        in LoreContext context)
    {
        // Skip combat lore when not in combat
        if (entry.Category.Equals(new FixedString32Bytes("combat")) && 
            context.InCombat == 0)
            return false;
        
        // Skip happy quotes when mood is low
        if (entry.Category.Equals(new FixedString32Bytes("celebration")) && 
            context.MoodLevel < 5)
            return false;
        
        return true;
    }
}
```

---

## Example Usage

```csharp
// === Space4X: Entering dangerous region ===
var zone = EntityManager.GetComponentData<LoreTriggerZone>(regionEntity);
float3 shipPosition = GetPosition(shipEntity);
float3 previousPosition = GetPreviousPosition(shipEntity);

if (LoreHelpers.CheckZoneTrigger(shipPosition, previousPosition, zone))
{
    var entries = EntityManager.GetBuffer<ZoneLoreEntry>(regionEntity);
    var context = EntityManager.GetComponentData<LoreContext>(playerEntity);
    
    int selectedIdx = LoreHelpers.SelectLoreEntry(entries, context, currentTick, 5000, currentTick);
    
    if (selectedIdx >= 0)
    {
        var entry = entries[selectedIdx];
        var quoteQueue = EntityManager.GetBuffer<PendingQuote>(playerEntity);
        var settings = EntityManager.GetComponentData<LoreDeliverySettings>(playerEntity);
        
        LoreHelpers.QueueQuote(ref quoteQueue, settings, 
            entry.Text, entry.SpeakerRole, 0.8f, currentTick, 10000);
        
        // Mark as triggered
        entry.LastTriggeredTick = currentTick;
        entries[selectedIdx] = entry;
    }
}

// Deliver queued quote to UI
var quoteQueue = EntityManager.GetBuffer<PendingQuote>(playerEntity);
var deliverySettings = EntityManager.GetComponentData<LoreDeliverySettings>(playerEntity);

if (LoreHelpers.TryDeliverQuote(ref quoteQueue, ref deliverySettings, currentTick, out var quote))
{
    DisplayQuote(quote.Text, quote.SpeakerRole);
}

// === Godgame: Discovery system ===
var discoveryLog = EntityManager.GetBuffer<DiscoveryLogEntry>(playerEntity);

// Check if location is new discovery
var locationId = new FixedString64Bytes("ancient_ruins_east");
if (LoreHelpers.IsNewDiscovery(discoveryLog, locationId))
{
    // Record discovery
    LoreHelpers.RecordDiscovery(ref discoveryLog, locationId, 
        new FixedString32Bytes("landmarks"), 0.8f, currentTick);
    
    // Queue discovery quote
    var discoveryQuote = new FixedString128Bytes("Ancient stones... The old ones built here.");
    LoreHelpers.QueueQuote(ref quoteQueue, settings,
        discoveryQuote, new FixedString32Bytes("explorer"), 1.0f, currentTick, 5000);
}

// Get exploration progress
int landmarkCount = LoreHelpers.GetDiscoveryCount(discoveryLog, 
    new FixedString32Bytes("landmarks"));
```

---

## Alternative Approaches Considered

- **Alternative 1**: Hardcoded quote arrays
  - **Rejected**: Need flexible triggers, context filtering, cooldowns

- **Alternative 2**: Game-specific narrative systems
  - **Rejected**: Core mechanics (triggers, queue, discovery) are identical

---

## Implementation Notes

**Dependencies:**
- FixedString types for text storage
- Random for weighted selection

**Performance Considerations:**
- Zone checks are spatial queries
- Queue management is lightweight
- Can cache discovered items

**Related Requests:**
- Event system (event-triggered lore)
- Regional modifiers (location context)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:


# Extension Request: Contract & Assignment System

**Status**: `[COMPLETED]`  
**Submitted**: 2025-11-26  
**Game Project**: Both (Space4X, Godgame)  
**Priority**: P2  
**Assigned To**: TBD

---

## Use Case

Both games need service contracts and work assignments:

**Space4X:**
- Officers sign service contracts (1-5 years) with fleets/manufacturers
- Contract expiry triggers negotiation events
- Alignment mismatch can push officers to rival factions
- Officers can own stakes in facilities/manufacturers

**Godgame:**
- Villagers assigned to work crews and facilities
- Temporary contracts for mercenaries and specialists
- Apprenticeship contracts for training
- Guild membership with dues and benefits

Shared needs:
- Contract duration tracking
- Contract expiry/renewal triggers
- Obligation/benefit tracking
- Breach detection and penalties
- Ownership stakes

---

## Proposed Solution

**Extension Type**: New Components + Static Helpers

### Components

```csharp
/// <summary>
/// Type of contract.
/// </summary>
public enum ContractType : byte
{
    Employment = 0,     // Standard work contract
    Service = 1,        // Time-limited service
    Apprenticeship = 2, // Training contract
    Military = 3,       // Military service
    Mercenary = 4,      // Combat for hire
    Guild = 5,          // Guild membership
    Partnership = 6,    // Business partnership
    Ownership = 7,      // Ownership stake
    Tenancy = 8,        // Renting/leasing
    Commission = 9      // One-time job
}

/// <summary>
/// Status of a contract.
/// </summary>
public enum ContractStatus : byte
{
    Negotiating = 0,
    Active = 1,
    Suspended = 2,
    Expiring = 3,       // Near end, can renew
    Expired = 4,
    Breached = 5,
    Terminated = 6,
    Completed = 7
}

/// <summary>
/// Main contract component.
/// </summary>
public struct Contract : IComponentData
{
    public ContractType Type;
    public ContractStatus Status;
    public Entity EmployerEntity;      // Who provides the contract
    public Entity ContractorEntity;    // Who fulfills the contract
    public uint StartTick;
    public uint EndTick;               // 0 = indefinite
    public uint LastPaymentTick;
    public float PaymentAmount;        // Per period
    public float PaymentPeriod;        // Ticks between payments
    public float OwnershipStake;       // 0-1 for ownership contracts
    public byte AutoRenew;             // Auto-renew on expiry
    public byte RequiresNotice;        // Needs advance notice to end
    public uint NoticePeriod;          // Ticks of notice required
}

/// <summary>
/// Benefits provided by contract.
/// </summary>
[InternalBufferCapacity(4)]
public struct ContractBenefit : IBufferElementData
{
    public FixedString32Bytes BenefitType;
    public float Value;
    public byte IsActive;
}

/// <summary>
/// Obligations under contract.
/// </summary>
[InternalBufferCapacity(4)]
public struct ContractObligation : IBufferElementData
{
    public FixedString32Bytes ObligationType;
    public float RequiredValue;
    public float CurrentValue;
    public uint DeadlineTick;
    public byte IsMet;
}

/// <summary>
/// Breach record for contract.
/// </summary>
[InternalBufferCapacity(4)]
public struct ContractBreach : IBufferElementData
{
    public FixedString32Bytes BreachType;
    public float Severity;             // 0-1
    public uint OccurredTick;
    public byte WasResolved;
    public float PenaltyPaid;
}

/// <summary>
/// Assignment to a specific role/location.
/// </summary>
public struct Assignment : IComponentData
{
    public Entity AssignedTo;          // Entity being assigned
    public Entity Location;            // Where they're assigned
    public FixedString32Bytes Role;    // What role they fill
    public float Efficiency;           // 0-1 how well they fit
    public uint StartTick;
    public uint ScheduledEndTick;
    public byte IsTemporary;
    public byte CanReassign;           // Allowed to move elsewhere
}

/// <summary>
/// Ownership stake in an entity.
/// </summary>
[InternalBufferCapacity(8)]
public struct OwnershipStake : IBufferElementData
{
    public Entity OwnerEntity;
    public float Percentage;           // 0-1 ownership %
    public float DividendsOwed;
    public uint AcquiredTick;
    public byte HasVotingRights;
    public byte CanSell;
}

/// <summary>
/// Negotiation state for contracts.
/// </summary>
public struct ContractNegotiation : IComponentData
{
    public Entity ProposerEntity;
    public Entity RecipientEntity;
    public ContractType ProposedType;
    public float ProposedPayment;
    public uint ProposedDuration;
    public float CounterOfferPayment;
    public uint CounterOfferDuration;
    public byte NegotiationRounds;
    public byte IsAccepted;
    public byte IsRejected;
}
```

### Static Helpers

```csharp
public static class ContractHelpers
{
    /// <summary>
    /// Checks if contract is near expiry.
    /// </summary>
    public static bool IsNearExpiry(
        in Contract contract,
        uint currentTick,
        uint warningThreshold)
    {
        if (contract.EndTick == 0) return false; // Indefinite
        if (contract.Status != ContractStatus.Active) return false;
        
        return contract.EndTick - currentTick <= warningThreshold;
    }

    /// <summary>
    /// Checks if payment is due.
    /// </summary>
    public static bool IsPaymentDue(
        in Contract contract,
        uint currentTick)
    {
        if (contract.PaymentAmount <= 0) return false;
        if (contract.PaymentPeriod <= 0) return false;
        
        return currentTick - contract.LastPaymentTick >= contract.PaymentPeriod;
    }

    /// <summary>
    /// Calculates total payment owed.
    /// </summary>
    public static float CalculateOwedPayment(
        in Contract contract,
        uint currentTick)
    {
        if (contract.PaymentPeriod <= 0) return 0;
        
        uint ticksSincePayment = currentTick - contract.LastPaymentTick;
        uint periodsOwed = ticksSincePayment / (uint)contract.PaymentPeriod;
        
        return periodsOwed * contract.PaymentAmount;
    }

    /// <summary>
    /// Checks if all obligations are met.
    /// </summary>
    public static bool AreObligationsMet(
        in DynamicBuffer<ContractObligation> obligations,
        uint currentTick)
    {
        for (int i = 0; i < obligations.Length; i++)
        {
            if (obligations[i].DeadlineTick > 0 && 
                currentTick > obligations[i].DeadlineTick &&
                obligations[i].IsMet == 0)
            {
                return false;
            }
            
            if (obligations[i].CurrentValue < obligations[i].RequiredValue &&
                obligations[i].IsMet == 0)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Updates contract status based on conditions.
    /// </summary>
    public static ContractStatus UpdateContractStatus(
        in Contract contract,
        in DynamicBuffer<ContractObligation> obligations,
        in DynamicBuffer<ContractBreach> breaches,
        uint currentTick,
        uint expiryWarningThreshold)
    {
        // Check for termination from breaches
        float totalBreachSeverity = 0;
        for (int i = 0; i < breaches.Length; i++)
        {
            if (breaches[i].WasResolved == 0)
                totalBreachSeverity += breaches[i].Severity;
        }
        if (totalBreachSeverity >= 1f)
            return ContractStatus.Breached;
        
        // Check expiry
        if (contract.EndTick > 0)
        {
            if (currentTick >= contract.EndTick)
                return ContractStatus.Expired;
            
            if (IsNearExpiry(contract, currentTick, expiryWarningThreshold))
                return ContractStatus.Expiring;
        }
        
        // Check obligations
        if (!AreObligationsMet(obligations, currentTick))
            return ContractStatus.Suspended;
        
        return ContractStatus.Active;
    }

    /// <summary>
    /// Records a contract breach.
    /// </summary>
    public static void RecordBreach(
        ref DynamicBuffer<ContractBreach> breaches,
        FixedString32Bytes breachType,
        float severity,
        uint currentTick)
    {
        breaches.Add(new ContractBreach
        {
            BreachType = breachType,
            Severity = math.saturate(severity),
            OccurredTick = currentTick,
            WasResolved = 0,
            PenaltyPaid = 0
        });
    }

    /// <summary>
    /// Calculates assignment efficiency.
    /// </summary>
    public static float CalculateAssignmentEfficiency(
        float skillMatch,
        float alignmentMatch,
        float happinessLevel)
    {
        // Skill matters most
        float skillFactor = skillMatch * 0.5f;
        
        // Alignment affects willingness
        float alignmentFactor = alignmentMatch * 0.3f;
        
        // Happiness affects effort
        float happinessFactor = happinessLevel * 0.2f;
        
        return math.saturate(skillFactor + alignmentFactor + happinessFactor);
    }

    /// <summary>
    /// Calculates dividend payment from ownership.
    /// </summary>
    public static float CalculateDividends(
        float ownershipPercentage,
        float totalProfits,
        float retentionRate)
    {
        float distributableProfits = totalProfits * (1f - retentionRate);
        return distributableProfits * ownershipPercentage;
    }

    /// <summary>
    /// Evaluates contract negotiation outcome.
    /// </summary>
    public static bool EvaluateNegotiation(
        in ContractNegotiation negotiation,
        float employerWillingness,
        float contractorWillingness)
    {
        // Both parties must find it acceptable
        float proposedValue = negotiation.ProposedPayment * negotiation.ProposedDuration;
        float counterValue = negotiation.CounterOfferPayment * negotiation.CounterOfferDuration;
        
        float midpoint = (proposedValue + counterValue) / 2f;
        
        float employerAcceptance = proposedValue > 0 ? midpoint / proposedValue : 0;
        float contractorAcceptance = counterValue > 0 ? midpoint / counterValue : 0;
        
        return employerAcceptance * employerWillingness >= 0.5f &&
               contractorAcceptance * contractorWillingness >= 0.5f;
    }

    /// <summary>
    /// Calculates notice period remaining.
    /// </summary>
    public static uint GetRemainingNoticePeriod(
        in Contract contract,
        uint terminationRequestTick)
    {
        if (contract.RequiresNotice == 0) return 0;
        
        uint noticeEndTick = terminationRequestTick + contract.NoticePeriod;
        return noticeEndTick;
    }

    /// <summary>
    /// Gets total ownership percentage for an owner.
    /// </summary>
    public static float GetTotalOwnership(
        in DynamicBuffer<OwnershipStake> stakes,
        Entity ownerEntity)
    {
        float total = 0;
        for (int i = 0; i < stakes.Length; i++)
        {
            if (stakes[i].OwnerEntity == ownerEntity)
                total += stakes[i].Percentage;
        }
        return total;
    }
}
```

---

## Example Usage

```csharp
// === Space4X: Officer service contract ===
var contract = new Contract
{
    Type = ContractType.Service,
    Status = ContractStatus.Active,
    EmployerEntity = fleetEntity,
    ContractorEntity = officerEntity,
    StartTick = currentTick,
    EndTick = currentTick + 50000, // ~3 years
    PaymentAmount = 100f,
    PaymentPeriod = 1000,          // Monthly
    AutoRenew = 1,
    RequiresNotice = 1,
    NoticePeriod = 5000            // ~3 months notice
};

// Check if payment is due
if (ContractHelpers.IsPaymentDue(contract, currentTick))
{
    float owed = ContractHelpers.CalculateOwedPayment(contract, currentTick);
    ProcessPayment(contract.EmployerEntity, contract.ContractorEntity, owed);
    contract.LastPaymentTick = currentTick;
}

// Check for near-expiry
if (ContractHelpers.IsNearExpiry(contract, currentTick, 10000))
{
    TriggerRenewalNegotiation(officerEntity, fleetEntity);
}

// Record a breach (officer refused orders)
var breaches = EntityManager.GetBuffer<ContractBreach>(contractEntity);
ContractHelpers.RecordBreach(ref breaches, "RefusedOrders", 0.3f, currentTick);

// Update status
var obligations = EntityManager.GetBuffer<ContractObligation>(contractEntity);
contract.Status = ContractHelpers.UpdateContractStatus(
    contract, obligations, breaches, currentTick, 10000);

// === Godgame: Villager work assignment ===
var assignment = new Assignment
{
    AssignedTo = villagerEntity,
    Location = farmEntity,
    Role = "farmhand",
    StartTick = currentTick,
    IsTemporary = 0,
    CanReassign = 1
};

// Calculate efficiency
float skillMatch = GetSkillMatch(villagerEntity, assignment.Role);
float alignmentMatch = GetAlignmentMatch(villagerEntity, farmEntity);
float happiness = GetHappiness(villagerEntity);

assignment.Efficiency = ContractHelpers.CalculateAssignmentEfficiency(
    skillMatch, alignmentMatch, happiness);

// === Ownership tracking ===
var stakes = EntityManager.GetBuffer<OwnershipStake>(facilityEntity);
stakes.Add(new OwnershipStake
{
    OwnerEntity = dynastyEntity,
    Percentage = 0.25f,    // 25% ownership
    HasVotingRights = 1,
    CanSell = 1,
    AcquiredTick = currentTick
});

// Calculate dividends
float profits = GetFacilityProfits(facilityEntity);
for (int i = 0; i < stakes.Length; i++)
{
    float dividend = ContractHelpers.CalculateDividends(
        stakes[i].Percentage, profits, 0.3f); // 30% retained
    
    var stake = stakes[i];
    stake.DividendsOwed += dividend;
    stakes[i] = stake;
}
```

---

## Alternative Approaches Considered

- **Alternative 1**: Simple employment flag
  - **Rejected**: Both games need contract terms, breaches, ownership

- **Alternative 2**: Game-specific contracts
  - **Rejected**: Core mechanics (duration, payment, breaches) are identical

---

## Implementation Notes

**Dependencies:**
- Entity references for parties

**Performance Considerations:**
- Contract checks can be batched
- Status updates don't need every tick

**Related Requests:**
- Aggregate membership (contracts with groups)
- Economy system (payment processing)

---

## Review Notes

*(PureDOTS team use)*

**Reviewer**:   
**Review Date**:   
**Decision**:   
**Notes**:


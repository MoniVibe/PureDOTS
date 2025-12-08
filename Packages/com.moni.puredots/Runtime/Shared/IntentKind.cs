namespace PureDOTS.Shared
{
    /// <summary>
    /// Intent kinds produced by the cognitive layer.
    /// </summary>
    public enum IntentKind : byte
    {
        None = 0,
        Move = 1,
        Attack = 2,
        Harvest = 3,
        Defend = 4,
        Patrol = 5,
        UseAbility = 6,
        Interact = 7,
        Rest = 8,
        Flee = 9
    }
}

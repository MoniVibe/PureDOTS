namespace PureDOTS.Runtime.Modifiers
{
    /// <summary>
    /// Operation applied by a modifier (additive, multiplicative, or override).
    /// </summary>
    public enum ModifierOperation : byte
    {
        Add = 0,
        Multiply = 1,
        Override = 2
    }

    /// <summary>
    /// Category used for modifier aggregation logic.
    /// </summary>
    public enum ModifierCategory : byte
    {
        Economy = 0,
        Military = 1,
        Environment = 2
    }
}

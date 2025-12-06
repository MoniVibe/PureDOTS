using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.AI.AQL
{
    /// <summary>
    /// AI Query Language parser for declarative world queries.
    /// Syntax: FIND enemies WHERE distance < 30 AND morale > 0.5
    /// </summary>
    public static class AQLParser
    {
        /// <summary>
        /// Parses an AQL query string into a structured query.
        /// </summary>
        public static AQLQuery Parse(FixedString512Bytes queryString)
        {
            // In full implementation, would:
            // 1. Parse query syntax (FIND ... WHERE ... AND/OR ...)
            // 2. Extract entity type, conditions, operators
            // 3. Return structured AQLQuery
            
            return new AQLQuery();
        }
    }

    /// <summary>
    /// Structured AQL query representation.
    /// </summary>
    public struct AQLQuery
    {
        public FixedString64Bytes EntityType;
        public NativeList<AQLCondition> Conditions;
    }

    /// <summary>
    /// AQL condition (field operator value).
    /// </summary>
    public struct AQLCondition
    {
        public FixedString64Bytes Field;
        public AQLOperator Operator;
        public float Value;
    }

    /// <summary>
    /// AQL operator types.
    /// </summary>
    public enum AQLOperator : byte
    {
        LessThan,
        GreaterThan,
        Equal,
        NotEqual,
        LessThanOrEqual,
        GreaterThanOrEqual
    }
}


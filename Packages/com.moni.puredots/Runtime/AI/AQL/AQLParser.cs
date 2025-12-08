using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI.AQL
{
    /// <summary>
    /// AI Query Language parser for declarative world queries.
    /// Syntax: FIND enemies WHERE distance < 30 AND morale > 0.5
    /// </summary>
    public static class AQLParser
    {
        /// <summary>
        /// Parses an AQL query string into a structured query (minimal placeholder parser).
        /// </summary>
        public static AQLQuery Parse(FixedString512Bytes queryString)
        {
            var query = new AQLQuery
            {
                EntityType = default,
                Conditions = default
            };

            // Minimal parsing: capture entity type after "FIND", ignore conditions.
            var tokens = queryString.ToString().Split(' ');
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i].Equals("FIND", System.StringComparison.OrdinalIgnoreCase) && i + 1 < tokens.Length)
                {
                    query.EntityType = tokens[i + 1];
                    continue;
                }

                if (tokens[i].Equals("WHERE", System.StringComparison.OrdinalIgnoreCase) && i + 3 < tokens.Length)
                {
                    // Try to parse one simple condition: <field> <op> <value>
                    var field = tokens[i + 1];
                    var opStr = tokens[i + 2];
                    var valStr = tokens[i + 3];

                    var op = ParseOperator(opStr);
                    if (op.HasValue && float.TryParse(valStr, out var value))
                    {
                        if (query.Conditions.Length < query.Conditions.Capacity)
                        {
                            query.Conditions.Add(new AQLCondition
                            {
                                Field = field,
                                Operator = op.Value,
                                Value = value
                            });
                        }
                    }
                }
            }

            return query;
        }

        private static AQLOperator? ParseOperator(string token)
        {
            return token switch
            {
                "<" => AQLOperator.LessThan,
                ">" => AQLOperator.GreaterThan,
                "=" => AQLOperator.Equal,
                "!=" => AQLOperator.NotEqual,
                "<=" => AQLOperator.LessThanOrEqual,
                ">=" => AQLOperator.GreaterThanOrEqual,
                _ => null
            };
        }
    }

    /// <summary>
    /// Structured AQL query representation.
    /// </summary>
    public struct AQLQuery
    {
        public FixedString64Bytes EntityType;
        public FixedList128Bytes<AQLCondition> Conditions;
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


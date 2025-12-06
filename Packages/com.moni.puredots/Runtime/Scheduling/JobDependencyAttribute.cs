using System;
using Unity.Entities;

namespace PureDOTS.Runtime.Scheduling
{
    /// <summary>
    /// Declares a dependency relationship between systems for job graph scheduling.
    /// Example: [JobDependency(typeof(PhysicsSystem), DependencyType.After)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class JobDependencyAttribute : Attribute
    {
        public Type DependencyType { get; }
        public DependencyType Relationship { get; }

        public JobDependencyAttribute(Type dependencyType, DependencyType relationship)
        {
            DependencyType = dependencyType;
            Relationship = relationship;
        }
    }

    /// <summary>
    /// Defines the type of dependency relationship between systems.
    /// </summary>
    public enum DependencyType : byte
    {
        /// <summary>
        /// This system must run after the dependency.
        /// </summary>
        After = 0,

        /// <summary>
        /// This system must run before the dependency.
        /// </summary>
        Before = 1
    }
}

